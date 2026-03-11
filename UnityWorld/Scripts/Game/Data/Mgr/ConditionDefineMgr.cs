using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Condition 定义数据管理器
    /// 负责加载 ConditionDefines.json 并提供查询
    /// </summary>
    public class ConditionDefineMgr : IDataMgrBase<ConditionDefine>
    {
        public static ConditionDefineMgr? Instance { get; private set; }

        private Dictionary<string, ConditionDefine> _conditions = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public ConditionDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ConditionDefineMgr] 警告：找不到 {filePath}，Condition库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<ConditionDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _conditions = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[ConditionDefineMgr] 加载完成：{_conditions.Count} 个Condition定义");
        }

        public ConditionDefine? Get(string id)
            => _conditions.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<ConditionDefine> GetAll() => _conditions.Values;

        public bool Contains(string id) => _conditions.ContainsKey(id);
    }
}
