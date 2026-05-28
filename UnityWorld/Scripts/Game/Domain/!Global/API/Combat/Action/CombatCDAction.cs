
        using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {/// <summary>充能目标卡牌，减少CD。参数：TargetCard(List<CombatCard>), ReduceTick(Int)</summary>
        [APIFunc("Charge",APIType.Action, "充能目标卡牌", Scope.Card, "TargetCard:List<CombatCard>", "ReduceTick:Int")]
        public static APIContext Charge(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            
            List<CombatCard> TargetCard = ctx.Get<List<CombatCard>>("TargetCard");
            if (TargetCard == null) TargetCard = new List<CombatCard>(){ctx.Get<CombatCard>("TargetCard")};
            if (TargetCard == null) return ctx;

            int ReduceTick = ctx.GetValue("ReduceTick", 10);
            foreach (var card in TargetCard.Where(c=> c.GetPhase() == CombatCardPhase.InCD))
            {
                card.Charge(ReduceTick);
            }
            return ctx;
        }
        /// <summary>冻结目标卡牌。参数：TargetCard(CombatCard), FreezeTime(Float)</summary>
        [APIFunc("Freeze", APIType.Action, "冻结目标卡牌", Scope.Card, "TargetCard:CombatCard", "FreezeTime:Float")]
        public static APIContext Freeze( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            int freezeSeconds = ctx.GetValue("FreezeTime", 10);

            var card = ctx.Get<CombatCard>("TargetCard");
            card.AddCardBuff(CardModifier.Freeze("Freeze"+card.Id, freezeSeconds));
            LogMgr.Instance.Dbg("[Freeze] {0} 冻结 {1} {2}/{3}", card.DisplayName, freezeSeconds,card.Ticks["CD"],card.GetCDMax());
            return ctx;
        }


        /// <summary>减速目标卡牌。参数：TargetCard(CombatCard), Stack(Int)</summary>
        [APIFunc("Slow", APIType.Action, "减速目标卡牌", Scope.Card,"TargetCard:CombatCard", "Stack:Int")]
        public static APIContext Slow( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            
            int Stack = ctx.GetValue("Stack", 1);
            var card = ctx.Get<CombatCard>("TargetCard");
            card.AddCardBuff(CardModifier.CDSpeed("Slow"+card.Id, -Stack));
            LogMgr.Instance.Dbg("[Slow] {0} 减速 {1} {2}/{3}", card.DisplayName, Stack,card.Ticks["CD"],card.GetCDMax());
            return ctx;
        }

        /// <summary>加速目标卡牌。参数：TargetCard(CombatCard), Stack(Int)</summary>
        [APIFunc("Haste" , APIType.Action, "加速目标卡牌", Scope.Card, "TargetCard:CombatCard", "Stack:Int")]
        public static APIContext Haste( APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            
            int Stack = ctx.GetValue("Stack", 1);
            var card = ctx.Get<CombatCard>("TargetCard");
            card.AddCardBuff(CardModifier.CDSpeed("Haste"+card.Id, Stack));
            LogMgr.Instance.Dbg("[Haste] {0} 加速 {1} {2}/{3}", card.DisplayName, Stack,card.Ticks["CD"],card.GetCDMax());
            return ctx;
        }

        /// <summary>给目标卡牌添加永久属性修正。参数：TargetCard(CombatCard), StatId(String), Value(Float)</summary>
        [APIFunc("AddCardStatBuff", APIType.Action, "给目标卡牌添加永久属性修正", Scope.CombatCard, "TargetCard:CombatCard", "StatId:String", "Value:Float")]
        public static APIContext AddCardStatBuff(APIContext ctx)
        {
            var card = ctx.Get<CombatCard>("TargetCard");
            if (card == null) return ctx;

            string statId = ctx.GetValue("StatId", "");
            float value = ctx.GetValue("Value", 0f);
            if (string.IsNullOrEmpty(statId)) return ctx;

            card.AddCardBuff(CardModifier.StatBuff("StatBuff_" + statId + "_" + card.Id, statId, value));
            LogMgr.Instance.Dbg("[AddCardStatBuff] {0} 属性 {1} += {2}", card.DisplayName, statId, value);

            return ctx;
        }
    }
}
    