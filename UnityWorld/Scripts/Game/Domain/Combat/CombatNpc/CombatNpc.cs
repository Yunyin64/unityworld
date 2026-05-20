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
            combatNpc.Ticks.Add("Main",0);
            combatNpc.Ticks.Add("ManaDraw",0);
            combatNpc.Ticks.Add("Straight",0);

            return combatNpc;
        }

        // ── 灵元池 ──────────────────────────────────────────────

        /// <summary>
        /// 战斗灵元池（元素名 → 数量），由 CombatManaHandler 在回合开始时转化填充。
        /// </summary>
        public Dictionary<ElementType, int> ManaPool { get; set; } = new();

        // ── 卡组 ──────────────────────────────────────────────

        public Queue<ContestData> PendingSlot { get; set; } = new();

        private Queue<DamageInfo> damageInfos { get; set; } = new();

        public void Tick()
        {
            CDTick();
            ModifierTick();
            foreach (var card in CardDeck)
            {
                card.Tick();
            }
        }

        private void CDTick()
        {
            Ticks["Main"]++;
            Ticks["ManaDraw"]++;
            Ticks["Straight"]++;
        }

        public void UseCard()
        {
            var readyCards = GetCards( CombatCardPhase.Ready);   
            foreach (var card in readyCards)
            {
                card.OnUse();
            }
        }
        // ── 目标 ──────────────────────────────────────────────

        /// <summary>
        /// 当前战斗目标（单向引用）。
        /// Target 被击败/逃跑后需由 CombatScene 重新分配。
        /// </summary>
        public CombatNpc Target { get; set; } = null;


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
            var finalval = info.Damage;
            finalval = ApplyShieldAbsorb(info);
            if(finalval > 0)
            {
                Hp -= finalval;
                Log($"受到伤害: {finalval}，剩余 HP: {Hp}");
                // 广播受击事件，供 Modifier 触发器响应
                Scene.TriggerCombatEvent("OnDamage" , new APIContext { Caster = this, SourceCard = info.SourceCard, Scene = Scene }, this);
            }

            if (Hp <= 0)
            {
                HandleHpZero(info);
            }
        }

        
        /// <summary>
        /// Shield 吸收：先扣盾再返回剩余伤害。
        /// </summary>
        private float ApplyShieldAbsorb(DamageInfo info)
        {
            var damage = info.Damage;
            if (ShieldValue <= 0 || damage <= 0) return damage;

            float absorbed = Math.Min(ShieldValue, damage);
            ChangeShield(-absorbed);
            float remaining = damage - absorbed;
            return remaining;
        }


        public override string ToString()
        {
            return  string.Format("Npc[{0}{1}]",Id,GetName());   
        }

        public void PreStart()
        {
           // 从 Npc 读取卡组，实例化 CardDeck
           InitDeck();
            foreach (var card in CardDeck)
            {
                card.PreStart();
            }
            // 3. ManaPool 初始化为空（由 DoManaConvert 统一按亲和权重转化）
            ManaPool = new Dictionary<ElementType, int>();
        }

        public void Start()
        {
            foreach (var card in CardDeck)
            {
                card.Start();
            }
            InitData();
        }


        public void End()
        {
             
        }
        public void Cleanup()
        {
             
        }

        public void Log(string msg)
        {
            CombatScene.Log($"[Npc|{GetName()}]{msg}");
        }



    }
}