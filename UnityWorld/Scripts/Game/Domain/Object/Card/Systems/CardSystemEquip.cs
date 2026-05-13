using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 装备子系统：管理 cardId → CardEquipData 的映射
    /// </summary>
    public class CardSystemEquip : CardSystemBase<CardEquipData>
    {
        protected override Dictionary<int, CardEquipData> _dataTable { get; set; } = new();

        public override void OnTick(Card card, float deltaTime)
        {
            // 装备卡暂无 Tick 逻辑
        }
    }
}
