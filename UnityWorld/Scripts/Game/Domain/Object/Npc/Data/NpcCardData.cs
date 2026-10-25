using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 卡牌/能力数据：管理 NPC 持有的卡牌集合
    /// </summary>
    public class NpcCardData : IDomainDataBase
    {
        public List<int> AllCardIds { get; set; } = new();
        public List<Card> AllCards { get; set; } = new();

        /// <summary>运转池：战斗中直接装载的卡 Id 列表</summary>
        public List<int> Field { get; set; } = new();

        /// <summary>候补池：战斗中需要 Deploy 才能上场的卡 Id 列表</summary>
        public List<int> Reserve { get; set; } = new();

        /// <summary>已装备的法宝卡 Id 列表（纯标记，分配 Field 时消费）</summary>
        public List<int> EquippedFaBao { get; set; } = new();

        /// <summary>唯一主功法卡及其布局等级；未选择时禁止进入背包战斗。</summary>
        public int? MainGongFaCardId { get; set; }
        public int MainGongFaLevel { get; set; } = 1;
        public Dictionary<int, List<BackpackCell>> Placements { get; set; } = new();

        public NpcCardData()
        {
            
        }
        public NpcCardData Clone()
        {
            var copy = (NpcCardData)MemberwiseClone();
            copy.AllCardIds = new List<int>(AllCardIds);
            copy.AllCards = new List<Card>(AllCards);
            copy.Field = new List<int>(Field);
            copy.Reserve = new List<int>(Reserve);
            copy.EquippedFaBao = new List<int>(EquippedFaBao);
            copy.Placements = Placements.ToDictionary(p => p.Key, p => new List<BackpackCell>(p.Value));
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            LogMgr.Instance.Dbg("│  AllCards: {0}", AllCards.Count);
            LogMgr.Instance.Dbg("│  Field: [{0}]", string.Join(", ", Field));
            LogMgr.Instance.Dbg("│  Reserve: [{0}]", string.Join(", ", Reserve));
        }

    }
    
    public partial class Npc
    {
        public List<Card> GetAllCards() => CardData.AllCards;
        public List<int> GetAllCardIds() => CardData.AllCardIds;

        /// <summary>获取运转池卡 Id 列表</summary>
        public List<int> GetFieldIds() => CardData.Field;

        /// <summary>获取候补池卡 Id 列表</summary>
        public List<int> GetReserveIds() => CardData.Reserve;
    }
}
