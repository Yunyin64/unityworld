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
        /// <summary>本场当前生命值</summary>
        private int Hp { get; set; }
        /// <summary>本场当前蓝条值</summary>
        private int Mp { get; set; }
        /// <summary>本场体量字段，占用量由场上卡求和</summary>
        private int Sp { get ; set; }

        // 持续轮数含当前轮。零对应本场持续。
        /// <summary>换算效果的最后有效轮次，零轮表示常驻</summary>
        internal int GetLastEffectRound(int rounds) => rounds == 0
            ? int.MaxValue
            : (int)Math.Min(int.MaxValue, (long)Backpack.Round + rounds - 1);

        /// <summary>读取当前生命值</summary>
        public int GetHp(){return (int)Hp;}
        /// <summary>读取战斗生命值上限</summary>
        public int GetCombatHpMax(){return GetHpMax();}
        /// <summary>读取当前蓝条值</summary>
        public int GetMp(){return (int)Mp;}
        /// <summary>读取战斗蓝条上限</summary>
        public int GetCombatMpMax(){return GetMpMax();}
        /// <summary>汇总场上卡牌占用的体量</summary>
        public int GetSp(){
            int sp = 0;
            foreach (var kv in Field) sp += kv.GetSize();
            return  sp;}
        /// <summary>读取角色可承载的体量上限</summary>
        public int GetCombatSpMax(){return GetSpMax();}

        /// <summary>调整额外灵盾。正值受灵盾获得量修正，盾值最低为零。</summary>
        public void ChangeShield(int val) => ChangeShield(val, null, null);

        /// <summary>应用获盾修正后调整护盾，并检查护盾耗尽</summary>
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

        /// <summary>变更护盾，限制在非负整数范围内并记录结果</summary>
        private void ApplyShieldChange(int val)
        {
            var before = ShieldValue;
            ShieldValue = (int)Math.Clamp((long)ShieldValue + val, 0, int.MaxValue);
            if (ShieldValue != before) Scene?.MarkPresentationEffect();
            Log($"护盾值变更：{val:+0.##;-0.##;0}，当前护盾={ShieldValue:0.##}");
        }

        /// <summary>护盾从正数降至零时派发耗尽事件</summary>
        private void DispatchShieldExhausted(int before, int after, string reason, CombatNpc source, CombatCard sourceCard)
        {
            if (before <= 0 || after != 0 || Scene == null) return;
            var ctx = new APIContext { Caster = this, SourceCard = sourceCard, Scene = Scene };
            ctx.Set("Reason", reason);
            ctx.Set("Before", before);
            ctx.Set("SourceNpc", source);
            Scene.TriggerCombatEvent("OnShieldExhausted", ctx);
        }

        /// <summary>为指定元素添加护盾蓄积或降低其触发阈值</summary>
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

        /// <summary>按元素累计实际吸收量并收集已达阈值的事件</summary>
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

        /// <summary>增加一次性护甲减伤量</summary>
        internal void AddArmor(int amount) => AddRoundModifier("Armor", amount, 0);

        /// <summary>增加本轮持续生效的坚甲减伤量</summary>
        internal void AddFortification(int amount) => AddRoundModifier("Fortification", amount, 1);

        /// <summary>扣除适用的护甲和坚甲减伤并将结果下限设为零</summary>
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
        /// <summary>用战斗上限初始化生命和蓝条并记录开场体量</summary>
        public void InitData()
        {
            // 从大世界 Npc 读取 HP/SP/MP
            Hp = GetCombatHpMax();
            Mp = GetCombatMpMax();
            
            Log(string.Format("NPC {0}, HP={1}/{2}, MP={3}/{4}, SP={5}/{6}",GetName(),
            Hp,GetCombatHpMax(),Mp,GetCombatMpMax(),GetSp(),GetCombatSpMax()));
        }

        // ── 招式轮转 ──────────────────────────────────────────
        /// <summary>招式轮转当前指向的卡牌编号</summary>
        public int CurrentZhaoShiCardId { get; set; } = -1;
    }
}
