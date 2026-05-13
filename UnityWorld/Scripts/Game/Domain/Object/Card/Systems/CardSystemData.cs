using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 基础数据子系统
    /// </summary>
    public class CardSystemData : CardSystemBase<CardBaseData>
    {
        protected override Dictionary<int, CardBaseData> _dataTable { get; set; } = new();

        public override void OnTick(Card card, float deltaTime)
        {
            // 装备卡暂无 Tick 逻辑
        }
    }
}
