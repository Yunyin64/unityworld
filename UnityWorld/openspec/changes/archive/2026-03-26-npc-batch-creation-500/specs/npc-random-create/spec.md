## ADDED Requirements

### Requirement: RandomCreate 方法创建修士 NPC
`NpcMgr` SHALL 提供 `RandomCreate()` 方法，接受以下参数直接创建修士 NPC：道途（PracticePath）、境界等级（int）、年龄（float）、寿元上限（float）、移动速度（float）、性别（Gender）、社会角色列表（string[]）、Trait ID 列表（string[]）、坐标 x/y（int）。该方法 MUST 绕过 `NpcDefine` 模板，不依赖任何 JSON 定义文件。

#### Scenario: 成功创建一个灵修修士
- **WHEN** 调用 `RandomCreate(PracticePath.Ling, realmLevel:2, age:80, lifespanMax:300, moveSpeed:5.0, gender:Male, roles:["cultivator"], traitIds:["brave","genius"], x:50, y:50)`
- **THEN** 返回一个 Npc 实体，其 Bio.CultivationLevel 为 2，Bio.AgeAccumulated 为 80，Bio.LifespanMax 为 300，位置为 (50,50)，持有 "brave" 和 "genius" 两个 Trait

### Requirement: RandomCreate 注册修炼数据
`RandomCreate()` MUST 在创建 NPC 后调用 `CultivationMgr.Register()` 注册道途，并调用 `CultivationMgr.AddCultivation()` 分配核心功法（如果提供了功法 ID）。

#### Scenario: 创建修士时自动注册修炼体系
- **WHEN** 调用 `RandomCreate()` 并传入有效的道途和功法 ID
- **THEN** NPC 在 `CultivationMgr` 中有对应的道途记录和功法槽位

#### Scenario: 无匹配功法时跳过修炼注册
- **WHEN** 调用 `RandomCreate()` 但未传入功法 ID（为 null）
- **THEN** NPC 仍被成功创建，`CultivationMgr` 中仅有道途记录，无功法槽位

### Requirement: RandomCreate 注册所有子系统
`RandomCreate()` MUST 按顺序注册 NPC 到以下子系统：NameSystem、BioSystem、RoleSystem、PositionSystem、TraitMgr、StatMgr。注册顺序与现有 `Create(NpcDefine)` 保持一致。

#### Scenario: 创建后所有子系统可查询
- **WHEN** `RandomCreate()` 返回 NPC
- **THEN** `npc.Name` 非空，`npc.Bio` 非 null，`npc.Position` 非 null，`npc.Roles` 包含 "cultivator"，`npc.Traits` 包含指定 Trait