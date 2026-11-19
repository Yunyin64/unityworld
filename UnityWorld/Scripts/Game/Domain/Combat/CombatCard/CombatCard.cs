
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
        private bool _buildingContest;
        private bool _awaitingApply;
        private bool _passiveActive;
        private int _lastFormUseRound;
        private int _formUsesInRound;
        internal long? PresentationActionId { get; private set; }
        internal long? PresentationContestId { get; set; }
        /// <summary>被动在场生效。冻结影响下一次发动，当前激活仍可响应事件。</summary>
        public bool IsPassiveActive => _passiveActive && Owner?.IsActive == true;
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
            if (BaseData.MainKeyword != CardMainKeyword.FaBao) CallLua("PreStart");
        }

        public void Start()
        {
            Ticks["Main"] = 0;
            Ticks["CD"] = 0;
            var main = BaseData.MainKeyword;
            SetPhase(main == CardMainKeyword.Passive ? CombatCardPhase.Passive : CombatCardPhase.Waiting);
            if (main == CardMainKeyword.FaBao) return;
            if (main == CardMainKeyword.Passive) SetPassiveActive(true);
            CallLua("Start");
            // 旧被动卡的显式 Apply 是开场效果。空被动仍只占格。
            if (main == CardMainKeyword.Passive && this.HasHook("Apply")) Apply();
        }
        public void ReLoad()
        {
            if(CheckPhase( CombatCardPhase.Finished)) SetPhase(CombatCardPhase.Waiting);
        }

        public void Tick()
        {
            ReLoad();
            var ctx = CreateCtx();
            var main = BaseData.MainKeyword;
            if (main is CardMainKeyword.FaShu or CardMainKeyword.ZhaoShi) InvokeOwnHook("Tick", ctx);
            // Tick/OnTick 是同一生命周期的两种历史拼写，只选一个。
            if (main != CardMainKeyword.FaBao || IsPassiveActive) RunKeywordHooks("OnTick", ctx, "Tick");
            if (IsPassiveActive) InvokeOwnHook("OnPassiveTick", ctx);
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



        internal void ResolveScanEnd(ref bool rowActivationUsed)
        {
            var main = BaseData.MainKeyword;
            var wasFrozen = IsFrozen;
            if (!wasFrozen && !rowActivationUsed && CanUseAtScanEnd(main))
            {
                CommitScanUse(main, ref rowActivationUsed);
                return;
            }

            // 即使本行额度已用，真实扫到末格也会结束未能续期的法宝被动。
            if (main == CardMainKeyword.FaBao) SetPassiveActive(false);
            if (wasFrozen) ConsumeFreezeAtScanEnd();
        }

        /// <summary>兼容旧调用。事件可驱动激活中的被动，主动发动只由扫描授权。</summary>
        public bool TryUseFromEvent(APIContext eventCtx) => false;

        private bool CanUseAtScanEnd(CardMainKeyword main)
        {
            if (!BaseData.HasScanOpportunity || Scene.IsFinished || !Owner.IsActive || _usingActiveOpportunity ||
                _awaitingApply || CheckPhase(CombatCardPhase.InPending) || !HasActivationResources()) return false;
            if (main == CardMainKeyword.FaBao) return true;
            if (ProducesContest && Owner.PendingSlot != null) return false;
            return main != CardMainKeyword.ZhaoShi || (Owner.HasCombatFormSource(this) &&
                (_lastFormUseRound == Owner.Backpack.Round ? _formUsesInRound : 0) < BaseData.MaxUsesPerRound);
        }

        private bool ProducesContest => BaseData.ProducesContest ?? this.HasHook("Contest");

        private void CommitScanUse(CardMainKeyword main, ref bool rowActivationUsed)
        {
            Scene.ThrowIfEventAborted();
            var resourcesBefore = Scene.PresentationRecordingEnabled
                ? CombatScene.PresentationCopy(AssignedElements.Select(e => e.ToString())) : null;
            // 开始 Lua、广播事件或写出表现记录前，一次性提交灵元、行额度及招式次数。
            rowActivationUsed = true;
            ClearAssignedMana();
            if (main == CardMainKeyword.ZhaoShi)
            {
                if (_lastFormUseRound != Owner.Backpack.Round)
                {
                    _lastFormUseRound = Owner.Backpack.Round;
                    _formUsesInRound = 0;
                }
                _formUsesInRound++;
            }
            _usingActiveOpportunity = true;
            _awaitingApply = main != CardMainKeyword.FaBao;
            UseCount++;
            var parentOrigin = Scene.CapturePresentationOrigin();
            PresentationActionId = Scene.NextPresentationIdentity();
            PresentationContestId = null;
            using var presentationOrigin = Scene.EnterPresentationOrigin(new PresentationOrigin(PresentationActionId,
                parentOrigin?.ActionId, parentOrigin?.ContestId, null));
            Log("末格扫描提交发动");
            if (resourcesBefore != null)
                Scene.RecordPresentation(CombatPresentationKind.CardUsed, Owner, this,
                    origin: new PresentationOrigin(PresentationActionId, parentOrigin?.ActionId, parentOrigin?.ContestId, null),
                    used: new CardUsedPresentation(resourcesBefore, Array.AsReadOnly(Array.Empty<string>())));
            Scene.BeginActivation(this);
            try { Use(); }
            finally
            {
                try { Scene.EndActivation(); }
                finally { _usingActiveOpportunity = false; }
            }
        }

        private void Use()
        {
            if (!_usingActiveOpportunity) throw new InvalidOperationException("Use 只能由末格扫描授权。");
            if (BaseData.MainKeyword == CardMainKeyword.FaBao)
            {
                SetPassiveActive(true);
                Scene.TriggerCombatEvent("OnUse", CreateCtx());
                return;
            }
            if (ProducesContest) Contest();
            Scene.TriggerCombatEvent("OnUse", CreateCtx());
            if (!CheckPhase(CombatCardPhase.InPending)) Apply();
        }

        private void Contest()
        {
            _buildingContest = true;
            try { CallLua("Contest"); }
            finally { _buildingContest = false; }
        }

        public void Apply()
        {
            if (BaseData.HasActiveAbility && !_awaitingApply) return;
            var ownsScope = !_usingActiveOpportunity;
            if (ownsScope)
            {
                _usingActiveOpportunity = true;
                Scene.BeginActivation(this);
            }
            try
            {
                using var presentation = Scene.BeginPresentationEffect(this, "Apply", active: true);
                _awaitingApply = false;
                ApplyCount++;
                CallLua("Apply");
                Scene.TriggerCombatEvent("OnApply", CreateCtx());
                SetPhase(BaseData.HasActiveAbility ? CombatCardPhase.Finished : CombatCardPhase.Passive);
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
            Scene = Owner?.Scene
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

        internal void DispatchCombatEvent(string hookName, APIContext ctx)
        {
            if ((BaseData.MainKeyword is CardMainKeyword.FaBao or CardMainKeyword.Passive) && !IsPassiveActive) return;
            CallLua(hookName, ctx);
        }

        private void SetPassiveActive(bool active)
        {
            if (_passiveActive == active) return;
            _passiveActive = active;
            CallLua(active ? "OnPassiveActivated" : "OnPassiveDeactivated");
        }

        internal void ModifyPairedContest(ContestData contest)
        {
            if (!IsPassiveActive || !this.HasHook("OnBeforePairedContest")) return;
            var ctx = new APIContext { Caster = contest.OwnerNpc, Scene = Scene, SourceCard = contest.SourceCard };
            ctx.Set("ContestData", contest);
            var previous = contest.ContestValue;
            using var presentation = Scene.BeginPresentationEffect(this, "OnBeforePairedContest");
            this.CallLuaHook<bool>("OnBeforePairedContest", env, ctx);
            Scene.ThrowIfEventAborted();
            if (contest.ContestValue != previous) Scene.MarkPresentationEffect();
            presentation?.Complete();
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
            SetPassiveActive(false);
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
