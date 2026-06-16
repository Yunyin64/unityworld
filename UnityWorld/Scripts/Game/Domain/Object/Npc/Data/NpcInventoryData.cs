using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 物品栏数据：从 NPC 的卡牌列表中筛选 Item 卡的查询接口
    /// </summary>
    public class NpcInventoryData : IDomainDataBase
    {
        /// <summary>所属 NPC 的 ID</summary>
        public int NpcId { get; set; }

        /// <summary>获取该 NPC 持有的所有 Item 卡</summary>
        public List<Card> GetAllItems()
        {
            var npc = NpcMgr.Instance?.GetById(NpcId);
            if (npc == null) return [];
            return npc.GetAllCards().Where(c => c.IsItemCard).ToList();
        }

        /// <summary>获取该 NPC 持有的所有可消耗 Item 卡</summary>
        public List<Card> GetConsumables()
        {
            return GetAllItems().Where(c => c.HasKeyword("Consume")).ToList();
        }

        public IDomainDataBase Clone()
        {
            return (NpcInventoryData)MemberwiseClone();
        }

        public void Log()
        {
            var items = GetAllItems();
            LogMgr.Instance.Dbg("┌── InventoryData · 物品栏 ─────────────────────");
            LogMgr.Instance.Dbg("│  物品数: {0}", items.Count);
            foreach (var item in items)
            {
                var stack = item.GetStack() > 0 ? $" x{item.GetStack()}" : "";
                LogMgr.Instance.Dbg("│    [{0}] {1}{2}", item.Id, item.DisplayName, stack);
            }
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }
}
