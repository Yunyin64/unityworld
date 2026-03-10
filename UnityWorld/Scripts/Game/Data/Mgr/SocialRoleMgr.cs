using System.Text.Json;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 社会角色数据管理器
    /// 负责加载 SocialRoles.json 并提供角色定义查询
    /// </summary>
    public class SocialRoleMgr : IDataMgrBase<SocialRoleDefine>
    {
        public static SocialRoleMgr? Instance { get; private set; } 
        private Dictionary<string, SocialRoleDefine> _roles = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private string _filePath;

        public SocialRoleMgr(string filePath)
        {
            _filePath = filePath;
        }

        public void Load()
        {
            Load(_filePath);
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[SocialRoleMgr] 警告：找不到 {filePath}，角色库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<SocialRoleDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _roles = list.ToDictionary(r => r.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[SocialRoleMgr] 加载完成：{_roles.Count} 个社会角色定义");
        }

        /// <summary>获取角色定义，不存在返回 null</summary>
        public SocialRoleDefine? Get(string id)
            => _roles.TryGetValue(id, out var r) ? r : null;

        /// <summary>获取所有角色定义</summary>
        public IEnumerable<SocialRoleDefine> GetAll() => _roles.Values;

        /// <summary>角色 id 是否存在</summary>
        public bool Contains(string id) => _roles.ContainsKey(id);

    }
}
