using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Trigger 定义数据管理器
    /// 负责加载 TriggerDefines.json 并提供查询
    /// </summary>
    public class TriggerDefineMgr : IDataMgrBase<TriggerDefine>
    {
        public static TriggerDefineMgr? Instance { get; private set; }

        private Dictionary<string, TriggerDefine> _triggers = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public TriggerDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[TriggerDefineMgr] 警告：找不到 {filePath}，Trigger库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<TriggerDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _triggers = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[TriggerDefineMgr] 加载完成：{_triggers.Count} 个Trigger定义");
        }

        public TriggerDefine? Get(string id)
            => _triggers.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<TriggerDefine> GetAll() => _triggers.Values;

        public bool Contains(string id) => _triggers.ContainsKey(id);
    }
}
