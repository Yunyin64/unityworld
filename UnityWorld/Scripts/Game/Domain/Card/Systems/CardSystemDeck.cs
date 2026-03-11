using UnityWorld.Game.Domain.Tag;

namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Deck（卡组）组合系统
    /// 复用 TagBagMatcher 逻辑，从 Card 池中按主题匹配组合卡组
    /// </summary>
    public class CardSystemDeck
    {
        private readonly TagMgr _tagMgr;
        private readonly CardMgr _cardMgr;

        public CardSystemDeck(TagMgr tagMgr, CardMgr cardMgr)
        {
            _tagMgr = tagMgr;
            _cardMgr = cardMgr;
        }

        /// <summary>
        /// 根据主题 TagBag 从卡池中匹配组合一套卡组
        /// </summary>
        /// <param name="themeTags">卡组主题 TagBag（重复表示浓度）</param>
        /// <param name="matchType">匹配类型</param>
        /// <param name="matchDegree">匹配度</param>
        /// <param name="deckSize">卡组张数</param>
        public List<CardData> BuildDeck(
            List<string> themeTags,
            TagMatchType matchType,
            float matchDegree,
            int deckSize)
        {
            // TODO: 实现 Deck 组合逻辑
            // 复用 TagMgr.Match，输入已有 CardData 的 TagBag
            return [];
        }
    }
}
