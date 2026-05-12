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

        /// <summary>盾牌防御（赢了叠盾）。参数：ShieldValue(Int)</summary>
        [APIFunc("Shield", APIType.Contest, "盾牌防御（赢了叠盾）", Scope.CombatNpc,"ShieldValue:Int")]
        public static APIContext Shield(APIContext ctx)
        {
            var card = ctx.SourceCard;
            if (card == null || card.Owner == null) return ctx;

            int shieldValue = ctx.GetValue("ShieldValue", 0);

            card.TryPushToPendingSlot(ContestType.Shield, ElementType.None, shieldValue);
            return ctx;
        }

        /// <summary>格挡防御（赢了差值消失）。参数：BlockValue(Int)</summary>
        [APIFunc("Block", APIType.Contest, "格挡防御（赢了差值消失）",Scope.CombatNpc, "BlockValue:Int")]
        public static APIContext Block(APIContext ctx)
        {
            var card = ctx.SourceCard;
            if (card == null || card.Owner == null) return ctx;

            int blockValue = ctx.GetValue("BlockValue", 0);

            card.TryPushToPendingSlot(ContestType.Block, ElementType.None, blockValue);
            return ctx;
        }

        // ── 效果类 ────────────────────────────────────────────

        /// <summary>恢复战斗中HP。参数：HealValue(Int)</summary>
        [APIFunc("Heal", APIType.Action, "恢复战斗中HP", Scope.CombatNpc,"HealValue:Int")]
        public static APIContext Heal( APIContext ctx)
        {
            var caster = ctx.Get<CombatNpc>("Caster");
            if (caster == null) return ctx;
            int healValue = ctx.GetValue("HealValue", 0);
            caster.ApplyHeal(healValue);
            return ctx;
         }

        /// <summary>自伤。参数：DamageValue(Int)</summary>
        [APIFunc("SelfDamage", APIType.Action, "自伤", Scope.CombatNpc, "DamageValue:Int")]
        public static APIContext SelfDamage( APIContext ctx)
        {
            var caster = ctx.Get<CombatNpc>("Caster");
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
            target.AddModifier(buffId, stacks);

          }

        // ── 卡组操作类 ────────────────────────────────────────

        /// <summary>移除己方一张伤势卡。参数：SizeList(String)</summary>
        [APIFunc("RemoveWound", APIType.Action, "移除己方一张伤势卡", Scope.CombatNpc)]
        public static APIContext RemoveWound( APIContext ctx)
        {
            return ctx;}

        /// <summary>位移目标卡牌到指定位置。参数：TargetCardId(String), Position(String)</summary>
        /// 估计会因为在tick中处理Card位置而报错，到时候看看
        [APIFunc("Displace", APIType.Action, "位移目标卡牌", Scope.CombatCard)]
        public static APIContext Displace( APIContext ctx)
        {
            return ctx;
        }

    }
}
