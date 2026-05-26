using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Action 定义数据管理器
    /// 负责加载 Data/Action/ 文件夹下所有 JSON 并提供查询
    /// </summary>
    public class ActionDefineMgr : IDataMgrBase<ActionDefine>
    {
        public static ActionDefineMgr Instance { get; private set; }

        private Dictionary<string, ActionDefine> _actions = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _dirPath;

        public ActionDefineMgr(string dirPath)
        {
            _dirPath = dirPath;
            Instance = this;
        }

        public void Load() => LoadFromDirectory(_dirPath);

        public void Load(string dirPath) => LoadFromDirectory(dirPath);

        /// <summary>读取目录下所有 .json 文件，合并到同一字典</summary>
        private void LoadFromDirectory(string dirPath)
        {
            _actions.Clear();

            if (!Directory.Exists(dirPath))
            {
                LogMgr.Dbg($"[ActionDefineMgr] 警告：找不到目录 {dirPath}，Action库为空");
                return;
            }

            var files = Directory.GetFiles(dirPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var list = JsonSerializer.Deserialize<List<ActionDefine>>(
                    File.ReadAllText(file), _jsonOpts) ?? [];
                foreach (var item in list)
                {
                    if (string.IsNullOrEmpty(item.ID)) continue;
                    _actions[item.ID] = item;
                }
                LogMgr.Dbg($"[ActionDefineMgr] 加载 {Path.GetFileName(file)}：{list.Count} 条");
            }

            LogMgr.Dbg($"[ActionDefineMgr] 加载完成：共 {_actions.Count} 个Action定义");
        }

        public ActionDefine? Get(string id)
            => _actions.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<ActionDefine> GetAll() => _actions.Values;

        public bool Contains(string id) => _actions.ContainsKey(id);
        public IEnumerable<ActionDefine> Query(Func<ActionDefine, bool> predicate) => _actions.Values.Where(predicate);
    }
}
