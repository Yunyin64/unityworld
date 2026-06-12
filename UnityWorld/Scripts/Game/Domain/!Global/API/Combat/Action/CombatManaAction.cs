using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        
        // ── 灵元转化类 ────────────────────────────────────────

        /// <summary>将灵元转化回蓝条MP（1:1）。参数：Domain(String), Element(String), MaxAmount(Int)</summary>
        [APIFunc("Convert", APIType.Action,"灵元转化回蓝条MP",Scope.CombatNpc, "Domain:String", "Element:String", "MaxAmount:Int")]
        public static APIContext Convert(APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int maxAmount = ctx.GetValue("MaxAmount", 1);

            foreach (var npc in ctx.NpcTargets)
            {
                var cost = new Dictionary<ElementType, int>();
                if (element.Kind == BaseElementType.None)
                {
                    var keys = npc.ManaPool.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                    int left = maxAmount;
                    while (left > 0 && keys.Count > 0)
                    {
                        int idx = Scene.Soul.Random(0, keys.Count);
                        var key = keys[idx];
                        int available = npc.GetManaCount(key) - (cost.TryGetValue(key, out var used) ? used : 0);
                        if (available <= 0)
                        {
                            keys.RemoveAt(idx);
                            continue;
                        }
                        if (!cost.ContainsKey(key)) cost[key] = 0;
                        cost[key]++;
                        left--;
                        if (cost[key] >= npc.GetManaCount(key))
                            keys.RemoveAt(idx);
                    }
                }
                else
                {
                    cost[element] = Math.Min(npc.GetManaCount(element), maxAmount);
                }
                npc.ManaConvert(cost);
            }

            return ctx;
        }

        // ── 五行 Buff 类 ────────────────────────────────────────

        /// <summary>循环Count次添加五行元素Buff。参数：Domain(String), Element(String), IsDebuff(Bool), Count(Int)</summary>
        [APIFunc("AddElementBuff", APIType.Action, "循环添加五行元素Buff", Scope.CombatNpc, "Domain:String", "Element:String", "IsDebuff:Bool", "Count:Int")]
        public static APIContext AddElementBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string element = ctx.GetValue("Element", "None");
            bool isDebuff = ctx.GetValue("IsDebuff", false);
            int count = ctx.GetValue("Count", 1);

            foreach (var npc in ctx.NpcTargets)
            {
                for (int i = 0; i < count; i++)
                {
                    string buffId;
                    if (element == "None")
                    {
                        buffId = npc.RandomBaseElementBuff(isDebuff);
                    }
                    else
                    {
                        var elemType = ElementType.GetElementType(element);
                        if (!ElementType.BaseElementBuff.TryGetValue(elemType, out var pair)) continue;
                        buffId = isDebuff ? pair.Item2 : pair.Item1;
                    }
                    npc.AddModifier(buffId, 1);
                }
            }
            return ctx;
        }

        /// <summary>循环Count次清除五行元素Buff。参数：Domain(String), Element(String), IsDebuff(Bool), Count(Int)</summary>
        [APIFunc("RemoveElementBuff", APIType.Action, "循环清除五行元素Buff", Scope.CombatNpc, "Domain:String", "Element:String", "IsDebuff:Bool", "Count:Int")]
        public static APIContext RemoveElementBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string element = ctx.GetValue("Element", "None");
            bool isDebuff = ctx.GetValue("IsDebuff", false);
            int count = ctx.GetValue("Count", 1);

            foreach (var npc in ctx.NpcTargets)
            {
                for (int i = 0; i < count; i++)
                {
                    var modifiers = npc.GetAllModifiers();
                    var matchIds = ElementType.BaseElementBuff.Values
                        .Select(pair => isDebuff ? pair.Item2 : pair.Item1).ToList();

                    List<CombatNpcModifier> candidates;
                    if (element == "None")
                    {
                        candidates = modifiers.Where(m => matchIds.Contains(m.DefineId)).ToList();
                    }
                    else
                    {
                        var elemType = ElementType.GetElementType(element);
                        if (!ElementType.BaseElementBuff.TryGetValue(elemType, out var pair)) break;
                        string targetId = isDebuff ? pair.Item2 : pair.Item1;
                        candidates = modifiers.Where(m => m.DefineId == targetId).ToList();
                    }

                    if (candidates.Count == 0) break;

                    var picked = candidates[Scene.Soul.Random(0, candidates.Count)];
                    picked.ReduceStack(1);
                }
            }
            return ctx;
        }

        // ── 灵元转化类 ────────────────────────────────────────

        /// <summary>立刻将MP转化为灵元。参数：Domain(String), Amount(Int)</summary>
        [APIFunc("Draw", APIType.Action,"MP转化为灵元", Scope.CombatNpc, "Domain:String", "Amount:Int")]
        public static APIContext Draw(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int amount = ctx.GetValue("Amount", 1);
            foreach (var npc in ctx.NpcTargets)
            {
                npc.DrawMana(amount);
            }

            return ctx;
        }

        /// <summary>减少目标指定元素的灵元。参数：Domain(String), Element(String), Amount(Int)</summary>
        [APIFunc("ReduceMana", APIType.Action, "减少目标指定元素的灵元", Scope.CombatNpc, "Domain:String", "Element:String", "Amount:Int")]
        public static APIContext ReduceMana( APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            ElementType element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;

            foreach (var npc in ctx.NpcTargets)
            {
                var cost = new Dictionary<ElementType, int>();
                if (element.Kind == BaseElementType.None)
                {
                    var keys = npc.ManaPool.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                    int left = amount;
                    while (left > 0 && keys.Count > 0)
                    {
                        int idx = Scene.Soul.Random(0, keys.Count);
                        var key = keys[idx];
                        int available = npc.GetManaCount(key) - (cost.TryGetValue(key, out var used) ? used : 0);
                        if (available <= 0)
                        {
                            keys.RemoveAt(idx);
                            continue;
                        }
                        if (!cost.ContainsKey(key)) cost[key] = 0;
                        cost[key]++;
                        left--;
                        if (cost[key] >= npc.GetManaCount(key))
                            keys.RemoveAt(idx);
                    }
                }
                else
                {
                    cost[element] = amount;
                }

                if (npc.CanAffordMana(cost))
                {
                    npc.ConsumeMana(cost);
                }
            }

            return ctx;
        }
        
    }
}