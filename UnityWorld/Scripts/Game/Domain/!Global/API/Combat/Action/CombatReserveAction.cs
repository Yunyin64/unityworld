using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 战斗域 Reserve 相关 API：Deploy / Recall。
    /// </summary>
    public static partial class CombatBaseFunc
    {
        // ── Reserve 操作类 ────────────────────────────────────────

        /// <summary>
        /// 部署：将一张卡从 Reserve 移入运转池。参数：Domain(String), CardId(Int)
        /// </summary>
        [APIFunc("Deploy", APIType.Action, "将卡从候补池部署到运转池", Scope.CombatNpc, "Domain:String", "CardId:Int")]
        public static APIContext Deploy(APIContext ctx)
        {
            LegacyCombatCompatibility.RejectPending("Deploy");
            return ctx;
        }

        /// <summary>
        /// 召回：将一张卡从运转池移回 Reserve。参数：Domain(String), CardId(Int)
        /// </summary>
        [APIFunc("Recall", APIType.Action, "将卡从运转池召回候补池", Scope.CombatNpc, "Domain:String", "CardId:Int")]
        public static APIContext Recall(APIContext ctx)
        {
            LegacyCombatCompatibility.RejectPending("Recall");
            return ctx;
        }
    }
}
