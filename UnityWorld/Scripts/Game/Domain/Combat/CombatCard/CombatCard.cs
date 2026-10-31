
using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard : Card,ICombatEntity,ILuaBindable
    {
        private CardBaseData _combatBaseData = new();
        /// <summary>战斗私有快照，Lua 派生关键词和临时修改不会污染世界卡。</summary>
        public override CardBaseData BaseData => _combatBaseData;
        public LuaTable env { get; set; }
        public Dictionary<string, LuaFunction> LuaHooks { get; set; } = new();
        public HashSet<string> HookUse { get; set; } = new();
        public CombatNpc Owner;    
        private CombatCardPhase Phase = CombatCardPhase.Waiting;

        private bool isReady = false;
        private bool _usingActiveOpportunity;
        private bool _awaitingApply;
        private APIContext _activationEvent;
        internal long? PresentationActionId { get; private set; }
        internal long? PresentationContestId { get; set; }
        public bool IsFrozen { get; private set; }
        public bool Freeze()
        {
            if (IsFrozen) return false;
            IsFrozen = true;
            Scene?.MarkPresentationEffect();
            Log("冻结：下次完整扫描跳过后解除");
            return true;
        }
        internal bool ConsumeFreezeAtScanEnd()
        {
            if (!IsFrozen) return false;
            IsFrozen = false;
            Log("完整扫描：跳过主动机会并解冻");
            return true;
        }
        public int UseCount { get; private set; }
        public int ApplyCount { get; private set; }
        public void SetReady(bool ready) => isReady = ready;
        public bool IsReady() => isReady;
        public Dictionary<string, float> Ticks { get ; set ; } = new();
        public CombatScene Scene
        {
            get => Owner?.Scene;
            set => Owner.Scene = value;
        }

        public void PreStart()
        {
            InitializeLuaCards();
            CallLua("PreStart");
        }

        public void Start()
        {
            Ticks["Main"] = 0;
            Ticks["CD"] = 0;
            SetPhase(BaseData.HasActiveAbility ? CombatCardPhase.Waiting : CombatCardPhase.Passive);
            CallLua("Start");
        }
        public void ReLoad()
        {
            if(CheckPhase( CombatCardPhase.Finished)) SetPhase(CombatCardPhase.Waiting);
        }

        public void Tick()
        {
            ReLoad();
            var ctx = CreateCtx();
            if (BaseData.HasActiveAbility) InvokeOwnHook("Tick", ctx);
            // Tick/OnTick 是同一生命周期的两种历史拼写，只选一个。
            RunKeywordHooks("OnTick", ctx, "Tick");
            if (!BaseData.HasActiveAbility || HasKeyword("Passive")) InvokeOwnHook("OnPassiveTick", ctx);
            CardModifierTick();
            HookUse.Clear();
            Ticks["Main"]++;
            

        }
        public void CDTick()
        {
            // 兼容旧调用签名；CD 不再推进或产生出手机会。
        }

        private float GetCDAdd()
        {
            var TickSpeed = GetStat("CDSpeed");
            var CDadd = 1f;
            if(TickSpeed > 0) CDadd = CDadd*(1+TickSpeed/10);
            if(TickSpeed < 0)
            {
                CDadd = CDadd / (1 + (-TickSpeed) / 10f);
            }
            if(CDadd <= 0.1f) CDadd = 0;
            return CDadd;
        }

        /// <summary>按当前CDSpeed计算还需多少真实Tick才能CD满，返回-1表示无法完成</summary>
        public int GetCDTickRemaining()
        {
            var CDadd = GetCDAdd();
            if(CDadd <= 0.1f) return -1;
            var remaining = GetCDMax() - Ticks["CD"];
            if(remaining <= 0) return 0;
            return (int)Math.Ceiling(remaining / CDadd);
        }



        internal bool TryUseFromScan()
        {
            return BaseData.HasScanOpportunity && TryCommitUse(null);
        }

        /// <summary>返回本事件是否接收该机会；两通道收集完后按位置统一提交，不代表已成功 Use。</summary>
        public bool TryUseFromEvent(APIContext eventCtx)
        {
            if (!BaseData.HasEventOpportunity || eventCtx == null || eventCtx.Scene != Scene || eventCtx.EventId == 0 || !Scene.IsCurrentEvent(eventCtx)) return false;
            if (eventCtx.SuccessfulEventCards.Contains(Id) || !HasUseEligibility()) return false;
            return Scene.RequestEventOpportunity(this, eventCtx);
        }

        internal bool TryCommitEventOpportunity(APIContext eventCtx)
        {
            if (!Scene.IsCurrentEvent(eventCtx) || eventCtx.SuccessfulEventCards.Contains(Id)) return false;
            return TryCommitUse(eventCtx);
        }

        private bool HasUseEligibility()
        {
            return !Scene.IsFinished && Owner.IsActive && BaseData.HasActiveAbility && !IsFrozen && !_usingActiveOpportunity
                && !_awaitingApply && !CheckPhase(CombatCardPhase.InPending) && Owner.Backpack.HasActivationResources(this)
                && (!(BaseData.ProducesContest ?? this.HasHook("Contest")) || Owner.PendingSlot == null);
        }

        private bool TryCommitUse(APIContext eventCtx)
        {
            Scene.ThrowIfEventAborted();
            if (!HasUseEligibility()) return false;
            // 只有本来能够提交的机会才可能构成循环；缺灵/冻结等自然停止不能误杀。
            if (eventCtx != null && eventCtx.ActivationPath.Contains(Id))
            {
                if (eventCtx.ActivationPath.LastOrDefault() == Id)
                {
                    Log($"拒绝自身派生事件重入 Event={eventCtx.EventId} Chain={eventCtx.ChainId}");
                    return false;
                }
                Scene.AbortEventChain($"主动事件循环 Card={Id}:{DefineId} path={string.Join(" -> ", eventCtx.ActivationPath)} -> {Id}");
            }
            _usingActiveOpportunity = true;
            _awaitingApply = true;
            _activationEvent = eventCtx;
            eventCtx?.SuccessfulEventCards.Add(Id);
            UseCount++;
            var parentOrigin = Scene.CapturePresentationOrigin();
            PresentationActionId = Scene.NextPresentationIdentity();
            PresentationContestId = null;
            using var presentationOrigin = Scene.EnterPresentationOrigin(new PresentationOrigin(PresentationActionId,
                parentOrigin?.ActionId, parentOrigin?.ContestId, null));
            Log(eventCtx == null ? "扫描机会提交 Use" : $"条件机会提交 Use Event={eventCtx.EventId} Chain={eventCtx.ChainId} Trigger={eventCtx.EventName} Caster={eventCtx.Caster?.Id} Source={eventCtx.SourceCard?.Id}:{eventCtx.SourceCard?.DefineId}");
            var resourcesBefore = Scene.PresentationRecordingEnabled
                ? CombatScene.PresentationCopy(Owner.Backpack.AssignedTo(Id).Select(e => e.ToString())) : null;
            Owner.Backpack.BeginNextActivation(Id);
            if (resourcesBefore != null)
                Scene.RecordPresentation(CombatPresentationKind.CardUsed, Owner, this,
                    origin: new PresentationOrigin(PresentationActionId, parentOrigin?.ActionId, parentOrigin?.ContestId, null),
                    used: new CardUsedPresentation(resourcesBefore, Array.AsReadOnly(Array.Empty<string>())));
            Scene.BeginActivation(this, eventCtx);
            try { Use(); }
            finally
            {
                try { Scene.EndActivation(); }
                finally { _usingActiveOpportunity = false; }
            }
            return true;
        }

        public void Use()
        {
            if (!_usingActiveOpportunity) throw new InvalidOperationException("Use 只能由扫描或条件机会授权。");
            if (BaseData.ProducesContest ?? this.HasHook("Contest")) Contest();
            Scene.TriggerCombatEvent("OnUse", CreateCtx());
            if (!CheckPhase(CombatCardPhase.InPending)) Apply();
        }

        public void Contest()
        {
            if (!_usingActiveOpportunity) throw new InvalidOperationException("Contest 只能在受控主动使用时产生待发数据。");
            CallLua("Contest");
        }

        public void Apply()
        {
            if (BaseData.HasActiveAbility && !_awaitingApply) return;
            var ownsScope = !_usingActiveOpportunity;
            if (ownsScope)
            {
                _usingActiveOpportunity = true;
                Scene.BeginActivation(this, _activationEvent);
            }
            try
            {
                using var presentation = Scene.BeginPresentationEffect(this, "Apply", active: true);
                _awaitingApply = false;
                ApplyCount++;
                CallLua("Apply");
                Scene.TriggerCombatEvent("OnApply", CreateCtx());
                if (BaseData.HasActiveAbility) SetPhase(CombatCardPhase.Finished);
                presentation?.Complete();
            }
            finally
            {
                if (ownsScope)
                {
                    try { Scene.EndActivation(); }
                    finally { _usingActiveOpportunity = false; }
                }
            }
        }

        /// <summary>
        /// 创建当前卡牌的默认 APIContext。
        /// </summary>
        private APIContext CreateCtx() => new APIContext
        {
            SourceCard = this,
            Caster = Owner,
            Scene = Owner?.Scene,
            TriggerEvent = _activationEvent
        };
        private APIContext CreateCtx<T>(string key,T value)
        {
            var api = CreateCtx();
            api.Set<T>(key,value);
            return api;
        }
        
        public CombatCardPhase GetPhase(CombatCardPhase phase) => Phase;
        
        public bool CheckPhase(CombatCardPhase phase) => Phase == phase;
        public void SetPhase(CombatCardPhase phase) => Phase = phase;

        /// <summary>
        /// 设置卡牌 Phase（供 Lua 调用）。
        /// 将字符串解析为 CombatCardPhase 枚举，解析失败时输出错误日志且不改变 Phase。
        /// </summary>
        public void SetPhase(string phaseName)
        {
            if (Enum.TryParse<CombatCardPhase>(phaseName, out var phase)) SetPhase(phase);
            else Log($"SetPhase 失败：无法解析 '{phaseName}' 为 CombatCardPhase");
        }

        /// <summary>
        /// 遍历卡牌 Keywords 列表，查 LuaMgr 注册表调用对应 hook 函数。
        /// keyword 未注册 → 报错日志；keyword 存在但无对应 hook → 静默跳过。
        /// </summary>
        private void RunKeywordHooks(string hookName, APIContext ctx, string fallback = null)
        {
            var keywords = GetKeywords();
            if (keywords == null || keywords.Count == 0) return;

            var luaMgr = LuaMgr.Instance;
            if (luaMgr == null) return;

            foreach (var kw in keywords.Distinct().ToArray())
            {
                var kwTable = luaMgr.GetKeyword(kw);
                if (kwTable == null)
                {
                    Log($"Keyword '{kw}' 未注册");
                    continue;
                }

                var func = kwTable[hookName] as NLua.LuaFunction;
                if (func == null && fallback != null) func = kwTable[fallback] as NLua.LuaFunction;
                if (func == null) continue; // hook 不存在，静默跳过

                try
                {
                    using var presentation = Scene?.BeginPresentationEffect(this, "Keyword:" + kw + ":" + hookName);
                    func.Call(this, ctx);
                    Scene?.ThrowIfEventAborted();
                    presentation?.Complete();
                }
                catch (Exception ex)
                {
                    Scene?.ThrowIfEventAborted();
                    Log($"Keyword '{kw}' hook '{hookName}' 异常: {ex.Message}");
                }
            }
        }

        public void CallLua(string hookName,APIContext eventCtx = null)
        {
            if(eventCtx == null) eventCtx = CreateCtx();
            string alias = hookName is "PreStart" or "Start" ? "On" + hookName : null;
            InvokeOwnHook(this.HasHook(hookName) ? hookName : alias ?? hookName, eventCtx);
            RunKeywordHooks(hookName, eventCtx, alias);
        }

        private void InvokeOwnHook(string hookName, APIContext ctx)
        {
            using var presentation = Scene?.BeginPresentationEffect(this, hookName);
            this.CallLuaHook<bool>(hookName, env, ctx);
            Scene?.ThrowIfEventAborted();
            presentation?.Complete();
        }

        public void End()
        {
            
        }
        public void Cleanup()
        {
             
        }
        public void Log(string msg)
        {
            if (Owner?.Scene != null) Owner.Scene.Trace(Owner, $"[Card|{DefineId}] {msg}");
            else CombatScene.Log($"[Card|{DisplayName}] {msg}");
        }


        public static CombatCard CreateFromData(Card card)
        {
            var combatCard = new CombatCard();
            combatCard.Id = card.Id;
            combatCard.DefineId = card.DefineId;
            combatCard.DisplayName = card.DisplayName;
            combatCard.ParentCardId = card.ParentCardId;
            combatCard._combatBaseData = card.BaseData.Clone();
            combatCard.Stats = card.Stats.Snapshot();


            combatCard.Ticks.Add("Main",0);
            combatCard.Ticks.Add("CD",0);

            return combatCard;
        }

        /// <summary>
        /// 获取属性最终值（含全场 Modifier OnModifierStat hook 贡献）。
        /// hook 内部读属性应使用 Stats.Get()（裸值）避免递归。
        /// </summary>
        public override float GetStat(string statId)
        {
            float val = base.GetStat(statId);
            // 全场 NpcModifier 的 OnModifierStat hook 贡献
            if (Owner?.Scene != null)
                val += Owner.Scene.CollectModifierStat(this, statId);
            // CardModifier 贡献（含 Flat/Percent/ClampMax/ClampMin/Override）
            val = ApplyCardModifierStat(statId, val);
            return val;
        }


    }
}
