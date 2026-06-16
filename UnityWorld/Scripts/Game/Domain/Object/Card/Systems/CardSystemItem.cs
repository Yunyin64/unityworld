using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 物品子系统：管理 cardId → CardItemData 的映射
    /// </summary>
    public class CardSystemItem : CardSystemBase<CardItemData>
    {
        protected override Dictionary<int, CardItemData> _dataTable { get; set; } = new();

        public override void OnTick(Card card, float deltaTime)
        {
        }
    }
}
