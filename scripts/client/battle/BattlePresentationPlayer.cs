using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>交手事实及可在镜头接触前确认的受击者，供紧接的镜头继承收势。</summary>
internal sealed class DuelExchange(CombatPresentationEvent fact, DuelExchange? previous)
{
    internal CombatPresentationEvent Fact { get; } = fact;
    // 只复制推算落点需要的前三个事实，连续演出不会保留整场的镜头链。
    internal IReadOnlyList<CombatPresentationEvent> ApproachHistory { get; } =
        previous == null ? Array.Empty<CombatPresentationEvent>() : [previous.Fact, .. previous.ApproachHistory.Take(2)];
    internal DamagePresentation? ContestImpact { get; set; }
    internal int? ContestInjuryTarget => ContestImpact is { HpDamage: > 0 } hit ? hit.TargetNpcId : null;
    internal int? FinishingNpcId { get; set; }

    internal static ContestSidePresentation? Winner(ContestPresentation? contest) =>
        contest?.WinnerNpcId is int winner ? (winner == contest.Left.NpcId ? contest.Left : contest.Right) : null;
}

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
    internal DuelExchange? Exchange { get; init; }
    internal DuelExchange? PreviousExchange { get; init; }
    internal bool StartsOnStage => PreviousExchange != null;
    internal bool ContinuesOnStage { get; set; }
    internal double End => Start + Duration;
    internal bool IsHoldingApproach { get; private set; }
    internal bool IsHoldingImpact { get; private set; }
    internal float ContactProgress => Event.Contest != null ? BattlePresentationPlayer.ContestContact : BattlePresentationPlayer.StrikeContact;
    internal float ApproachProgress => ContactProgress - .15f;
    internal float ImpactProgress => ContactProgress + .012f;
    internal double MotionDuration => Exchange == null ? Duration : Duration -
        BattlePresentationPlayer.ApproachHoldSeconds - BattlePresentationPlayer.ImpactHoldSeconds;
    private const double AttackTimeScale = .62;

    internal PresentationCue(CombatPresentationEvent fact, BattleSnapshot before, BattleSnapshot after,
        double start, double duration, int generation)
    { Event = fact; BeforeSnapshot = before; AfterSnapshot = after; Start = start; Duration = duration; Generation = generation; }

    internal double ContactTime => Start + MotionDuration *
        (ApproachProgress + (ContactProgress - ApproachProgress) * AttackTimeScale) + BattlePresentationPlayer.ApproachHoldSeconds;

    internal void Sample(double time)
    {
        IsHoldingApproach = IsHoldingImpact = false;
        double local = Math.Clamp(time - Start, 0, Duration);
        if (Exchange == null)
        {
            Progress = (float)(local / Math.Max(Duration, 1e-9));
            return;
        }

        // 两个停顿在镜头总时长内。加速贴身出手那一段，余下的时间交给收招。
        double approach = MotionDuration * ApproachProgress;
        double attack = MotionDuration * (ImpactProgress - ApproachProgress) * AttackTimeScale;
        double attackStart = approach + BattlePresentationPlayer.ApproachHoldSeconds;
        double impact = attackStart + attack;
        double recovery = impact + BattlePresentationPlayer.ImpactHoldSeconds;
        IsHoldingApproach = local >= approach && local < attackStart;
        IsHoldingImpact = local >= impact && local < recovery;
        if (local < approach) Progress = (float)(local / MotionDuration);
        else if (IsHoldingApproach) Progress = ApproachProgress;
        else if (local < impact) Progress = (float)(ApproachProgress + (local - attackStart) / (MotionDuration * AttackTimeScale));
        else if (IsHoldingImpact) Progress = ImpactProgress;
        else Progress = (float)(ImpactProgress + (1 - ImpactProgress) * (local - recovery) / (Duration - recovery));
    }
}

/// <summary>跨 Tick 的显示时间线；仅短位移/资源过渡占用对象，文字淡出不阻塞后续动作。</summary>
public sealed class BattlePresentationPlayer
{
    internal const float ContestContact = .52f;
    internal const float StrikeContact = .34f;
    internal const double ApproachHoldSeconds = .105;
    internal const double ImpactHoldSeconds = .090;
    internal const double PendingFlightSeconds = .32;
    private const string DuelStage = "duel-stage";
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
    internal int? DepartedNpcId { get; private set; }
    internal bool IsHoldingApproach => _active.Any(cue => cue.IsHoldingApproach);
    internal bool IsHoldingImpact => _active.Any(cue => cue.IsHoldingImpact);
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
        DepartedNpcId = null;
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
            bool ownsStage = OwnsDuelStage(fact);
            var previousShot = ownsStage ? _timeline.LastOrDefault(c => !c.Completed && c.Exchange != null) : null;
            // 可无缝衔接时只交接上一击事实，续招按同一舞台尺寸重新求落脚点。
            bool continues = previousShot != null && Math.Abs(previousShot.End - start) < 1e-8 &&
                _elapsed < previousShot.Start + previousShot.Duration * .70;
            var priorExchange = continues ? previousShot!.Exchange : null;
            // 旧拼点在镜头之外另等一拍。把这段时间并入镜头，保持整段的原有长度。
            double duration = Duration(fact) * (continues ? .66 : 1) + (fact.Contest != null ? ApproachHoldSeconds : 0);
            var cue = new PresentationCue(fact, before, frame.Snapshot, start, duration, Generation)
            {
                Exchange = ownsStage ? new DuelExchange(fact, priorExchange) : null,
                PreviousExchange = priorExchange
            };
            if (continues) previousShot!.ContinuesOnStage = true;
            _timeline.Add(cue);
            if (fact.ContestId is { } injuryContest && fact.Damage is { } hit && (hit.HpDamage > 0 || hit.ShieldAbsorbed > 0))
            {
                var shot = _timeline.LastOrDefault(c => c.Event.ContestId == injuryContest && c.Event.Contest != null);
                // 接触前可以用扣血替换护盾反馈。接触后的数值照常显示，脚步保持连续。
                if (shot?.Exchange is { } exchange && _elapsed <= shot.ContactTime &&
                    (exchange.ContestImpact == null || exchange.ContestImpact.HpDamage == 0 && hit.HpDamage > 0))
                    exchange.ContestImpact = hit;
            }
            foreach (var key in HoldKeys(fact)) _ready[key] = Math.Max(_ready.GetValueOrDefault(key), cue.End);
            // 本次拼点的伤害可在接触时出现，待发槽仍保留到镜头回场。
            if (fact.Contest != null && fact.ContestId is { } contestId)
                _ready["contest-contact:" + contestId] = cue.ContactTime;
            // 生效关系以出现时点交付；0.7/0.8 秒文字寿命从不成为其他 cue 的锁。
            if (fact.Applied != null && fact.EffectId.HasValue)
                _ready["effect:" + fact.EffectId] = Math.Max(_ready.GetValueOrDefault("effect:" + fact.EffectId), cue.Start);
        }
        MarkFinishingExchange(frame.Snapshot, previous);
        RefreshDisplay();
        var generation = Generation;
        ApplyAtTime();
        if (Generation != generation) return;
        if (frame.Events.Count == 0) SnapshotSettled?.Invoke(frame.Snapshot);
    }

    private void MarkFinishingExchange(BattleSnapshot snapshot, BattleSnapshot previous)
    {
        if (!snapshot.IsFinished || snapshot.Result is not { } result) return;
        var last = _timeline.LastOrDefault(c => !c.Completed && c.Exchange != null);
        if (last?.Exchange is not { } exchange || last.Event.Tick != snapshot.Tick ||
            (last.Event.Damage ?? exchange.ContestImpact) is not { HpDamage: > 0 } hit) return;
        // 核心判负也可能来自伤口挤爆 SP。用终局的团队与状态确认，气血只负责此次命中。
        if (result.Combatants.Any(a => a.NpcId == hit.TargetNpcId && a.Status == "Defeated" && a.Team != result.WinnerTeam) &&
            previous.Participants.Any(a => a.Id == hit.TargetNpcId && a.Status == "Active"))
            exchange.FinishingNpcId = hit.TargetNpcId;
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
        // 单个镜头及前置资源过渡按原速播放。舞台确有积压时才追赶，文字寿命不参与。
        double blockingEnd = _timeline.Where(c => !c.Completed && IsShortTransition(c.Event))
            .Select(c => c.End).DefaultIfEmpty(_elapsed).Max();
        var firstShot = _timeline.FirstOrDefault(c => !c.Completed && OwnsDuelStage(c.Event));
        double playbackWindow = firstShot == null ? .8 : firstShot.Duration + .45;
        double speed = Math.Clamp((blockingEnd - _elapsed) / playbackWindow, 1, 16);
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
                if (cue.Exchange?.FinishingNpcId is { } lost) DepartedNpcId = lost;
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
            cue.Sample(_elapsed);
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

    internal static bool OwnsDuelStage(CombatPresentationEvent fact) => fact.Contest != null || IsDirectStrike(fact);

    // 直击由核心的 Straight 处理流程携带 ActionId；被动和环境伤害只显示实际扣血与扣盾。
    internal static bool IsDirectStrike(CombatPresentationEvent fact) =>
        fact.ActionId.HasValue && !fact.ContestId.HasValue && !fact.EffectId.HasValue &&
        fact.Damage is { SourceNpcId: int source } damage && source != damage.TargetNpcId &&
        damage.DamageType is "Zhan" or "Da" or "Ci" or "SheJi" &&
        (damage.HpDamage > 0 || damage.ShieldAbsorbed > 0);

    private static bool IsShortTransition(CombatPresentationEvent e) => e.ManaAssigned != null || e.CardUsed != null ||
        e.CardAdded != null || e.Pending != null || OwnsDuelStage(e);
    private static double Duration(CombatPresentationEvent fact) => fact.Kind switch
    {
        CombatPresentationKind.ManaAssigned => .18,
        CombatPresentationKind.CardUsed => .10,
        CombatPresentationKind.PendingChanged when fact.Pending is { Entered: true } && fact.Card is { Cells.Count: > 0 } => PendingFlightSeconds,
        CombatPresentationKind.ContestResolved => DuelExchange.Winner(fact.Contest)?.ContestType switch
        {
            "Dodge" => 1.80,
            "Shield" or "Block" => 1.94,
            "Da" => 2.40,
            _ => 2.24
        },
        CombatPresentationKind.CardAddedToBackpack => .40,
        CombatPresentationKind.CardApplied => .70,
        CombatPresentationKind.DamageResolved when IsDirectStrike(fact) => fact.Damage switch
        {
            { HpDamage: 0 } => 1.36,
            { DamageType: "Da" } => 1.82,
            { DamageType: "SheJi" } => 1.48,
            _ => 1.56
        },
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
        if (e.ContestId is { } contestId)
            yield return (e.Damage != null || e.Applied != null || e.CardAdded != null ? "contest-contact:" : "contest:") + contestId;
        if (e.EffectId.HasValue) yield return "effect:" + e.EffectId;
        if (e.Pending != null) yield return "pending:" + e.NpcId;
        if (OwnsDuelStage(e)) yield return DuelStage;
        if (IsDirectStrike(e) && e.Damage?.SourceNpcId is int striker) yield return "pending:" + striker;
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
        if ((e.ManaAssigned != null || e.CardUsed != null || e.Pending is { Entered: true }) && e.ActionId.HasValue)
            yield return "action:" + e.ActionId;
        if (e.Pending != null) yield return "pending:" + e.NpcId;
        if (OwnsDuelStage(e)) yield return DuelStage;
        if (e.Contest != null)
        {
            yield return "pending:" + e.Contest.Left.NpcId;
            yield return "pending:" + e.Contest.Right.NpcId;
            if (e.ContestId.HasValue) yield return "contest:" + e.ContestId;
        }
    }
}
