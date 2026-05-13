using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 装备基本形态定义（如"剑"、"炉"、"刀"）。
    /// 只描述骨架基础数值，具体战斗效果走 Card 逻辑。
    /// 字段带 Base 后缀，表示模板值；运行时 Equip 实例去掉 Base 后缀为最终生效值。
    /// </summary>
    public class EquipDefine : DefineBase
    {
        /// <summary>对应卡牌 Size（短剑=1, 长剑=2...），表示此装备属性作用于哪个 Size 的卡牌</summary>
        [JsonPropertyName("Size")]
        public int Size { get; set; } = 1;

        /// <summary>攻击基础值（非最终值，最终值由运行时机制计算）</summary>
        [JsonPropertyName("AttackBase")]
        public int AttackBase { get; set; } = 0;

        /// <summary>防御基础值（非最终值，最终值由运行时机制计算）</summary>
        [JsonPropertyName("DefendBase")]
        public int DefendBase { get; set; } = 0;

        /// <summary>速度基础值（非最终值，最终值由运行时机制计算）</summary>
        [JsonPropertyName("SpeedBase")]
        public float SpeedBase { get; set; } = 0;

        /// <summary>数量/耐久基础值（非最终值，最终值由运行时机制计算）</summary>
        [JsonPropertyName("AmountBase")]
        public int AmountBase { get; set; } = 1;

        /// <summary>附带招式卡 ID 列表基础值（引用 CardDefine.ID，非最终值）</summary>
        [JsonPropertyName("FormListBase")]
        public List<string> FormListBase { get; set; } = [];
    }
}
