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
        // ── 拼点类 ────────────────────────────────────────────

        /// <summary>造成伤害（攻击拼点）。参数：Element(String), PhysicalType(String), AttackValue(Int)</summary>
        [APIFunc("Attack", APIType.Contest, "造成伤害（攻击拼点）",Scope.CombatNpc, "Element:String", "PhysicalType:String", "AttackValue:Int")]
        public static APIContext Attack(APIContext ctx)
        {
            var card = ctx.SourceCard;
            if (card == null || card.Owner == null) return ctx;

            string element = ctx.GetValue("Element", "None");
            string physicalType = ctx.GetValue("PhysicalType", "Zhan");
            int attackValue = ctx.GetValue("AttackValue", 0);

            // 解析拼点类型
            if (!Enum.TryParse<ContestType>(physicalType, true, out var contestType))
                contestType = ContestType.Zhan;

            var Element = ElementType.GetElementType(element);

            card.TryPushToPendingSlot(contestType, Element, attackValue);
            return ctx;
        }

        /// <summary>防御拼点（统一入口）。参数：DefendType(String), DefendValue(Int)</summary>
        [APIFunc("Defend", APIType.Contest, "防御拼点", Scope.CombatNpc, "DefendType:String", "DefendValue:Int")]
        public static APIContext Defend(APIContext ctx)
        {
            var card = ctx.SourceCard;
            if (card == null || card.Owner == null) return ctx;

            string defendType = ctx.GetValue("DefendType", "Block");
            int defendValue = ctx.GetValue("DefendValue", 0);

            if (!Enum.TryParse<ContestType>(defendType, true, out var contestType))
                contestType = ContestType.Block;

            card.TryPushToPendingSlot(contestType, ElementType.None, defendValue);
            return ctx;
        }

        // ── 效果类 ────────────────────────────────────────────

        /// <summary>恢复战斗中HP。参数：HealValue(Int)</summary>
        [APIFunc("Heal", APIType.Action, "恢复战斗中HP", Scope.CombatNpc,"HealValue:Int")]
        public static APIContext Heal( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            int healValue = ctx.GetValue("HealValue", 0);
            caster.ApplyHeal(healValue);
            return ctx;
         }

        /// <summary>自伤。参数：DamageValue(Int)</summary>
        [APIFunc("SelfDamage", APIType.Action, "自伤", Scope.CombatNpc, "DamageValue:Int")]
        public static APIContext SelfDamage( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            int damageValue = ctx.GetValue("DamageValue", 0);
            var dmg = new DamageInfo();
            dmg.Damage = damageValue;
            caster.AddDamage(dmg);
            return ctx;
          }



        // ── 破甲/护盾类 ──────────────────────────────────────

        /// <summary>破甲：消除对方护盾值。参数：BreakValue(Int)</summary>
        [APIFunc("ArmorBreak", APIType.Action, "消除对方护盾值", Scope.CombatNpc, "BreakValue:Int")]
        public static APIContext ArmorBreak( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            int breakValue = ctx.GetValue("BreakValue", 0);
            var target = caster.Target;
            if (target == null) return ctx;

            float actual = Math.Min(target.ShieldValue, breakValue);
            if (actual > 0)
                target.ChangeShield(-actual);

            return ctx;
        }

        // ── Buff 类 ──────────────────────────────────────────

        /// <summary>给目标NPC添加Buff。参数：Target(CombatNpc), BuffId(String), Stacks(Int), Duration(Float,可选)</summary>
        [APIFunc("AddNpcBuff", APIType.Action, "给目标NPC添加Buff", Scope.Npc, "Target:CombatNpc", "BuffId:String", "Stacks:Int", "[Duration:Float]")]
        public static void AddNpcBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            var scene = ctx.Scene;
            if (caster == null || scene == null) return;

            CombatNpc target = ctx.Get<CombatNpc>("Target");
            string buffId = ctx.GetValue("BuffId", "");
            int stacks = ctx.GetValue("Stacks", 1);
            float duration = ctx.GetValue("Duration", -1f);

            if (string.IsNullOrEmpty(buffId)) return;

            // 查找目标 NPC
            target.AddModifier(buffId, stacks,duration);

          }

        // ── 轻量属性修正类 ──────────────────────────────────────

        /// <summary>给施法者添加永久属性修正。参数：StatId(String), Value(Float), ?ModifierType(String), ?SourceId(String)</summary>
        [APIFunc("AddStatBuff", APIType.Action, "给施法者添加永久属性修正", Scope.CombatNpc, "Target:CombatNpc","StatId:String", "Value:Float", "?ModifierType:String", "?SourceId:String")]
        public static APIContext AddStatBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;


            CombatNpc target = ctx.Get<CombatNpc>("Target");
            string statId = ctx.GetValue("StatId", "");
            float value = ctx.GetValue("Value", 0f);
            string modifierType = ctx.GetValue("ModifierType", "Flat");
            string sourceId = ctx.GetValue("SourceId", "");

            
            if (!Enum.TryParse<ModifierType>(modifierType, true, out var type))
            {
                LogMgr.Warn($"[StatBuff] 无法解析 ModifierType: '{modifierType}'，已忽略");
                return ctx;
            }

            if (string.IsNullOrEmpty(statId)) return ctx;

            target.AddStatBuff(statId, value, type, string.IsNullOrEmpty(sourceId) ? null : sourceId);
            return ctx;
        }

        // ── 卡组操作类 ────────────────────────────────────────

        /// <summary>移除目标随机一张伤势卡（按体量筛选）。参数：Target(CombatNpc), Size(Int), ?Exact(Bool)</summary>
        [APIFunc("RemoveRandomWound", APIType.Action, "移除目标随机一张伤势卡", Scope.CombatNpc, "Target:CombatNpc", "Size:Int", "?Exact:Bool")]
        public static APIContext RemoveRandomWound(APIContext ctx)
        {
            var target = ctx.Get<CombatNpc>("Target");
            if (target == null) return ctx;

            int size = ctx.GetValue("Size", 1);
            bool exact = ctx.GetValue("Exact", true);

            var deck = target.GetField();
            var wounds = deck.Where(c => c.HasKeyword("Wound") && (exact ? c.GetSize() == size : c.GetSize() <= size)).ToList();
            if (wounds.Count == 0) return ctx;

            var picked = wounds[target.Scene.Soul.Random(0, wounds.Count)];
            target.RemoveCombatCard(picked);
            LogMgr.Dbg("[RemoveRandomWound] {0} 移除伤势卡: {1} (Size:{2})", target.GetName(), picked.DisplayName, picked.GetSize());

            return ctx;
        }

        /// <summary>位移目标卡牌到指定位置。参数：TargetCard(CombatCard), Position(String: First/Last/Random)</summary>
        [APIFunc("Displace", APIType.Action, "位移目标卡牌", Scope.CombatCard, "TargetCard:CombatCard", "Position:String")]
        public static APIContext Displace(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            var card = ctx.Get<CombatCard>("TargetCard");
            if (card == null) return ctx;

            string posStr = ctx.GetValue("Position", "Random");
            if (!Enum.TryParse<ComabtCardDisplaceType>(posStr, true, out var pos))
                pos = ComabtCardDisplaceType.Random;

            var owner = card.Owner;
            if (owner == null) return ctx;

            owner.DisplaceCombatCard(card, pos);
            LogMgr.Dbg("[Displace] {0} 的卡牌 {1} 位移至 {2}", owner.GetName(), card.DisplayName, pos);

            return ctx;
        }

    }
}
