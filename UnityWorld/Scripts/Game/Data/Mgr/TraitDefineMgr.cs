using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 特质定义数据管理器
    /// 负责加载 Traits.json 并提供特质定义查询
    /// </summary>
    public class TraitDefineMgr : IDataMgrBase<TraitDefine>
    {
        public static TraitDefineMgr? Instance { get; private set; }

        private Dictionary<string, TraitDefine> _traits = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public TraitDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[TraitDefineMgr] 警告：找不到 {filePath}，特质库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<TraitDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _traits = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[TraitDefineMgr] 加载完成：{_traits.Count} 个特质定义");
        }

        /// <summary>获取特质定义，不存在返回 null</summary>
        public TraitDefine? Get(string id)
            => _traits.TryGetValue(id, out var t) ? t : null;

        /// <summary>获取所有特质定义</summary>
        public IEnumerable<TraitDefine> GetAll() => _traits.Values;

        /// <summary>特质 id 是否存在</summary>
        public bool Contains(string id) => _traits.ContainsKey(id);
    }
}
