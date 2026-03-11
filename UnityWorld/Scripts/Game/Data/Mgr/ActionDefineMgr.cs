using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Action 定义数据管理器
    /// 负责加载 ActionDefines.json 并提供查询
    /// </summary>
    public class ActionDefineMgr : IDataMgrBase<ActionDefine>
    {
        public static ActionDefineMgr? Instance { get; private set; }

        private Dictionary<string, ActionDefine> _actions = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public ActionDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ActionDefineMgr] 警告：找不到 {filePath}，Action库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<ActionDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _actions = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[ActionDefineMgr] 加载完成：{_actions.Count} 个Action定义");
        }

        public ActionDefine? Get(string id)
            => _actions.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<ActionDefine> GetAll() => _actions.Values;

        public bool Contains(string id) => _actions.ContainsKey(id);
    }
}
