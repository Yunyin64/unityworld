using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Tag 定义（从 TagDefines.json 加载）
    /// Tag 是系统中一切节点的语义描述，是生成匹配的基础单元
    /// </summary>
    public class TagDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";
    }
}
