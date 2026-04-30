using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        
        // ── 灵元转化类 ────────────────────────────────────────

        /// <summary>将灵元转化回蓝条MP（1:1）。参数：Element(String), MaxAmount(Int)</summary>
        [APIFunc("Convert", APIType.Action,"灵元转化回蓝条MP",Scope.CombatNpc, "Element:String", "MaxAmount:Int")]
        public static APIContext Convert(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int maxAmount = ctx.GetValue("MaxAmount", 1);

             var cost = new Dictionary<ElementType, int>();
            if (element.Kind == BaseElementType.None)
            {
                //如果是None，就转化随机npc剩下的灵元
            }
            else
            {
                cost[element] = Math.Min(caster.GetManaCount(element),maxAmount);
            }
            caster.ManaConvert(cost);

            
            return ctx;
        }

        /// <summary>立刻将MP转化为灵元。参数：Amount(Int)</summary>
        [APIFunc("Draw", APIType.Action,"MP转化为灵元", Scope.CombatNpc, "Amount:Int")]
        public static APIContext Draw(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            int amount = ctx.GetValue("Amount", 1);
            caster.DrawMana(amount);

            return ctx;
        }

        /// <summary>减少自身指定元素的灵元。参数：Element(String), Amount(Int)</summary>
        [APIFunc("ReduceMana", APIType.Action, "减少自身指定元素的灵元", Scope.CombatNpc, "Element:String", "Amount:Int")]
        public static APIContext ReduceMana( APIContext ctx)
        {
            var caster = ctx.Get<CombatNpc>("Caster");
            if (caster == null) return ctx;

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;
            var cost = new Dictionary<ElementType, int>();
            if (element.Kind == BaseElementType.None)
            {
                //如果是None，就减少随机npc剩下的灵元
            }
            else
            {
                cost[element] = amount;
            }

            if (caster.CanAffordMana(cost)){
                caster.ConsumeMana(cost);
            }

            return ctx;
        }
        
    }
}