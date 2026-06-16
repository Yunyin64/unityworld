using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 侧物品附属数据：提供便捷方法壳子，实际数据问 ItemMgr。
    /// </summary>
    public class CardItemData : IDomainDataBase
    {
        /// <summary>所属 Card 的 Id（创建时传入）</summary>
        public int CardId { get; set; }

        // ── 便捷查询 ────────────────────────────────────────

        /// <summary>从 ItemMgr 获取物品实例</summary>
        public Item GetItem()
            => ItemMgr.Instance?.GetById(CardId);

        // ── IDomainDataBase ─────────────────────────────────

        public IDomainDataBase Clone()
        {
            return (CardItemData)MemberwiseClone();
        }

        public void Log()
        {
            var item = GetItem();
            LogMgr.Instance.Dbg("┌── CardItemData ────────────────────────────");
            LogMgr.Instance.Dbg("│  CardId:    {0}", CardId);
            LogMgr.Instance.Dbg("│  Item:      {0}", item?.ToString() ?? "null");
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Card
    {
        /// <summary>物品附属数据（通过 CardMgr.ItemSystem 访问）</summary>
        protected CardItemData ItemData => CardMgr.Instance.ItemSystem.GetData(Id);

        /// <summary>是否为物品卡</summary>
        public bool IsItemCard => ItemMgr.Instance?.GetById(Id) != null;

        /// <summary>获取物品实例</summary>
        public Item GetItem() => ItemData.GetItem();
    }
}
