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

        public void GainCard(NpcCardData data, string cardDefineId)
        {
            var card = CardMgr.Instance.InstantiateFromDefine(cardDefineId);
            data.AllCardIds.Add(card.Id);
            data.AllCards.Add(card);
        }

        public override void OnTick(Npc npc, float deltaTime)
        {
            // TODO: 卡牌系统逻辑
        }
    }

    public partial class Npc
    {
        public void GainCard(string cardDefineId) => NpcMgr.Instance.CardSystem.GainCard(CardData, cardDefineId);
    }
}