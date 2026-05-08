# DomainData 编写规范

## 命名

- **类名**：`{宿主前缀}{领域名}Data`，PascalCase
- **文件名**：与类名完全一致（`{Name}Data.cs`）
- **namespace**：`UnityWorld.Game.Domain`
- **属性名**：PascalCase（`public float LifespanMax { get; set; }`）
- **字段名**（struct 内部公有字段除外）：`_camelCase`
- **访问器方法名**：`Get{PropertyName}()`，返回值类型与源属性一致

## 注释

- Data 类必须有 `<summary>` XML 注释描述业务含义
- 每个 public 属性必须有 `<summary>` 注释
- 辅助 struct 及其字段也需要注释
- 使用 `// ── 分组名 ────────────────────────────────────` 对字段进行业务分组

## 字段默认值

| 类型 | 默认值写法 |
|---|---|
| `string` | `""` |
| `int` | `0` |
| `float` | `0f` |
| `bool` | `false` 或 `true`（按业务含义） |
| `List<T>` | `new()` 或 `[]` |
| 其他 Data 类型 | `new()` |
| 自定义 struct | `{StructName}.Zero`（如果定义了 Zero 工厂） |
| 可空引用 | 默认为 `null`，用 `?` 标注 |

## Log() 方法

Log() 方法体留空或只写 TODO 注释。后续由 **DomainData Log 技能** 统一补全。

```csharp
public void Log()
{
    // TODO: 由 DomainData Log 技能补全
}
```

## using 引用

- 始终引用 `UnityWorld.Core`（`LogMgr` 在此命名空间）
- 如果字段用到了 Data 层枚举（如 `PracticePath`、`BaseElementType`），添加 `using UnityWorld.Game.Data;`
- 如果字段用到了集合类型，添加 `using System.Collections.Generic;`
- 如果字段用到了 LINQ，添加 `using System.Linq;`
- using 排序：System 系列在前，项目命名空间在后

## partial class 访问器规则

- 访问器的职责是**简化外部调用路径**，让使用者不必了解 Data 的嵌套结构
- **必须为 Data 类中每一个 public 字段/属性都生成对应的访问器**，不要遗漏
- 基本值类型/字符串字段 → `Get{Name}()` 方法
- 子 Data 类型字段 → 属性级别的 getter（如 `public NpcGongFaData GongFa => CultivationData.GongFa;`）
- struct 类型字段 → 如果 struct 有多个子字段，为每一个子字段也生成独立的 getter
- 枚举类型字段 → 同样生成 `Get{Name}()` 方法