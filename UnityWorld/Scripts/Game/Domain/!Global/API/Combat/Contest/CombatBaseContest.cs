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
        }
}