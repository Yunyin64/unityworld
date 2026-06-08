# 完整示例：LandMarkDefine

以 LandMark（地标）为例，展示最终产出的 4 个文件。

## LandMarkDefine.cs

```csharp
using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 地标静态配置模板。
    /// 描述一种地标（自然奇观或后天建筑）的属性、生成条件及对地块的元气影响。
    /// 运行时由 <see cref="LandMarkMgr"/> 实例化为 <see cref="LandMark"/>。
    /// </summary>
    public class LandMarkDefine : DefineBase
    {
        // ── 分类 ──────────────────────────────────────────

        /// <summary>是否为自然奇观</summary>
        [JsonPropertyName("IsNatural")]
        public bool IsNatural { get; set; } = true;

        /// <summary>全图唯一（同一 DefineId 最多生成 1 个实例）</summary>
        [JsonPropertyName("IsSingleton")]
        public bool IsSingleton { get; set; } = false;

        // ── 生成规则 ──────────────────────────────────────

        /// <summary>散布生成权重</summary>
        [JsonPropertyName("SpawnWeight")]
        public float SpawnWeight { get; set; } = 1f;

        /// <summary>允许生成的基础地形列表（空 = 不限）</summary>
        [JsonPropertyName("PlacementTerrains")]
        public List<TerrainType> PlacementTerrains { get; set; } = new();

        /// <summary>要求区域持有的拓展地形标签 ID</summary>
        [JsonPropertyName("PlacementExtraTerrains")]
        public List<string> PlacementExtraTerrains { get; set; } = new();

        // ── 元气影响 ──────────────────────────────────────

        /// <summary>对所在地块施加的元气修正 Define ID 列表</summary>
        [JsonPropertyName("ModifierDefineIds")]
        public List<string> ModifierDefineIds { get; set; } = new();

        // ── 叙事 ──────────────────────────────────────────

        /// <summary>叙事标签</summary>
        [JsonPropertyName("Tags")]
        public List<string> Tags { get; set; } = new();
    }
}
```

## LandMarkDefineMgr.cs

```csharp
using System.Text.Json;

namespace UnityWorld.Game.Data
{
    public class LandMarkDefineMgr : IDataMgrBase<LandMarkDefine>
    {
        public static LandMarkDefineMgr? Instance { get; private set; }
        private Dictionary<string, LandMarkDefine> _defines = new();
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        private readonly string _filePath;

        public LandMarkDefineMgr(string filePath) { _filePath = filePath; Instance = this; }
        public void Load() => Load(_filePath);
        public void Load(string filePath)
        {
            if (!File.Exists(filePath)) { Console.WriteLine($"[LandMarkDefineMgr] 找不到 {filePath}"); return; }
            var list = JsonSerializer.Deserialize<List<LandMarkDefine>>(File.ReadAllText(filePath), _jsonOpts) ?? new();
            _defines = list.ToDictionary(t => t.ID, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[LandMarkDefineMgr] 加载完成：{_defines.Count} 个地标定义");
        }
        public LandMarkDefine? Get(string id) => _defines.TryGetValue(id, out var d) ? d : null;
        public IEnumerable<LandMarkDefine> GetAll() => _defines.Values;
        public bool Contains(string id) => _defines.ContainsKey(id);
    }
}
```

## GameDataMgr.cs 注册行

```csharp
_datamgrs.Add(new LandMarkDefineMgr(Path.Combine(dataDir, "LandMarkDefines.json")));
```

## LandMarkDefines.json

```json
[
  {
    "ID": "natural_leyline_eye",
    "DisplayName": "灵脉眼",
    "IsNatural": true,
    "IsSingleton": false,
    "SpawnWeight": 2.0,
    "PlacementTerrains": [],
    "PlacementExtraTerrains": [],
    "ModifierDefineIds": ["aura_leyline_spring"],
    "Tags": ["圣地", "灵脉", "修炼"]
  }
]
```