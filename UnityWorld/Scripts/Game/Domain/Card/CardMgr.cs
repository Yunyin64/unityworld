using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Card 管理器
    /// 负责持有运行时生成的所有 Card 实例，并提供从 Define 实例化的入口
    /// </summary>
    public class CardMgr
    {
        public static CardMgr? Instance { get; private set; }

        private readonly Dictionary<CardId, CardData> _cards = [];
        private int _nextId = 1;

        public CardMgr()
        {
            Instance = this;
        }

        // ── 注册 / 查询 ──────────────────────────────────────

        /// <summary>注册一张已生成的 Card</summary>
        public void Add(CardData card)
        {
            _cards[card.Id] = card;
        }

        /// <summary>获取 Card 实例</summary>
        public CardData? Get(CardId id)
            => _cards.TryGetValue(id, out var c) ? c : null;

        /// <summary>获取所有 Card</summary>
        public IEnumerable<CardData> GetAll() => _cards.Values;

        // ── 从 Define 实例化 ──────────────────────────────────

        /// <summary>
        /// 从 CardDefine 实例化一张 CardData，加入管理并返回。
        /// CardDefine → 遍历 EffectIds → 每个 EffectDefine 构造 EffectData → 拼 TagBag。
        /// </summary>
        public CardData? InstantiateFromDefine(string cardDefineId)
        {
            var cardDefine = CardDefineMgr.Instance?.Get(cardDefineId);
            if (cardDefine == null)
            {
                Console.WriteLine($"[CardMgr] 找不到 CardDefine：{cardDefineId}");
                return null;
            }

            var card = new CardData
            {
                Id = new CardId(_nextId++),
                DefineId = cardDefineId,
            };

            foreach (var effectId in cardDefine.EffectIds)
            {
                var effectData = BuildEffectFromDefine(effectId);
                if (effectData != null)
                {
                    card.Effects.Add(effectData);
                    card.Tags.AddRange(effectData.Tags); // TagBag 自然涌现
                }
            }

            // 若 CardDefine 手配了额外 Tags，追加进来
            card.Tags.AddRange(cardDefine.Tags);

            Add(card);
            return card;
        }

        /// <summary>
        /// 从 EffectDefine 构造一个 EffectData 运行时对象，包含完整 TagBag 和分数。
        /// </summary>
        public EffectData? BuildEffectFromDefine(string effectDefineId)
        {
            var effectDefine = EffectDefineMgr.Instance?.Get(effectDefineId);
            if (effectDefine == null)
            {
                Console.WriteLine($"[CardMgr] 找不到 EffectDefine：{effectDefineId}");
                return null;
            }

            var triggerDefine   = TriggerDefineMgr.Instance?.Get(effectDefine.TriggerId);
            var conditionDefine = string.IsNullOrEmpty(effectDefine.ConditionId)
                ? null
                : ConditionDefineMgr.Instance?.Get(effectDefine.ConditionId);

            var effectData = new EffectData
            {
                TriggerId   = effectDefine.TriggerId,
                ConditionId = effectDefine.ConditionId,
                ActionIds   = new List<string>(effectDefine.ActionIds),
            };

            // ── 聚合 TagBag ──────────────────────────────────
            if (triggerDefine != null)
                effectData.Tags.AddRange(triggerDefine.Tags);
            if (conditionDefine != null)
                effectData.Tags.AddRange(conditionDefine.Tags);

            int powerScore = 0;
            int complexityScore = 0;

            powerScore      += triggerDefine?.Score ?? 0;
            complexityScore += Math.Abs(triggerDefine?.Score ?? 0);
            powerScore      += conditionDefine?.Score ?? 0;
            complexityScore += Math.Abs(conditionDefine?.Score ?? 0);

            foreach (var actionId in effectDefine.ActionIds)
            {
                var actionDefine = ActionDefineMgr.Instance?.Get(actionId);
                if (actionDefine == null) continue;

                effectData.Tags.AddRange(actionDefine.Tags);
                powerScore      += actionDefine.Score;
                complexityScore += Math.Abs(actionDefine.Score);
            }

            effectData.PowerScore      = powerScore;
            effectData.ComplexityScore = complexityScore;

            return effectData;
        }
    }
}