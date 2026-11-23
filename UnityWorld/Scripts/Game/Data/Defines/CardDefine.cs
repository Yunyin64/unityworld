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

        /// <summary>卡牌占用格数。策划配表显式填写，缺省为单格。</summary>
        [JsonPropertyName("size")]
        public int Size { get; set; } = 1;

        /// <summary>形状定义 ID，留空时按 Size 生成横条。</summary>
        public string SizeShape { get; set; } = "";

        /// <summary>仅供专用几何测试直接指定。</summary>
        public List<BackpackCell> Shape { get; set; } = [];
        /// <summary>使用专用测试的显式几何。</summary>
        public bool UseCustomShapeForTests { get; set; }

        /// <summary>兼容旧定义中的扫描标记。发动方式由唯一 Main Keyword 决定。</summary>
        public bool ScanActivatable { get; set; }
        /// <summary>招式实例每个背包扫描轮的成功发动上限。</summary>
        /// <remarks>因强度调整次数前，先核对单次收益、灵元费用和实际发动频率。单独限次应有明确的玩法含义。</remarks>
        public int MaxUsesPerRound { get; set; } = 1;
        public bool? ProducesContest { get; set; }
        public string CompatibilityStatus { get; set; } = "Migrated";
        public string CompatibilityReason { get; set; } = "";

        /// <summary>从运行时目录解析形状，返回独立坐标列表。</summary>
        public List<BackpackCell> GetShape() => GetShape(id => ShapeDefineMgr.Instance?.Get(id));

        /// <summary>从调用方的数据目录解析形状，编辑器和战斗共用此校验。</summary>
        public List<BackpackCell> GetShape(Func<string, ShapeDefine?> findShape)
        {
            if (Size < 1) throw new InvalidOperationException($"卡 {ID} 的 Size 必须为正整数。");
            if (SizeShape == null) throw new InvalidOperationException($"卡 {ID} 的 SizeShape 必须为字符串。");
            if (SizeShape.Length > 0)
            {
                var definition = findShape(SizeShape)
                    ?? throw new InvalidOperationException($"卡 {ID} 引用了不存在的形状 {SizeShape}。");
                var cells = definition.Cells;
                if (cells == null || cells.Count != Size || cells.Distinct().Count() != Size ||
                    cells.Any(cell => cell.X < 0 || cell.Y < 0) ||
                    cells.Min(cell => cell.X) != 0 || cells.Min(cell => cell.Y) != 0)
                    throw new InvalidOperationException($"卡 {ID} 的形状 {SizeShape} 必须从外接框左上角归零、无重复且格数等于 Size {Size}。");
                return [.. cells];
            }

            if (UseCustomShapeForTests) return [.. Shape];
            var horizontal = BackpackCoordinates.HorizontalShape(Size);
            if (Shape.Count != 0 && !Shape.SequenceEqual(horizontal))
                throw new InvalidOperationException($"卡 {ID} 的异形需要通过 SizeShape 引用。");
            return horizontal;
        }

        /// <summary>冷却回合数</summary>
        [JsonPropertyName("cooldown")]
        public float Cooldown { get; set; } = 4;

        /// <summary>弹药容量。Amount 关键词要求正数上限，每场战斗从满弹开始。</summary>
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
