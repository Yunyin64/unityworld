using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 物品子系统：管理 cardId → CardItemData 的映射
    /// </summary>
    public class CardSystemItem : CardSystemBase<CardItemData>
    {
        /// <summary>按卡牌编号索引的物品数据</summary>
        protected override Dictionary<int, CardItemData> _dataTable { get; set; } = new();

        /// <summary>保留物品卡的时步入口</summary>
        public override void OnTick(Card card, float deltaTime)
        {
        }
    }
}
