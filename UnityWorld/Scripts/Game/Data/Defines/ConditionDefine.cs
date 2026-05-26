using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Condition（触发条件检查）模板定义
    /// 包含参数化定义，Score为负数代表条件越苛刻（用于自动组卡强度计算）
    /// </summary>
    public class ConditionDefine : DefineBase
    {
        /// <summary>描述模板，用{ParamName}插值</summary>
        [JsonPropertyName("Desc")]
        public string Desc { get; set; } = "";

        /// <summary>冲突Tag列表</summary>
        [JsonPropertyName("ConflictTags")]
        public List<string> ConflictTags { get; set; } = [];

        /// <summary>权重（随机选取时使用）</summary>
        [JsonPropertyName("Weight")]
        public float Weight { get; set; } = 1;

        /// <summary>参数定义列表</summary>
        [JsonPropertyName("ParamDefs")]
        public List<APIParamDef> ParamDefs { get; set; } = [];

        /// <summary>Lua条件表达式模板，用{ParamName}占位</summary>
        [JsonPropertyName("LuaTemplate")]
        public string LuaTemplate { get; set; } = "";
    }

}
