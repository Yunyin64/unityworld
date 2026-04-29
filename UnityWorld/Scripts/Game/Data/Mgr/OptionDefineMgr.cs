using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 选项定义数据管理器
    /// 负责加载 OptionDefines.json
    /// </summary>
    public class OptionDefineMgr : IDataMgrBase<OptionDefine>
    {
        // ── 单例 ─────────────────────────────────────────────
        public static OptionDefineMgr? Instance { get; private set; }

        // ── 内部数据 ──────────────────────────────────────────
        private Dictionary<string, OptionDefine> _options = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        // ── 构造 ──────────────────────────────────────────────
        public OptionDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance  = this;
        }

        // ── IDataMgrBase ─────────────────────────────────────

        /// <summary>加载 JSON 文件</summary>
        public void Load() => Load(_filePath);

        /// <summary>加载指定路径的 JSON 文件</summary>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogMgr.Warn("[OptionDefineMgr] 找不到 {0}，选项库为空", filePath);
                return;
            }
            var list = JsonSerializer.Deserialize<List<OptionDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _options = list.ToDictionary(o => o.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg("[OptionDefineMgr] 加载完成：{0} 个选项定义", _options.Count);
        }

        /// <summary>通过 ID 查询选项定义，不存在返回 null</summary>
        public OptionDefine? Get(string id)
            => _options.TryGetValue(id, out var o) ? o : null;

        /// <summary>获取所有选项定义</summary>
        public IEnumerable<OptionDefine> GetAll() => _options.Values;

        /// <summary>选项 ID 是否存在</summary>
        public bool Contains(string id) => _options.ContainsKey(id);
        public IEnumerable<OptionDefine> Query(Func<OptionDefine, bool> predicate) => _options.Values.Where(predicate);
    }
}
