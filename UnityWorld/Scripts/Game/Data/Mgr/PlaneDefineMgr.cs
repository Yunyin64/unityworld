using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 位面定义数据管理器
    /// 负责加载 PlaneDefines.json 并提供 PlaneDefine 查询
    /// </summary>
    public class PlaneDefineMgr : IDataMgrBase<PlaneDefine>
    {
        public static PlaneDefineMgr? Instance { get; private set; }

        private Dictionary<string, PlaneDefine> _defines = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _filePath;

        public PlaneDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        public void Load() => Load(_filePath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[PlaneDefineMgr] 警告：找不到 {filePath}，位面定义库为空");
                return;
            }

            var list = JsonSerializer.Deserialize<List<PlaneDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];

            _defines = list.ToDictionary(d => d.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[PlaneDefineMgr] 加载完成：{_defines.Count} 个位面定义");
        }

        /// <summary>根据 DefineId 获取位面定义，不存在返回 null</summary>
        public PlaneDefine? Get(string defineId)
            => _defines.TryGetValue(defineId, out var d) ? d : null;

        /// <summary>获取所有位面定义</summary>
        public IEnumerable<PlaneDefine> GetAll() => _defines.Values;

        /// <summary>是否存在指定 DefineId</summary>
        public bool Contains(string defineId) => _defines.ContainsKey(defineId);

        /// <summary>按位面类型筛选</summary>
        public IEnumerable<PlaneDefine> GetByKind(Domain.PlaneTypes.PlaneKind kind)
            => _defines.Values.Where(d => d.Kind == kind);
    }
}
