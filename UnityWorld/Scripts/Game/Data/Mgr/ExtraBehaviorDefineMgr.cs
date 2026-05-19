using System.Text.Json;
using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为拓展定义加载器
    /// 实现 IDataMgrBase&lt;ExtraBehaviorDefine&gt;，从 JSON 文件加载行为拓展定义
    /// </summary>
    public class ExtraBehaviorDefineMgr : IDataMgrBase<ExtraBehaviorDefine>
    {
        public static ExtraBehaviorDefineMgr Instance { get; private set; }

        private readonly string _jsonPath;
        private Dictionary<string, ExtraBehaviorDefine> _data = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public ExtraBehaviorDefineMgr(string jsonPath)
        {
            _jsonPath = jsonPath;
            Instance = this;
        }

        public void Load() => Load(_jsonPath);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogMgr.Dbg($"[ExtraBehaviorDefineMgr] 警告：找不到 {filePath}，行为拓展库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<ExtraBehaviorDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _data = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg($"[ExtraBehaviorDefineMgr] 加载完成：{_data.Count} 个行为拓展定义");
        }

        /// <summary>
        /// 日志输出（输出存的数据信息的数量与概括）
        /// </summary>
        public void Log()
        {
             
        }

        public ExtraBehaviorDefine? Get(string id)
            => _data.TryGetValue(id, out var def) ? def : null;

        public IEnumerable<ExtraBehaviorDefine> GetAll() => _data.Values;

        public bool Contains(string id) => _data.ContainsKey(id);
        public IEnumerable<ExtraBehaviorDefine> Query(Func<ExtraBehaviorDefine, bool> predicate) => _data.Values.Where(predicate);
    }
}
