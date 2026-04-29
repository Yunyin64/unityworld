using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为卡静态定义数据管理器
    /// 负责加载 BehaviorCardDefines.json
    /// </summary>
    public class BehaviorCardDataMgr : IDataMgrBase<BehaviorCardDefine>
    {
        // ── 单例 ─────────────────────────────────────────────
        public static BehaviorCardDataMgr Instance { get; private set; }

        // ── 内部数据 ──────────────────────────────────────────
        private Dictionary<string, BehaviorCardDefine> _cards = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        // ── 构造 ──────────────────────────────────────────────
        public BehaviorCardDataMgr(string filePath)
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
                LogMgr.Warn("[BehaviorCardDataMgr] 找不到 {0}，行为卡库为空", filePath);
                return;
            }
            var list = JsonSerializer.Deserialize<List<BehaviorCardDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _cards = list.ToDictionary(c => c.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg("[BehaviorCardDataMgr] 加载完成：{0} 个行为卡定义", _cards.Count);
        }

        /// <summary>通过 ID 查询行为卡定义，不存在返回 null</summary>
        public BehaviorCardDefine Get(string id)
            => _cards.TryGetValue(id, out var c) ? c : null;

        /// <summary>获取所有行为卡定义</summary>
        public IEnumerable<BehaviorCardDefine> GetAll() => _cards.Values;

        /// <summary>行为卡 ID 是否存在</summary>
        public bool Contains(string id) => _cards.ContainsKey(id);
        public IEnumerable<BehaviorCardDefine> Query(Func<BehaviorCardDefine, bool> predicate) => _cards.Values.Where(predicate);

        public void Log()
        {
            LogMgr.Dbg("[BehaviorCardDataMgr] 行为卡定义数量：{0}", _cards.Count);
        }
    }
}