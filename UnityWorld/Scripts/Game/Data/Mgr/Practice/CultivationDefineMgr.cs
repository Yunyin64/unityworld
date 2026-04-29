using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 功法定义数据管理器
    /// 负责加载 CultivationDefines.json 并提供功法定义查询
    /// </summary>
    public class CultivationDefineMgr : IDataMgrBase<CultivationDefine>
    {
        public static CultivationDefineMgr Instance { get; private set; }

        private Dictionary<string, CultivationDefine> _defines = [];
        private List<CultivationDefine> _list = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _filePath;

        public CultivationDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[CultivationDefineMgr] 警告：找不到 {filePath}，功法库为空");
                return;
            }

            var list = JsonSerializer.Deserialize<List<CultivationDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];

            _list = list;
            _defines = list.ToDictionary(d => d.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[CultivationDefineMgr] 加载完成：{_defines.Count} 个功法定义");
        }

        /// <summary>根据 ID 获取功法定义，不存在返回 null</summary>
        public CultivationDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取所有功法定义</summary>
        public IEnumerable<CultivationDefine> GetAll() => _list;

        /// <summary>指定 ID 是否存在</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<CultivationDefine> Query(Func<CultivationDefine, bool> predicate) => _defines.Values.Where(predicate);

        /// <summary>获取指定道途的所有功法</summary>
        public IEnumerable<CultivationDefine> GetByPath(PracticePath path)
            => _list.Where(d => d.PathType == path);

        /// <summary>获取指定道途和境界的功法</summary>
        public IEnumerable<CultivationDefine> GetByPathAndRealm(PracticePath path, int realmLevel)
            => _list.Where(d => d.PathType == path && (d.RealmLevel == 0 || d.RealmLevel == realmLevel));

        public void Log()
        {
            
        }
    }
}
