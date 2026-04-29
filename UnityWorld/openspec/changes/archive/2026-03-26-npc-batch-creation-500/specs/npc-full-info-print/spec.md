## ADDED Requirements

### Requirement: 打印修士完整信息
`NpcMgr` SHALL 提供 `PrintFullInfo(Npc npc)` 方法，以格式化方式打印一个修士 NPC 的全部信息，包括：姓名、性别、年龄/寿元、道途、境界名称（从 RealmDefineMgr 查询）、核心功法名称及进度（从 CultivationMgr 查询）、社会角色、Trait 列表、位置坐标。

#### Scenario: 打印玩家角色完整信息
- **WHEN** 调用 `PrintFullInfo(playerNpc)`
- **THEN** 控制台输出包含以下信息段：姓名、性别、年龄/寿元、道途、境界、功法（含进度）、角色、Trait、位置，格式清晰可读

### Requirement: 境界名称从 RealmDefine 获取
`PrintFullInfo()` MUST 使用 `RealmDefineMgr.Instance.GetByPathAndLevel()` 查询当前境界的 `DisplayName`（如 "练气期"、"筑基期"），而非仅打印数字等级。

#### Scenario: 显示境界名称而非数字
- **WHEN** 修士道途为 Ling、境界 Level 1
- **THEN** 输出中显示 "灵修" 和 "练气期" 而非 "Level 1"

### Requirement: 功法进度展示
`PrintFullInfo()` MUST 从 `CultivationMgr` 查询修士的核心功法信息，展示功法名称和当前修炼进度。若无功法则显示 "无核心功法"。

#### Scenario: 有功法时显示进度
- **WHEN** 修士有核心功法 "基础聚气术"，当前进度为 0/200
- **THEN** 输出中包含 "核心功法: 基础聚气术 (进度: 0/200)"

#### Scenario: 无功法时显示占位
- **WHEN** 修士无核心功法
- **THEN** 输出中包含 "核心功法: 无"

### Requirement: 生成统计概况打印
`NpcGenerator` 在完成批量生成后 MUST 打印统计概况，包括：总数、各道途人数、各境界人数。

#### Scenario: 500 人统计概况
- **WHEN** 生成 500 个修士后
- **THEN** 控制台打印类似 "共 500 修士 | 灵修:168 武修:165 魂修:167 | Lv1:170 Lv2:163 Lv3:167"