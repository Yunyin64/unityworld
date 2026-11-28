using System;
using System.Collections.Generic;
using UnityWorld.Core;

namespace Xianxia.Client;

/// <summary>在 Godot 主线程按资源地址复用客户端实例，租约由使用实例的场景归还。</summary>
internal sealed class ClientAssetPool : IDisposable
{
    private readonly Dictionary<(string Path, Type Type), IDisposable> _buckets = new();
    private bool _disposed;

    /// <summary>池空时使用资源工厂创建，归还时用 reset 清除实例状态，超限时调用 discard。</summary>
    internal Lease<T>? Rent<T>(string assetPath, Func<T?> create, Action<T>? reset = null, Action<T>? discard = null)
        where T : class
        => BucketFor<T>(assetPath).Rent(create, reset, discard);

    /// <summary>按资源限制空闲保留量，null 使用并发峰值，0 表示归还时直接丢弃。</summary>
    internal void SetRetentionLimit<T>(string assetPath, int? limit) where T : class
    {
        if (limit is int value)
            ArgumentOutOfRangeException.ThrowIfNegative(value);
        BucketFor<T>(assetPath).SetRetentionLimit(limit);
    }

    /// <summary>资源更新后废弃旧实例，尚在使用的实例在归还时释放。</summary>
    internal void Drop<T>(string assetPath) where T : class
    {
        if (_buckets.Remove((PoolKey(assetPath), typeof(T)), out var bucket))
            bucket.Dispose();
    }

    /// <summary>清理空闲实例，退出时尚未归还的实例会在归还时丢弃。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var bucket in _buckets.Values)
            bucket.Dispose();
        _buckets.Clear();
    }

    private Bucket<T> BucketFor<T>(string assetPath) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string path = PoolKey(assetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var key = (path, typeof(T));
        if (!_buckets.TryGetValue(key, out var bucket))
            _buckets.Add(key, bucket = new Bucket<T>());
        return (Bucket<T>)bucket;
    }

    // 资源地址沿用 Core 的 src 别名规则，Godot 控件模板仍可用引擎路径做键。
    private static string PoolKey(string path) => AssetMgr.NormalizeAddress(path) ?? AssetMgr.GetFixedPath(path);

    /// <summary>场景持有租约期间独占该实例，可重复调用 Dispose。</summary>
    internal sealed class Lease<T>(T instance, Action release) : IDisposable where T : class
    {
        private Action? _release = release;

        /// <summary>当前借出的实例，归还后不可继续使用。</summary>
        internal T Instance => _release != null ? instance : throw new ObjectDisposedException(nameof(Lease<T>));

        /// <summary>归还实例。</summary>
        public void Dispose()
        {
            var callback = _release;
            _release = null;
            callback?.Invoke();
        }
    }

    private sealed class Bucket<T> : IDisposable where T : class
    {
        private readonly record struct Entry(T Instance, Action<T>? Discard);

        private readonly Stack<Entry> _idle = new();
        private int _active;
        private int _peak;
        private int? _limit;
        private bool _disposed;
        private int RetainedLimit => Math.Min(_peak, _limit ?? _peak);

        internal Lease<T>? Rent(Func<T?> create, Action<T>? reset, Action<T>? discard)
        {
            if (!_idle.TryPop(out var entry))
            {
                var instance = create();
                if (instance == null) return null;
                entry = new(instance, discard);
            }

            _active++;
            _peak = Math.Max(_peak, _active);
            return new Lease<T>(entry.Instance, () => Return(entry, reset));
        }

        internal void SetRetentionLimit(int? limit)
        {
            _limit = limit;
            Trim(RetainedLimit);
        }

        private void Return(Entry entry, Action<T>? reset)
        {
            _active--;
            if (_disposed || _idle.Count >= RetainedLimit)
            {
                entry.Discard?.Invoke(entry.Instance);
                return;
            }

            try
            {
                reset?.Invoke(entry.Instance);
            }
            catch
            {
                entry.Discard?.Invoke(entry.Instance);
                throw;
            }

            if (_disposed || _idle.Count >= RetainedLimit)
                entry.Discard?.Invoke(entry.Instance);
            else
                _idle.Push(entry);
        }

        private void Trim(int limit)
        {
            while (_idle.Count > limit)
            {
                var entry = _idle.Pop();
                entry.Discard?.Invoke(entry.Instance);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Trim(0);
        }
    }
}
