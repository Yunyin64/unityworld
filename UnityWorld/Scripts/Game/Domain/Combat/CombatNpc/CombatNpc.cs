using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗参与者：继承自 Npc，附加战斗专用状态。
    /// 战斗内的所有变化（HP损耗、临时增益等）均记录在此实例上，
    /// 不直接影响大世界的原始 Npc 数据，结算后由 CombatResult 回写。
    ///
    /// </summary>
    public partial class CombatNpc : Npc,ICombatEntity
    {
        /// <summary>所属战斗阵营</summary>
        public CombatTeam Team { get; set; } = CombatTeam.None;

        public  Npc Owner{get;private set;}

        private NpcCardData _loadoutSnapshot = new();
        public CombatBackpack Backpack { get; private set; } = null!;

        /// <summary>所属战斗场景引用</summary>
        public CombatScene Scene { get; set; }

        // ── 战斗状态 ──────────────────────────────────────────

        /// <summary>当前战斗状态（行动中/已阵亡/逃跑/跳过）</summary>
        public CombatantStatus Status { get; set; } = CombatantStatus.Active;

        /// <summary>是否可以继续参与战斗</summary>
        public bool IsActive => Status == CombatantStatus.Active;

        public Dictionary<string, float> Ticks { get ; set ; } = new();

        public static CombatNpc CreateCombatNpc(Npc npc)
        {
            CombatNpc combatNpc = new CombatNpc(npc.Id);
            combatNpc.Stats = npc.Stats.Snapshot();
            combatNpc.Owner = npc;
            combatNpc._loadoutSnapshot = npc.GetCardLoadoutSnapshot();
            combatNpc.Backpack = new CombatBackpack(combatNpc._loadoutSnapshot);
            combatNpc._loadoutSnapshot.AllCards = combatNpc._loadoutSnapshot.AllCards
                .Select(c => (Card)CombatCard.CreateFromData(c)).ToList();
            if (combatNpc.GetSpMax() > combatNpc.Backpack.Capacity)
                throw new InvalidOperationException("SP 上限不能超过主功法背包有效格数。");
            combatNpc.Ticks.Add("Main",0);

            return combatNpc;
        }

        // ── 卡组 ──────────────────────────────────────────────

        /// <summary>唯一待发位置；null 表示空槽。</summary>
        public ContestData PendingSlot { get; private set; }
        public BackpackManaTargetSelector ManaTargetSelector { get; set; } = new();

        private Queue<DamageInfo> damageInfos { get; set; } = new();

        public void Tick()
        {
            if (!IsActive) return;
            Ticks["Main"]++;
            ModifierTick();
            foreach (var card in Field) card.Tick();
        }

        public void Refresh()
        {
            foreach (var card in Field) card.ReLoad();
        }

        public void UseCard()
        {
            throw new NotSupportedException("卡牌只通过背包逐格扫描使用，不再收集 Ready 卡。");
        }
        // ── 目标 ──────────────────────────────────────────────

        /// <summary>
        /// 当前战斗目标（单向引用）。
        /// Target 被击败/逃跑后需由 CombatScene 重新分配。
        /// </summary>
        protected CombatNpc Target { get; set; } = null;


        // ── 构造 ──────────────────────────────────────────────

        public CombatNpc(int id) : base(id)
        {
            
        }

        /// <summary>
        /// 获取属性最终值（含全场 Modifier OnModifierStat hook 贡献）。
        /// 战斗内需要光环/被动效果生效的属性读取使用此方法。
        /// hook 内部读属性应使用 Stats.Get()（裸值）避免递归。
        /// </summary>
        public override float GetStat(string statId)
        {
            float val = base.GetStat(statId);
            if (Scene != null)
                val += Scene.CollectModifierStat(this, statId);
            return val;
        }

        public void DealDamage()
        {
            while (damageInfos.Count > 0)
            {
                ApplyDamage(damageInfos.Dequeue());
            }

        }

        /// <summary>
        /// 应用伤害，自动检测击败条件。
        /// </summary>
        public void ApplyDamage(DamageInfo info)
        {
            // 守方减伤管线
            var ctx = new APIContext { Caster = this, SourceCard = info.SourceCard, Scene = Scene };
            ctx.Set("DamageInfo", info);
            this.ModifyHook("DamageIn", ctx);

            var finalval = info.Damage;
            finalval = ApplyShieldAbsorb(info);
            if(finalval > 0)
            {
                Hp -= finalval;
                Scene.TriggerCombatEvent("OnDamage" , new APIContext { Caster = this, SourceCard = info.SourceCard, Scene = Scene });
            }
            Log($"受到伤害: {finalval}，剩余 HP: {Hp}");

            if (Hp <= 0)
            {
                HandleHpZero(info);
            }
        }

        
        /// <summary>
        /// Shield 吸收：先扣盾再返回剩余伤害。
        /// </summary>
        private int ApplyShieldAbsorb(DamageInfo info)
        {
            var damage = info.Damage;
            if (ShieldValue <= 0 || damage <= 0) return damage;

            int absorbed = Math.Min(ShieldValue, damage);
            ChangeShield(-absorbed);
            int remaining = damage - absorbed;
            return remaining;
        }


        public override string ToString()
        {
            return  string.Format("Npc[{0}{1}]",Id,GetName());   
        }

        public void PreStart()
        {
           // 从 Npc 读取卡组，实例化 Field
           InitDeck();
            foreach (var card in Field)
            {
                card.PreStart();
            }
            // Reserve 中的卡也执行 PreStart（初始化 Lua env），但不执行 Start
            foreach (var card in Reserve)
            {
                card.PreStart();
            }
        }

        public void Start()
        {
            InitData();
            foreach (var card in Field)
            {
                card.Start();
            }
        }


        public void End()
        {
             
        }
        public void Cleanup()
        {
             
        }

        public void Log(string msg)
        {
            if (Scene != null) Scene.Trace(this, msg);
            else CombatScene.Log($"[Npc|{GetName()}]{msg}");
        }



    }
}
