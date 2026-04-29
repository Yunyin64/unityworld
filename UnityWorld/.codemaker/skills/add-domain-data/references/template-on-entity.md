# 场景 A：Data 直接挂载在实体上

当 Data 直接作为实体的一级数据块（如 `NpcBioData` 之于 `Npc`）：

```csharp
using UnityWorld.Core;
// 按需添加其他 using

namespace UnityWorld.Game.Domain
{
    // ── 辅助结构体（如果有）────────────────────────────────

    /// <summary>
    /// {辅助结构体描述}
    /// </summary>
    public struct {StructName}
    {
        /// <summary>{字段说明}</summary>
        public {Type} {FieldName};

        /// <summary>创建全零/默认值的实例</summary>
        public static {StructName} Zero => new {StructName}
        {
            {FieldName} = {默认值},
            // ...
        };
    }

    // ── Data 类本体 ────────────────────────────────────

    /// <summary>
    /// {业务描述}
    /// </summary>
    public class {Name}Data : IDomainDataBase
    {
        // ── {分组名} ────────────────────────────────────

        /// <summary>{字段说明}</summary>
        public {Type} {PropertyName} { get; set; } = {默认值};

        // ... 其余字段按业务分组排列

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }

    // ── 便捷访问器 ────────────────────────────────────

    public partial class {Entity}
    {
        /// <summary>{访问器说明}</summary>
        public {ReturnType} {GetterName}() => {DataProperty}.{Field};

        // ... 其余访问器
    }
}
```