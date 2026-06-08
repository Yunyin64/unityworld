# GameDataMgr 注册 + JSON 模板 + 类型速查

## GameDataMgr 注册

**文件位置**：`Scripts/Game/Data/GameDataMgr.cs`

在构造函数中找到合适的位置，添加一行注册代码：

```csharp
_datamgrs.Add(new {Name}DefineMgr(Path.Combine(dataDir, "{Name}Defines.json")));
```

**放置原则**：
- 如果 JSON 在 `Data/` 根目录下：用 `dataDir` 拼接
- 如果 JSON 在子目录下（如 `Data/Practice/`）：先获取子目录路径再拼接，参照已有的 `practiceDir` 模式
- 注册行按业务分组插入，可加注释分隔线

---

## JSON 数据模板

**文件位置**：`Data/{Name}Defines.json`（或子目录下）

```json
[
  {
    "ID": "example_{name}",
    "DisplayName": "示例{中文名}",
    // ... 其余字段按 Define 中的 JsonPropertyName 对应填入默认值
    // ⚠️ JSON key 必须使用 PascalCase，与 C# 属性名和 JsonPropertyName 完全一致
    // 正确: "TriggerId": "xxx"
    // 错误: "triggerId": "xxx"
  }
]
```

> **重要**：JSON 文件中的 key 名必须与 C# 属性名（PascalCase）完全一致。虽然 DefineMgr 配置了 `PropertyNameCaseInsensitive = true` 可以兼容不同大小写，但为了维护一致性，JSON key 统一使用 PascalCase。

---

## 常见字段类型速查

| 游戏概念 | C# 类型 | JSON 值示例 | 默认值 |
|---|---|---|---|
| 描述文本 | `string` | `"这是一把剑"` | `""` |
| 数值属性 | `int` / `float` | `10` / `1.5` | `0` / `0f` |
| 开关 | `bool` | `true` | `false` |
| ID 引用列表 | `List<string>` | `["id_a","id_b"]` | `new()` |
| 数值列表 | `List<int>` | `[1,2,3]` | `new()` |
| 标签 | `List<string>` | `["火","攻击"]` | `new()` |
| 五行枚举 | `BaseElementType` | `3` | `BaseElementType.Jin` |
| 地形枚举 | `TerrainType` | `5` | 按需 |

## JSON 值类型对照

| C# 默认值 | JSON 值 |
|---|---|
| `""` | `""` |
| `0` / `0f` | `0` / `0.0` |
| `false` | `false` |
| `true` | `true` |
| `new()` (List) | `[]` |
| 枚举值 | 对应的整数值 |