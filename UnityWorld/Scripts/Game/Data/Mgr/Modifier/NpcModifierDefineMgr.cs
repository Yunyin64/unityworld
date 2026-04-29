using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 修正定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="NpcModifierDefine"/> 并提供查询。
    /// </summary>
    public class NpcModifierDefineMgr : IDataMgrBase<NpcModifierDefine>
    {
        public static NpcModifierDefineMgr? Instance { get; private set; }

        private Dictionary<string, NpcModifierDefine> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public NpcModifierDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        /// <summary>从默认路径加载</summary>
        public void Load() => Load(_filePath);

        /// <summary>从指定路径加载</summary>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[NpcModifierDefineMgr] 警告：找不到 {filePath}，NPC修正定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<NpcModifierDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[NpcModifierDefineMgr] 加载完成：{_defines.Count} 个NPC修正定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public NpcModifierDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取全部定义</summary>
        public IEnumerable<NpcModifierDefine> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<NpcModifierDefine> Query(Func<NpcModifierDefine, bool> predicate) => _defines.Values.Where(predicate);
    }
}
