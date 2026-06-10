
        using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {/// <summary>充能目标卡牌，减少CD。参数：Domain(String),ReduceTick(Int)</summary>
        [APIFunc("Charge",APIType.Action, "充能目标卡牌", Scope.Card, "Domain:String","ReduceTick:Int")]
        public static APIContext Charge(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"),ctx);

            int ReduceTick = ctx.GetValue("ReduceTick", 10);
            foreach (var card in ctx.CardTargets.Where(c=> c.CheckPhase(CombatCardPhase.InCD)))
            {
                card.Charge(ReduceTick);
            }
            return ctx;
        }
        /// <summary>冻结目标卡牌。参数：Domain(String), FreezeTime(Int)</summary>
        [APIFunc("Freeze", APIType.Action, "冻结目标卡牌", Scope.Card, "Domain:String", "FreezeTime:Int")]
        public static APIContext Freeze( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);

            int freezeSeconds = ctx.GetValue("FreezeTime", 10);
            foreach (var card in ctx.CardTargets)
            {
                card.AddCardBuff(CardModifier.Freeze("Freeze"+card.Id, freezeSeconds));
                LogMgr.Instance.Dbg("[Freeze] {0} 冻结 {1} {2}/{3}", card.DisplayName, freezeSeconds,card.Ticks["CD"],card.GetCDMax());
            }
            return ctx;
        }


        /// <summary>减速目标卡牌。参数：Domain(String), Stack(Int)</summary>
        [APIFunc("Slow", APIType.Action, "减速目标卡牌", Scope.Card, "Domain:String", "Stack:Int")]
        public static APIContext Slow( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);

            int Stack = ctx.GetValue("Stack", 1);
            foreach (var card in ctx.CardTargets)
            {
                card.AddCardBuff(CardModifier.CDSpeed("Slow"+card.Id, -Stack));
                LogMgr.Instance.Dbg("[Slow] {0} 减速 {1} {2}/{3}", card.DisplayName, Stack,card.Ticks["CD"],card.GetCDMax());
            }
            return ctx;
        }

        /// <summary>加速目标卡牌。参数：Domain(String), Stack(Int)</summary>
        [APIFunc("Haste" , APIType.Action, "加速目标卡牌", Scope.Card, "Domain:String", "Stack:Int")]
        public static APIContext Haste( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);

            int Stack = ctx.GetValue("Stack", 1);
            foreach (var card in ctx.CardTargets)
            {
                card.AddCardBuff(CardModifier.CDSpeed("Haste"+card.Id, Stack));
                LogMgr.Instance.Dbg("[Haste] {0} 加速 {1} {2}/{3}", card.DisplayName, Stack,card.Ticks["CD"],card.GetCDMax());
            }
            return ctx;
        }

        /// <summary>给目标卡牌添加永久属性修正。参数：Domain(String), StatId(String), Value(Float)</summary>
        [APIFunc("AddCardStatBuff", APIType.Action, "给目标卡牌添加永久属性修正", Scope.Card, "Domain:String", "StatId:String", "Value:Float")]
        public static APIContext AddCardStatBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);

            string statId = ctx.GetValue("StatId", "");
            float value = ctx.GetValue("Value", 0f);
            if (string.IsNullOrEmpty(statId)) return ctx;

            foreach (var card in ctx.CardTargets)
            {
                card.AddCardBuff(CardModifier.StatBuff("StatBuff_" + statId + "_" + card.Id, statId, value));
                LogMgr.Instance.Dbg("[AddCardStatBuff] {0} 属性 {1} += {2}", card.DisplayName, statId, value);
            }
            return ctx;
        }
    }
}
    