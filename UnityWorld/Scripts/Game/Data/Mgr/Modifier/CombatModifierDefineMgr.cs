using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 战斗修正定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="CombatNpcModifierDefine"/> 并提供查询。
    /// </summary>
    public class CombatNpcModifierDefineMgr : IDataMgrBase<CombatNpcModifierDefine>
    {
        public static CombatNpcModifierDefineMgr? Instance { get; private set; }

        private Dictionary<string, CombatNpcModifierDefine> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public CombatNpcModifierDefineMgr(string filePath)
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
                Console.WriteLine($"[CombatNpcModifierDefineMgr] 警告：找不到 {filePath}，战斗修正定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<CombatNpcModifierDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[CombatNpcModifierDefineMgr] 加载完成：{_defines.Count} 个战斗修正定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public CombatNpcModifierDefine Get(string id)
        {
            if (_defines.ContainsKey(id))
            {
                return _defines[id];
            }
            else
            {
                LogMgr.Warn($"Define不存在:{id}");
                return null;
            }
        }

        /// <summary>获取全部定义</summary>
        public IEnumerable<CombatNpcModifierDefine> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);

        public void Log()
        {
             
        }

        public IEnumerable<CombatNpcModifierDefine> Query(Func<CombatNpcModifierDefine, bool> predicate) => _defines.Values.Where(predicate);
    }
}
