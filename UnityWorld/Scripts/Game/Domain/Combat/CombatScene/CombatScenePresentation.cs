#nullable enable
namespace UnityWorld.Game.Domain.Combat;

public partial class CombatScene
{
    private readonly List<CombatPresentationEvent> _presentationEvents = new();
    private readonly Stack<PresentationScope> _presentationScopes = new();
    // 玩法事件稍后才派发，借事件 Id 找回产生它的动作。清理表现时可单独释放这些关联。
    private readonly Dictionary<long, PresentationOrigin> _queuedPresentationOrigins = new();
    private long _presentationSequence;
    private long _presentationIdentity;
    private PresentationOrigin? _presentationOrigin;

    /// <summary>本场是否记录供客户端播放的战斗结果。</summary>
    public bool PresentationRecordingEnabled { get; private set; }

    /// <summary>应在 PreStart 前启用；关闭即释放未消费事实，序号不倒退。</summary>
    public void EnablePresentationRecording(bool enabled)
    {
        PresentationRecordingEnabled = enabled;
        if (!enabled) ClearPresentationEvents();
    }
    /// <summary>读取当前表现记录的快照，保留未消费的记录。</summary>
    public IReadOnlyList<CombatPresentationEvent> ReadPresentationEvents() => Array.AsReadOnly(_presentationEvents.ToArray());

    /// <summary>取走自上次消费以来的所有表现记录，供客户端构建本帧。</summary>
    public IReadOnlyList<CombatPresentationEvent> DrainPresentationEvents()
    {
        var copy = ReadPresentationEvents();
        _presentationEvents.Clear();
        return copy;
    }

    /// <summary>丢弃尚未消费的表现记录。</summary>
    public void ClearPresentationEvents() => _presentationEvents.Clear();

    internal long? NextPresentationIdentity() => PresentationRecordingEnabled ? ++_presentationIdentity : null;
    internal static IReadOnlyList<T> PresentationCopy<T>(IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
    internal static CombatPresentationCard PresentationCard(CombatCard card) => new(card.Id, card.DefineId,
        card.DisplayName ?? card.DefineId, card.GetSize(), PresentationCopy(card.GetKeywords()),
        PresentationCopy(card.GetManaCost().OrderBy(p => p.Key.ToString()).Select(p => new PresentationManaCost(p.Key.ToString(), p.Value))),
        PresentationCopy(card.AssignedElements.Select(e => e.ToString())),
        PresentationCopy(card.Owner?.Backpack.GetCardCells(card.Id).Select(c => new PresentationCell(c.X, c.Y)) ?? Array.Empty<PresentationCell>()),
        PresentationCopy(card.BaseData.Shape.Select(c => new PresentationCell(c.X, c.Y)))) { Phase = card.Phase };
    internal PresentationOrigin? CapturePresentationOrigin() => !PresentationRecordingEnabled ? null :
        _presentationScopes.TryPeek(out var scope) ? scope.Origin : _presentationOrigin;

    private void CaptureQueuedPresentationOrigin(long eventId)
    {
        var origin = CapturePresentationOrigin();
        if (origin != null) _queuedPresentationOrigins.Add(eventId, origin);
    }

    private IDisposable EnterQueuedPresentationOrigin(long eventId)
    {
        _queuedPresentationOrigins.Remove(eventId, out var origin);
        return EnterPresentationOrigin(origin);
    }

    internal IDisposable EnterPresentationOrigin(PresentationOrigin? origin)
    {
        var previous = _presentationOrigin;
        _presentationOrigin = origin;
        return new PresentationRestore(() => _presentationOrigin = previous);
    }

    internal void RecordPresentation(CombatPresentationKind kind, CombatNpc npc, CombatCard? card = null,
        PresentationOrigin? origin = null, ManaAssignedPresentation? mana = null, CardUsedPresentation? used = null,
        PendingPresentation? pending = null, ContestPresentation? contest = null, DamagePresentation? damage = null,
        CardAddedPresentation? added = null, CardAppliedPresentation? applied = null)
    {
        if (!PresentationRecordingEnabled) return;
        origin ??= CapturePresentationOrigin();
        _presentationEvents.Add(new CombatPresentationEvent(CurrentTick, ++_presentationSequence, CurrentTick == 0,
            kind, npc.Id, card == null ? null : PresentationCard(card), origin?.ActionId, origin?.ParentActionId,
            origin?.ContestId, origin?.EffectId, mana, used, pending, contest, damage, added, applied));
    }

    /// <summary>只在真实动作提交后标记；空 Hook、条件读取和零变化不会产生名称提示。</summary>
    internal void MarkPresentationEffect()
    {
        if (PresentationRecordingEnabled && _presentationScopes.TryPeek(out var scope)) scope.Committed = true;
    }

    internal PresentationScope? BeginPresentationEffect(CombatCard card, string origin, bool active = false)
    {
        if (!PresentationRecordingEnabled) return null;
        // Apply 内同一卡自身/关键词调用归入本次生效；嵌套另一张卡独立归属。
        if (_presentationScopes.TryPeek(out var current) && current.Card == card && current.Active) return null;
        var parent = CapturePresentationOrigin();
        var relation = new PresentationOrigin(active ? card.PresentationActionId : parent?.ActionId,
            parent?.ActionId, parent?.ContestId ?? (active ? card.PresentationContestId : null), NextPresentationIdentity());
        var scope = new PresentationScope(this, card, origin, active, relation);
        _presentationScopes.Push(scope);
        return scope;
    }

    internal sealed class PresentationScope : IDisposable
    {
        private readonly CombatScene _scene;
        internal CombatCard Card { get; }
        internal PresentationOrigin Origin { get; }
        internal bool Active { get; }
        internal bool Committed { get; set; }
        private readonly string _label;
        private bool _completed;
        internal PresentationScope(CombatScene scene, CombatCard card, string label, bool active, PresentationOrigin origin)
        { _scene = scene; Card = card; _label = label; Active = active; Origin = origin; }
        internal void Complete() => _completed = true;
        public void Dispose()
        {
            _scene._presentationScopes.Pop();
            if (_completed && (Active || Committed))
                _scene.RecordPresentation(CombatPresentationKind.CardApplied, Card.Owner, Card, Origin,
                    applied: new CardAppliedPresentation(_label, !Active || !Card.HasScanOpportunity));
        }
    }
    private sealed class PresentationRestore(Action restore) : IDisposable { public void Dispose() => restore(); }
}
