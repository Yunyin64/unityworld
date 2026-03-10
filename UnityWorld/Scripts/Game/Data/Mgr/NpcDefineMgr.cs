using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 定义数据管理器
    /// 负责加载 NpcDefines.json 并提供 NpcDefine 查询
    /// </summary>
    public class NpcDefineMgr : IDataMgrBase<NpcDefine>
    {
        public static NpcDefineMgr? Instance { get; private set; }

        private Dictionary<string, NpcDefine> _defines = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _filePath;

        public NpcDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[NpcDefineMgr] 警告：找不到 {filePath}，NPC定义库为空");
                return;
            }

            var list = JsonSerializer.Deserialize<List<NpcDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];

            _defines = list.ToDictionary(d => d.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[NpcDefineMgr] 加载完成：{_defines.Count} 个 NPC 定义");
        }

        /// <summary>根据 DefineId 获取定义，不存在返回 null</summary>
        public NpcDefine? Get(string defineId)
            => _defines.TryGetValue(defineId, out var d) ? d : null;

        /// <summary>获取所有 NPC 定义</summary>
        public IEnumerable<NpcDefine> GetAll() => _defines.Values;

        /// <summary>是否存在指定 DefineId</summary>
        public bool Contains(string defineId) => _defines.ContainsKey(defineId);

        /// <summary>从已加载定义中随机获取一个</summary>
        public NpcDefine? GetRandom(Random rng)
        {
            if (_defines.Count == 0) return null;
            var values = _defines.Values.ToList();
            return values[rng.Next(values.Count)];
        }




    }
}
