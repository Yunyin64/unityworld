using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Tag 定义数据管理器
    /// 负责加载 TagDefines.json 并提供 Tag 定义查询
    /// </summary>
    public class TagDefineMgr : IDataMgrBase<TagDefine>
    {
        public static TagDefineMgr? Instance { get; private set; }

        private Dictionary<string, TagDefine> _tags = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public TagDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[TagDefineMgr] 警告：找不到 {filePath}，Tag库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<TagDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _tags = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[TagDefineMgr] 加载完成：{_tags.Count} 个Tag定义");
        }

        public TagDefine? Get(string id)
            => _tags.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<TagDefine> GetAll() => _tags.Values;

        public bool Contains(string id) => _tags.ContainsKey(id);
    }
}
