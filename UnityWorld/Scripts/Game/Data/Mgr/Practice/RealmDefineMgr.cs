using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 境界定义数据管理器
    /// 负责加载 RealmDefines.json 并提供境界定义查询
    /// </summary>
    public class RealmDefineMgr : IDataMgrBase<RealmDefine>
    {
        public static RealmDefineMgr Instance { get; private set; }

        private Dictionary<string, RealmDefine> _defines = [];
        private List<RealmDefine> _list = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _filePath;

        public RealmDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[RealmDefineMgr] 警告：找不到 {filePath}，境界库为空");
                return;
            }

            var list = JsonSerializer.Deserialize<List<RealmDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];

            _list = list;
            _defines = list.ToDictionary(d => d.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[RealmDefineMgr] 加载完成：{_defines.Count} 个境界定义");
        }

        /// <summary>根据 ID 获取境界定义，不存在返回 null</summary>
        public RealmDefine Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取所有境界定义</summary>
        public IEnumerable<RealmDefine> GetAll() => _list;

        /// <summary>指定 ID 是否存在</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<RealmDefine> Query(Func<RealmDefine, bool> predicate) => _defines.Values.Where(predicate);

        /// <summary>获取指定道途的所有境界（按 Level 排序）</summary>
        public IEnumerable<RealmDefine> GetByPath(PracticePath path)
            => _list.Where(d => d.Type == path).OrderBy(d => d.Level);

        /// <summary>获取指定道途和等级的境界</summary>
        public RealmDefine? GetByPathAndLevel(PracticePath path, int level)
            => _list.FirstOrDefault(d => d.Type == path && d.Level == level);

        public void Log()
        {
            
        }
    }
}
