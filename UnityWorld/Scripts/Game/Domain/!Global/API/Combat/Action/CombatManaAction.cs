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
            var Scene = ctx.Scene;
            if (caster == null) return ctx;

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int maxAmount = ctx.GetValue("MaxAmount", 1);

             var cost = new Dictionary<ElementType, int>();
            if (element.Kind == BaseElementType.None)
            {
                // None：从所有有余量的元素中随机凑满 MaxAmount
                var keys = caster.ManaPool.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                int left = maxAmount;
                while (left > 0 && keys.Count > 0)
                {
                    int idx = Scene.Soul.Random(0, keys.Count);
                    var key = keys[idx];
                    int available = caster.GetManaCount(key) - (cost.TryGetValue(key, out var used) ? used : 0);
                    if (available <= 0)
                    {
                        keys.RemoveAt(idx);
                        continue;
                    }
                    if (!cost.ContainsKey(key)) cost[key] = 0;
                    cost[key]++;
                    left--;
                    if (cost[key] >= caster.GetManaCount(key))
                        keys.RemoveAt(idx);
                }
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
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;
            var cost = new Dictionary<ElementType, int>();
            if (element.Kind == BaseElementType.None)
            {
                // None：从所有有余量的元素中随机凑满 amount
                var keys = caster.ManaPool.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                int left = amount;
                while (left > 0 && keys.Count > 0)
                {
                    int idx = Scene.Soul.Random(0, keys.Count);
                    var key = keys[idx];
                    int available = caster.GetManaCount(key) - (cost.TryGetValue(key, out var used) ? used : 0);
                    if (available <= 0)
                    {
                        keys.RemoveAt(idx);
                        continue;
                    }
                    if (!cost.ContainsKey(key)) cost[key] = 0;
                    cost[key]++;
                    left--;
                    if (cost[key] >= caster.GetManaCount(key))
                        keys.RemoveAt(idx);
                }
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