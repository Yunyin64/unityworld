using UnityWorld.Core;
using UnityWorld.Game.Data;

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

        // 继承的卡牌/装配查询统一指向本场私有数据，避免相同 NPC Id 查询到世界仓库。
        protected override NpcCardData CardData => Backpack.CardData;
        protected override NpcBackpackData BackpackData => Backpack.LayoutData;
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
            combatNpc.Backpack = new CombatBackpack(npc.GetBackpackSnapshot(), npc.GetCombatCardSnapshot());
            combatNpc.Backpack.PresentationOwner = combatNpc;
            // 先从世界卡快照取装备底值，随后所有招式读本场装备。
            combatNpc.SnapshotEquipment(npc);
            combatNpc.CardData.AllCards = combatNpc.CardData.AllCards
                .Select(c => (Card)CombatCard.CreateFromData(c)).ToList();
            if (combatNpc.GetSpMax() > combatNpc.Backpack.Capacity)
                throw new InvalidOperationException("SP 上限不能超过主功法背包有效格数。");
            combatNpc.Ticks.Add("Main",0);

            return combatNpc;
        }

        // ── 卡组 ──────────────────────────────────────────────

        /// <summary>唯一待发位置；null 表示空槽。</summary>
        public ContestData PendingSlot { get; private set; }

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
            using var presentationOrigin = Scene.EnterPresentationOrigin(info.PresentationOrigin);
            // 守方减伤管线
            var ctx = new APIContext { Caster = this, SourceCard = info.SourceCard, Scene = Scene };
            ctx.Set("DamageInfo", info);
            if (info.Damage > 0) ModifyHook("DamageIn", ctx);
            info.Damage = Math.Clamp(info.Damage, 0, int.MaxValue);

            var hpBefore = Hp;
            var shieldBefore = ShieldValue;
            var finalval = ApplyShieldAbsorb(ApplyFixedDamageReduction(info));
            if (finalval > 0) Hp -= finalval;
            int shieldAfter = ShieldValue;
            int absorbed = shieldBefore - shieldAfter;
            var charged = AccumulateShieldCharge(info, absorbed);
            // 先保存真实扣盾和扣血，再派发可能改变两者的玩法事件。
            Scene.RecordPresentation(CombatPresentationKind.DamageResolved, this, info.SourceCard, info.PresentationOrigin,
                damage: new DamagePresentation(info.SourceNpc?.Id, info.SourceCard == null ? null : CombatScene.PresentationCard(info.SourceCard),
                    Id, hpBefore, Hp, Math.Max(0, hpBefore - Hp), absorbed, info.damageType.ToString(), info.ElementType.ToString()));
            DispatchShieldExhausted(shieldBefore, shieldAfter, "Damage", info.SourceNpc, info.SourceCard);
            if (charged != null)
                foreach (var charge in charged) Scene.TriggerCombatEvent("OnShieldChargeReady", charge);
            if (finalval > 0)
            {
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
        private int ApplyShieldAbsorb(int damage)
        {
            if (ShieldValue <= 0 || damage <= 0) return damage;

            int absorbed = Math.Min(ShieldValue, damage);
            ApplyShieldChange(-absorbed);
            return damage - absorbed;
        }

        /// <summary>只在行时钟到点后调用；一行按列即时处理，成功使用才占额度。</summary>
        internal void ScanRow()
        {
            if (!IsActive) return;
            var rowActivationUsed = false;
            for (int x = 0; x < Backpack.Width; x++)
            {
                Backpack.ScannedColumn = x;
                var cell = new BackpackCell(x, Backpack.Row);
                if (Backpack.Mana.ContainsKey(cell))
                {
                    // 选卡与转移统一由背包负责，候选卡仍由本次扫描传入。
                    var selected = Backpack.AssignManaAt(cell, Field);
                   // Log(selected == null ? $"格({x + 1},{cell.Y + 1}) 灵元留格" : $"格({x + 1},{cell.Y + 1}) 灵元进入 {selected.DefineId} 点亮={selected.LitCount}/{selected.GetSize()}");
                }
                if (Backpack.CardAt(cell) is not int id || !Backpack.IsCardCompleteAt(id, cell)) continue;
                Field.Single(c => c.Id == id).ResolveScanEnd(ref rowActivationUsed);
            }
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
            CancelPending("CombatEnded");
            // 战斗可能在两个末格之间结束。让仍在场的持续效果也能完成本场收尾。
            foreach (var card in Field.ToArray()) card.CallLua("OnCombatEnd");
            DealFieldChange();
        }
        /// <summary>清理本场角色和所属卡牌的独立监听。</summary>
        public void Cleanup()
        {
            foreach (var trigger in _triggerRefCounts.Keys)
                EventMgr.Instance?.RemoveEvent(trigger, GetModifierScope(), _modifierTriggerListener);
            _triggerRefCounts.Clear();
            foreach (var card in CardData.AllCards.OfType<CombatCard>()) card.Cleanup();
        }

        public void Log(string msg)
        {
            if (Scene != null) Scene.Trace(this, msg);
            else CombatScene.Log($"[Npc|{GetName()}]{msg}");
        }



    }
}
