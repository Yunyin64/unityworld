## Context

当前 NPC 修炼状态仅靠 `NpcBioData.CultivationLevel: int` 表示，没有道途、境界、功法等概念。游戏需要 500 个修士 NPC 各自拥有完整的修炼生命周期。

现有架构：
- `NpcBioData` 直接存储 CultivationLevel（底层机制属性，不走 Stat 系统）
- `NpcMgr.Create()` 通过 `NpcDefine.InitCultivationLevelMin/Max` 随机初始化
- `CombatScene` 硬编码 `damage = 10f + CultivationLevel * 5f`
- 数据加载模式统一为 `IDataMgrBase<T>` + `DefineBase` + JSON

## Goals / Non-Goals

**Goals:**
- 建立 `PracticePath` 枚举（9 个核心道途），作为修炼体系的类型标识
- 实现 `RealmDefine` + `RealmDefineMgr`：数据驱动的境界定义系统，每个道途拥有独立境界序列
- 实现 `CultivationDefine` + `CultivationDefineMgr`：功法定义系统，含节点序列（Card/BehaviorCard/Modifier/Story）和核心效果
- 实现 `CultivationMgr`：运行时管理器骨架，管理 NPC 功法持有、修炼进度、节点解锁
- 提供手配 JSON 数据示例（至少覆盖灵修、武修、魂修各一套境界 + 各一本示例功法）

**Non-Goals:**
- 功法生成逻辑（Tag 驱动的功法涌现，未来 CultivationMgr 实现）
- NpcSystemPractice 的 Tick 逻辑（修炼速度计算、元气关联、AI 决策"当前修行功法"）
- 突破机制的运行时逻辑（突破概率、突破事件）
- NPC 创建流程改造（本次不改 NpcMgr.Create，保留 CultivationLevel 兼容）
- CoreEffect 的具体效果实现（仅定义数据结构，不实现效果执行）

## Decisions

### D1：道途用枚举而非 Define
**选择**：`PracticePath` 硬编码为 C# 枚举（9 个值）
**理由**：道途数量已确定（None + 灵/仙/道/武/脉/荒/魂/神/异），不需要数据驱动扩展。枚举更安全、类型检查更强、代码可读性更好。
**替代方案**：string ID + PracticePathDefine.json → 过度设计，增加序列化复杂度

### D2：境界按道途独立序列
**选择**：每个道途有自己独立的境界名称和序列（如武修：锻体→铜皮→铁骨…；灵修：练气→筑基→金丹…），通过 RealmDefine.Type 字段关联道途
**理由**：更有沉浸感，每条道途的境界讲述不同的成长故事
**结构**：RealmDefine 用 Level 字段（1,2,3...）表示同一道途内的境界先后顺序

### D3：功法节点类型用枚举
**选择**：`CultivationPointType` 枚举：Card / BehaviorCard / Modifier / Story
**理由**：四种类型已覆盖当前所有奖励形式。Story 类型用于手配叙事事件（当前生成管线未通）。每个节点通过 RefId 引用对应定义。

### D4：CultivationDefine 同时支持手配和生成
**选择**：手配功法直接写入 `CultivationDefines.json`，生成功法未来由 CultivationMgr 动态创建
**理由**：手配功法是"叙事案例库"（经典传承），生成功法是"Tag 语义涌现"。两者共存，Define 结构统一。

### D5：CultivationMgr 放在 Domain/GamePlay/Practice/
**选择**：`Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs`
**理由**：修炼是跨 NPC 的玩法系统，不是 NPC 子系统。与 AuraDaoMgr 同级，属于 GamePlay 领域。

### D6：数据文件放 Data/Practice/
**选择**：`Data/Practice/RealmDefines.json`、`Data/Practice/CultivationDefines.json`
**理由**：修炼相关数据统一目录，与 Data/Story/、Data/Tag/ 平级

### D7：境界进度独立于功法
**选择**：NPC 有独立的境界进度（存在 CultivationMgr 管理的运行时数据中），修炼功法时同步积累境界进度
**理由**：功法修满不等于突破，但一般情况下一本同级功法的 MaxPoint 足够覆盖对应境界所需进度。这样设计允许多本功法共同推进境界进度。

## Risks / Trade-offs

- **[BREAKING 迁移]** CultivationLevel 被替代后，CombatScene、NpcMgr.Create、NpcAllInfoLog 等处均需适配 → 本次仅搭建新系统骨架，旧字段暂时保留，后续单独迁移
- **[数据量]** 9 个道途 × 多个境界 × 多本功法 = JSON 数据量较大 → 本次只配示例数据，后续用 json-data-extra 技能批量扩展
- **[CoreEffect 未实现]** 功法核心效果的运行时执行逻辑不在本次范围 → CultivationDefine 中 CoreEffect 仅为 JSON 字段占位，具体效果系统后续设计
- **[NpcSystemPractice 未创建]** 修炼的 Tick 驱动逻辑未在本次范围 → CultivationMgr 仅提供数据管理 API，不注册 Tick