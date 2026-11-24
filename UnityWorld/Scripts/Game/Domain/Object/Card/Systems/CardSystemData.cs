using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 基础数据子系统
    /// </summary>
    public class CardSystemData : CardSystemBase<CardBaseData>
    {
        /// <summary>按卡牌编号索引的基础数据</summary>
        protected override Dictionary<int, CardBaseData> _dataTable { get; set; } = new();

        /// <summary>保留卡牌基础数据的时步入口</summary>
        public override void OnTick(Card card, float deltaTime)
        {
            // 装备卡暂无 Tick 逻辑
        }
    }
}
