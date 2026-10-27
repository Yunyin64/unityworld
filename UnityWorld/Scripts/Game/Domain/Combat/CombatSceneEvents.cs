using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        // 诊断上限，不是发动次数或冷却规则；正常独立事件各自建立新链。
        public const int EventChainBudget = 10000;
        private readonly Queue<APIContext> _eventQueue = new();
        private readonly Queue<string> _eventHistory = new();
        private readonly Stack<(long ChainId, List<int> Path)> _activationFrames = new();
        private long _nextEventId;
        private long _nextChainId;
        private bool _drainingEvents;
        private APIContext _currentEvent;
        private bool _collectingEventOpportunities;
        private readonly Dictionary<int, CombatCard> _eventOpportunities = new();
        public string EventFailure { get; private set; }
        public int LargestEventChain { get; private set; }
        internal bool IsCurrentEvent(APIContext ctx) => _drainingEvents && ReferenceEquals(_currentEvent, ctx);
        internal bool RequestEventOpportunity(CombatCard card, APIContext ctx)
        {
            if (!_collectingEventOpportunities || !IsCurrentEvent(ctx)) return false;
            return _eventOpportunities.TryAdd(card.Id, card);
        }

        internal void BeginActivation(CombatCard card, APIContext trigger)
        {
            var parent = trigger ?? _currentEvent;
            var path = parent == null ? new List<int>() : new List<int>(parent.ActivationPath);
            path.Add(card.Id);
            _activationFrames.Push((parent?.ChainId ?? ++_nextChainId, path));
        }

        internal void EndActivation()
        {
            _activationFrames.Pop();
            if (_activationFrames.Count == 0) DrainCombatEvents();
        }

        internal void EnqueueCombatEvent(string eventName, APIContext source)
        {
            ThrowIfEventAborted();
            ArgumentNullException.ThrowIfNull(source);
            var ctx = source.CopyForEvent();
            ctx.Scene = this;
            ctx.EventName = eventName;
            ctx.EventId = ++_nextEventId;
            if (_activationFrames.TryPeek(out var frame))
            {
                ctx.ChainId = frame.ChainId;
                ctx.ActivationPath = [.. frame.Path];
            }
            else if (_currentEvent != null)
            {
                ctx.ChainId = _currentEvent.ChainId;
                ctx.ActivationPath = [.. _currentEvent.ActivationPath];
            }
            else ctx.ChainId = ++_nextChainId;
            _eventQueue.Enqueue(ctx);
            if (_activationFrames.Count == 0) DrainCombatEvents();
        }

        private void DrainCombatEvents()
        {
            if (_drainingEvents || _activationFrames.Count != 0) return;
            ThrowIfEventAborted();
            _drainingEvents = true;
            var counts = new Dictionary<long, int>();
            try
            {
                while (_eventQueue.TryDequeue(out var ctx))
                {
                    _currentEvent = ctx;
                    counts.TryGetValue(ctx.ChainId, out int count);
                    counts[ctx.ChainId] = ++count;
                    LargestEventChain = Math.Max(LargestEventChain, count);
                    _eventHistory.Enqueue($"Event={ctx.EventId} Chain={ctx.ChainId} {ctx.EventName} Caster={ctx.Caster?.Id} Source={ctx.SourceCard?.Id}:{ctx.SourceCard?.DefineId} Path=[{string.Join(",", ctx.ActivationPath)}]");
                    while (_eventHistory.Count > 64) _eventHistory.Dequeue();
                    if (count > EventChainBudget) AbortEventChain($"事件链超过诊断预算 {EventChainBudget}");
                    _eventOpportunities.Clear();
                    _collectingEventOpportunities = true;
                    DispatchHookToAll(ctx.EventName, ctx);
                    var scopes = new List<(Scope scope, string id)>();
                    if (ctx.Caster != null) scopes.Add((Scope.CombatNpc, ctx.Caster.Id.ToString()));
                    if (ctx.SourceCard != null) scopes.Add((Scope.CombatCard, ctx.SourceCard.Id.ToString()));
                    EventMgr.Instance?.TriggerEvent(ctx.EventName, ctx, scopes.ToArray());
                    ThrowIfEventAborted();
                    _collectingEventOpportunities = false;
                    // 两通道全部条件收集完成后统一争槽；注册顺序不会抢先提交。
                    foreach (var card in _eventOpportunities.Values.OrderBy(c => c.Owner.Id)
                        .ThenBy(c => c.Position?.Y ?? int.MaxValue).ThenBy(c => c.Position?.X ?? int.MaxValue).ThenBy(c => c.Id).ToArray())
                        card.TryCommitEventOpportunity(ctx);
                    _eventOpportunities.Clear();
                }
            }
            finally
            {
                _currentEvent = null;
                _collectingEventOpportunities = false;
                _eventOpportunities.Clear();
                _drainingEvents = false;
                if (EventFailure != null) _eventQueue.Clear();
            }
        }

        internal void AbortEventChain(string reason)
        {
            EventFailure ??= $"战斗事件链中止 Tick={CurrentTick}: {reason}; 最近事件: {string.Join(" | ", _eventHistory)}";
            SetPhaseFinished();
            throw new InvalidOperationException(EventFailure);
        }

        /// <summary>Lua/NLua 和 EventMgr 包装层必须让已标记的致命战斗错误冒泡。</summary>
        public void ThrowIfEventAborted()
        {
            if (EventFailure != null) throw new InvalidOperationException(EventFailure);
        }
    }
}
