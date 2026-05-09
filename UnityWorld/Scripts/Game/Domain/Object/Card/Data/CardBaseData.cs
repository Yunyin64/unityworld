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

        /// <summary>卡牌类型（招式/法术/丹药/阵法/神通/伤势等）</summary>
        public CardType CardType { get; set; } = CardType.ZhaoShi;

        // ── 体量与节奏 ────────────────────────────────────────

        /// <summary>卡牌体量（占用 SP 的大小）</summary>
        public int Size { get; set; } = 1;

        /// <summary>冷却时间（秒）</summary>
        public float Cooldown { get; set; } = 3f;

        // ── 灵元消耗 ──────────────────────────────────────────

        /// <summary>灵元消耗（key=元素名称字符串，value=数量）</summary>
        public Dictionary<ElementType, int> ManaCost { get; set; } = new();

        // ── 标签 ──────────────────────────────────────────────

        /// <summary>
        /// 合并后的 TagBag（所有 Effect Tags + CardDefine 手配 Tags 拼接，自动涌现）
        /// </summary>
        public List<string> Tags { get; set; } = [];

        public CardBaseData Clone()
        {
            var copy = (CardBaseData)MemberwiseClone();
            copy.ManaCost = new Dictionary<ElementType, int>(ManaCost);
            copy.Tags = new List<string>(Tags);
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
            LogMgr.Dbg("┌── BaseData · 基础属性 ──────────────────────");
            LogMgr.Dbg("│  类型:    {0}", CardType);
            LogMgr.Dbg("│  体量:    {0}", Size);
            LogMgr.Dbg("│  冷却:    {0:F1}s", Cooldown);
            LogMgr.Dbg("│  灵元:    {0}", mana);
            LogMgr.Dbg("│  标签:    [{0}]", string.Join(", ", Tags));
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

        
    /// <summary>
    /// Card 便捷访问器扩展
    /// </summary>
    public partial class Card
    {
        public float GetCooldown() => BaseData.Cooldown;
        public int GetSize() => BaseData.Size;

        public Dictionary<ElementType, int> GetManaCost() => BaseData.ManaCost;
    }
    
}
