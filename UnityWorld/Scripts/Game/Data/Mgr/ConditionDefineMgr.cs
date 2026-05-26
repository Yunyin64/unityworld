using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Condition 定义数据管理器
    /// 负责加载 Data/Condition/ 文件夹下所有 JSON 并提供查询
    /// </summary>
    public class ConditionDefineMgr : IDataMgrBase<ConditionDefine>
    {
        public static ConditionDefineMgr Instance { get; private set; }

        private Dictionary<string, ConditionDefine> _conditions = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _dirPath;

        public ConditionDefineMgr(string dirPath)
        {
            _dirPath = dirPath;
            Instance = this;
        }

        public void Load() => LoadFromDirectory(_dirPath);

        public void Load(string dirPath) => LoadFromDirectory(dirPath);

        /// <summary>读取目录下所有 .json 文件，合并到同一字典</summary>
        private void LoadFromDirectory(string dirPath)
        {
            _conditions.Clear();

            if (!Directory.Exists(dirPath))
            {
                LogMgr.Dbg($"[ConditionDefineMgr] 警告：找不到目录 {dirPath}，Condition库为空");
                return;
            }

            var files = Directory.GetFiles(dirPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var list = JsonSerializer.Deserialize<List<ConditionDefine>>(
                    File.ReadAllText(file), _jsonOpts) ?? [];
                foreach (var item in list)
                {
                    if (string.IsNullOrEmpty(item.ID)) continue;
                    _conditions[item.ID] = item;
                }
                LogMgr.Dbg($"[ConditionDefineMgr] 加载 {Path.GetFileName(file)}：{list.Count} 条");
            }

            LogMgr.Dbg($"[ConditionDefineMgr] 加载完成：共 {_conditions.Count} 个Condition定义");
        }

        public ConditionDefine? Get(string id)
            => _conditions.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<ConditionDefine> GetAll() => _conditions.Values;

        public bool Contains(string id) => _conditions.ContainsKey(id);
        public IEnumerable<ConditionDefine> Query(Func<ConditionDefine, bool> predicate) => _conditions.Values.Where(predicate);
    }
}
