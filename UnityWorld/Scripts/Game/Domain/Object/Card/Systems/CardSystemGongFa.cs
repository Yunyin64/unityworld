using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 功法子系统：管理 cardId → CardGongFaData 的映射
    /// </summary>
    public class CardSystemGongFa : CardSystemBase<CardGongFaData>
    {
        protected override Dictionary<int, CardGongFaData> _dataTable { get; set; } = new();

        public override void OnTick(Card card, float deltaTime)
        {
            // 功法卡暂无 Tick 逻辑
        }
    }
}
