using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 卡牌/能力数据：管理 NPC 持有的卡牌集合
    /// </summary>
    public class NpcCardData : IDomainDataBase
    {
        public List<Card> AllCards { get; set; } = [];

        public NpcCardData Clone()
        {
            var copy = (NpcCardData)MemberwiseClone();
            copy.AllCards = new List<Card>(AllCards);
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }

    }
    
    public partial class Npc
    {
        public List<Card> GetAllCards() => CardData.AllCards;
    }
}