using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 装备子系统：管理 cardId → CardEquipData 的映射
    /// </summary>
    public class CardSystemEquip : CardSystemBase<CardEquipData>
    {
        /// <summary>按卡牌编号索引的装备数据</summary>
        protected override Dictionary<int, CardEquipData> _dataTable { get; set; } = new();

        /// <summary>保留装备卡的时步入口</summary>
        public override void OnTick(Card card, float deltaTime)
        {
            // 装备卡暂无 Tick 逻辑
        }
    }
}
