
        using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        /// <summary>获得目标所有卡牌。</summary>
        [APIFunc("AllCard",APIType.Scope, "获得目标所有卡牌", Scope.CombatNpc, "Target:CombatNpc", "Result:List<CombatCard>")]
        public static APIContext AllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var target = ctx.Get<CombatNpc>("Target");
            var cards = target.GetField();
            ctx.Set<List<CombatCard>>("Result", cards);
            ctx.Set<bool>("Ret",true);

            return ctx;
        }

        /// <summary>获得目标在CD中的一张卡牌。</summary>
        [APIFunc("RandomCardInCD",APIType.Scope, "获得目标在CD中的一张卡牌", Scope.CombatNpc, "Target:CombatNpc", "Result:CombatCard")]
        public static APIContext RandomCardInCD(APIContext ctx)
        {
            var caster = ctx.Caster;
            var target = ctx.Get<CombatNpc>("Target");
            var cards = target.GetField();
            cards = cards.Where(c => c.GetPhase() == CombatCardPhase.InCD).ToList();
            var result = cards.Count > 0 ? cards[caster.Scene.Soul.Random(0, cards.Count)] : null;
            ctx.Set<bool>("Ret",false);
            if (result != null)
            {
                ctx.Set<bool>("Ret",true);
                ctx.Set<CombatCard>("Result", result);
            }

            return ctx;
        }
        
        /// <summary>获得目标相邻卡牌（Direction: Above/Below）。</summary>
        [APIFunc("AdjacentCards", APIType.Scope, "获得目标相邻卡牌", Scope.CombatCard, "Target:CombatCard", "Direction:string", "Result:List<CombatCard>")]
        public static APIContext AdjacentCards(APIContext ctx)
        {
            var caster = ctx.Caster;
            var target = ctx.Get<CombatCard>("Target");
            var direction = ctx.Get<string>("Direction");
            var deck = caster.GetField();
            var index = caster.GetIndexByCard(target);

            List<CombatCard> cards = null;
            if (direction == "Above" && index > 0)
            {
                cards = deck.Take(index).ToList();
            }
            else if (direction == "Below" && index >= 0 && index < deck.Count - 1)
            {
                cards = deck.Skip(index + 1).ToList();
            }

            if (cards != null && cards.Count > 0)
            {
                ctx.Set<List<CombatCard>>("Result", cards);
                ctx.Set<bool>("Ret", true);
            }
            else
            {
                ctx.Set<bool>("Ret", false);
            }

            return ctx;
        }

    }
}
    