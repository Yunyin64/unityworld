namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Card 运行时实例数据
    /// 由一个或多个 Effect 组合而成
    /// </summary>
    public class CardData
    {
        public CardId Id { get; set; } = CardId.Invalid;

        /// <summary>来源的 CardDefine ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>组成这张卡的 Effect 列表</summary>
        public List<EffectData> Effects { get; set; } = [];

        /// <summary>
        /// 合并后的 TagBag（所有 Effect Tags 拼接，自动涌现）
        /// </summary>
        public List<string> Tags { get; set; } = [];
    }
}
