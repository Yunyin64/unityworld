using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        
        private static string FormatManaCells(IReadOnlyList<KeyValuePair<BackpackCell, ElementType>> cells) =>
            string.Join(",", cells.Select(p => $"({p.Key.X + 1},{p.Key.Y + 1}):{p.Value}"));

        // ── 灵元转化类 ────────────────────────────────────────

        /// <summary>将灵元转化回蓝条MP（1:1）。参数：Domain(String), Element(String), MaxAmount(Int)</summary>
        [APIFunc("Convert", APIType.Action,"回收格上灵元为MP（1:1，受MP空缺限制，不换元素）",Scope.CombatNpc, "Domain:String", "Element:String", "MaxAmount:Int")]
        public static APIContext Convert(APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx).DistinctBy(n => n.Id).ToList();
            var element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            int requested = Math.Max(0, ctx.GetValue("MaxAmount", 1));
            foreach (var npc in ctx.NpcTargets)
            {
                var amount = Math.Min(requested, Math.Max(0, npc.GetCombatMpMax() - npc.GetMp()));
                var removed = npc.Backpack.RemoveMana(amount, element.Kind == BaseElementType.None ? null : element, ctx.Scene.Soul);
                var restored = removed.Count > 0 ? npc.RecoverMP(removed.Count) : 0;
                npc.Log($"回收灵元 请求={requested} 实际={removed.Count} MP差额=+{restored} 格={FormatManaCells(removed)}");
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
                    var before = picked.CurrentStack;
                    picked.ReduceStack(1);
                    if (picked.CurrentStack != before) Scene.MarkPresentationEffect();
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
        [APIFunc("ReduceMana", APIType.Action, "移除格上指定元素灵元（不足部分成功，不触碰卡上点亮）", Scope.CombatNpc, "Domain:String", "Element:String", "Amount:Int")]
        public static APIContext ReduceMana( APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx).DistinctBy(n => n.Id).ToList();
            var element = ElementType.GetElementType(ctx.GetValue("Element", "None"));
            var requested = Math.Max(0, ctx.GetValue("Amount", 0));
            foreach (var npc in ctx.NpcTargets)
            {
                var removed = npc.Backpack.RemoveMana(requested, element.Kind == BaseElementType.None ? null : element, ctx.Scene.Soul);
                npc.Log($"减灵 请求={requested} 实际={removed.Count} MP差额=0 格={FormatManaCells(removed)}");
            }
            return ctx;
        }
        
        /// <summary>直接获得MP。参数：Domain(String),  Amount(Int)</summary>
        [APIFunc("RecoverMP", APIType.Action, "目标直接获得MP。", Scope.CombatNpc, "Domain:String",  "Amount:Int")]
        public  static APIContext RecoverMP(APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.RecoverMP(amount);
            }
            return ctx;
        }
        
        /// <summary>直接减少MP。参数：Domain(String),  Amount(Int)</summary>
        [APIFunc("ReduceMP", APIType.Action, "目标直接减少MP。", Scope.CombatNpc, "Domain:String",  "Amount:Int")]
        public  static APIContext ReduceMP(APIContext ctx)
        {
            var caster = ctx.Caster;
            var Scene = ctx.Scene;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0) return ctx;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.ReduceMP(amount);
            }
            return ctx;
        }
    }
}
