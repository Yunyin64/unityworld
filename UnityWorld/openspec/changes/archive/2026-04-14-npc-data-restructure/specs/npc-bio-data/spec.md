## ADDED Requirements

### Requirement: NpcBioData 凡间身份字段
`NpcBioData` SHALL 包含以下凡间身份字段：
- `Name` (string)：NPC 名字
- `Gender` (NpcTypes.Gender)：性别
- `NpcType` (NpcTypes.NpcType)：种族（Human/Monster/Animal）

#### Scenario: 创建 NPC 时设置身份字段
- **WHEN** 通过 NpcMgr.Create() 创建 NPC
- **THEN** NpcBioData 的 Name、Gender、NpcType 字段 MUST 被正确赋值

### Requirement: NpcBioData 年龄与出生时刻
`NpcBioData` SHALL 包含以下生命周期字段：
- `AgeAccumulated` (float)：当前年龄，由 NpcSystemBio 每 Tick 推进
- `BirthTick` (int)：出生时的世界 Tick，用于推算生辰

#### Scenario: Tick 推进年龄
- **WHEN** NpcSystemBio.OnTick 被调用，deltaTime = 0.5
- **THEN** NpcBioData.AgeAccumulated MUST 增加 0.5

#### Scenario: 出生时刻记录
- **WHEN** NPC 被创建
- **THEN** BirthTick MUST 记录为当前 WorldTime.CurrentTick

### Requirement: NpcBioData 基础移动速度
`NpcBioData` SHALL 包含 `BaseMoveSpeed` (float) 字段，表示凡人基础移动速度。
此字段标注为 TODO，未来将迁移到 NpcSystemBehavior。

#### Scenario: 读取基础移动速度
- **WHEN** 查询 NPC 的 BaseMoveSpeed
- **THEN** MUST 返回 NpcBioData 中存储的值（默认 3f）

### Requirement: NpcBioData 外观引用
`NpcBioData` SHALL 包含 `AppearanceId` (string) 字段，引用外观配置表。
此字段标注为 TODO，具体外观系统待后续实现。
SHALL 同时创建 `AppearanceData` 空类作为未来外观数据容器的占位。

#### Scenario: 创建 NPC 时设置默认外观
- **WHEN** NPC 被创建且未指定外观
- **THEN** AppearanceId MUST 为空字符串 ""

### Requirement: NpcBioData 生死状态
`NpcBioData` SHALL 包含以下生死状态字段（TODO: 未来迁到独立生死 System）：
- `IsAlive` (bool)：是否存活，默认 true
- `DeathTick` (int?)：死亡时的世界 Tick，null 表示活着

#### Scenario: NPC 创建时默认存活
- **WHEN** NPC 被创建
- **THEN** IsAlive MUST 为 true，DeathTick MUST 为 null

#### Scenario: NPC 死亡时记录
- **WHEN** NPC 寿元耗尽或被击杀
- **THEN** IsAlive MUST 设为 false，DeathTick MUST 记录当前 Tick

### Requirement: NpcBioData 不包含修行属性
`NpcBioData` SHALL NOT 包含以下字段（已迁移到 NpcCultivationData 或移除）：
- `CultivationLevel`：迁移到 NpcCultivationData.CurrentRealmLevel
- `LifespanMax`：迁移到 NpcCultivationData.LifespanMax
- `TimeFlowRate`：移除，TODO 迁移到 Plane 环境属性

#### Scenario: BioData 无修行字段
- **WHEN** 检查 NpcBioData 类定义
- **THEN** MUST 不存在 CultivationLevel、LifespanMax、TimeFlowRate 字段

### Requirement: NpcSystemBio 管理 NpcBioData
`NpcSystemBio` SHALL 提供以下能力：
- `Register(Npc, NpcBioData)`：注册 NPC 的生物数据
- `GetBio(int id)`：查询 NPC 的生物数据
- `OnTick(Npc, float)`：每 Tick 推进年龄（AgeAccumulated += deltaTime）
- `GetAge(Npc)`：获取年龄
- `GetMoveSpeed(Npc)`：获取基础移动速度

#### Scenario: 注册并查询
- **WHEN** 调用 Register 注册 NPC 生物数据后调用 GetBio
- **THEN** MUST 返回注册时的 NpcBioData 实例

### Requirement: NpcBioData 独立文件
`NpcBioData` 类 SHALL 定义在独立文件 `Scripts/Game/Domain/Object/Npc/Data/NpcBioData.cs` 中，
不再与 `NpcSystemBio` 混写在同一文件。`AppearanceData` 类 SHALL 定义在 `Scripts/Game/Domain/Object/Npc/Data/AppearanceData.cs` 中。

#### Scenario: 文件结构
- **WHEN** 检查文件系统
- **THEN** `Npc/Data/NpcBioData.cs` MUST 存在且包含 NpcBioData 类
- **THEN** `Npc/Data/AppearanceData.cs` MUST 存在且包含 AppearanceData 空类
- **THEN** `Npc/Systems/NpcSystemBio.cs` MUST 仅包含 NpcSystemBio 类