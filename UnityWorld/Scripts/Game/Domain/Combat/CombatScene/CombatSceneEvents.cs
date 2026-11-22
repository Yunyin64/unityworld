using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        /// <summary>同一条事件链的诊断上限。超过上限时终止本场并报告最近的事件。</summary>
        public const int EventChainBudget = 10000;

        private readonly Queue<APIContext> _eventQueue = new();
        private readonly Stack<ActivationScope> _activationFrames = new();
        private readonly Queue<string> _eventHistory = new();
        private APIContext? _currentEvent;
        private long _nextEventId;
        private long _nextChainId;

        /// <summary>本场事件链中止的原因和最近事件。正常运行时为空。</summary>
        public string? EventFailure { get; private set; }

        /// <summary>本场已派发过的各条事件链中，单链处理的最大事件数。</summary>
        public int LargestEventChain { get; private set; }

        /// <summary>
        /// 触发本场玩法事件。每个事件先派给卡牌、关键词和 Modifier 的 Lua 回调，
        /// 再通过 EventMgr 通知 C# 监听者。回调中新触发的事件排在队尾。
        /// </summary>
        public void TriggerCombatEvent(string eventId, APIContext ctx)
        {
            ThrowIfEventAborted();
            ArgumentNullException.ThrowIfNull(ctx);

            var queued = ctx.CopyForEvent();
            queued.Scene = this;
            queued.EventName = eventId;
            queued.EventId = ++_nextEventId;
            AssignEventChain(queued);
            CaptureQueuedPresentationOrigin(queued.EventId);
            _eventQueue.Enqueue(queued);

            // 一张牌发动时，先让它提交本次发动产生的全部事件，再让监听者响应。
            if (_activationFrames.Count == 0) DrainCombatEvents();
        }

        private void AssignEventChain(APIContext ctx)
        {
            if (_activationFrames.TryPeek(out var frame) && ReferenceEquals(frame.TriggerEvent, _currentEvent))
            {
                // 发动卡自己的 Lua 正在运行，这张卡已在发动路径里。
                ctx.ChainId = frame.ChainId;
                ctx.ActivationPath = [.. frame.Path];
            }
            else if (_currentEvent != null)
            {
                // 来自监听者的后续事件，沿用正在派发的事件链和发动路径。
                ctx.ChainId = _currentEvent.ChainId;
                ctx.ActivationPath = [.. _currentEvent.ActivationPath];
            }
            else
            {
                ctx.ChainId = ++_nextChainId;
            }
        }

        private void DrainCombatEvents()
        {
            // 外层循环会处理新入队的事件，两个通道都完成之后才轮到下一件。
            if (_currentEvent != null) return;
            ThrowIfEventAborted();

            var chains = new Dictionary<long, EventChain>();
            try
            {
                while (_eventQueue.TryDequeue(out var ctx))
                {
                    _currentEvent = ctx;
                    using var presentation = EnterQueuedPresentationOrigin(ctx.EventId);

                    if (!chains.TryGetValue(ctx.ChainId, out var chain))
                    {
                        chain = new EventChain();
                        chains.Add(ctx.ChainId, chain);
                    }
                    RecordDispatchedEvent(ctx, ++chain.Count);
                    var respondedCards = chain.GetRespondedCards(ctx);

                    DispatchToLua(ctx, respondedCards);
                    DispatchToEventMgr(ctx);
                    ThrowIfEventAborted();
                }
            }
            finally
            {
                _currentEvent = null;
                if (EventFailure != null)
                {
                    _eventQueue.Clear();
                    _queuedPresentationOrigins.Clear();
                }
            }
        }

        private void DispatchToLua(APIContext ctx, HashSet<int> respondedCards)
        {
            var hookName = ctx.EventName;
            foreach (var npc in OrderedCombatants)
            {
                // 一名 NPC 的场上牌按行、列、Id 响应。回调可能改变场上牌，先取本次遍历的快照。
                var cards = npc.GetField()
                    .OrderBy(card => npc.Backpack.GetCardPosition(card.Id)?.Y ?? int.MaxValue)
                    .ThenBy(card => npc.Backpack.GetCardPosition(card.Id)?.X ?? int.MaxValue)
                    .ThenBy(card => card.Id).ToArray();
                foreach (var card in cards)
                {
                    card.DispatchCombatEvent(hookName, ctx, respondedCards);
                    ThrowIfEventAborted();
                }

                foreach (var mod in npc.GetAllModifiers())
                {
                    if (mod.HasHook(hookName)) mod.CallLuaHook<bool>(hookName, mod.env, ctx);
                    ThrowIfEventAborted();
                }
            }
        }

        private static void DispatchToEventMgr(APIContext ctx)
        {
            // EventMgr 自行补上 Global；本场只提供这次事件涉及的 NPC 和卡牌作用域。
            var scopes = new List<(Scope scope, string id)>();
            if (ctx.Caster != null) scopes.Add((Scope.CombatNpc, ctx.Caster.Id.ToString()));
            if (ctx.SourceCard != null) scopes.Add((Scope.CombatCard, ctx.SourceCard.Id.ToString()));
            EventMgr.Instance?.TriggerEvent(ctx.EventName, ctx, scopes.ToArray());
        }

        // 每次排空队列时按 ChainId 分开计数和去重。同一 Tick 的独立发动可以再次触发同一张卡。
        private sealed class EventChain
        {
            /// <summary>本次排队派发中，这条事件链已取出的事件数。</summary>
            public int Count;
            private readonly Dictionary<(string Name, CombatNpc Caster, CombatCard Card, CombatNpcModifier Buff),
                List<(APIContext Original, HashSet<int> Cards)>> _responses = new();

            /// <summary>取得这条链中，对同一来源和原始内容已响应的卡牌 Id。</summary>
            public HashSet<int> GetRespondedCards(APIContext ctx)
            {
                var key = (ctx.EventName, ctx.Caster, ctx.SourceCard, ctx.SourceBuff);
                if (!_responses.TryGetValue(key, out var responses))
                {
                    responses = new();
                    _responses.Add(key, responses);
                }
                foreach (var response in responses)
                {
                    if (ctx.HasSameEventPayload(response.Original)) return response.Cards;
                }

                // 以派发前的目标、参数和占格为准。后续 Lua 对 ctx 的写入不能改变去重依据。
                var cards = new HashSet<int>();
                responses.Add((ctx.CopyForEvent(), cards));
                return cards;
            }
        }

        // 拼点要先广播再读取双方点数，只能在当前发动和事件派发都结束时开始。
        internal bool AtContestBoundary => _currentEvent == null && _activationFrames.Count == 0;

        internal bool IsActivating(CombatCard card) => _activationFrames.Any(frame => frame.Card == card);

        // 排队的监听者运行期间，原卡仍受重入保护，但监听者已经离开原卡的发动回调。
        internal bool IsExecutingActivation(CombatCard card) => _activationFrames.Any(frame =>
            frame.Card == card && ReferenceEquals(frame.TriggerEvent, _currentEvent));

        internal IDisposable? BeginActivation(CombatCard card)
        {
            if (IsActivating(card)) return null;
            var frame = new ActivationScope(this, card, _currentEvent, _currentEvent?.ChainId ?? ++_nextChainId);
            _activationFrames.Push(frame);
            return frame;
        }

        private sealed class ActivationScope(CombatScene scene, CombatCard card, APIContext? trigger, long chainId) : IDisposable
        {
            /// <summary>正在发动的卡牌。</summary>
            public CombatCard Card { get; } = card;
            /// <summary>进入本次发动时正在派发的事件，独立发动时为空。</summary>
            public APIContext? TriggerEvent { get; } = trigger;
            /// <summary>本次发动产生的事件所属的链。</summary>
            public long ChainId { get; } = chainId;
            /// <summary>从触发事件到当前卡的发动路径，供后续事件继承。</summary>
            public List<int> Path { get; } = trigger == null ? [card.Id] : [.. trigger.ActivationPath, card.Id];

            /// <summary>最外层发动结束时派发积压事件，随后释放当前卡的重入保护。</summary>
            public void Dispose()
            {
                try
                {
                    // 最外层发动要留在栈上直到队列排空，避免监听者再次发动原卡。
                    if (scene._activationFrames.Count == 1) scene.DrainCombatEvents();
                }
                finally
                {
                    scene._activationFrames.Pop();
                }
            }
        }

        private void RecordDispatchedEvent(APIContext ctx, int count)
        {
            LargestEventChain = Math.Max(LargestEventChain, count);
            _eventHistory.Enqueue($"Event={ctx.EventId} Chain={ctx.ChainId} {ctx.EventName} " +
                $"Caster={ctx.Caster?.Id} Source={ctx.SourceCard?.Id}:{ctx.SourceCard?.DefineId} " +
                $"Path=[{string.Join(",", ctx.ActivationPath)}]");
            if (_eventHistory.Count > 64) _eventHistory.Dequeue();
            if (count > EventChainBudget) AbortEventChain($"事件链超过诊断预算 {EventChainBudget}");
        }

        internal void AbortEventChain(string reason)
        {
            EventFailure ??= $"战斗事件链中止 Tick={CurrentTick}: {reason}; 最近事件: {string.Join(" | ", _eventHistory)}";
            SetPhaseFinished();
            throw new InvalidOperationException(EventFailure);
        }

        /// <summary>让已中止的战斗穿过 Lua 和 EventMgr 的异常处理，停止后续监听者。</summary>
        public void ThrowIfEventAborted()
        {
            if (EventFailure != null) throw new InvalidOperationException(EventFailure);
        }
    }
}
