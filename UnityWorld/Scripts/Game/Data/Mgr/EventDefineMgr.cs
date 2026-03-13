using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 事件定义管理器：加载 EventDefines.json，提供事件元数据查询。
    /// 策划和玩家可通过 JSON 扩展新事件类型。
    /// </summary>
    public class EventDefineMgr : IDataMgrBase<EventDefine>
    {
        /// <summary>单例访问入口</summary>
        public static EventDefineMgr? Instance { get; private set; }

        private Dictionary<string, EventDefine> _defines = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        private readonly string _filePath;

        /// <summary>
        /// 构造 EventDefineMgr。
        /// </summary>
        /// <param name="filePath">EventDefines.json 的文件路径</param>
        public EventDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance = this;
        }

        /// <summary>从构造时指定的路径加载事件定义</summary>
        public void Load() => Load(_filePath);

        /// <summary>从指定路径加载事件定义</summary>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogMgr.Warn("[EventDefineMgr] 找不到 {0}，事件定义库为空", filePath);
                return;
            }

            var list = JsonSerializer.Deserialize<List<EventDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];

            _defines = list.ToDictionary(d => d.ID, System.StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg("[EventDefineMgr] 加载完成：{0} 个事件定义", _defines.Count);
        }

        /// <summary>根据事件 ID 获取定义，不存在返回 null</summary>
        public EventDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取所有事件定义</summary>
        public IEnumerable<EventDefine> GetAll() => _defines.Values;

        /// <summary>是否存在指定事件 ID</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
    }
}
