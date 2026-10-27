using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public static partial class CombatBaseFunc
    {
        // Self/Target are role targets. Legacy card selectors are deduplicated by owner.
        private static List<CombatNpc> TimeTargets(APIContext ctx)
        {
            var domain = ctx.GetStringValue("Domain");
            var targets = domain == "Self" || domain == "Target"
                ? APIMgr.Instance.GetTargetNpc(domain, ctx)
                : APIMgr.Instance.GetTargetCard(domain, ctx).Select(c => c.Owner).Where(n => n != null).ToList();
            return ctx.NpcTargets = targets.DistinctBy(n => n.Id).OrderBy(n => n.Id).ToList();
        }

        /// <summary>充能增加整包基准进度；ReduceTick 保留旧参数名，单位是正常速度 Tick。</summary>
        [APIFunc("Charge", APIType.Action, "整包充能，增加基准Tick进度（不受速度倍率影响）", Scope.CombatCard, "Domain:String", "ReduceTick:Int")]
        public static APIContext Charge(APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            int ticks = ctx.GetValue("ReduceTick", 10);
            foreach (var npc in TimeTargets(ctx))
            {
                npc.Backpack.Charge(ticks);
                npc.Log($"充能 基准Tick={Math.Max(0, ticks)} 进度={npc.Backpack.ScanProgress}");
            }
            return ctx;
        }

        /// <summary>冻结不计时、不叠加。FreezeTime 仅为旧签名兼容，下一次完整扫描跳过并解冻。</summary>
        [APIFunc("Freeze", APIType.Action, "冻结至下次完整扫描，旧FreezeTime参数忽略", Scope.CombatCard, "Domain:String", "FreezeTime:Int")]
        public static APIContext Freeze(APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            var domain = ctx.GetStringValue("Domain");
            var random = domain == "Random" || domain == "TargetRandom";
            var range = domain == "Random" ? "All" : domain == "TargetRandom" ? "TargetAll" : domain;
            var cards = APIMgr.Instance.GetTargetCard(range, ctx).DistinctBy(c => c.Id)
                .Where(c => !c.IsFrozen).OrderBy(c => c.Id).ToList();
            if (random && cards.Count > 0) cards = [cards[ctx.Scene.Soul.Random(0, cards.Count)]];
            ctx.CardTargets = cards;
            foreach (var card in cards)
                if (card.Freeze()) card.Owner.Log($"冻结 {card.DisplayName}#{card.Id}，下次完整扫描跳过并解冻");
            return ctx;
        }

        /// <summary>减速为整包半速，DurationTicks 是真实战斗 Tick，同类加时。</summary>
        [APIFunc("Slow", APIType.Action, "整包半速，持续真实Tick，同类加时", Scope.CombatCard, "Domain:String", "DurationTicks:Int")]
        public static APIContext Slow(APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            int ticks = ctx.GetValue("DurationTicks", ctx.GetValue("Stack", 10));
            foreach (var npc in TimeTargets(ctx))
            {
                npc.Backpack.AddSlow(ticks);
                npc.Log($"减速 剩余真实Tick={npc.Backpack.SlowTicks}");
            }
            return ctx;
        }

        /// <summary>加速为整包双速，DurationTicks 是真实战斗 Tick，同类加时。</summary>
        [APIFunc("Haste", APIType.Action, "整包双速，持续真实Tick，同类加时", Scope.CombatCard, "Domain:String", "DurationTicks:Int")]
        public static APIContext Haste(APIContext ctx)
        {
            if (ctx.Caster == null) return ctx;
            int ticks = ctx.GetValue("DurationTicks", ctx.GetValue("Stack", 10));
            foreach (var npc in TimeTargets(ctx))
            {
                npc.Backpack.AddHaste(ticks);
                npc.Log($"加速 剩余真实Tick={npc.Backpack.HasteTicks}");
            }
            return ctx;
        }

        [APIFunc("AddCardStatBuff", APIType.Action, "给目标卡牌添加永久属性修正；Min/Max/ManaAdj已废弃禁用", Scope.CombatCard, "Domain:String", "StatId:String", "Value:Float")]
        public static APIContext AddCardStatBuff(APIContext ctx)
        {
            string statId = ctx.GetValue("StatId", "");
            LegacyCombatCompatibility.RequireSupportedStat(statId);
            if (ctx.Caster == null || string.IsNullOrEmpty(statId)) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);
            float value = ctx.GetValue("Value", 0f);
            foreach (var card in ctx.CardTargets)
                card.AddCardBuff(CardModifier.StatBuff("StatBuff_" + statId + "_" + card.Id, statId, value));
            return ctx;
        }
    }
}
