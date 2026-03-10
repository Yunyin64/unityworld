using System.Text.Json.Serialization;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Trait 修正器数据（JSON 内嵌结构）
    /// </summary>
    public class TraitModifierData
    {
        [JsonPropertyName("statId")]
        public string StatId { get; set; } = "";

        /// <summary>"flat"（固定值）或 "percent"（百分比）</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "flat";

        [JsonPropertyName("value")]
        public float Value { get; set; }
    }

    /// <summary>
    /// Trait（特质）定义（从 Traits.json 加载）
    /// 策划可在 Traits.json 中新增/修改特质及其属性加成
    /// </summary>
    public class TraitDefine:DefineBase
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>特质携带的属性修正（可为空，表示纯描述性特质）</summary>
        [JsonPropertyName("statModifiers")]
        public List<TraitModifierData> StatModifiers { get; set; } = [];

        /// <summary>是否有属性修正效果</summary>
        [JsonIgnore]
        public bool HasModifiers => StatModifiers.Count > 0;

        /// <summary>将 JSON 数据转换为游戏内 StatModifier 列表（带 sourceId）</summary>
        public IEnumerable<(string statId, StatModifier modifier)> BuildModifiers()
        {
            string sourceId = $"trait:{ID}";
            foreach (var m in StatModifiers)
            {
                var mod = m.Type == "percent"
                    ? StatModifier.Percent(m.Value, sourceId)
                    : StatModifier.Flat(m.Value, sourceId);
                yield return (m.StatId, mod);
            }
        }
    }
}
