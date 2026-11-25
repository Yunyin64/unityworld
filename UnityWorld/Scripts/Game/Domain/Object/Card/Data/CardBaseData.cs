using System.Text.Json.Serialization;
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
        /// <summary>卡牌相对锚点的占格形状</summary>
        public List<BackpackCell> Shape { get; set; } = [];
        /// <summary>配置中的扫描发动标记</summary>
        public bool ScanActivatable { get; set; }
        /// <summary>配置的每轮使用次数上限</summary>
        public int MaxUsesPerRound { get; set; } = 1;
        /// <summary>配置的拼点生成标记</summary>
        public bool? ProducesContest { get; set; }
        /// <summary>卡定义的迁移状态</summary>
        public string CompatibilityStatus { get; set; } = "Migrated";
        /// <summary>卡定义的迁移说明</summary>
        public string CompatibilityReason { get; set; } = "";
        // Main 由 Keywords 得出，卡实例持久化时也不单独写一份。
        /// <summary>按关键词列表确定的唯一主词</summary>
        [JsonIgnore]
        public CardMainKeyword MainKeyword => CardKeywordRoles.RequireMain(Keywords);
        /// <summary>主关键词是否属于扫描发动类</summary>
        public bool HasScanOpportunity => MainKeyword != CardMainKeyword.Passive;
        /// <summary>卡牌是否有主动发动机会</summary>
        public bool HasActiveAbility => HasScanOpportunity;
        // 旧事件调用方可读取此值；法宝现在只在背包末格发动。
        /// <summary>事件发动能力标记，当前为否</summary>
        public bool HasEventOpportunity => false;

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

        /// <summary>灵元消耗（key=元素类型，value=数量）</summary>
        public Dictionary<ElementType, int> ManaCost { get; set; } = new();

        // ── 标签 ──────────────────────────────────────────────

        /// <summary>
        /// CardDefine 配置的卡牌标签列表
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>关键词列表（如 Passive 等，决定卡牌运行模式）</summary>
        public List<string> Keywords { get; set; } = [];


        /// <summary>复制卡牌基础数据及灵元、形状、标签和词表</summary>
        public CardBaseData Clone()
        {
            var copy = (CardBaseData)MemberwiseClone();
            copy.ManaCost = new Dictionary<ElementType, int>(ManaCost);
            copy.Shape = [.. Shape];
            copy.Tags = new List<string>(Tags);
            copy.Keywords = new List<string>(Keywords);
            return copy;
        }
        /// <summary>通过领域数据接口复制卡牌基础数据</summary>
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
        /// <summary>父卡 Id（招式卡 → 绑定的装备卡 Id，-1 表示无父卡）</summary>
        public int ParentCardId { get; set; } = -1;
        /// <summary>世界卡的基础数据</summary>
        public virtual CardBaseData BaseData => CardMgr.Instance.DataSystem.GetData(Id);

        /// <summary>独立的世界布局快照，编辑候选位置不接触管理器里的原实例。</summary>
        public Card CreateDataSnapshot() => new SnapshotCard(this);
        private sealed class SnapshotCard : Card
        {
            /// <summary>卡牌快照私有的基础数据</summary>
            private readonly CardBaseData _data;
            /// <summary>快照中的基础数据</summary>
            public override CardBaseData BaseData => _data;
            /// <summary>复制来源卡标识、属性和基础数据创建独立快照</summary>
            public SnapshotCard(Card source)
            {
                Id = source.Id; DefineId = source.DefineId; DisplayName = source.DisplayName;
                ParentCardId = source.ParentCardId; Stats = source.Stats.Snapshot();
                _data = source.BaseData.Clone();
            }
        }
        /// <summary>读取卡牌配置的冷却时间</summary>
        public float GetCooldown() => BaseData.Cooldown;
        /// <summary>读取卡牌体量</summary>
        public int GetSize() => BaseData.Size;

        /// <summary>读取各元素所需的灵元数量</summary>
        public Dictionary<ElementType, int> GetManaCost() => BaseData.ManaCost;
        /// <summary>读取卡牌弹药容量上限</summary>
        public virtual int GetAmountMax() => BaseData.AmountMax;
        /// <summary>读取当前消耗堆叠数</summary>
        public int GetStack() => BaseData.Stack;
        /// <summary>读取消耗堆叠上限</summary>
        public int GetStackMax() => BaseData.StackMax;
        
        /// <summary>读取卡牌关键词列表</summary>
        public List<string> GetKeywords() => BaseData.Keywords;

        /// <summary>读取卡牌标签列表</summary>
        public List<string> GetTags() => BaseData.Tags;
        
        /// <summary>检查卡牌是否含指定关键词</summary>
        public bool HasKeyword(string keyword) => BaseData.Keywords.Contains(keyword);
        /// <summary>由配置的五行费用读取本卡单一出手元素。</summary>
        public ElementType GetElementType() => ElementType.FromManaCost(GetManaCost());
    }
    
}
