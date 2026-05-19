using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 地标定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="LandMarkDefine"/> 并提供查询。
    /// </summary>
    public class LandMarkDefineMgr : IDataMgrBase<LandMarkDefine>
    {
        public static LandMarkDefineMgr Instance { get; private set; }

        private Dictionary<string, LandMarkDefine> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public LandMarkDefineMgr(string filePath)
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
                LogMgr.Dbg($"[LandMarkDefineMgr] 警告：找不到 {filePath}，地标定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<LandMarkDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg($"[LandMarkDefineMgr] 加载完成：{_defines.Count} 个地标定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public LandMarkDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取全部定义</summary>
        public IEnumerable<LandMarkDefine> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<LandMarkDefine> Query(Func<LandMarkDefine, bool> predicate) => _defines.Values.Where(predicate);

        public void Log()
        {
            
        }
    }
}
