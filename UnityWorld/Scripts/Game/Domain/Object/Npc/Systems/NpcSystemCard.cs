using UnityWorld.Core;
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

        /// <summary>
        /// 移除卡牌，同步清理 Field/Reserve 分配
        /// </summary>
        public void RemoveCard(NpcCardData data, Card card)
        {
            data.AllCardIds.Remove(card.Id);
            data.AllCards.Remove(card);
            data.Field.Remove(card.Id);
            data.Reserve.Remove(card.Id);
        }

        /// <summary>
        /// 将卡分配到运转池
        /// </summary>
        public void AssignToField(NpcCardData data, int cardId)
        {
            data.Reserve.Remove(cardId);
            if (!data.Field.Contains(cardId))
                data.Field.Add(cardId);
        }

        /// <summary>
        /// 将卡分配到候补池
        /// </summary>
        public void AssignToReserve(NpcCardData data, int cardId)
        {
            data.Field.Remove(cardId);
            if (!data.Reserve.Contains(cardId))
                data.Reserve.Add(cardId);
        }

        /// <summary>
        /// 取消卡的部署分配
        /// </summary>
        public void UnassignCard(NpcCardData data, int cardId)
        {
            data.Field.Remove(cardId);
            data.Reserve.Remove(cardId);
        }

        /// <summary>
        /// 获取运转池（Field）中卡的总 Size
        /// </summary>
        public int GetFieldSize(NpcCardData data)
        {
            int size = 0;
            foreach (var cardId in data.Field)
            {
                var card = data.AllCards.Find(c => c.Id == cardId);
                if (card != null) size += card.GetSize();
            }
            return size;
        }

        public int GetAllCardSize(NpcCardData data)
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

        /// <summary>
        /// 装备法宝（纯标记，不实例化招式卡）
        /// </summary>
        public void EquipFaBao(NpcCardData data, int fabaoCardId)
        {
            if (!data.EquippedFaBao.Contains(fabaoCardId))
                data.EquippedFaBao.Add(fabaoCardId);
        }

        /// <summary>
        /// 卸装法宝（移除标记）
        /// </summary>
        public void UnequipFaBao(NpcCardData data, int fabaoCardId)
        {
            data.EquippedFaBao.Remove(fabaoCardId);
        }
    }

    public partial class Npc
    {
        public Card GainCard(string cardDefineId) => NpcMgr.Instance.CardSystem.GainCard(CardData, cardDefineId);
        public void GainCard(Card card) => NpcMgr.Instance.CardSystem.GainCard(CardData, card);
        public void RemoveCard(Card card) => NpcMgr.Instance.CardSystem.RemoveCard(CardData, card);
        public int GetAllCardSize() => NpcMgr.Instance.CardSystem.GetAllCardSize(CardData);
        /// <summary>获取运转池 SP 占用</summary>
        public int GetFieldSize() => NpcMgr.Instance.CardSystem.GetFieldSize(CardData);
        /// <summary>将卡分配到运转池</summary>
        public void AssignToField(int cardId) => NpcMgr.Instance.CardSystem.AssignToField(CardData, cardId);
        /// <summary>将卡分配到候补池</summary>
        public void AssignToReserve(int cardId) => NpcMgr.Instance.CardSystem.AssignToReserve(CardData, cardId);
        /// <summary>取消卡的部署分配</summary>
        public void UnassignCard(int cardId) => NpcMgr.Instance.CardSystem.UnassignCard(CardData, cardId);

        /// <summary>装备法宝（纯标记）</summary>
        public void EquipFaBao(int fabaoCardId) => NpcMgr.Instance.CardSystem.EquipFaBao(CardData, fabaoCardId);
        /// <summary>卸装法宝</summary>
        public void UnequipFaBao(int fabaoCardId) => NpcMgr.Instance.CardSystem.UnequipFaBao(CardData, fabaoCardId);

        /// <summary>
        /// 测试用：将所有持有卡牌全部塞入运转池（Field）。
        /// 已装备法宝会附带实例化 FormList 招式卡。
        /// </summary>
        public void AssignAllToField()
        {
            // 先把已有卡全入 Field
            foreach (var cardId in CardData.AllCardIds)
            {
                if (!CardData.Field.Contains(cardId))
                    CardData.Field.Add(cardId);
            }
            CardData.Reserve.Clear();

            // 已装备法宝 → 实例化 FormList 招式卡
            foreach (var fabaoId in CardData.EquippedFaBao)
            {
                var equip = EquipMgr.Instance?.GetById(fabaoId);
                if (equip == null || equip.FormList == null || equip.FormList.Count == 0) continue;

                // 防重复：检查是否已有 ParentCardId = fabaoId 的招式卡
                bool alreadyCreated = CardData.AllCards.Exists(c => c.ParentCardId == fabaoId);
                if (alreadyCreated) continue;

                foreach (var formDefineId in equip.FormList)
                {
                    var formCard = CardMgr.Instance.InstantiateFromDefine(formDefineId);
                    if (formCard == null) continue;
                    formCard.ParentCardId = fabaoId;
                    CardData.AllCardIds.Add(formCard.Id);
                    CardData.AllCards.Add(formCard);
                    CardData.Field.Add(formCard.Id);
                }
            }
        }
    }
}