using System.Text.Json.Serialization;
using UnityWorld.Core;

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

        /// <summary>卡牌占用手牌格数</summary>
        [JsonPropertyName("size")]
        public int Size { get; set; } = 1;

        /// <summary>冷却回合数</summary>
        [JsonPropertyName("cooldown")]
        public float Cooldown { get; set; } = 4;

        /// <summary>灵元消耗（元素名 → 数量）</summary>
        [JsonPropertyName("manaCost")]
        public Dictionary<string, int> ManaCost { get; set; } = new();

        /// <summary>附加卡牌 ID 列表（引用其他 CardDefine 的 ID）</summary>
        [JsonPropertyName("attachedCardIds")]
        public List<string> AttachedCardIds { get; set; } = [];

        /// <summary>关键词列表（如 Passive、OnHit 等，决定卡牌运行模式）</summary>
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = [];

    }
}