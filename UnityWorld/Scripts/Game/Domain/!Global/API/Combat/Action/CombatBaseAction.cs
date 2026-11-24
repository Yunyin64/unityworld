using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 战斗域的 API 函数实现集合。
    /// 每个方法以 [APIFunc] 标记，由 APIMgr 反射扫描自动注册。
    /// 从  ContextBase.Env 中取 "Caster"(CombatNpc) 和 "Target"(CombatNpc) 作为操作主体。
    /// </summary>
    public  static partial class CombatBaseFunc
    {
        /// <summary>补充指定卡的弹药，返回实际补充的总发数。</summary>
        [APIFunc("RefillAmmo", APIType.Action, "补充目标卡的弹药", Scope.CombatCard,
            "Domain:String", "Amount:Int")]
        public static APIContext RefillAmmo(APIContext ctx)
        {
            var count = ctx.GetValue("Amount", 0);
            long restored = 0;
            foreach (var card in APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx).Distinct())
                restored += card.RefillAmount(count);
            ctx.Set("RestoredAmmo", restored);
            ctx.Set("Ret", restored > 0);
            return ctx;
        }

        /// <summary>请求指定卡离场。实际执行时，场地将本次原因交给 OnLeave。</summary>
        [APIFunc("RemoveCard", APIType.Action, "使指定战斗卡离场", Scope.CombatCard,
            "Domain:String", "Reason:String")]
        public static APIContext RemoveCard(APIContext ctx)
        {
            var reason = ctx.GetStringValue("Reason", "Effect");
            foreach (var card in APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx).Distinct())
            {
                var owner = card.Owner;
                if (owner != null && card.Id != owner.Backpack.MainCardId &&
                    (owner.GetField().Contains(card) || owner.GetReserve().Contains(card)))
                    owner.QueueCardRemoval(card, reason);
            }
            return ctx;
        }

        /// <summary>选择目标并尝试沿指定方向滑移，统计成功移动的卡牌。</summary>
        [APIFunc("SlideCard", APIType.Action, "将目标卡沿四向滑动至阻碍前", Scope.CombatCard,
            "Domain:String", "Direction:String")]
        public static APIContext SlideCard(APIContext ctx)
        {
            var direction = CombatBackpack.ReadSlideDirection(ctx.GetObject("Direction"));
            var domain = ctx.GetStringValue("Domain");
            var randomTarget = domain == "TargetRandom";
            var candidates = APIMgr.Instance.GetTargetCard(randomTarget ? "TargetAll" : domain, ctx)
                .Distinct().ToArray();
            int movedCount = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (randomTarget)
                {
                    // 随机遍历候选，直到一张卡确实可以在该方向滑移。
                    var index = ctx.Caster.Scene.Soul.Random(i, candidates.Length);
                    (candidates[i], candidates[index]) = (candidates[index], candidates[i]);
                }
                var card = candidates[i];
                if (card.Owner == null || !card.Owner.SlideCombatCard(card, direction, ctx.Caster, ctx.SourceCard)) continue;
                movedCount++;
                if (randomTarget) break;
            }
            ctx.Set("MovedCount", movedCount);
            ctx.Set("Ret", movedCount > 0);
            return ctx;
        }

        /// <summary>修改目标装备或目标招式所属装备的攻、防、速。</summary>
        [APIFunc("ChangeEquipmentStat", APIType.Action, "修改目标装备的攻防速", Scope.CombatCard,
            "Domain:String", "Stat:String", "Delta:Float", "Rounds:Int")]
        public static APIContext ChangeEquipmentStat(APIContext ctx)
        {
            var stat = ctx.GetStringValue("Stat");
            var delta = ctx.GetValue("Delta", 0f);
            var rounds = ctx.GetValue("Rounds", 0);
            var ids = new HashSet<(int OwnerId, int EquipmentId)>();
            foreach (var target in APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx))
            {
                if (target.Owner == null) continue;
                var id = target.BaseData.MainKeyword == CardMainKeyword.ZhaoShi ? target.ParentCardId : target.Id;
                if (ids.Add((target.Owner.Id, id))) target.Owner.ChangeEquipmentStat(target, stat, delta, rounds);
            }
            return ctx;
        }

        /// <summary>给予额外灵盾，减伤后的伤害优先扣盾。</summary>
        [APIFunc("GainShield", APIType.Action, "获得灵盾", Scope.CombatNpc, "Domain:String", "Amount:Int")]
        public static APIContext GainShield(APIContext ctx)
        {
            var amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.ChangeShield(amount, ctx.Caster, ctx.SourceCard);
            return ctx;
        }

        /// <summary>获得按实际灵盾吸收量累计的蓄力效果。</summary>
        [APIFunc("AddShieldCharge", APIType.Action, "以盾蓄力，按指定五行累计吸收", Scope.CombatNpc,
            "Domain:String", "Element:String", "Amount:Int")]
        public static APIContext AddShieldCharge(APIContext ctx)
        {
            string element = ctx.GetStringValue("Element", "None");
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0 || element is not ("None" or "Jin" or "Mu" or "Shui" or "Huo" or "Tu")) return ctx;
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.AddShieldCharge(element, amount);
            return ctx;
        }

        /// <summary>给予一次性护甲，仅下一次来自敌方的正向伤害消耗。</summary>
        [APIFunc("AddArmor", APIType.Action, "获得护甲，减免下一次敌方伤害", Scope.CombatNpc, "Domain:String", "Amount:Int")]
        public static APIContext AddArmor(APIContext ctx)
        {
            var amount = ctx.GetValue("Amount", 0);
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.AddArmor(amount);
            return ctx;
        }

        /// <summary>给予持续到持有者本轮结束的固定减伤。</summary>
        [APIFunc("AddFortification", APIType.Action, "获得坚甲，减免本轮受到的每次伤害", Scope.CombatNpc, "Domain:String", "Amount:Int")]
        public static APIContext AddFortification(APIContext ctx)
        {
            var amount = ctx.GetValue("Amount", 0);
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.AddFortification(amount);
            return ctx;
        }

        /// <summary>施加卸力 Buff，由 Buff 在下次敌方攻击配对时结算。</summary>
        [APIFunc("AddDeflection", APIType.Action, "获得卸力，减少下次配对时的敌方攻击点数", Scope.CombatNpc, "Domain:String", "Amount:Int")]
        public static APIContext AddDeflection(APIContext ctx)
        {
            var amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.AddModifier("Deflection", amount);
            return ctx;
        }


        // ── 效果类 ────────────────────────────────────────────

        /// <summary>对 Domain 指定的角色施加 HealValue 点治疗，逐个结算施术者和目标的治疗修正。</summary>
        [APIFunc("Heal", APIType.Action, "恢复战斗中HP", Scope.CombatNpc, "Domain:String", "HealValue:Int")]
        public static APIContext Heal(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int healValue = ctx.GetValue("HealValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                npc.ApplyHeal(healValue, caster);
            }
            return ctx;
        }

        /// <summary>排入直接伤害。对敌方造成伤害时记录来源角色及来源卡。</summary>
        [APIFunc("SelfDamage", APIType.Action, "造成直接伤害", Scope.CombatNpc, "Domain:String", "DamageValue:Int")]
        public static APIContext SelfDamage(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int damageValue = ctx.GetValue("DamageValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                var dmg = new DamageInfo { Damage = damageValue, TargetNpc = npc };
                // 敌方护甲依赖来源阵营。自伤仍沿用无来源的伤害路径。
                if (npc.Team != caster.Team)
                {
                    dmg.SourceNpc = caster;
                    if (ReferenceEquals(ctx.SourceCard?.Owner, caster)) dmg.SourceCard = ctx.SourceCard;
                }
                npc.AddDamage(dmg);
            }
            return ctx;
        }



        // ── 破甲/护盾类 ──────────────────────────────────────

        /// <summary>破甲：消除目标护盾值。参数：Domain(String), BreakValue(Int)</summary>
        [APIFunc("ArmorBreak", APIType.Action, "消除目标护盾值", Scope.CombatNpc, "Domain:String", "BreakValue:Int")]
        public static APIContext ArmorBreak(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int breakValue = ctx.GetValue("BreakValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                int actual = Math.Min(npc.ShieldValue, breakValue);
                if (actual > 0)
                    npc.ChangeShield(-actual, ctx.Caster, ctx.SourceCard);
            }
            return ctx;
        }

        // ── Buff 类 ──────────────────────────────────────────

        /// <summary>给目标NPC添加Buff。参数：Domain(String), BuffId(String), Stacks(Int), Duration(Float)</summary>
        [APIFunc("AddNpcBuff", APIType.Action, "给目标NPC添加Buff", Scope.CombatNpc, "Domain:String", "BuffId:String", "Stacks:Int", "Duration:Float")]
        public static void AddNpcBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string buffId = ctx.GetValue("BuffId", "");
            int stacks = ctx.GetValue("Stacks", 1);
            float duration = ctx.GetValue("Duration", -1f);

            if (string.IsNullOrEmpty(buffId)) return;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.AddModifier(buffId, stacks, duration);
            }
        }

        // ── 轻量属性修正类 ──────────────────────────────────────

        /// <summary>给目标添加永久属性修正。参数：Domain(String), StatId(String), Value(Float), ModifierType(String), SourceId(String)</summary>
        [APIFunc("AddStatBuff", APIType.Action, "给目标添加永久属性修正", Scope.CombatNpc, "Domain:String", "StatId:String", "Value:Float", "ModifierType:String", "SourceId:String")]
        public static APIContext AddStatBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string statId = ctx.GetValue("StatId", "");
            LegacyCombatCompatibility.RequireSupportedStat(statId);
            float value = ctx.GetValue("Value", 0f);
            string modifierType = ctx.GetValue("ModifierType", "Flat");
            string sourceId = ctx.GetValue("SourceId", "");

            if (!Enum.TryParse<ModifierType>(modifierType, true, out var type))
            {
                LogMgr.Instance.Warn($"[StatBuff] 无法解析 ModifierType: '{modifierType}'，已忽略");
                return ctx;
            }

            if (string.IsNullOrEmpty(statId)) return ctx;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.AddStatBuff(statId, value, type, string.IsNullOrEmpty(sourceId) ? null : sourceId);
            }
            return ctx;
        }

        // ── 卡组操作类 ────────────────────────────────────────

        /// <summary>移除目标随机一张伤势卡（按体量筛选）。参数：Domain(String), Size(Int), Exact(Bool)</summary>
        [APIFunc("RemoveRandomWound", APIType.Action, "移除目标随机一张伤势卡", Scope.CombatNpc, "Domain:String", "Size:Int", "Exact:Bool")]
        public static APIContext RemoveRandomWound(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int size = ctx.GetValue("Size", 1);
            bool exact = ctx.GetValue("Exact", true);

            foreach (var npc in ctx.NpcTargets)
            {
                var deck = npc.GetField();
                var wounds = deck.Where(c => c.HasKeyword("Wound") && (exact ? c.GetSize() == size : c.GetSize() <= size)).ToList();
                if (wounds.Count == 0) continue;

                var picked = wounds[npc.Scene.Soul.Random(0, wounds.Count)];
                npc.HealWound(picked);
                LogMgr.Instance.Dbg("[RemoveRandomWound] {0} 移除伤势卡: {1} (Size:{2})", npc.GetName(), picked.DisplayName, picked.GetSize());
            }

            return ctx;
        }

        /// <summary>位移目标卡牌到指定位置的预留入口，当前拒绝执行。参数：Domain(String), Position(String: First/Last/Random)</summary>
        [APIFunc("Displace", APIType.Action, "位移目标卡牌", Scope.CombatCard, "Domain:String", "Position:String")]
        public static APIContext Displace(APIContext ctx)
        {
            LegacyCombatCompatibility.RejectPending("Displace");
            return ctx;
        }

    }
}
