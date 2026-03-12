using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 额外元素类型定义
    /// 除了基础元素之外的拓展元素类型
    /// </summary>
    public class ExtraElementDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

    }
}
