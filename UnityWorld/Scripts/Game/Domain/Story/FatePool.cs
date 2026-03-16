using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 天·宿命池：时间与事件 ID 的有序映射
    /// 当 WorldTime 到达指定时间，检查 Conditions 后触发对应 StoryDefine
    /// 宿命只有一次：无论是否满足条件，条目触发后均从池中移除
    /// </summary>
    public class FatePool
    {
        // ── 内部数据 ──────────────────────────────────────────
        /// <summary>时间 → StoryId 列表（有序，支持同一时间多个事件）</summary>
        private readonly SortedDictionary<float, List<string>> _pool = new();

        // ── 回调 ──────────────────────────────────────────────
        /// <summary>到达触发时间时的回调，参数为 (storyId, triggerTime)</summary>
        public Action<string, float>? OnTrigger { get; set; }

        // ── 操作 ──────────────────────────────────────────────

        /// <summary>向宿命池添加一个条目</summary>
        public void Add(float triggerTime, string storyId)
        {
            if (!_pool.TryGetValue(triggerTime, out var list))
            {
                list = new List<string>();
                _pool[triggerTime] = list;
            }
            list.Add(storyId);
        }

        /// <summary>
        /// 推进时间检查：处理所有时间 <= currentTime 的条目
        /// 触发后从池中移除（无论条件是否满足）
        /// </summary>
        public void Tick(float currentTime)
        {
            var toRemove = new List<float>();

            foreach (var (time, storyIds) in _pool)
            {
                if (time > currentTime) break; // SortedDictionary 有序，可提前退出
                foreach (var storyId in storyIds)
                {
                    OnTrigger?.Invoke(storyId, time);
                }
                toRemove.Add(time);
            }

            foreach (var time in toRemove)
                _pool.Remove(time);
        }

        /// <summary>当前池中条目总数</summary>
        public int Count => _pool.Values.Sum(list => list.Count);

        /// <summary>清空宿命池</summary>
        public void Clear() => _pool.Clear();
    }
}
