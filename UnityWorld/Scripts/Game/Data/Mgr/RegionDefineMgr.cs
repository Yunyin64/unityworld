using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 区域定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="RegionDefine"/> 并提供查询。
    /// </summary>
    public class RegionDefineMgr : IDataMgrBase<RegionDefine>
    {
        public static RegionDefineMgr Instance { get; private set; }

        private Dictionary<string, RegionDefine> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public RegionDefineMgr(string filePath)
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
                LogMgr.Dbg($"[RegionDefineMgr] 警告：找不到 {filePath}，区域定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<RegionDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg($"[RegionDefineMgr] 加载完成：{_defines.Count} 个区域定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public RegionDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取全部定义</summary>
        public IEnumerable<RegionDefine> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<RegionDefine> Query(Func<RegionDefine, bool> predicate) => _defines.Values.Where(predicate);

        public void Log()
        {
            
        }
    }
}
