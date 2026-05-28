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
        /// 部署：将一张卡从 Reserve 移入运转池。参数：CardId(Int)
        /// </summary>
        [APIFunc("Deploy", APIType.Action, "将卡从候补池部署到运转池", Scope.CombatNpc, "CardId:Int")]
        public static APIContext Deploy(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            int cardId = ctx.GetValue("CardId", 0);
            var reserve = caster.GetReserve();
            var card = reserve.Find(c => c.Id == cardId);
            if (card == null)
            {
                LogMgr.Instance.Warn($"[Deploy] 卡Id={cardId} 不在 {caster.GetName()} 的 Reserve 中");
                return ctx;
            }

            caster.Deploy(card);
            return ctx;
        }

        /// <summary>
        /// 召回：将一张卡从运转池移回 Reserve。参数：CardId(Int)
        /// </summary>
        [APIFunc("Recall", APIType.Action, "将卡从运转池召回候补池", Scope.CombatNpc, "CardId:Int")]
        public static APIContext Recall(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;

            int cardId = ctx.GetValue("CardId", 0);
            var deck = caster.GetField();
            var card = deck.Find(c => c.Id == cardId);
            if (card == null)
            {
                LogMgr.Instance.Warn($"[Recall] 卡Id={cardId} 不在 {caster.GetName()} 的运转池中");
                return ctx;
            }

            caster.Recall(card);
            return ctx;
        }
    }
}
