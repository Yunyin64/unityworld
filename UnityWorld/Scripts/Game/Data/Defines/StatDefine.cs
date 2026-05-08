using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    public enum StatBaseType
    {
        Derived,   // 派生属性：通过公式计算得出，依赖其他
        Primary,   // 基础属性：直接定义的数值属性
    }
    /// <summary>
    /// 属性定义：预定义的数值属性配置
    /// 与 Flag 的区别：Stat 是预定义的数值属性，Flag 是随意命名的状态标记
    /// </summary>
    public class StatDefine : DefineBase
    {
        /// <summary>归属 Object 类型（如 "Npc" / "Tile" / "Global"）</summary>
        public string Type { get; set; } = "Npc";
        
        /// <summary>归属 Object 类型（如 "Npc" / "Tile" / "Global"）</summary>
        public StatBaseType BaseType { get; set; } = StatBaseType.Derived;

        /// <summary>默认基础值</summary>
        public float DefaultValue { get; set; } = 0f;
        public string ExtraBase { get; set; } = "";

        /// <summary>可选的全局下限（null 表示无限制）</summary>
        public float? MinValue { get; set; } = null;

        /// <summary>可选的全局上限（null 表示无限制）</summary>
        public float? MaxValue { get; set; } = null;

        /// <summary>显示格式标识（如 "Integer" / "Float2" / "Percent"）</summary>
        public string DisplayFormat { get; set; } = "Integer";

        /// <summary>UI 分类（如 "生命" / "社会"）</summary>
        public string Category { get; set; } = "";

        /// <summary>描述</summary>
        public string Description { get; set; } = "";

        /// <summary>是否隐藏</summary>
        public bool IsHidden { get; set; } = false;
    }
}
