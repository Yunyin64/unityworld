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
    private static readonly string[] EventNames =
    {
        "OnUse", "OnApply", "OnStraight", "OnAttack", "OnDefend", "OnDamage", "OnHeal",
        "OnBaseManaDraw", "OnManaDraw", "OnMPChanged", "OnContestOverflow"
    };
    private static readonly IReadOnlyDictionary<string, string> EmptyFields =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    private readonly List<BattleRecord> _records = new();
    private readonly Dictionary<int, (float? Hp, float? Mp, float? Sp)> _initialReferences = new();
    private readonly bool _observeEvents;
    private CombatScene? _scene;
    private DelegateEventListener? _listener;
    private EventMgr? _eventManager;
    private bool _initialized;
    private bool _ownsWorld;
    private bool _disposed;
    private int _traceOffset;
    private long _sequence;
    private double _accumulator;
    private string? _observationError;
    private BattleResultSnapshot? _result;

    /// <summary>Create an uninitialized session; event observation may be disabled for baseline verification.</summary>
    public BattleObserverSession(bool observeEvents = true)
    {
        _observeEvents = observeEvents;
        Records = _records.AsReadOnly();
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
    public IReadOnlyList<BattleRecord> Records { get; }
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
            if (_observeEvents) Subscribe();
            _scene.PreStart();
            _scene.Start();
            foreach (var npc in _scene.GetCombatants().Values)
            {
                // Read existing boxed cache values only. Even Snapshot().Get can mutate a shared NCalc cache.
                var cache = npc.Stats.GetFinalCache();
                _initialReferences[npc.Id] = (Existing(cache, "HpMax"), Existing(cache, "MpMax"), Existing(cache, "SpMax"));
            }
            FlushTrace();
            if (_observationError != null) throw new InvalidOperationException(_observationError);
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
            FlushTrace();
            if (_observationError != null) throw new InvalidOperationException(_observationError);
            if (_scene.IsFinished)
            {
                // Only reached after Tick returned normally. Failed Tick never enters GetResult.
                _result = CopyResult(_scene.GetResult());
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
            ReadOnly(_scene.GetCombatants().Values.OrderBy(n => n.Id).Select(CopyNpc)), _result, Diagnostic);
    }

    /// <summary>Copy current state and the complete original trace for a canonical per-Tick comparison.</summary>
    public BattleVerificationFrame CaptureVerificationFrame() => new(CurrentSnapshot,
        ReadOnly(_scene?.TraceLog ?? Array.Empty<string>()));

    private void PublishFrame()
    {
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

    private NpcSnapshot CopyNpc(CombatNpc npc)
    {
        var pack = npc.Backpack;
        var valid = new List<CellSnapshot>();
        for (var y = 0; y < pack.Height; y++)
        for (var x = 0; x < pack.Width; x++)
            if (pack.IsValid(new BackpackCell(x, y))) valid.Add(new(x, y));
        var backpack = new BackpackSnapshot(pack.Width, pack.Height, pack.Capacity, pack.MainCardId,
            pack.Row, pack.Round, pack.ScanProgress, pack.HasteTicks, pack.SlowTicks, valid.AsReadOnly(),
            ReadOnly(pack.Mana.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X)
                .Select(p => new ManaSnapshot(p.Key.X, p.Key.Y, p.Value.ToString()))));
        var cards = ReadOnly(npc.GetField().OrderBy(c => c.Id).Select(card =>
        {
            if (!pack.Placements.TryGetValue(card.Id, out var placement))
                throw new InvalidOperationException($"战斗卡实例 {card.Id}:{card.DefineId} 缺少背包布局，观察器不能补造位置。");
            var cells = ReadOnly(placement.Select(c => new CellSnapshot(c.X, c.Y)));
            var phase = Enum.GetValues<CombatCardPhase>().First(card.CheckPhase).ToString();
            return new CardSnapshot(card.Id, card.DefineId, card.DisplayName, card.BaseData.Size, phase,
                card.IsFrozen, card.UseCount, card.ApplyCount, card.Id == pack.MainCardId,
                card.BaseData.Keywords.Contains("Wound"), cells, ReadOnly(pack.AssignedTo(card.Id).Select(e => e.ToString())),
                ReadOnly(card.BaseData.ManaCost.OrderBy(p => p.Key.ToString(), StringComparer.Ordinal)
                    .Select(p => new ElementAmount(p.Key.ToString(), p.Value))));
        }));
        var buffs = ReadOnly(npc.GetAllModifiers().OrderBy(m => m.Id, StringComparer.Ordinal)
            .Select(m => new BuffSnapshot(m.Id, m.DefineId, m.DisplayName ?? m.DefineId, m.CurrentStack,
                m.Duration, m.RemainingTime, m.ExpirePolicy.ToString())));
        _initialReferences.TryGetValue(npc.Id, out var reference);
        var pending = npc.PendingSlot;
        return new(npc.Id, npc.GetName(), npc.Team.ToString(), npc.Status.ToString(), npc.GetHp(), npc.GetMp(),
            npc.GetSp(), npc.ShieldValue, reference.Hp, reference.Mp, reference.Sp, backpack, cards, buffs,
            pending == null ? null : new(pending.SourceCard.Id, pending.SourceCard.DefineId,
                pending.SourceCard.DisplayName, pending.ContestType.ToString(), pending.ContestValue, pending.Element.ToString()));
    }

    private static float? Existing(Dictionary<string, object> cache, string key) =>
        cache.TryGetValue(key, out var value) && value is float number && float.IsFinite(number) ? number : null;
    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
    private static BattleResultSnapshot CopyResult(CombatResult result) => new(
        result.GetValue<CombatEndReason>("EndReason").ToString(), result.GetValue<CombatTeam>("WinnerTeam").ToString(),
        result.GetValue<int>("TotalTicks"), ReadOnly((result.Get<List<CombatantResult>>("Combatants") ?? new())
            .OrderBy(c => c.NpcId).Select(c => new CombatantResultSnapshot(c.NpcId, c.Team.ToString(), c.FinalStatus.ToString()))));

    private void Subscribe()
    {
        _eventManager = EventMgr.Instance;
        _listener = new DelegateEventListener(OnCoreEvent);
        foreach (var name in EventNames)
            _eventManager.RegisterEvent("ClientBattleObserver", name, ScopeKey.Global, _listener);
    }

    private void OnCoreEvent(string eventName, ScopeKey scope, object args)
    {
        if (args is not APIContext ctx || !ReferenceEquals(ctx.Scene, _scene)) return;
        try
        {
            FlushTrace();
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            if (ctx.Caster != null) fields["casterName"] = ctx.Caster.GetName();
            if (ctx.SourceCard != null)
            {
                fields["sourceDefineId"] = ctx.SourceCard.DefineId;
                fields["sourceName"] = ctx.SourceCard.DisplayName;
            }
            // Copy only payloads the core actually supplied. No formula, inferred damage, or guessed pairing.
            foreach (var key in new[] { "Winner", "Loser", "Overflow", "WinnerType", "LoserType", "WinnerCard", "LoserCard" })
            {
                var value = ctx.GetObject(key);
                var copied = value switch
                {
                    CombatNpc npc => npc.Id.ToString(CultureInfo.InvariantCulture),
                    CombatCard card => card.Id.ToString(CultureInfo.InvariantCulture),
                    Enum e => e.ToString(),
                    int n => n.ToString(CultureInfo.InvariantCulture),
                    float n => n.ToString(CultureInfo.InvariantCulture),
                    double n => n.ToString(CultureInfo.InvariantCulture),
                    string s => s,
                    _ => null
                };
                if (copied != null) fields[key] = copied;
            }
            var text = eventName + (fields.TryGetValue("casterName", out var caster) ? " · " + caster : "")
                + (fields.TryGetValue("sourceName", out var source) ? " · " + source : "");
            _records.Add(new(++_sequence, _scene!.CurrentTick, "event", eventName, text,
                ctx.EventId, ctx.ChainId, ctx.Caster?.Id, ctx.SourceCard?.Id,
                new ReadOnlyDictionary<string, string>(fields)));
        }
        catch (Exception ex) { _observationError ??= "观察事件复制失败：" + ex; }
    }

    private void FlushTrace()
    {
        if (_scene == null) return;
        var trace = _scene.TraceLog;
        while (_traceOffset < trace.Count)
            _records.Add(new(++_sequence, _scene.CurrentTick, "trace", "TraceLog", trace[_traceOffset++],
                null, null, null, null, EmptyFields));
    }

    private void Fail(Exception exception)
    {
        Pause();
        Diagnostic ??= exception.ToString();
        try { FlushTrace(); } catch { /* Preserve the original failure. */ }
        _records.Add(new(++_sequence, _scene?.CurrentTick ?? 0, "diagnostic", "Error", Diagnostic,
            null, null, null, null, EmptyFields));
        try { CurrentSnapshot = CaptureSnapshot() with { Error = Diagnostic }; }
        catch { CurrentSnapshot = CurrentSnapshot with { Error = Diagnostic }; }
    }

    /// <summary>Unsubscribe only this observer, clean its scene, and end only its owned world.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        Pause();
        try
        {
            if (_eventManager != null && _listener != null)
                foreach (var name in EventNames) _eventManager.RemoveEvent(name, ScopeKey.Global, _listener);
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
