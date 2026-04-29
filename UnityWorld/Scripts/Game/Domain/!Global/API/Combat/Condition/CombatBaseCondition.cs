
        using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {/// <summary>获得目标所有卡牌。参数：Target(CombatNpc), Result(List<CombatCard>)</summary>
        [APIFunc("AllCard",APIType.Condition, "获得目标所有卡牌", Scope.Npc, "Target:CombatNpc", "Result:List<CombatCard>")]
        public static APIContext AllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var cards = caster.CardDeck;
            ctx.Set<List<CombatCard>>("Result", cards);

            return ctx;
        }
    }
}
    