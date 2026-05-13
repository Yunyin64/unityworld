using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC卡牌/能力系统
    /// </summary>
    public class NpcSystemCard : NpcSystemBase<NpcCardData>
    {
        protected override Dictionary<int, NpcCardData> _dataTable { get; set ; } = new();

        /// <summary>
        /// NPC 诞生时：创建空卡牌数据并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            Register(npc, new NpcCardData());
        }

        public Card GainCard(NpcCardData data, string cardDefineId)
        {
            var card = CardMgr.Instance.InstantiateFromDefine(cardDefineId);
            
            return GainCard(data, card);
        }
        
        public Card GainCard(NpcCardData data, Card card)
        {
            data.AllCardIds.Add(card.Id);
            data.AllCards.Add(card);
            return card;
        }

        public int  GetAllCardSize(NpcCardData data)
        {
            int size = 0;
            foreach (var card in data.AllCards)
            {
                size += card.GetSize(); 
            }
            return size;
        }

        public override void OnTick(Npc npc, float deltaTime)
        {
            // TODO: 卡牌系统逻辑
        }
    }

    public partial class Npc
    {
        public Card GainCard(string cardDefineId) => NpcMgr.Instance.CardSystem.GainCard(CardData, cardDefineId);
        public void GainCard(Card card) => NpcMgr.Instance.CardSystem.GainCard(CardData, card);
        public int GetAllCardSize() => NpcMgr.Instance.CardSystem.GetAllCardSize(CardData);
    }
}