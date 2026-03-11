using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Card 手配定义：描述一张具体卡牌的完整数据。
    /// 持有一组 EffectDefine 的引用，是手配卡牌的入口。
    /// 动态生成的卡牌不需要 CardDefine，由代码逻辑直接构造 CardData。
    /// </summary>
    public class CardDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        /// <summary>稀有度等级（0=普通，1=非常规，2=稀有，3=传说）</summary>
        [JsonPropertyName("rarity")]
        public int Rarity { get; set; } = 0;

        /// <summary>卡牌所属的 Effect 定义 ID 列表（引用 EffectDefine）</summary>
        [JsonPropertyName("effectIds")]
        public List<string> EffectIds { get; set; } = [];

        /// <summary>
        /// 卡牌的 Tag 列表（重复表示浓度）。
        /// 可手动填写以标识主题；若为空，系统可从 EffectDefine 中聚合推导。
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];
    }
}