using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace Xianxia.Client.Battle;

/// <summary>Owns one world and one battle in a fresh process. Rendering only consumes copied values.</summary>
public sealed class BattleObserverSession : IDisposable
{
    private readonly BattleSnapshotProjector _projector = new();
    private BattleRecordCollector? _collector;
    private readonly bool _observePresentation;
    private long _lastPresentationSequence;
    private readonly bool _observeEvents;
    private CombatScene? _scene;
    private bool _initialized;
    private bool _ownsWorld;
    private bool _disposed;
    private double _accumulator;
    private BattleResultSnapshot? _result;

    /// <summary>Create an uninitialized session; event observation may be disabled for baseline verification.</summary>
    public BattleObserverSession(bool observeEvents = true, bool observePresentation = true)
    {
        _observeEvents = observeEvents;
        _observePresentation = observePresentation;
    }

    /// <summary>Complete input, including verified data and core identity, suitable for new-process replay.</summary>
    public BattleCase Case { get; private set; } = new();
    /// <summary>Aggregate SHA256 of the actual JSON and Lua files loaded by this session.</summary>
    public string DataFingerprint { get; private set; } = "";
    /// <summary>Detailed source file identity for compatibility reports.</summary>
    public BattleDataFingerprint? DataIdentity { get; private set; }
    /// <summary>Last committed value snapshot; safe to read on every display frame.</summary>
    public BattleSnapshot CurrentSnapshot { get; private set; } = BattleSnapshot.Empty;
    /// <summary>Immutable record values in receipt order; this read-only view grows only on simulation work.</summary>
    public IReadOnlyList<BattleRecord> Records => _collector?.Records ?? Array.Empty<BattleRecord>();
    /// <summary>Whether the independent battle clock is running.</summary>
    public bool IsPlaying { get; private set; }
    /// <summary>Whether a valid unfinished session can accept another logical Tick.</summary>
    public bool CanAdvance => !_disposed && Diagnostic == null && _scene != null && !_scene.IsFinished;
    /// <summary>Original initialization, simulation, or observation error; never replaced by missing-result errors.</summary>
    public string? Diagnostic { get; private set; }
    /// <summary>One-times playback uses ten complete core Ticks per second.</summary>
    public const double TicksPerSecond = 10;
    /// <summary>Optional verification sink called once at Tick 0 and after every committed Tick, even within one frame.</summary>
    public event Action<BattleVerificationFrame>? FrameCommitted;
    /// <summary>事件与终态共同提交，订阅方不能回到核心结算栈。</summary>
    public event Action<BattlePresentationFrame>? PresentationCommitted;
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public BattlePresentationFrame? CurrentPresentationFrame { get; private set; }
    public IReadOnlyList<CombatPresentationEvent> UncommittedEvents { get; private set; } = Array.Empty<CombatPresentationEvent>();

    /// <summary>Initialize the only core host, assemble participants in input order, and pause at Tick 0.</summary>
    public void Initialize(BattleCase input, string dataRoot)
    {
        if (_initialized || _disposed) throw new InvalidOperationException("每个观察会话只能初始化一次；重开需要新进程。");
        _initialized = true;
        try
        {
            Case = input.Copy();
            BattleCaseFactory.Validate(Case);
            DataIdentity = BattleCaseFactory.Fingerprint(dataRoot);
            DataFingerprint = DataIdentity.Sha256;
            if (Case.DataFingerprint.Length != 0 && Case.DataFingerprint != DataFingerprint)
                throw new InvalidOperationException($"数据版本已变化，已停止重放。期待 {Case.DataFingerprint}；实际 {DataFingerprint}。");
            var coreVersion = BattleCaseFactory.CoreVersion;
            if (Case.CoreVersion.Length != 0 && Case.CoreVersion != coreVersion)
                throw new InvalidOperationException($"核心版本已变化，已停止重放。期待 {Case.CoreVersion}；实际 {coreVersion}。");
            Case = Case with { DataFingerprint = DataFingerprint, CoreVersion = coreVersion };
            if (WorldMgr._domains.Count != 0 || WorldMgr._gameplays.Count != 0)
                throw new InvalidOperationException("其他宿主已初始化世界；观察器不会接管或结束它。");
            _ownsWorld = true;
            WorldMgr.Initialize(Case.WorldSeed, dataRoot);
            WorldMgr.Start();
            _scene = BattleCaseFactory.Assemble(Case);
            _scene.EnablePresentationRecording(_observePresentation);
            _collector = new BattleRecordCollector(_scene);
            if (_observeEvents) _collector.Subscribe();
            _scene.PreStart();
            _scene.Start();
            _projector.CaptureInitial(_scene);
            _collector.FlushTrace();
            if (_collector.Error != null) throw new InvalidOperationException(_collector.Error);
            CurrentSnapshot = CaptureSnapshot();
            PublishFrame();
        }
        catch (Exception ex) { Fail(ex); }
    }

    /// <summary>Resume logical time if the session is valid and unfinished.</summary>
    public bool Play()
    {
        if (!CanAdvance) return false;
        IsPlaying = true;
        return true;
    }

    /// <summary>Stop logical time and discard fractional pre-pause elapsed time.</summary>
    public void Pause()
    {
        IsPlaying = false;
        _accumulator = 0;
    }

    /// <summary>While paused, advance exactly one complete core Tick and remain paused.</summary>
    public bool Step()
    {
        if (IsPlaying || !CanAdvance) return false;
        _accumulator = 0;
        return Advance();
    }

    /// <summary>Advance the independent accumulator by display elapsed seconds; never call WorldMgr.Tick.</summary>
    public void Update(double delta)
    {
        if (!IsPlaying || !CanAdvance) return;
        if (!double.IsFinite(delta) || delta < 0) { Fail(new ArgumentOutOfRangeException(nameof(delta))); return; }
        _accumulator += delta;
        // Bound work per display frame without discarding logical time. The next frame drains any remainder.
        for (int i = 0; i < 2000 && IsPlaying && CanAdvance && _accumulator + 1e-12 >= 1 / TicksPerSecond; i++)
        {
            _accumulator -= 1 / TicksPerSecond;
            if (_accumulator < 0) _accumulator = 0;
            Advance();
        }
    }

    private bool Advance()
    {
        try
        {
            _scene!.Tick();
            _collector!.FlushTrace();
            if (_collector!.Error != null) throw new InvalidOperationException(_collector.Error);
            if (_scene.IsFinished)
            {
                // Only reached after Tick returned normally. Failed Tick never enters GetResult.
                _result = BattleSnapshotProjector.CopyResult(_scene.GetResult());
                Pause();
            }
            CurrentSnapshot = CaptureSnapshot();
            PublishFrame();
            return Diagnostic == null;
        }
        catch (Exception ex) { Fail(ex); return false; }
    }

    /// <summary>Verification only: audit repeated reads against independent fresh-process baselines.</summary>
    public BattleSnapshot CaptureSnapshot()
    {
        if (_scene == null || _disposed) return CurrentSnapshot;
        return new(_scene.CurrentTick, WorldTime.CurrentTick, _scene.TicksPerRow, _scene.IsFinished,
            Array.AsReadOnly(_scene.GetCombatants().Values.OrderBy(n => n.Id).Select(_projector.CopyNpc).ToArray()), _result, Diagnostic);
    }

    /// <summary>Copy current state and the complete original trace for a canonical per-Tick comparison.</summary>
    public BattleVerificationFrame CaptureVerificationFrame() => new(CurrentSnapshot,
        Array.AsReadOnly((_scene?.TraceLog ?? Array.Empty<string>()).ToArray()));

    private void PublishFrame()
    {
        var events = _scene!.DrainPresentationEvents();
        var first = events.Count == 0 ? _lastPresentationSequence : events[0].Sequence;
        if (events.Count != 0) _lastPresentationSequence = events[^1].Sequence;
        var batch = new BattlePresentationFrame(BattlePresentationFrame.CurrentSchemaVersion, SessionId,
            CurrentSnapshot.Tick, CurrentSnapshot.Tick == 0, CurrentSnapshot, events, first, _lastPresentationSequence);
        CurrentPresentationFrame = batch;
        if (PresentationCommitted != null)
            foreach (Action<BattlePresentationFrame> sink in PresentationCommitted.GetInvocationList())
                try { sink(batch); }
                catch (Exception ex) { Fail(new InvalidOperationException("表现批次接收失败；模拟已暂停。", ex)); break; }
        if (FrameCommitted == null) return;
        var frame = CaptureVerificationFrame();
        foreach (Action<BattleVerificationFrame> sink in FrameCommitted.GetInvocationList())
        {
            try { sink(frame); }
            catch (Exception ex)
            {
                // The core Tick is already complete. A failed client sink must never re-enter core dispatch.
                Fail(new InvalidOperationException("观察帧接收器失败；模拟已暂停。", ex));
                break;
            }
        }
    }

    private void Fail(Exception exception)
    {
        Pause();
        Diagnostic ??= exception.ToString();
        try { _collector?.FlushTrace(); } catch { /* 保留最初错误。 */ }
        _collector?.AddDiagnostic(Diagnostic);
        UncommittedEvents = _scene?.DrainPresentationEvents() ?? Array.Empty<CombatPresentationEvent>();
        // 核心可能只执行了一部分 Tick；不能把它包装成完整快照。
        CurrentSnapshot = CurrentSnapshot with { Error = Diagnostic };
    }

    /// <summary>Unsubscribe only this observer, clean its scene, and end only its owned world.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        Pause();
        try
        {
            _collector?.Dispose();
            _scene?.EnablePresentationRecording(false);
            _scene?.Cleanup();
        }
        catch (Exception ex) { Diagnostic ??= "会话清理失败：" + ex; }
        finally
        {
            if (_ownsWorld)
            {
                try { WorldMgr.End(); }
                catch (Exception ex) { Diagnostic ??= "核心宿主退出失败：" + ex; }
            }
            _disposed = true;
        }
    }
}
