using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 功法子系统：管理 cardId → CardGongFaData 的映射
    /// </summary>
    public class CardSystemGongFa : CardSystemBase<CardGongFaData>
    {
        /// <summary>按卡牌编号索引的功法数据</summary>
        protected override Dictionary<int, CardGongFaData> _dataTable { get; set; } = new();

        /// <summary>保留功法卡的时步入口</summary>
        public override void OnTick(Card card, float deltaTime)
        {
            // 功法卡暂无 Tick 逻辑
        }
    }
}
