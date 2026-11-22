using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    //这个文件只允许做，CombatCard的时序、核心方法。不允许新加变量和功能，尤其是私有的。
    /// <summary>战斗卡的创建、发动、结算与离场时序。</summary>
    public partial class CombatCard : Card, ICombatEntity, ILuaBindable
    {
        private CardBaseData _combatBaseData = new();
        /// <summary>战斗私有快照，Lua 派生关键词和临时修改不会污染世界卡。</summary>
        public override CardBaseData BaseData => _combatBaseData;

        /// <summary>持有这张战斗卡的角色。</summary>
        public CombatNpc Owner;

        /// <summary>持有者所在的战斗场景。</summary>
        public CombatScene Scene
        {
            get => Owner?.Scene;
            set => Owner.Scene = value;
        }

        /// <summary>本卡当前的战斗阶段。</summary>
        public CombatCardPhase Phase { get; private set; } = CombatCardPhase.Waiting;

        /// <summary>读取当前阶段，保留旧调用签名。</summary>
        public CombatCardPhase GetPhase(CombatCardPhase phase) => Phase;

        /// <summary>检查本卡所处的阶段。</summary>
        public bool CheckPhase(CombatCardPhase phase) => Phase == phase;
        /// <summary>更新本卡所处的阶段。</summary>
        public void SetPhase(CombatCardPhase phase)
        {
            if (!Enum.IsDefined(phase)) { Log($"未知卡牌阶段 {(int)phase}"); return; }
            if (Phase == phase) return;
            var previous = Phase;
            Phase = phase;

            CallMainKeyword("OnPhaseChanged", CreateCtx(), previous);
        }

        /// <summary>
        /// 设置卡牌 Phase（供 Lua 调用）。
        /// 将字符串解析为 CombatCardPhase 枚举，解析失败时输出错误日志且不改变 Phase。
        /// </summary>
        public void SetPhase(string phaseName)
        {
            if (Enum.TryParse<CombatCardPhase>(phaseName, out var phase)) SetPhase(phase);
            else Log($"SetPhase 失败：无法解析 '{phaseName}' 为 CombatCardPhase");
        }

        /// <summary>本场卡牌按名称保存的计时值。</summary>
        public Dictionary<string, float> Ticks { get; set; } = new();

        /// <summary>本场成功发动的总次数。</summary>
        public int UseCount { get; private set; }
        /// <summary>本场执行 Apply 的总次数。</summary>
        public int ApplyCount { get; private set; }

        /// <summary>从世界卡复制本场使用的数据和属性。</summary>
        public static CombatCard CreateFromData(Card card)
        {
            var combatCard = new CombatCard();
            combatCard.Id = card.Id;
            combatCard.DefineId = card.DefineId;
            combatCard.DisplayName = card.DisplayName;
            combatCard.ParentCardId = card.ParentCardId;
            combatCard._combatBaseData = card.BaseData.Clone();
            combatCard.InitialConsumeStack = combatCard.ConsumeStack;
            combatCard.Stats = card.Stats.Snapshot();
            combatCard._currentAmount = combatCard.GetAmountMax();

            combatCard.Ticks.Add("Main", 0);
            combatCard.Ticks.Add("CD", 0);

            return combatCard;
        }

        /// <summary>初始化本卡脚本并执行战前回调。</summary>
        public void PreStart()
        {
            InitializeLuaCards();
            CallMainKeyword("OnCardPreStart", CreateCtx());
        }

        /// <summary>开始在场生命周期，激活常驻被动。</summary>
        public void Start()
        {
            Ticks["Main"] = 0;
            Ticks["CD"] = 0;
            CallMainKeyword("OnCardStart", CreateCtx());
        }

        /// <summary>推进本卡的脚本、关键词与修正器。</summary>
        public void Tick()
        {
            ReLoad();
            CallMainKeyword("OnCardTick", CreateCtx());
            CardModifierTick();
            HookUse.Clear();
            Ticks["Main"]++;
        }

        /// <summary>将已完成结算的卡恢复为等待状态。</summary>
        public void ReLoad()
        {
            if (Phase == CombatCardPhase.Finished) SetPhase(ChargePhase);
        }

        private void Use(IReadOnlyList<ElementType> paidMana)
        {
            // 脚本可在广播真正分派前移动卡。使用事件的方位始终取提交发动时的布局。
            var useContext = CreateCtx();
            useContext.EventPlacements = Owner.Backpack.Placements.ToDictionary(pair => pair.Key, pair => pair.Value);
            DispatchManaUsed(paidMana);
            CallMainKeyword("OnScanEnd", useContext, true);
            Scene.TriggerCombatEvent("OnUse", useContext);
            if (Phase == CombatCardPhase.InPending) return;
            if (AwaitingApply) Apply();
        }

        /// <summary>结算一次卡牌效果，入槽的卡在对拼或直击后从这里继续。</summary>
        public void Apply()
        {
            if (Phase is not (CombatCardPhase.Active or CombatCardPhase.InPending)) return;
            if (CallMainKeyword("ApplyEndPhase") is not CombatCardPhase endPhase) return;

            using var activation = Scene.BeginActivation(this);
            using var presentation = Scene.BeginPresentationEffect(this, "Apply", active: true);
            SetPhase(CombatCardPhase.Applying);
            ApplyCount++;
            CallLua("Apply");
            Scene.TriggerCombatEvent("OnApply", CreateCtx());
            if (Phase == CombatCardPhase.Applying)
                SetPhase(endPhase);
            presentation?.Complete();
        }

        /// <summary>退出战斗场地，结束被动状态和监听。</summary>
        public void End()
        {
            if (Phase != CombatCardPhase.InPending) SetPhase(CombatCardPhase.Waiting);
            Cleanup();
        }
    }
}
