using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        // ── 战斗 ──────────────────────────────────
        /// <summary>
        /// 战斗内当前数值（与大世界 StatBlock 独立）
        /// </summary>
        /// 
        public  int ShieldValue { get; private  set; }
        private int Hp { get; set; }
        private int Mp { get; set; }
        private int Sp { get ; set; }

        // 持续轮数含当前轮。零对应本场持续。
        internal int GetLastEffectRound(int rounds) => rounds == 0
            ? int.MaxValue
            : (int)Math.Min(int.MaxValue, (long)Backpack.Round + rounds - 1);

        public int GetHp(){return (int)Hp;}
        public int GetCombatHpMax(){return GetHpMax();}
        public int GetMp(){return (int)Mp;}
        public int GetCombatMpMax(){return GetMpMax();}
        public int GetSp(){
            int sp = 0;
            foreach (var kv in Field) sp += kv.GetSize();
            return  sp;}
        public int GetCombatSpMax(){return GetSpMax();}

        /// <summary>调整额外灵盾。正值受灵盾获得量修正，盾值最低为零。</summary>
        public void ChangeShield(int val) => ChangeShield(val, null, null);

        internal void ChangeShield(int val, CombatNpc source, CombatCard sourceCard)
        {
            // 获盾修正只作用于仍能增加的盾值。扣盾和盾值已达上限时保留单次状态。
            if (val > 0 && ShieldValue < int.MaxValue)
            {
                var ctx = new APIContext { Caster = source, SourceCard = sourceCard, Scene = Scene, NpcTargets = [this] };
                val = ModifyAmount("ShieldGain", val, ctx);
            }

            int before = ShieldValue;
            ApplyShieldChange(val);
            DispatchShieldExhausted(before, ShieldValue, "Effect", source, sourceCard);
        }

        private void ApplyShieldChange(int val)
        {
            var before = ShieldValue;
            ShieldValue = (int)Math.Clamp((long)ShieldValue + val, 0, int.MaxValue);
            if (ShieldValue != before) Scene?.MarkPresentationEffect();
            Log($"护盾值变更：{val:+0.##;-0.##;0}，当前护盾={ShieldValue:0.##}");
        }

        private void DispatchShieldExhausted(int before, int after, string reason, CombatNpc source, CombatCard sourceCard)
        {
            if (before <= 0 || after != 0 || Scene == null) return;
            var ctx = new APIContext { Caster = this, SourceCard = sourceCard, Scene = Scene };
            ctx.Set("Reason", reason);
            ctx.Set("Before", before);
            ctx.Set("SourceNpc", source);
            Scene.TriggerCombatEvent("OnShieldExhausted", ctx);
        }

        internal void AddShieldCharge(string element, int threshold)
        {
            string id = $"ShieldCharge_{element}";
            var existing = GetAllModifiers().FirstOrDefault(modifier => modifier.DefineId == id);
            if (existing == null) AddModifier(id, threshold);
            else if (threshold < existing.CurrentStack)
            {
                existing.CurrentStack = threshold;
                Scene?.MarkPresentationEffect();
            }
        }

        private List<APIContext> AccumulateShieldCharge(DamageInfo damage, int absorbed)
        {
            if (absorbed <= 0) return null;
            var element = damage.ElementType.Kind.ToString();
            List<APIContext> ready = null;
            foreach (var modifier in GetAllModifiers())
            {
                if (modifier.DefineId != "ShieldCharge_None" && modifier.DefineId != $"ShieldCharge_{element}") continue;
                var progress = modifier.ShieldChargeProgress;
                if (progress.Round != Backpack.Round) progress = (Backpack.Round, 0L, false);
                if (progress.Triggered) continue;
                progress.Absorbed += absorbed;
                progress.Triggered = progress.Absorbed >= modifier.CurrentStack;
                modifier.ShieldChargeProgress = progress;
                if (!progress.Triggered) continue;

                var ctx = new APIContext { Caster = this, SourceCard = damage.SourceCard, SourceBuff = modifier, Scene = Scene };
                ctx.Set("Round", progress.Round);
                ctx.Set("Absorbed", progress.Absorbed);
                ctx.Set("Threshold", modifier.CurrentStack);
                ctx.Set("DamageElement", element);
                ctx.Set("ChargeBuffId", modifier.DefineId);
                (ready ??= new()).Add(ctx);
            }
            return ready;
        }

        internal void AddArmor(int amount) => AddRoundModifier("Armor", amount, 0);

        internal void AddFortification(int amount) => AddRoundModifier("Fortification", amount, 1);

        private int ApplyFixedDamageReduction(DamageInfo info)
        {
            if (info.Damage <= 0) return 0;

            long damage = info.Damage;
            // 护甲只消耗在一次来自不同阵营的伤害上，坚甲在持有者本轮的每次伤害上生效。
            if (info.SourceNpc != null && info.SourceNpc.Team != Team)
                damage -= ConsumeBattleModifier("Armor");

            damage -= ReadBattleModifierAmount("Fortification");
            return (int)Math.Max(0, damage);
        }
        public void InitData()
        {
            // 从大世界 Npc 读取 HP/SP/MP
            Hp = GetCombatHpMax();
            Mp = GetCombatMpMax();
            
            Log(string.Format("NPC {0}, HP={1}/{2}, MP={3}/{4}, SP={5}/{6}",GetName(),
            Hp,GetCombatHpMax(),Mp,GetCombatMpMax(),GetSp(),GetCombatSpMax()));
        }

        // ── 招式轮转 ──────────────────────────────────────────
        public int CurrentZhaoShiCardId { get; set; } = -1;
    }
}
