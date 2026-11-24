using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 卡牌/能力数据：管理 NPC 持有的卡牌集合
    /// </summary>
    public class NpcCardData : IDomainDataBase
    {
        /// <summary>角色持有的卡牌编号</summary>
        public List<int> AllCardIds { get; set; } = new();
        /// <summary>角色实际持有的卡牌实例</summary>
        public List<Card> AllCards { get; set; } = new();

        // 持有关系与卡牌分组标记仍归这里；空间参数和卡位归 NpcBackpackData。
        /// <summary>选中上场的卡 Id，摆放坐标另外记录在背包数据中。</summary>
        public List<int> Field { get; set; } = new();
        /// <summary>明确选中的候补，不包含其他闲置持有卡。</summary>
        public List<int> Reserve { get; set; } = new();
        /// <summary>既有法宝装备标记。</summary>
        public List<int> EquippedFaBao { get; set; } = new();
        /// <summary>选中的主功法卡实例。</summary>
        public int? MainGongFaCardId { get; set; }
        /// <summary>主功法布局等级。</summary>
        public int MainGongFaLevel { get; set; } = 1;

        /// <summary>创建空的角色卡牌数据</summary>
        public NpcCardData()
        {
            
        }
        /// <summary>复制持卡实例及全部出战选择</summary>
        public NpcCardData Clone()
        {
            var copy = (NpcCardData)MemberwiseClone();
            copy.AllCardIds = new List<int>(AllCardIds);
            copy.AllCards = AllCards.Select(c => c.CreateDataSnapshot()).ToList();
            copy.Field = [.. Field]; copy.Reserve = [.. Reserve]; copy.EquippedFaBao = [.. EquippedFaBao];
            return copy;
        }
        /// <summary>通过领域数据接口复制持卡数据</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        /// <summary>仅复制集合和选择标记，装配候选继续引用原持有卡，不修改卡对象。</summary>
        public NpcCardData CloneSelection()
        {
            var copy = (NpcCardData)MemberwiseClone();
            copy.AllCardIds = [.. AllCardIds]; copy.AllCards = [.. AllCards];
            copy.Field = [.. Field]; copy.Reserve = [.. Reserve]; copy.EquippedFaBao = [.. EquippedFaBao];
            return copy;
        }

        /// <summary>只复制本场选中的卡及标记，闲置卡不进入战斗实例集合。</summary>
        public NpcCardData CloneSelectedCards()
        {
            var selected = Field.Concat(Reserve).ToHashSet();
            var copy = CloneSelection();
            copy.AllCardIds = AllCardIds.Where(selected.Contains).ToList();
            copy.AllCards = AllCards.Where(card => selected.Contains(card.Id)).Select(card => card.CreateDataSnapshot()).ToList();
            // 父装备可以在包外提供已放置的招式，战斗保留装备标记但不复制闲置卡实体。
            return copy;
        }

        // ── 日志 ────────────────────────────────────

        /// <summary>记录持有卡数量和场上、候补卡编号</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("│  AllCards: {0}", AllCards.Count);
            LogMgr.Instance.Dbg("│  Field: [{0}]", string.Join(", ", Field));
            LogMgr.Instance.Dbg("│  Reserve: [{0}]", string.Join(", ", Reserve));
        }

    }
    
    public partial class Npc
    {
        /// <summary>读取角色持有的卡牌列表</summary>
        public List<Card> GetAllCards() => CardData.AllCards;
        /// <summary>读取角色持有的卡牌编号列表</summary>
        public List<int> GetAllCardIds() => CardData.AllCardIds;

    }
}
