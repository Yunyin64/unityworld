## Why

当前 `NpcBioData` 将凡间肉身属性、修行属性、移动属性混杂在一起，导致：
1. 凡人 NPC 的数据结构中包含修行字段（如 `CultivationLevel`），语义不清
2. `NpcCultivationData` 已存在于 `GamePlay/Practice/` 但与 `NpcBioData` 字段重叠
3. `NpcSystemPractice` 和 `NpcSystemFaction` 是空壳，无法承载修行和社会数据
4. 缺少八大基础修行属性、战斗三维、五行亲和等核心修行数据

需要按「凡间肉身 / 修行超凡 / 社会身份」三层重新划分 NPC 数据。

## What Changes

- **BREAKING** 重构 `NpcBioData`：移除修行相关字段（`CultivationLevel`, `LifespanMax`, `TimeFlowRate`），新增 `BirthTick`, `AppearanceId`, `IsAlive`, `DeathTick` 等凡间属性
- **BREAKING** 扩充 `NpcCultivationData`：从 `GamePlay/Practice/` 迁移到 `Npc/Data/`，新增 `BaseProperty`（八大基础属性 struct）、`ElementalAffinity`（五行亲和 struct）、战斗三维（HpMax/MpMax/SpMax）、`LifespanMax`、`SpiritRoot`、`IsInCultivation`
- 完善 `NpcSystemBio`：适配新的 BioData 字段，更新寿元逻辑
- 完善 `NpcSystemPractice`：注册/查询/Tick 修行数据，接管原 BioSystem 的修行相关职责
- `NpcFactionData` + `NpcSystemFaction`：建立 TODO 空壳，字段待后续设计
- 新增 `AppearanceData` 空类（TODO 占位）
- 联动更新 `Npc.cs`、`NpcMgr.cs`、`NpcGenerator.cs` 适配新数据结构
- `TimeFlowRate` 从 NPC 移除，标注 TODO 迁到 Plane 环境属性
- `BaseMoveSpeed` 保留在 BioData，标注 TODO 未来迁到 NpcSystemBehavior

## Capabilities

### New Capabilities
- `npc-bio-data`: NPC 凡间肉身数据定义（Name, Gender, NpcType, AgeAccumulated, BirthTick, BaseMoveSpeed, AppearanceId, IsAlive, DeathTick）
- `npc-cultivation-data`: NPC 修行数据定义（道途、境界、八大基础属性、战斗三维、五行亲和、功法槽位、寿元）
- `npc-faction-data`: NPC 社会数据定义（TODO 空壳占位）

### Modified Capabilities
<!-- 无已有 spec 需要修改 -->

## Impact

- **核心文件变更**：
  - `Scripts/Game/Domain/Object/Npc/Data/NpcTypes.cs` — 可能新增枚举
  - `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBio.cs` — 重构 BioData + BioSystem
  - `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemPractice.cs` — 完善 PracticeData + PracticeSystem
  - `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemFaction.cs` — TODO 空壳
  - `Scripts/Game/Domain/Object/Npc/Npc.cs` — 新增属性访问器
  - `Scripts/Game/Domain/Object/Npc/NpcMgr.cs` — 注册新 System + 更新 Create/RandomCreate
  - `Scripts/Game/Domain/Object/Npc/NpcGenerator.cs` — 适配新数据结构
- **迁移文件**：
  - `Scripts/Game/Domain/GamePlay/Practice/NpcCultivationData.cs` 内容迁移到 `Npc/Data/`
  - `Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs` 保留原位，引用新路径
- **新增文件**：
  - `Scripts/Game/Domain/Object/Npc/Data/NpcBioData.cs`
  - `Scripts/Game/Domain/Object/Npc/Data/NpcCultivationData.cs`
  - `Scripts/Game/Domain/Object/Npc/Data/NpcFactionData.cs`
  - `Scripts/Game/Domain/Object/Npc/Data/AppearanceData.cs`