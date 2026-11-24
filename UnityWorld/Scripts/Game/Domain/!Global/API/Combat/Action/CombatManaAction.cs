using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        /// <summary>在目标方战斗属性上增减一个五行亲和。</summary>
        [APIFunc("ChangeAffinity", APIType.Action, "增减战斗中的五行亲和", Scope.CombatNpc,
            "Domain:String", "Element:String", "Delta:Int", "Rounds:Int")]
        public static APIContext ChangeAffinity(APIContext ctx)
        {
            var element = ctx.GetStringValue("Element");
            var delta = ctx.GetValue("Delta", 0);
            var rounds = ctx.GetValue("Rounds", 0);
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.ChangeBattleAffinity(element, delta, rounds);
            return ctx;
        }

        /// <summary>登记法宝轮首供能，None 允许任意匹配五行。</summary>
        [APIFunc("AddArtifactPower", APIType.Action, "获得按轮自动为法宝补充匹配灵元的供能", Scope.CombatNpc,
            "Domain:String", "Element:String", "Amount:Int", "Rounds:Int")]
        public static APIContext AddArtifactPower(APIContext ctx)
        {
            var element = ctx.GetStringValue("Element", "None");
            if (element is not ("None" or "Jin" or "Mu" or "Shui" or "Huo" or "Tu")) return ctx;
            var amount = ctx.GetValue("Amount", 0);
            var rounds = ctx.GetValue("Rounds", 0);
            foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                target.AddArtifactPower(ElementType.GetElementType(element), amount, rounds);
            return ctx;
        }

        
        /// <summary>用一基格坐标和元素格式化灵元格列表</summary>
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
                // 回收按一格灵元换一点 MP，直接回蓝的 Buff 在回收时不生效也不消耗。
                var restored = removed.Count > 0 ? npc.RestoreMP(removed.Count) : 0;
                npc.Log($"回收灵元 请求={requested} 实际={removed.Count} MP差额=+{restored} 格={FormatManaCells(removed)}");
            }
            return ctx;
        }

        /// <summary>从指定卡上移除实际已充入的灵元。</summary>
        [APIFunc("StripCardMana", APIType.Action, "剥离指定卡的蓄灵", Scope.CombatCard,
            "Domain:String", "Element:String", "Amount:Int")]
        public static APIContext StripCardMana(APIContext ctx)
        {
            int amount = ctx.GetValue("Amount", 0);
            string element = ctx.GetStringValue("Element", "None");
            if (amount <= 0 || element is not ("None" or "Jin" or "Mu" or "Shui" or "Huo" or "Tu")) return ctx;

            var kind = ElementType.GetElementType(element);
            int removed = 0;
            foreach (var card in APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx).Distinct())
                removed += card.StripAssignedMana(amount, kind);
            ctx.Set("RemovedCount", removed);
            return ctx;
        }

        /// <summary>获得抵消指定五行状态施加的次数。</summary>
        [APIFunc("AddElementImmunity", APIType.Action, "获得指定五行状态免疫", Scope.CombatNpc,
            "Domain:String", "BuffId:String", "Amount:Int")]
        public static APIContext AddElementImmunity(APIContext ctx)
        {
            string buffId = ctx.GetStringValue("BuffId");
            int amount = ctx.GetValue("Amount", 0);
            if (amount <= 0 || !CombatNpc.IsBaseElementStatus(buffId)) return ctx;

            foreach (var npc in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
                npc.AddModifier($"ElementImmunity_{buffId}", amount);
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
                    npc.ReduceElementModifier(picked);
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
