# DomainData 完整示例

## 示例 1：NpcBioData（直接挂在 Npc 实体上）

```csharp
using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    public class NpcBioData : IDomainDataBase
    {
        // ── 身份核心 ─────────────────────────────
        /// <summary>名字</summary>
        public string Name { get; set; } = "";
        /// <summary>性别</summary>
        public NpcTypes.Gender Gender { get; set; }
        /// <summary>种族</summary>
        public NpcTypes.NpcType NpcType { get; set; }

        // ── 生命周期 ────────────────────────────────────
        /// <summary>当前年龄积累</summary>
        public float AgeAccumulated { get; set; } = 0f;
        /// <summary>出生时的世界 Tick</summary>
        public int BirthTick { get; set; } = 0;

        // ── 体质 ────────────────────────────────────
        /// <summary>基础移动速度</summary>
        public float BaseMoveSpeed { get; set; } = 3f;

        // ── 外观 ────────────────────────────────────
        /// <summary>外观配置引用</summary>
        public NpcAppearanceData AppearanceData { get; set; } = new();

        // ── 生死状态 ────────────────────────────────────
        /// <summary>是否存活</summary>
        public bool IsAlive { get; set; } = true;
        /// <summary>死亡时的 Tick</summary>
        public int DeathTick { get; set; }

        public void Log() { /* TODO */ }
    }

    public partial class Npc
    {
        public string GetName() => BioData.Name;
        public NpcTypes.Gender GetGender() => BioData.Gender;
        public NpcTypes.NpcType GetNpcType() => BioData.NpcType;
        public NpcAppearanceData AppearanceData => BioData.AppearanceData;
    }
}
```

---

## 示例 2：NpcAppearanceData（嵌套在 NpcBioData 中）

```csharp
using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    public class NpcAppearanceData : IDomainDataBase
    {
        /// <summary>身高</summary>
        public float Height;
        public void Log() { /* TODO */ }
    }

    public partial class Npc
    {
        // AppearanceData 属性已在 NpcBioData.cs 定义
        public float GetHeight() => AppearanceData.Height;
    }
}
```

---

## 示例 3：带辅助 struct 的 NpcCultivationData

```csharp
using System.Collections.Generic;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    public struct BaseProperty
    {
        public int QiXue;
        public int TiPo;
        // ... 其余字段
        public static BaseProperty Zero => new BaseProperty { QiXue = 0, TiPo = 0 };
    }

    public class NpcCultivationData : IDomainDataBase
    {
        /// <summary>道途类型</summary>
        public PracticePath Path { get; set; } = PracticePath.None;
        /// <summary>修行基础属性</summary>
        public BaseProperty Properties { get; set; } = BaseProperty.Zero;
        public NpcGongFaData GongFa { get; set; } = new();
        public NpcPraticeData PracticeData { get; set; } = new();
        public void Log() { /* TODO */ }
    }

    public partial class Npc
    {
        public PracticePath GetPath() => CultivationData.Path;
        public int GetQiXue() => CultivationData.Properties.QiXue;
        public NpcGongFaData GongFa => CultivationData.GongFa;
        public NpcPraticeData PracticeData => CultivationData.PracticeData;
    }
}
```