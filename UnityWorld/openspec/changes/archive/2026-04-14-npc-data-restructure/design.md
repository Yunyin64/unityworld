## Context

当前 NPC 数据全部塞在 `NpcBioData` 中，凡间肉身属性与修行属性混杂。已有 `NpcCultivationData` 在 `GamePlay/Practice/` 管理功法槽位，但八大基础修行属性、战斗三维、五行亲和等核心数据缺失。`NpcSystemPractice` 和 `NpcSystemFaction` 是空壳。

需要将 NPC 数据按三层重新划分，每层由对应的 System 管理：

```
NpcBioData           ←→ NpcSystemBio       （凡间肉身）
NpcCultivationData   ←→ NpcSystemPractice  （修行超凡）
NpcFactionData       ←→ NpcSystemFaction   （社会身份，TODO）
```

## Goals / Non-Goals

**Goals:**
- 将 `NpcBioData` 精简为纯凡间属性，移除修行相关字段
- 扩充 `NpcCultivationData` 为完整修行数据容器，含八大基础属性、战斗三维、五行亲和
- 完善 `NpcSystemBio` 和 `NpcSystemPractice` 的注册/查询/Tick 逻辑
- 保持 `CultivationMgr` 与 `NpcSystemPractice` 的职责分离（Mgr 管功法逻辑，System 管 NPC 修行数据）
- 联动更新 `Npc.cs`、`NpcMgr.cs`、`NpcGenerator.cs` 编译通过

**Non-Goals:**
- 不实现 `NpcFactionData` 的具体字段（仅 TODO 空壳）
- 不实现 `AppearanceData` 的具体逻辑（仅 TODO 空类）
- 不实现 `NpcSystemBehavior`（移动行为系统是未来任务）
- 不迁移 `TimeFlowRate` 到 Plane（仅标注 TODO）
- 不改变 `CultivationMgr` 的现有功法槽位逻辑

## Decisions

### 1. 数据文件从 NpcSystemXxx.cs 中拆出，放到 Npc/Data/ 目录

**选择**：`NpcBioData` 和 `NpcCultivationData` 各自独立为 `Npc/Data/NpcBioData.cs` 和 `Npc/Data/NpcCultivationData.cs`，System 文件只包含 System 类。

**原因**：当前 `NpcBioData` 和 `NpcSystemBio` 写在同一个文件里，随着 Data 变复杂（新增 struct），应分离。`Data/` 目录已存在 `NpcTypes.cs`，保持一致。

**备选**：继续混写在 System 文件里 → 拒绝，文件会过长。

### 2. NpcCultivationData 迁移到 Npc/Data/，CultivationMgr 保持原位

**选择**：`NpcCultivationData.cs`（含 `GongFa`）从 `GamePlay/Practice/` 迁移到 `Npc/Data/`。`CultivationMgr` 保留在 `GamePlay/Practice/`。

**原因**：`NpcCultivationData` 是 NPC 实体的运行时数据，逻辑上属于 NPC 领域。`CultivationMgr` 是跨实体的功法业务管理器，保留在 GamePlay 层合理。

**备选**：两者都留原位 → 拒绝，Data 和 NPC 实体距离太远。

### 3. 八大基础属性用 struct 封装

**选择**：`BaseProperty` 为 `struct`，包含 8 个 `int` 字段：QiXue, TiPo, QiGan, LingJi, ShenShi, WuXing, JiYuan, MeiLi。

**原因**：struct 值类型，无 GC 开销，适合高频读取的属性数据。8 个 int 共 32 字节，一个 cache line 放得下。

### 4. 五行亲和用 struct 封装

**选择**：`ElementalAffinity` 为 `struct`，包含 5 个 `int` 字段：Jin, Mu, Shui, Huo, Tu。

**原因**：同上，且命名与已有 `BaseElementType` 枚举保持一致。

### 5. 战斗三维直接平铺在 NpcCultivationData

**选择**：`HpMax`, `MpMax`, `SpMax` 作为 `NpcCultivationData` 的直接字段，不额外封装 struct。

**原因**：只有 3 个字段，封装 struct 过度设计。

### 6. NpcSystemPractice 与 CultivationMgr 的职责划分

**选择**：
- `NpcSystemPractice`：管理每个 NPC 的 `NpcCultivationData` 注册/查询/Tick（寿元推进、基础属性读取）
- `CultivationMgr`：管理功法槽位的业务逻辑（添加功法、修炼进度、节点解锁）

**原因**：NpcSystemPractice 是 NPC 子系统（per-NPC Tick），CultivationMgr 是全局业务管理器。两者通过 `NpcCultivationData` 共享数据。

### 7. 生死字段先放 BioData，标注 TODO

**选择**：`IsAlive`, `DeathTick` 暂放 `NpcBioData`，标注 TODO 未来迁到独立的生死 System。

**原因**：当前没有复杂的生死逻辑需求，但字段需要存在。避免过早抽象。

### 8. 寿元（LifespanMax）放 NpcCultivationData

**选择**：`LifespanMax` 从 BioData 迁到 `NpcCultivationData`。

**原因**：修行者的寿元体系与凡人完全不同。凡人的基础寿命可通过 NpcDefine 模板赋值到 CultivationData.LifespanMax（即使不修行也有寿命上限）。寿元判定逻辑从 BioSystem 迁到 PracticeSystem。

## Risks / Trade-offs

- **[Breaking Change]** `NpcBioData` 字段大幅变更 → 所有使用 BioData 的地方需同步更新。缓解：一次性在 NpcMgr、NpcGenerator、日志方法中全量适配。
- **[CultivationMgr 引用路径变更]** `NpcCultivationData` 迁移后，`CultivationMgr` 的 using 可能需要更新 → 由于同在 `UnityWorld.Game.Domain` namespace，实际无需改动。
- **[凡人寿命]** 凡人 NPC 没有修行数据，但仍需寿命上限 → NpcSystemPractice 为凡人也注册 `NpcCultivationData`（Path=None, LifespanMax=凡人基础值），或由 BioSystem 做 fallback 判定。选择前者，保持数据一致性。