using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 社会角色定义（从 SocialRoles.json 加载）
    /// 策划可在 SocialRoles.json 中新增/修改角色
    /// </summary>
    public class SocialRoleDefine:DefineBase
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
