using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Tag;

namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Card/Effect 动态生成系统。
    /// 流程：query Tags → TagMgr.Match 筛选节点 → 加权随机组合 Effect → 组合 Card。
    /// 生成结果可导出为 EffectDefine + CardDefine 写入 CardTemp.json。
    /// </summary>
    public class CardSystemGenerate
    {
        private readonly TagMgr _tagMgr;
        private readonly TriggerDefineMgr _triggerMgr;
        private readonly ConditionDefineMgr _conditionMgr;
        private readonly ActionDefineMgr _actionMgr;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public CardSystemGenerate(
            TagMgr tagMgr,
            TriggerDefineMgr triggerMgr,
            ConditionDefineMgr conditionMgr,
            ActionDefineMgr actionMgr)
        {
            _tagMgr = tagMgr;
            _triggerMgr = triggerMgr;
            _conditionMgr = conditionMgr;
            _actionMgr = actionMgr;
        }

        // ── 生成 Effect ───────────────────────────────────────

        /// <summary>
        /// 根据 queryTags 和约束，动态生成一个 EffectData。
        /// 1. 按 Tag 匹配选 Trigger（苛刻 Trigger 释放更多预算）
        /// 2. 按 Tag 匹配选 Condition（可选）
        /// 3. 在剩余预算内，循环按 Tag 匹配贪心选 Action，直到预算耗尽
        /// </summary>
        public EffectData? GenerateEffect(
            List<string> queryTags,
            TagMatchType matchType,
            float matchDegree,
            int scoreLimit,
            Random? rng = null)
        {
            rng ??= Random.Shared;

            // ── 1. 选 Trigger ────────────────────────────────
            var triggerPool = _triggerMgr.GetAll()
                .ToDictionary(t => t.ID, t => t.Tags);
            var triggerWeights = _tagMgr.Match(queryTags, triggerPool, matchType, matchDegree);
            if (triggerWeights.Count == 0)
            {
                Console.WriteLine("[CardSystemGenerate] 没有匹配到任何 Trigger，生成失败");
                return null;
            }
            var triggerId = _tagMgr.WeightedRandom(triggerWeights, rng)!;
            var triggerDefine = _triggerMgr.Get(triggerId)!;

            // ── 2. 选 Condition（可选） ───────────────────────
            var conditionPool = _conditionMgr.GetAll()
                .ToDictionary(c => c.ID, c => c.Tags);
            var conditionWeights = _tagMgr.Match(queryTags, conditionPool, matchType, matchDegree);
            var conditionId = conditionWeights.Count > 0
                ? _tagMgr.WeightedRandom(conditionWeights, rng)
                : null;
            var conditionDefine = conditionId != null ? _conditionMgr.Get(conditionId) : null;

            // ── 3. 计算剩余预算 ───────────────────────────────
            int budget = scoreLimit
                + Math.Abs(triggerDefine.Score)   // trigger 负分释放预算
                + Math.Abs(conditionDefine?.Score ?? 0); // condition 负分释放预算

            // ── 4. 贪心选 Actions ────────────────────────────
            var selectedActionIds = new List<string>();
            var usedActionIds = new HashSet<string>();

            while (budget > 0)
            {
                // 只考虑分数 <= 剩余预算 且 未使用的 Action
                var actionPool = _actionMgr.GetAll()
                    .Where(a => a.Score <= budget && !usedActionIds.Contains(a.ID))
                    .ToDictionary(a => a.ID, a => a.Tags);

                if (actionPool.Count == 0) break;

                var actionWeights = _tagMgr.Match(queryTags, actionPool, matchType, matchDegree);
                if (actionWeights.Count == 0) break;

                var actionId = _tagMgr.WeightedRandom(actionWeights, rng)!;
                var actionDefine = _actionMgr.Get(actionId)!;

                selectedActionIds.Add(actionId);
                usedActionIds.Add(actionId);
                budget -= actionDefine.Score;
            }

            if (selectedActionIds.Count == 0)
            {
                Console.WriteLine("[CardSystemGenerate] 预算内无法选出任何 Action，生成失败");
                return null;
            }

            // ── 5. 组装 EffectData ────────────────────────────
            return BuildEffectData(triggerDefine, conditionDefine, selectedActionIds);
        }

        // ── 生成 Card ─────────────────────────────────────────

        /// <summary>
        /// 根据 queryTags 动态生成一张 CardData（不依赖 CardDefine）。
        /// </summary>
        /// <param name="queryTags">主题 TagBag（决定卡牌风格）</param>
        /// <param name="matchType">节点筛选匹配类型</param>
        /// <param name="matchDegree">匹配严格度 0.0~1.0</param>
        /// <param name="effectCount">需要生成的 Effect 数量</param>
        /// <param name="effectScoreLimit">每个 Effect 的强度分上限</param>
        /// <param name="rarity">卡牌稀有度（仅记录，暂不影响逻辑）</param>
        public CardData? GenerateCard(
            List<string> queryTags,
            TagMatchType matchType = TagMatchType.Weighted,
            float matchDegree = 0.6f,
            int effectCount = 1,
            int effectScoreLimit = 5,
            int rarity = 0,
            Random? rng = null)
        {
            rng ??= Random.Shared;

            var card = new CardData
            {
                Id = CardId.Invalid, // 由 CardMgr 分配
                DefineId = "",       // 动态生成，无 Define
            };

            for (int i = 0; i < effectCount; i++)
            {
                var effect = GenerateEffect(queryTags, matchType, matchDegree, effectScoreLimit, rng);
                if (effect == null) continue;

                card.Effects.Add(effect);
                card.Tags.AddRange(effect.Tags);
            }

            if (card.Effects.Count == 0)
            {
                Console.WriteLine("[CardSystemGenerate] 没有生成任何 Effect，卡牌生成失败");
                return null;
            }

            return card;
        }

        // ── 导出到 CardTemp.json ──────────────────────────────

        /// <summary>
        /// 将一批 CardData 序列化为 EffectDefine + CardDefine，追加写入 CardTemp.json。
        /// 每次调用会追加到现有文件（如文件不存在则新建）。
        /// </summary>
        public void ExportToCardTemp(IEnumerable<CardData> cards, string outputPath)
        {
            // 读现有数据（如果有）
            var existingEffects = new List<EffectDefine>();
            var existingCards = new List<CardDefine>();
            if (File.Exists(outputPath))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<CardTempFile>(
                        File.ReadAllText(outputPath), _jsonOpts);
                    if (existing != null)
                    {
                        existingEffects = existing.Effects ?? [];
                        existingCards   = existing.Cards   ?? [];
                    }
                }
                catch { /* 文件损坏则忽略，重新生成 */ }
            }

            var existingEffectIds = new HashSet<string>(existingEffects.Select(e => e.ID));
            int effectSeq = existingEffects.Count;
            int cardSeq   = existingCards.Count;

            foreach (var card in cards)
            {
                var effectIds = new List<string>();

                foreach (var effect in card.Effects)
                {
                    // 生成一个唯一 ID
                    var effectId = $"gen_effect_{++effectSeq:D4}";
                    while (existingEffectIds.Contains(effectId))
                        effectId = $"gen_effect_{++effectSeq:D4}";

                    existingEffectIds.Add(effectId);

                    existingEffects.Add(new EffectDefine
                    {
                        ID            = effectId,
                        Desc          = $"生成 Effect（{string.Join(",", effect.Tags.Distinct().Take(3))}）",
                        TriggerId     = effect.TriggerId,
                        ConditionId   = effect.ConditionId,
                        ActionIds     = new List<string>(effect.ActionIds),
                        PowerScore    = effect.PowerScore,
                        ComplexityScore = effect.ComplexityScore,
                    });

                    effectIds.Add(effectId);
                }

                var cardId = $"gen_card_{++cardSeq:D4}";
                existingCards.Add(new CardDefine
                {
                    ID        = cardId,
                    Desc      = $"生成卡牌（{string.Join(",", card.Tags.Distinct().Take(3))}）",
                    Rarity    = 0,
                    EffectIds = effectIds,
                    Tags      = card.Tags.Distinct().ToList(),
                });
            }

            var output = new CardTempFile
            {
                Effects = existingEffects,
                Cards   = existingCards,
            };

            File.WriteAllText(outputPath, JsonSerializer.Serialize(output, _jsonOpts));
            Console.WriteLine($"[CardSystemGenerate] 已导出 {existingCards.Count} 张卡牌 到 {outputPath}");
        }

        // ── 内部工具 ─────────────────────────────────────────

        private EffectData BuildEffectData(
            TriggerDefine trigger,
            ConditionDefine? condition,
            List<string> actionIds)
        {
            var effect = new EffectData
            {
                TriggerId   = trigger.ID,
                ConditionId = condition?.ID ?? "",
                ActionIds   = actionIds,
            };

            effect.Tags.AddRange(trigger.Tags);
            if (condition != null) effect.Tags.AddRange(condition.Tags);

            int power = trigger.Score + (condition?.Score ?? 0);
            int complexity = Math.Abs(trigger.Score) + Math.Abs(condition?.Score ?? 0);

            foreach (var actionId in actionIds)
            {
                var a = _actionMgr.Get(actionId);
                if (a == null) continue;
                effect.Tags.AddRange(a.Tags);
                power      += a.Score;
                complexity += Math.Abs(a.Score);
            }

            effect.PowerScore      = power;
            effect.ComplexityScore = complexity;
            return effect;
        }
    }

    /// <summary>CardTemp.json 的文件结构</summary>
    public class CardTempFile
    {
        [System.Text.Json.Serialization.JsonPropertyName("effects")]
        public List<EffectDefine> Effects { get; set; } = [];

        [System.Text.Json.Serialization.JsonPropertyName("cards")]
        public List<CardDefine> Cards { get; set; } = [];
    }
}