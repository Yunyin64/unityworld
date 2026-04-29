# Define 类模板

**文件位置**：`Scripts/Game/Data/Defines/{SubDir?}{Name}Define.cs`

```csharp
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// {业务描述}。
    /// 由 <see cref="{Name}DefineMgr"/> 从 JSON 加载，只读静态配置。
    /// </summary>
    public class {Name}Define : DefineBase
    {
        // ── 字段（按业务分组，每组用区域分隔线） ──────────────

        /// <summary>{字段说明}</summary>
        [JsonPropertyName("{PropertyName}")]
        public {Type} {PropertyName} { get; set; } = {默认值};

        // ... 其余字段
    }
}
```

## 要点

- namespace 固定为 `UnityWorld.Game.Data`
- 开头 using `UnityWorld.Game.Core`
- 继承 `DefineBase`，不需要重复声明 `ID` 和 `DisplayName`（基类已有）
- 每个公开属性都加 `[JsonPropertyName("...")]` 特性和 `<summary>` 注释
- **`JsonPropertyName` 的值必须与 C# 属性名完全一致（PascalCase）**，例如属性名是 `TriggerId`，则写 `[JsonPropertyName("TriggerId")]`，绝对不允许写成 `"triggerId"` 等 camelCase 形式
- 使用 `using System.Text.Json.Serialization;`，不要用 Newtonsoft
- 字段较多时用 `// ── 分类名 ──────────────────────────────────` 分隔
- 集合类型默认值用 `new()` 或 `[]`，字符串默认值用 `""`
- 如果字段引用了枚举（如 `BaseElementType`、`TerrainType`），需要额外引入对应 using