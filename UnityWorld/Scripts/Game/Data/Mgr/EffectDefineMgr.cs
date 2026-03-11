using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Effect 定义数据管理器
    /// 负责加载 EffectDefines.json 并提供查询
    /// </summary>
    public class EffectDefineMgr : IDataMgrBase<EffectDefine>
    {
        public static EffectDefineMgr? Instance { get; private set; }

        private Dictionary<string, EffectDefine> _effects = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public EffectDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[EffectDefineMgr] 警告：找不到 {filePath}，Effect定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<EffectDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _effects = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[EffectDefineMgr] 加载完成：{_effects.Count} 个Effect定义");
        }

        public EffectDefine? Get(string id)
            => _effects.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<EffectDefine> GetAll() => _effects.Values;

        public bool Contains(string id) => _effects.ContainsKey(id);
    }
}
