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

        /// <summary>仅专用几何测试可自定义；正式内容从 Size 自动派生横条。</summary>
        public List<BackpackCell> Shape { get; set; } = [];
        public bool UseCustomShapeForTests { get; set; }

        /// <summary>是否参与扫描激活。开启时 ManaCost 总量必须等于 Size。</summary>
        public bool ScanActivatable { get; set; }
        public bool? ProducesContest { get; set; }
        public string CompatibilityStatus { get; set; } = "Migrated";
        public string CompatibilityReason { get; set; } = "";

        public List<BackpackCell> GetShape()
        {
            var horizontal = BackpackCoordinates.HorizontalShape(Size);
            if (UseCustomShapeForTests) return [.. Shape];
            if (Shape.Count != 0 && !Shape.SequenceEqual(horizontal))
                throw new InvalidOperationException($"卡 {ID} 正式形状必须由 Size 自动生成横条。");
            return horizontal;
        }

        /// <summary>冷却回合数</summary>
        [JsonPropertyName("cooldown")]
        public float Cooldown { get; set; } = 4;

        /// <summary>最大弹药数，0就是无弹药机制，基本都是0。非0的基本都要加上Amount KeyWord</summary>
        [JsonPropertyName("AmountMax")]
        public int AmountMax  { get; set; } = 0;

        /// <summary>堆叠上限（Consume keyword 时生效，0=不可堆叠）</summary>
        [JsonPropertyName("StackMax")]
        public int StackMax { get; set; } = 0;

        /// <summary>灵元消耗（元素名 → 数量）</summary>
        [JsonPropertyName("manaCost")]
        public Dictionary<string, int> ManaCost { get; set; } = new();

        /// <summary>关键词列表（如 Passive、OnHit 等，决定卡牌运行模式）</summary>
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = [];

    }
}
