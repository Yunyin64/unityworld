# DefineMgr 类模板

**文件位置**：`Scripts/Game/Data/Mgr/{Name}DefineMgr.cs`

```csharp
using System.Text.Json;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// {中文名}定义数据管理器。
    /// 负责从 JSON 文件加载所有 <see cref="{Name}Define"/> 并提供查询。
    /// </summary>
    public class {Name}DefineMgr : IDataMgrBase<{Name}Define>
    {
        public static {Name}DefineMgr? Instance { get; private set; }

        private Dictionary<string, {Name}Define> _defines = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        public {Name}DefineMgr(string filePath)
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
                Console.WriteLine($"[{Name}DefineMgr] 警告：找不到 {filePath}，{中文名}定义库为空");
                return;
            }
            var list = JsonSerializer.Deserialize<List<{Name}Define>>(
                File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[{Name}DefineMgr] 加载完成：{_defines.Count} 个{中文名}定义");
        }

        /// <summary>按 ID 查询，不存在返回 null</summary>
        public {Name}Define? Get(string id)
            => _defines.TryGetValue(id, out var d) ? d : null;

        /// <summary>获取全部定义</summary>
        public IEnumerable<{Name}Define> GetAll() => _defines.Values;

        /// <summary>存在性检查</summary>
        public bool Contains(string id) => _defines.ContainsKey(id);
    }
}
```

## 要点

- DefineMgr 的结构是完全固定的模板，除了类名和日志文案之外不需要任何改动
- `_defines` 字段名保持 `_defines` 不变，便于维护一致性
- 构造函数中 `Instance = this` 实现单例
- Load 方法的异常处理模式：文件不存在时打印警告并返回，不抛异常