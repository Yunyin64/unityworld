using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>客户端时间线中的一次表现；Progress 只由显示时间推进。</summary>
public sealed class PresentationCue
{
    public CombatPresentationEvent Event { get; }
    public BattleSnapshot BeforeSnapshot { get; }
    public BattleSnapshot AfterSnapshot { get; }
    public double Duration { get; }
    public int Generation { get; }
    public float Progress { get; internal set; }
    internal double Start { get; }
    internal bool Started { get; set; }
    internal bool Completed { get; set; }
    internal double End => Start + Duration;
    internal PresentationCue(CombatPresentationEvent fact, BattleSnapshot before, BattleSnapshot after,
        double start, double duration, int generation)
    { Event = fact; BeforeSnapshot = before; AfterSnapshot = after; Start = start; Duration = duration; Generation = generation; }
}

/// <summary>跨 Tick 的显示时间线；仅短位移/资源过渡占用对象，文字淡出不阻塞后续动作。</summary>
public sealed class BattlePresentationPlayer
{
    private readonly Dictionary<int, BattlePresentationFrame> _frames = new();
    private readonly List<PresentationCue> _timeline = new();
    private readonly List<PresentationCue> _active = new();
    private readonly Dictionary<string, double> _ready = new();
    private readonly HashSet<string> _retiredSessions = new(StringComparer.Ordinal);
    private BattleSnapshot _latest = BattleSnapshot.Empty;
    private string? _session;
    private int _lastTick = -1;
    private long _lastSequence;
    private double _elapsed;
    public BattleSnapshot DisplaySnapshot { get; private set; } = BattleSnapshot.Empty;
    public IReadOnlyList<PresentationCue> ActiveCues { get; }
    public bool IsIdle => _timeline.Count == 0;
    public bool AnimationsEnabled { get; set; } = true;
    public int MaxQueuedEvents { get; set; } = 2048;
    public int MaxQueuedFrames { get; set; } = 2048;
    public int Generation { get; private set; }
    public string? Diagnostic { get; private set; }
    public long StartedCueCount { get; private set; }
    public long CompletedCueCount { get; private set; }
    public event Action<PresentationCue>? CueStarted;
    public event Action<PresentationCue>? CueCompleted;
    /// <summary>该批局部动画已完成；允许跨 Tick 完成，不会用此旧快照回写当前显示。</summary>
    public event Action<BattleSnapshot>? SnapshotSettled;
    public BattlePresentationPlayer() => ActiveCues = _active.AsReadOnly();

    public void Reset()
    {
        if (_session != null) _retiredSessions.Add(_session);
        ClearPlayback();
        _session = null; _lastTick = -1; _lastSequence = 0;
        Diagnostic = null; _latest = DisplaySnapshot = BattleSnapshot.Empty;
    }
    public void Cancel() => Reset();
    private void ClearPlayback()
    {
        Generation++; _frames.Clear(); _timeline.Clear(); _active.Clear(); _ready.Clear();
        _elapsed = 0;
    }

    /// <summary>新 Tick 立即加入时间线；最新普通状态立即可见，局部过渡独立保留。</summary>
    public void Submit(BattlePresentationFrame frame)
    {
        if (frame.SessionId != _session)
        {
            if (!frame.IsInitialization || frame.Tick != 0 || _retiredSessions.Contains(frame.SessionId)) return;
            Reset(); _session = frame.SessionId;
        }
        if (frame.Tick <= _lastTick) return;
        if (frame.SchemaVersion != BattlePresentationFrame.CurrentSchemaVersion)
        { Recover(frame, "表现版本不兼容，已切换为完整状态显示。"); return; }
        if (frame.Tick != _lastTick + 1 || frame.Snapshot.Tick != frame.Tick)
        { Recover(frame, "表现批次缺失或 Tick 不一致，已对齐完整快照。"); return; }
        long sequence = _lastSequence;
        foreach (var fact in frame.Events)
        {
            if (fact.Tick != frame.Tick || fact.Sequence != ++sequence)
            { Recover(frame, "表现事件序号不连续，已对齐完整快照。"); return; }
        }
        if (frame.LastSequence != sequence || (frame.Events.Count > 0 && frame.FirstSequence != frame.Events[0].Sequence))
        { Recover(frame, "表现批次序号范围无效，已对齐完整快照。"); return; }
        _lastTick = frame.Tick; _lastSequence = sequence;
        var before = DisplaySnapshot;
        var previous = _latest;
        _latest = frame.Snapshot;
        if (frame.IsInitialization || !AnimationsEnabled)
        { ClearPlayback(); DisplaySnapshot = _latest; SnapshotSettled?.Invoke(_latest); return; }
        if (_timeline.Count + frame.Events.Count > MaxQueuedEvents ||
            (frame.Events.Count > 0 && _frames.Count + 1 > MaxQueuedFrames))
        { Recover(frame, "已跳过部分动画：表现积压超限，已对齐完整快照。"); return; }
        if (frame.Events.Count > 0) _frames.Add(frame.Tick, frame);
        foreach (var fact in PlaybackOrder(frame, previous))
        {
            double start = Math.Max(_elapsed, WaitKeys(fact).Select(k => _ready.GetValueOrDefault(k)).DefaultIfEmpty(0).Max());
            var cue = new PresentationCue(fact, before, frame.Snapshot, start, Duration(fact.Kind), Generation);
            _timeline.Add(cue);
            foreach (var key in HoldKeys(fact)) _ready[key] = Math.Max(_ready.GetValueOrDefault(key), cue.End);
            // 生效关系以出现时点交付；0.7/0.8 秒文字寿命从不成为其他 cue 的锁。
            if (fact.Applied != null && fact.EffectId.HasValue)
                _ready["effect:" + fact.EffectId] = Math.Max(_ready.GetValueOrDefault("effect:" + fact.EffectId), cue.Start);
        }
        RefreshDisplay();
        var generation = Generation;
        ApplyAtTime();
        if (Generation != generation) return;
        if (frame.Events.Count == 0) SnapshotSettled?.Invoke(frame.Snapshot);
    }

    private void Recover(BattlePresentationFrame frame, string message)
    {
        ClearPlayback(); Diagnostic = message; _lastTick = frame.Tick; _lastSequence = frame.LastSequence;
        _latest = DisplaySnapshot = frame.Snapshot;
        SnapshotSettled?.Invoke(frame.Snapshot);
    }

    public void Update(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        // 只以短过渡的真实积压追赶；长飘字本身不会加速或阻塞其他演员。
        double blockingEnd = _timeline.Where(c => !c.Completed && IsShortTransition(c.Event))
            .Select(c => c.End).DefaultIfEmpty(_elapsed).Max();
        double speed = Math.Clamp((blockingEnd - _elapsed) / .8, 1, 16);
        _elapsed += delta * speed;
        ApplyAtTime();
    }

    private void ApplyAtTime()
    {
        var transitions = _timeline.SelectMany((c, order) => new[]
            { (Time: c.Start, Finish: false, Cue: c, Order: order), (Time: c.End, Finish: true, Cue: c, Order: order) })
            .Where(t => t.Time <= _elapsed + 1e-9 && (t.Finish ? !t.Cue.Completed : !t.Cue.Started))
            .OrderBy(t => t.Time).ThenBy(t => t.Order).ThenBy(t => t.Finish).ToArray();
        foreach (var t in transitions)
        {
            var cue = t.Cue;
            if (cue.Generation != Generation) return;
            if (t.Finish ? cue.Completed : cue.Started) continue;
            if (!t.Finish)
            {
                cue.Started = true; StartedCueCount++;
                DisplaySnapshot = BattlePresentationState.Apply(DisplaySnapshot, cue, false);
                RefreshDisplay();
                CueStarted?.Invoke(cue);
            }
            else
            {
                cue.Completed = true; cue.Progress = 1; CompletedCueCount++;
                DisplaySnapshot = BattlePresentationState.Apply(DisplaySnapshot, cue, true);
                RefreshDisplay();
                CueCompleted?.Invoke(cue);
            }
            if (cue.Generation != Generation) return;
        }
        _timeline.RemoveAll(c => c.Completed);
        _active.Clear();
        foreach (var cue in _timeline.Where(c => c.Started))
        {
            cue.Progress = (float)Math.Clamp((_elapsed - cue.Start) / Math.Max(cue.Duration, 1e-9), 0, 1);
            _active.Add(cue);
        }
        foreach (var key in _ready.Where(p => p.Value <= _elapsed).Select(p => p.Key).ToArray()) _ready.Remove(key);
        RefreshDisplay();
        var generation = Generation;
        foreach (var pair in _frames.Where(p => !_timeline.Any(c => c.Event.Tick == p.Key)).OrderBy(p => p.Key).ToArray())
        {
            _frames.Remove(pair.Key);
            SnapshotSettled?.Invoke(pair.Value.Snapshot);
            if (Generation != generation) return;
        }
    }

    private void RefreshDisplay() => DisplaySnapshot = BattlePresentationState.Reconcile(_latest, DisplaySnapshot,
        _timeline.Where(c => !c.Completed).ToArray());

    private static bool IsShortTransition(CombatPresentationEvent e) => e.ManaAssigned != null || e.CardUsed != null ||
        e.CardAdded != null || e.Pending != null || e.Contest != null;
    private static double Duration(CombatPresentationKind kind) => kind switch
    {
        CombatPresentationKind.ManaAssigned => .18,
        CombatPresentationKind.CardUsed => .10,
        CombatPresentationKind.ContestResolved => .25,
        CombatPresentationKind.CardAddedToBackpack => .40,
        CombatPresentationKind.CardApplied => .70,
        CombatPresentationKind.DamageResolved => .80,
        _ => 0
    };

    private static IEnumerable<CombatPresentationEvent> PlaybackOrder(BattlePresentationFrame frame, BattleSnapshot before)
    {
        var existing = before.Participants.SelectMany(n => n.Cards).Select(c => c.Id).ToHashSet();
        var additions = frame.Events.Where(e => e.CardAdded != null && e.Card != null && !existing.Contains(e.Card.Id))
            .GroupBy(e => e.Card!.Id).ToDictionary(g => g.Key, g => g.First());
        var scheduled = new HashSet<long>();
        foreach (var fact in frame.Events)
        {
            if (scheduled.Contains(fact.Sequence)) continue;
            var cards = fact.Contest is { } contest
                ? new[] { contest.Left.Card.Id, contest.Right.Card.Id }
                : fact.Card == null ? Array.Empty<int>() : new[] { fact.Card.Id };
            foreach (var cardId in cards)
                if (additions.TryGetValue(cardId, out var addition) && addition.Sequence != fact.Sequence && scheduled.Add(addition.Sequence))
                    yield return addition;
            if (scheduled.Add(fact.Sequence)) yield return fact;
        }
    }

    private static IEnumerable<string> WaitKeys(CombatPresentationEvent e)
    {
        if (e.Card != null) yield return "card:" + e.Card.Id;
        if (e.ActionId.HasValue) yield return "action:" + e.ActionId;
        if (e.ParentActionId.HasValue) yield return "action:" + e.ParentActionId;
        if (e.ContestId.HasValue) yield return "contest:" + e.ContestId;
        if (e.EffectId.HasValue) yield return "effect:" + e.EffectId;
        if (e.Pending != null) yield return "pending:" + e.NpcId;
        if (e.Contest != null)
        {
            foreach (var side in new[] { e.Contest.Left, e.Contest.Right })
            {
                yield return "pending:" + side.NpcId;
                yield return "card:" + side.Card.Id;
                if (side.ActionId.HasValue) yield return "action:" + side.ActionId;
            }
        }
    }

    private static IEnumerable<string> HoldKeys(CombatPresentationEvent e)
    {
        if ((e.ManaAssigned != null || e.CardUsed != null || e.CardAdded != null) && e.Card != null)
            yield return "card:" + e.Card.Id;
        if ((e.ManaAssigned != null || e.CardUsed != null) && e.ActionId.HasValue)
            yield return "action:" + e.ActionId;
        if (e.Pending != null) yield return "pending:" + e.NpcId;
        if (e.Contest != null)
        {
            yield return "pending:" + e.Contest.Left.NpcId;
            yield return "pending:" + e.Contest.Right.NpcId;
            if (e.ContestId.HasValue) yield return "contest:" + e.ContestId;
        }
    }
}