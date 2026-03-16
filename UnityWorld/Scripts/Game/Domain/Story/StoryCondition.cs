using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 故事条件：判断某个 Story 或 Option 是否可以触发/出现
    /// 多个 Condition 之间为 AND 关系，全部满足才通过
    /// </summary>
    public class StoryCondition
    {
        /// <summary>判断目标类型（NpcStat / NpcTag / AuraElement / WorldTime / Relation）</summary>
        [JsonPropertyName("targetType")]
        public StoryConditionTargetType TargetType { get; set; }

        /// <summary>
        /// 判断的字段名
        /// - NpcStat：StatId 字符串（如 "age_accumulated"）
        /// - NpcTag：Tag 字符串（如 "火"）
        /// - NpcTrait：TraitId 字符串
        /// - AuraElement：元素名（如 "Huo"）
        /// - WorldTime：忽略此字段，直接比较 CurrentTime
        /// - Relation：对方 NpcId 字符串
        /// </summary>
        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; } = "";

        /// <summary>比较运算符</summary>
        [JsonPropertyName("operator")]
        public StoryConditionOperator Operator { get; set; }

        /// <summary>比较值（字符串形式，运行时按 TargetType 解析为对应类型）</summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        /// <summary>
        /// 对指定上下文求值，返回此条件是否满足
        /// </summary>
        public bool Evaluate(StoryContext ctx)
        {
            try
            {
                return TargetType switch
                {
                    StoryConditionTargetType.WorldTime    => EvaluateWorldTime(ctx),
                    StoryConditionTargetType.NpcStat      => EvaluateNpcStat(ctx),
                    StoryConditionTargetType.NpcTag       => EvaluateNpcTag(ctx),
                    StoryConditionTargetType.NpcTrait     => EvaluateNpcTrait(ctx),
                    StoryConditionTargetType.AuraElement  => EvaluateAuraElement(ctx),
                    StoryConditionTargetType.Relation     => EvaluateRelation(ctx),
                    _ => false,
                };
            }
            catch (Exception e)
            {
                LogMgr.Warn("[StoryCondition] Evaluate 异常 TargetType={0} Field={1}: {2}", TargetType, FieldName, e.Message);
                return false;
            }
        }

        // ── 各类型求值 ─────────────────────────────────────────

        private bool EvaluateWorldTime(StoryContext ctx)
        {
            if (!float.TryParse(Value, out float threshold)) return false;
            return CompareFloat(ctx.CurrentTime, threshold);
        }

        private bool EvaluateNpcStat(StoryContext ctx)
        {
            if (ctx.Subject is not Npc npc) return false;
            float statVal = npc.Stats.Get(FieldName);
            if (!float.TryParse(Value, out float threshold)) return false;
            return CompareFloat(statVal, threshold);
        }

        private bool EvaluateNpcTag(StoryContext ctx)
        {
            // TODO: 待 NpcSystemTag 实现后接入，当前返回 false
            LogMgr.Dbg("[StoryCondition] NpcTag 条件暂未实现，跳过");
            return false;
        }

        private bool EvaluateNpcTrait(StoryContext ctx)
        {
            if (ctx.Subject is not Npc npc) return false;
            var traitId = new TraitId(FieldName);
            bool hasTrait = NpcMgr.Instance?.Traits?.HasTrait(npc.Id, traitId) ?? false;
            return Operator == StoryConditionOperator.Contains ? hasTrait
                 : Operator == StoryConditionOperator.NotContains ? !hasTrait
                 : false;
        }

        private bool EvaluateAuraElement(StoryContext ctx)
        {
            // TODO: 待接入 AuraDaoMgr 查询五行浓度，当前返回 false
            LogMgr.Dbg("[StoryCondition] AuraElement 条件暂未实现，跳过");
            return false;
        }

        private bool EvaluateRelation(StoryContext ctx)
        {
            // TODO: 待关系系统实现后接入，当前返回 false
            LogMgr.Dbg("[StoryCondition] Relation 条件暂未实现，跳过");
            return false;
        }

        // ── 通用比较 ──────────────────────────────────────────

        private bool CompareFloat(float actual, float threshold) => Operator switch
        {
            StoryConditionOperator.GreaterThan        => actual > threshold,
            StoryConditionOperator.LessThan           => actual < threshold,
            StoryConditionOperator.Equal              => MathF.Abs(actual - threshold) < 0.001f,
            StoryConditionOperator.NotEqual           => MathF.Abs(actual - threshold) >= 0.001f,
            StoryConditionOperator.GreaterThanOrEqual => actual >= threshold,
            StoryConditionOperator.LessThanOrEqual    => actual <= threshold,
            _ => false,
        };
    }
}
