using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xianxia.Client.Battle;

namespace Xianxia.Client.BattleWindows;

/// <summary>仅在 Godot 主线程提交已收到的值。私有管道的后台读取不会阻塞窗口输入。</summary>
internal sealed class RemoteObserver : IBattleObserverState, IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly Process _process;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<ObserverPacket> _incoming = Channel.CreateBounded<ObserverPacket>(16);
    private readonly List<BattleRecord> _records = new();
    private StreamWriter? _commands;
    private string? _session;
    private bool _canAdvance, _pending, _disposed;
    private long _sequence;
    public int ProcessId { get; }
    public bool Ready => _session != null;
    public bool AwaitingCommand => _pending;
    public string Directory { get; private set; } = "";
    public BattleCase Case { get; private set; }
    public string DataFingerprint => Case.DataFingerprint;
    public BattleSnapshot CurrentSnapshot { get; private set; } = BattleSnapshot.Empty;
    public IReadOnlyList<BattleRecord> Records => _records;
    public bool IsPlaying { get; private set; }
    public bool CanAdvance => Ready && _canAdvance && !_pending && Diagnostic == null;
    public string? Diagnostic { get; private set; }

    public RemoteObserver(string inputPath, string dataRoot)
    {
        Case = BattleCase.FromJson(File.ReadAllText(inputPath));
        string pipeName = "unityworld-observer-" + Guid.NewGuid().ToString("N");
        _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            var start = new ProcessStartInfo(OS.GetExecutablePath()) { UseShellExecute = false, CreateNoWindow = true };
            foreach (string arg in BattleLauncher.ProcessArguments("res://scenes/tests/battle_worker.tscn", inputPath, true))
                start.ArgumentList.Add(arg);
            start.ArgumentList.Add("--observer-worker=" + pipeName);
            start.ArgumentList.Add("--observer-case-input=" + inputPath);
            start.ArgumentList.Add("--observer-data-root=" + dataRoot);
            _process = Process.Start(start) ?? throw new IOException("无法启动后台观察进程。");
            ProcessId = _process.Id;
        }
        catch { _pipe.Dispose(); throw; }
        _ = ReadPackets();
    }

    private async Task ReadPackets()
    {
        try
        {
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
            startup.CancelAfter(TimeSpan.FromSeconds(30));
            var connect = _pipe.WaitForConnectionAsync(startup.Token);
            var exited = _process.WaitForExitAsync(startup.Token);
            if (await Task.WhenAny(connect, exited).ConfigureAwait(false) != connect && !_pipe.IsConnected)
                throw new IOException("后台观察进程未连接就已退出。");
            await connect.ConfigureAwait(false);
            startup.Cancel();
            using var reader = new StreamReader(_pipe, new UTF8Encoding(false), false, 65536, true);
            while (await reader.ReadLineAsync(_stop.Token).ConfigureAwait(false) is { } line)
                await _incoming.Writer.WriteAsync(ObserverPacket.Decode(line), _stop.Token).ConfigureAwait(false);
            throw new IOException("与后台观察进程的连接已关闭。");
        }
        catch (Exception ex) { _incoming.Writer.TryComplete(ex); }
    }

    public void Poll(Action<BattlePresentationFrame> submit)
    {
        if (Diagnostic != null || _disposed) return;
        try
        {
            // 每个显示帧限制处理量，保留其余逻辑步及原顺序。
            for (int i = 0; i < 16 && _incoming.Reader.TryRead(out var packet); i++)
            {
                if (packet.Kind == "error") throw new InvalidDataException(packet.Error);
                if (_session != null && packet.Session != _session) throw new InvalidDataException("观察器会话身份不一致。");
                if (packet.Kind == "frame")
                {
                    var frame = packet.Frame ?? throw new InvalidDataException("观察器缺少完整帧。");
                    int expectedTick = Ready ? CurrentSnapshot.Tick + 1 : 0;
                    if (frame.SchemaVersion != BattlePresentationFrame.CurrentSchemaVersion || frame.SessionId != packet.Session ||
                        frame.Snapshot.Tick != frame.Tick || frame.Tick != expectedTick || frame.IsInitialization != (expectedTick == 0))
                        throw new InvalidDataException("观察器 Tick、会话或版本不一致。");
                    if (!Ready)
                    {
                        if (packet.Case?.ToJson() != Case.ToJson() || string.IsNullOrEmpty(packet.Directory))
                            throw new InvalidDataException("后台观察器未确认本次冻结输入。");
                        Directory = packet.Directory;
                        _session = packet.Session;
                    }
                    var records = packet.Records ?? throw new InvalidDataException("观察器缺少战报。");
                    long sequence = _sequence;
                    foreach (var record in records)
                        if (record.Sequence != ++sequence) throw new InvalidDataException("战报序号不连续。");
                    _sequence = sequence;
                    _records.AddRange(records);
                    CurrentSnapshot = frame.Snapshot;
                    submit(frame);
                }
                else if (packet.Kind == "ack" && Ready) _pending = false;
                else throw new InvalidDataException("观察器发出了未知或过早的消息。");
                IsPlaying = packet.Playing;
                _canAdvance = packet.CanAdvance;
            }
            if (_incoming.Reader.Completion.IsCompleted) _incoming.Reader.Completion.GetAwaiter().GetResult();
        }
        catch (Exception ex) { Fault(ex); }
    }

    public void Command(string command, bool forcePause = false)
    {
        if (!Ready || _disposed || Diagnostic != null || (_pending && !forcePause)) return;
        try
        {
            _commands ??= new StreamWriter(_pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
            _commands.WriteLine(command);
            _pending = true;
        }
        catch (Exception ex) { Fault(ex); }
    }

    private void Fault(Exception error)
    {
        Diagnostic ??= error.ToString();
        IsPlaying = false;
        _canAdvance = false;
        _pending = false;
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // EOF 表示父端正常离开或崩溃。只有自己的子进程在宽限期内没退出才强制结束。
        _stop.Cancel();
        _pipe.Dispose();
        _ = Reap(_process, _stop);
    }

    private static async Task Reap(Process process, CancellationTokenSource stop)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { if (!process.HasExited) process.Kill(); }
        }
        catch (InvalidOperationException) { /* 子进程可能刚退出。 */ }
        finally { process.Dispose(); stop.Dispose(); }
    }
}
