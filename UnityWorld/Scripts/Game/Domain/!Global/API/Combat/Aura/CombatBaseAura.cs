using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public static partial class CombatBaseFunc
    {
        /// <summary>给来源卡登记冻结免疫光环，覆盖随位置及激活状态判定。</summary>
        [APIFunc("GrantFreezeImmunityAura", APIType.Aura, "激活时使指定范围的卡免疫冻结", Scope.CombatCard,
            "Direction:String", "Keyword:String")]
        public static APIContext GrantFreezeImmunityAura(APIContext ctx)
        {
            ctx.SourceCard?.GrantFreezeImmunityAura(ctx.GetStringValue("Direction"), ctx.GetStringValue("Keyword", "Any"));
            return ctx;
        }
    }
}
    