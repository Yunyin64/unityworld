
        using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {/// <summary>充能目标卡牌，减少CD。参数：TargetCard(List<CombatCard>), ReduceTick(Int)</summary>
        [APIFunc("Charge",APIType.Action, "充能目标卡牌", Scope.Card, "TargetCard:List<CombatCard>", "ReduceTick:Int")]
        public static APIContext Charge(APIContext ctx)
        {
            var caster = ctx.Get<CombatNpc>("Caster");
            if (caster == null) return ctx;
            
            List<CombatCard> TargetCard = ctx.Get<List<CombatCard>>("TargetCard");
            if (TargetCard == null) return ctx;

            int ReduceTick = ctx.GetValue("ReduceTick", 10);
            foreach (var card in TargetCard)
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



            int freezeSeconds = ctx.GetValue("FreezeTime", 1);
            int freezeTick = freezeSeconds * 10;

            var card = ctx.Get<CombatCard>("TargetCard");
            card.AddCardBuff();
            return ctx;
        }


        /// <summary>减速目标卡牌。参数：TargetCardId(String), X(Int)</summary>
        [APIFunc("Slow", APIType.Action, "减速目标卡牌", Scope.Card)]
        public static APIContext Slow( APIContext ctx)
        {
            return ctx;
        }

        /// <summary>加速目标卡牌。参数：TargetCardId(String), X(Int)</summary>
        [APIFunc("Haste" , APIType.Action, "加速目标卡牌")]
        public static APIContext Haste( APIContext ctx)
        {
            return ctx;
        }
    }
}
    