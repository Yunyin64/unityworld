using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Action（效果动作）模板定义
    /// 包含参数化定义，用于自动/半自动组卡时的强度计算与Lua代码生成
    /// </summary>
    public class ActionDefine : DefineBase
    {
        /// <summary>描述模板，用{ParamName}插值</summary>
        [JsonPropertyName("Desc")]
        public string Desc { get; set; } = "";

        /// <summary>对应的API函数名</summary>
        [JsonPropertyName("FuncName")]
        public string FuncName { get; set; } = "";

        /// <summary>冲突Tag列表</summary>
        [JsonPropertyName("ConflictTags")]
        public List<string> ConflictTags { get; set; } = [];

        /// <summary>权重（随机选取时使用）</summary>
        [JsonPropertyName("Weight")]
        public float Weight { get; set; } = 1;

        /// <summary>参数定义列表</summary>
        [JsonPropertyName("ParamDefs")]
        public List<APIParamDef> ParamDefs { get; set; } = [];

        /// <summary>Lua调用模板，用{ParamName}占位</summary>
        [JsonPropertyName("LuaTemplate")]
        public string LuaTemplate { get; set; } = "";
    }

}
