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

        /// <summary>在卡牌 Contest 回调中创建攻击待发拼点。</summary>
        [APIFunc("Attack", APIType.Contest, "生成攻击待发拼点",Scope.CombatNpc, "Element:String", "PhysicalType:String", "AttackValue:Int")]
        public static APIContext Attack(APIContext ctx)
        {
            var card = ctx.SourceCard;
            // 轮首和治疗也可能没有来源卡，仍要报出错误的拼点调用。
            if (!string.IsNullOrEmpty(ctx.EventName) || card?.Owner == null)
            {
                LogMgr.Instance?.Warn("[Combat] Attack 已跳过。事件={0}，来源卡={1}。请在卡牌自己的 Contest 回调中生成攻击拼点。",
                    ctx.EventName, card?.DefineId ?? "无");
                return ctx;
            }

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
            if (!string.IsNullOrEmpty(ctx.EventName) || card?.Owner == null)
            {
                LogMgr.Instance?.Warn("[Combat] Defend 已跳过。事件={0}，来源卡={1}。请在卡牌自己的 Contest 回调中生成防御拼点。",
                    ctx.EventName, card?.DefineId ?? "无");
                return ctx;
            }

            string defendType = ctx.GetValue("DefendType", "Block");
            int defendValue = ctx.GetValue("DefendValue", 0);

            if (!Enum.TryParse<ContestType>(defendType, true, out var contestType))
                contestType = ContestType.Block;

            card.TryPushToPendingSlot(contestType, ElementType.None, defendValue);
            return ctx;
        }
    }
}
