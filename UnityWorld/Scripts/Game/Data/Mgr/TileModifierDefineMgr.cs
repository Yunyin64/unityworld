using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 元气修正定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="TileModifierDefine"/> 并提供查询。
    /// </summary>
    public class TileModifierDefineMgr : IDataMgrBase<TileModifierDefine>
    {
        public static TileModifierDefineMgr Instance { get; private set; }

        private Dictionary<string, TileModifierDefine> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public TileModifierDefineMgr(string filePath)
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
                LogMgr.Dbg($"[TileModifierDefineMgr] 警告：找不到 {filePath}，修正定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<TileModifierDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg($"[TileModifierDefineMgr] 加载完成：{_defines.Count} 个元气修正定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public TileModifierDefine? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取全部定义</summary>
        public IEnumerable<TileModifierDefine> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
        public IEnumerable<TileModifierDefine> Query(Func<TileModifierDefine, bool> predicate) => _defines.Values.Where(predicate);

        public void Log()
        {
             
        }
    }
}
