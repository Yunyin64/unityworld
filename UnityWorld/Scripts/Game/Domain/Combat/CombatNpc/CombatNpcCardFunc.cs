using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        public List<CombatCard> CardDeck { get; set; } = [];

        private Queue<(ComabtCardDeckChangeType,CombatCard,ComabtCardDisplaceType)> _changes = new();

        public void RemoveCombatCard(CombatCard card)
        {
            _changes.Enqueue((ComabtCardDeckChangeType.Remove,card,ComabtCardDisplaceType.None));
        }
        public void AddCombatCard(CombatCard card)
        {
            _changes.Enqueue((ComabtCardDeckChangeType.Add,card,ComabtCardDisplaceType.None));
        }
        
        public void DisplaceCombatCard(CombatCard card,ComabtCardDisplaceType toPlace)
        {
            _changes.Enqueue((ComabtCardDeckChangeType.Displace,card,toPlace));
        }

        public  void DealCardDeckChange()
        {
            while (_changes.Count > 0)
            {
                var (changeType, card, displaceType) = _changes.Dequeue();
                switch (changeType)
                {
                    case ComabtCardDeckChangeType.Add:
                        CardDeck.Add(card);
                        card.Owner = this;
                        LogMgr.Dbg("[CombatNpc:{0}] 卡组新增卡牌: {1}", card.DisplayName, card.Id);
                        break;

                    case ComabtCardDeckChangeType.Remove:
                        CardDeck.Remove(card);
                        LogMgr.Dbg("[CombatNpc:{0}] 卡组移除卡牌: {1}", card.DisplayName, card.Id);
                        break;

                    case ComabtCardDeckChangeType.Displace:
                        CardDeck.Remove(card);
                        switch (displaceType)
                        {
                            case ComabtCardDisplaceType.First:
                                CardDeck.Insert(0, card);
                                break;
                            case ComabtCardDisplaceType.Last:
                                CardDeck.Add(card);
                                break;
                            case ComabtCardDisplaceType.Random:
                                var idx = CardDeck.Count > 0
                                    ? Soul.Random(0, CardDeck.Count + 1)
                                    : 0;
                                CardDeck.Insert(idx, card);
                                break;
                            default:
                                CardDeck.Add(card);
                                break;
                        }
                        LogMgr.Dbg("[CombatNpc:{0}] 卡牌位移: {1} -> {2}", card.DisplayName, card.Id, displaceType);
                        break;
                }
            }
        }
    }
}