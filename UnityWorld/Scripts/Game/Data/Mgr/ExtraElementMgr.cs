using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Card 模板定义数据管理器
    /// 负责加载 CardDefines.json 并提供查询
    /// </summary>
    public class ExtraElementMgr : IDataMgrBase<ExtraElementDefine>
    {
        public static ExtraElementMgr? Instance { get; private set; }

        private Dictionary<string, ExtraElementDefine> _elements = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public ExtraElementMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[CardDefineMgr] 警告：找不到 {filePath}，Card模板库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<ExtraElementDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _elements = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[CardDefineMgr] 加载完成：{_elements.Count} 个Card模板定义");
        }

        public ExtraElementDefine? Get(string id)
            => _elements.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<ExtraElementDefine> GetAll() => _elements.Values;

        public bool Contains(string id) => _elements.ContainsKey(id);
    }
}
