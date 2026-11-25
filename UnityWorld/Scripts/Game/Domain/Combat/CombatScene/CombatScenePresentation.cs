#nullable enable
namespace UnityWorld.Game.Domain.Combat;

public partial class CombatScene
{
    /// <summary>按顺序保存的战斗回放事件</summary>
    private readonly List<CombatPresentationEvent> _presentationEvents = new();
    /// <summary>嵌套效果回放作用域栈</summary>
    private readonly Stack<PresentationScope> _presentationScopes = new();
    // 玩法事件稍后才派发，借事件 Id 找回产生它的动作。清理表现时可单独释放这些关联。
    /// <summary>按玩法事件编号缓存的回放来源</summary>
    private readonly Dictionary<long, PresentationOrigin> _queuedPresentationOrigins = new();
    /// <summary>回放事件顺序计数器</summary>
    private long _presentationSequence;
    /// <summary>回放关联编号计数器</summary>
    private long _presentationIdentity;
    /// <summary>当前继承的回放来源</summary>
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

    /// <summary>读取指定偏移之后的表现事实。观察者独立推进偏移，完整回放仍可读取。</summary>
    public IReadOnlyList<CombatPresentationEvent> ReadPresentationEvents(int offset)
    {
        if (offset == _presentationEvents.Count) return Array.Empty<CombatPresentationEvent>();
        return _presentationEvents.GetRange(offset, _presentationEvents.Count - offset).AsReadOnly();
    }

    /// <summary>取走自上次消费以来的所有表现记录，供客户端构建本帧。</summary>
    public IReadOnlyList<CombatPresentationEvent> DrainPresentationEvents()
    {
        var copy = ReadPresentationEvents();
        _presentationEvents.Clear();
        return copy;
    }

    /// <summary>丢弃尚未消费的表现记录。</summary>
    public void ClearPresentationEvents() => _presentationEvents.Clear();

    /// <summary>开启回放时分配下一关联编号</summary>
    internal long? NextPresentationIdentity() => PresentationRecordingEnabled ? ++_presentationIdentity : null;
    /// <summary>复制回放数据为只读列表</summary>
    internal static IReadOnlyList<T> PresentationCopy<T>(IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
    /// <summary>记录卡牌状态和当前占格为回放快照</summary>
    internal static CombatPresentationCard PresentationCard(CombatCard card) => new(card.Id, card.DefineId,
        card.DisplayName ?? card.DefineId, card.GetSize(), PresentationCopy(card.GetKeywords()),
        PresentationCopy(card.GetManaCost().OrderBy(p => p.Key.ToString()).Select(p => new PresentationManaCost(p.Key.ToString(), p.Value))),
        PresentationCopy(card.AssignedElements.Select(e => e.ToString())),
        PresentationCopy(card.Owner?.Backpack.GetCardCells(card.Id).Select(c => new PresentationCell(c.X, c.Y)) ?? Array.Empty<PresentationCell>()),
        PresentationCopy(card.BaseData.Shape.Select(c => new PresentationCell(c.X, c.Y)))) { Phase = card.Phase };
    /// <summary>读取当前效果作用域或继承的回放来源</summary>
    internal PresentationOrigin? CapturePresentationOrigin() => !PresentationRecordingEnabled ? null :
        _presentationScopes.TryPeek(out var scope) ? scope.Origin : _presentationOrigin;

    /// <summary>为待派发事件缓存触发时的回放来源</summary>
    private void CaptureQueuedPresentationOrigin(long eventId)
    {
        var origin = CapturePresentationOrigin();
        if (origin != null) _queuedPresentationOrigins.Add(eventId, origin);
    }

    /// <summary>取出事件缓存的来源并进入相应回放上下文</summary>
    private IDisposable EnterQueuedPresentationOrigin(long eventId)
    {
        _queuedPresentationOrigins.Remove(eventId, out var origin);
        return EnterPresentationOrigin(origin);
    }

    /// <summary>切换当前回放来源，退出作用域时恢复</summary>
    internal IDisposable EnterPresentationOrigin(PresentationOrigin? origin)
    {
        var previous = _presentationOrigin;
        _presentationOrigin = origin;
        return new PresentationRestore(() => _presentationOrigin = previous);
    }

    /// <summary>生成带序号及来源关联的回放事件</summary>
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

    /// <summary>开启卡牌效果的回放作用域，合并同一卡的嵌套调用</summary>
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
        /// <summary>该效果所属的战斗场景</summary>
        private readonly CombatScene _scene;
        /// <summary>正在生效的卡牌</summary>
        internal CombatCard Card { get; }
        /// <summary>效果的回放关联编号</summary>
        internal PresentationOrigin Origin { get; }
        /// <summary>是否处于主动卡效果内</summary>
        internal bool Active { get; }
        /// <summary>效果是否已提交实际变化</summary>
        internal bool Committed { get; set; }
        /// <summary>记录效果来源的标签</summary>
        private readonly string _label;
        /// <summary>效果流程是否已执行完成</summary>
        private bool _completed;
        /// <summary>保存卡牌效果的回放来源和关联信息</summary>
        internal PresentationScope(CombatScene scene, CombatCard card, string label, bool active, PresentationOrigin origin)
        { _scene = scene; Card = card; _label = label; Active = active; Origin = origin; }
        /// <summary>标记卡牌效果已执行完成</summary>
        internal void Complete() => _completed = true;
        /// <summary>退出效果作用域并记录满足条件的生效事件</summary>
        public void Dispose()
        {
            _scene._presentationScopes.Pop();
            if (_completed && (Active || Committed))
                _scene.RecordPresentation(CombatPresentationKind.CardApplied, Card.Owner, Card, Origin,
                    applied: new CardAppliedPresentation(_label, !Active || !Card.HasScanOpportunity));
        }
    }
    /// <summary>用释放回调构造回放来源恢复作用域</summary>
    private sealed class PresentationRestore(Action restore) : IDisposable
    {
        /// <summary>执行回调以恢复上一个回放来源</summary>
        public void Dispose() => restore();
    }
}
