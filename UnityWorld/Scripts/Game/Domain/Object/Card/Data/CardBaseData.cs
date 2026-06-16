using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 基础属性数据（简单不变属性）
    /// 从 CardDefine 实例化后不再变化
    /// </summary>
    public class CardBaseData : IDomainDataBase
    {
        // ── 卡牌类型 ──────────────────────────────────────────

        // ── 体量与节奏 ────────────────────────────────────────

        /// <summary>卡牌体量（占用 SP 的大小）</summary>
        public int Size { get; set; } = 1;

        /// <summary>冷却时间（秒）</summary>
        public float Cooldown { get; set; } = 3f;
        

        /// <summary>弹药上限</summary>
        public int AmountMax { get; set; } = 0;

        // ── 消耗堆叠 ──────────────────────────────────────────

        /// <summary>世界侧堆叠数量（Consume keyword 时生效）</summary>
        public int Stack { get; set; } = 0;

        /// <summary>堆叠上限</summary>
        public int StackMax { get; set; } = 0;

        // ── 灵元消耗 ──────────────────────────────────────────

        /// <summary>灵元消耗（key=元素名称字符串，value=数量）</summary>
        public Dictionary<ElementType, int> ManaCost { get; set; } = new();

        // ── 标签 ──────────────────────────────────────────────

        /// <summary>
        /// 合并后的 TagBag（所有 Effect Tags + CardDefine 手配 Tags 拼接，自动涌现）
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>关键词列表（如 Passive 等，决定卡牌运行模式）</summary>
        public List<string> Keywords { get; set; } = [];


        public CardBaseData Clone()
        {
            var copy = (CardBaseData)MemberwiseClone();
            copy.ManaCost = new Dictionary<ElementType, int>(ManaCost);
            copy.Tags = new List<string>(Tags);
            copy.Keywords = new List<string>(Keywords);
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();


        // ── IDomainDataBase ───────────────────────────────────

        /// <summary>日志输出</summary>

        public void Log()
        {
            var mana = ManaCost.Count > 0
                ? string.Join(",", ManaCost.Select(kv => $"{kv.Key}:{kv.Value}"))
                : "无";
            LogMgr.Instance.Dbg("┌── BaseData · 基础属性 ──────────────────────");
            LogMgr.Instance.Dbg("│  体量:    {0}", Size);
            LogMgr.Instance.Dbg("│  冷却:    {0:F1}s", Cooldown);
            LogMgr.Instance.Dbg("│  灵元:    {0}", mana);
            LogMgr.Instance.Dbg("│  标签:    [{0}]", string.Join(", ", Tags));
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }

        
    /// <summary>
    /// Card 便捷访问器扩展
    /// </summary>
    public partial class Card
    {
        /// <summary>父卡 Id（招式卡 → 所属法宝卡 Id，-1 表示无父卡）</summary>
        public int ParentCardId { get; set; } = -1;
        public CardBaseData BaseData => CardMgr.Instance.DataSystem.GetData(Id);
        public float GetCooldown() => BaseData.Cooldown;
        public int GetSize() => BaseData.Size;

        public Dictionary<ElementType, int> GetManaCost() => BaseData.ManaCost;
        public int GetAmountMax() => BaseData.AmountMax;
        public int GetStack() => BaseData.Stack;
        public int GetStackMax() => BaseData.StackMax;
        
        public List<string> GetKeywords() => BaseData.Keywords;

        public List<string> GetTags() => BaseData.Tags;
        
        public bool HasKeyword(string keyword) => BaseData.Keywords.Contains(keyword);
        /// <summary>
        /// 获取卡牌的主元素类型：取灵元消耗中数量最大的元素；若有多个并列最大则返回Mix；无消耗返回None
        /// </summary>
        public ElementType GetElementType()
        {
            var manaCost = GetManaCost();
            if (manaCost == null || manaCost.Count == 0)
                return ElementType.None;

            int maxVal = -1;
            ElementType maxType = ElementType.None;
            bool tied = false;

            foreach (var kv in manaCost)
            {
                if (kv.Key.Kind == BaseElementType.Extra || kv.Key.Kind == BaseElementType.None)
                    continue;

                if (kv.Value > maxVal)
                {
                    maxVal = kv.Value;
                    maxType = kv.Key;
                    tied = false;
                }
                else if (kv.Value == maxVal)
                {
                    tied = true;
                }
            }

            if (maxVal < 0)
                return ElementType.None;

            return tied ? ElementType.Mix : maxType;
        }
    }
    
}
