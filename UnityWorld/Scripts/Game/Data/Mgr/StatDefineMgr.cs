using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 属性定义数据管理器
    /// 负责加载 Data/Stat/ 文件夹下所有 JSON 文件并提供 StatDefine 查询
    /// 每个 JSON 文件按 Type 分类存放（如 NpcStat.json / TileStat.json）
    /// </summary>
    public class StatDefineMgr : IDataMgrBase<StatDefine>
    {
        public static StatDefineMgr Instance { get; private set; }

        private Dictionary<string, StatDefine> _defines = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _folderPath;

        /// <summary>
        /// 构造函数，接收 Stat 定义所在的文件夹路径（如 Data/Stat）
        /// </summary>
        public StatDefineMgr(string folderPath)
        {
            _folderPath = folderPath;
            Instance = this;
        }

        /// <summary>加载文件夹下所有 JSON 文件</summary>
        public void Load() => LoadFolder(_folderPath);

        /// <summary>兼容接口：按单文件加载（内部仍走文件夹逻辑）</summary>
        public void Load(string filePath) => LoadFolder(filePath);

        /// <summary>
        /// 遍历指定文件夹下所有 *.json 文件，合并加载到 _defines
        /// </summary>
        private void LoadFolder(string folderPath)
        {
            _defines.Clear();

            if (!Directory.Exists(folderPath))
            {
                LogMgr.Warn("[StatDefineMgr] 警告：找不到文件夹 {0}，Stat定义库为空", folderPath);
                return;
            }

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            if (jsonFiles.Length == 0)
            {
                LogMgr.Warn("[StatDefineMgr] 警告：文件夹 {0} 下没有 JSON 文件", folderPath);
                return;
            }

            foreach (var file in jsonFiles)
            {
                var text = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    LogMgr.Dbg("[StatDefineMgr] 跳过空文件：{0}", Path.GetFileName(file));
                    continue;
                }

                var list = JsonSerializer.Deserialize<List<StatDefine>>(text, _jsonOpts);
                if (list == null || list.Count == 0)
                {
                    LogMgr.Dbg("[StatDefineMgr] 文件无有效数据：{0}", Path.GetFileName(file));
                    continue;
                }

                foreach (var define in list)
                {
                    if (_defines.ContainsKey(define.ID))
                    {
                        LogMgr.Warn("[StatDefineMgr] 重复 ID '{0}'（文件 {1}），已跳过",
                            define.ID, Path.GetFileName(file));
                        continue;
                    }
                    _defines[define.ID] = define;
                }

                LogMgr.Dbg("[StatDefineMgr] 已加载 {0}：{1} 条", Path.GetFileName(file), list.Count);
            }

            LogMgr.Dbg("[StatDefineMgr] 加载完成：共 {0} 个 Stat 定义（来自 {1} 个文件）",
                _defines.Count, jsonFiles.Length);
        }

        /// <summary>根据 StatId 获取定义，不存在返回 null</summary>
        public StatDefine? Get(string statId)
            => _defines.TryGetValue(statId, out var d) ? d : null;

        /// <summary>获取所有 Stat 定义</summary>
        public IEnumerable<StatDefine> GetAll() => _defines.Values;

        /// <summary>是否存在指定 StatId</summary>
        public bool Contains(string statId) => _defines.ContainsKey(statId);
        public IEnumerable<StatDefine> Query(Func<StatDefine, bool> predicate) => _defines.Values.Where(predicate);

        /// <summary>按 Object 类型过滤 StatDefine</summary>
        public IEnumerable<StatDefine> GetByType(string type)
            => _defines.Values.Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        /// <summary>日志输出</summary>
        public void Log()
        {

        }
    }
}