using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 卡牌/能力数据：管理 NPC 持有的卡牌集合
    /// </summary>
    public class NpcCardData : IDomainDataBase
    {
        public List<Card> AllCards { get; set; } = [];
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