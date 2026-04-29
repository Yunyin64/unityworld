## ADDED Requirements

### Requirement: 批量生成修士 NPC
`NpcGenerator` SHALL 提供 `GenerateCultivators(int count, Rng rng)` 方法，一次性生成指定数量的修士 NPC。每个修士的道途、境界、功法、性别、年龄、Trait、位置 MUST 全部随机决定，等概率分布，无权重偏向。

#### Scenario: 生成 500 个修士
- **WHEN** 调用 `GenerateCultivators(500, rng)`
- **THEN** 返回包含 500 个 Npc 的列表，`NpcMgr.Instance.Count` 增加 500

### Requirement: 道途随机分配
每个修士的道途 MUST 从 `[Ling, Wu, Hun]` 三选一，等概率随机。

#### Scenario: 道途均匀分布
- **WHEN** 生成 500 个修士
- **THEN** 三条道途各约 166~167 个（允许随机波动）

### Requirement: 境界随机分配
每个修士的境界等级 MUST 从 `[1, 2, 3]` 三选一，等概率随机。

#### Scenario: 境界均匀分布
- **WHEN** 生成 500 个修士
- **THEN** 三个境界各约 166~167 个（允许随机波动）

### Requirement: 功法从数据池匹配
Generator MUST 根据修士的道途（pathType）和境界等级（realmLevel）从 `CultivationDefineMgr` 中查询可用功法，等概率随机选一本。若无匹配功法，MUST 打印警告日志并跳过功法分配。

#### Scenario: 匹配成功
- **WHEN** 修士道途为 Ling、境界为 1
- **THEN** 从 CultivationDefines 中筛选 pathType=Ling 且 realmLevel=1 的功法，随机分配一本

#### Scenario: 匹配失败
- **WHEN** 某道途+境界组合在数据池中无对应功法
- **THEN** 打印 `[NpcGenerator] 警告` 日志，该修士无核心功法

### Requirement: Trait 随机抽取
每个修士 MUST 从 Trait 池中随机抽取 2~4 个 Trait（数量也随机），不做互斥检查，允许任意组合。

#### Scenario: Trait 数量在 2~4 范围内
- **WHEN** 生成一个修士
- **THEN** 其 Trait 列表长度在 [2, 4] 之间

### Requirement: 位置随机散布
每个修士 MUST 被放置在主世界范围内的随机坐标 (0~Width-1, 0~Height-1)。代码中 MUST 留有 TODO 注释标记未来由叙事/势力系统重分布。

#### Scenario: 位置在世界范围内
- **WHEN** 生成 500 个修士
- **THEN** 所有修士的坐标 x ∈ [0, 199]，y ∈ [0, 199]

### Requirement: 年龄和寿元基于境界查表
Generator MUST 使用内部查表确定每个境界的年龄范围和基础寿元，年龄在范围内等概率随机。

#### Scenario: Level 1 修士的年龄和寿元
- **WHEN** 修士境界为 Level 1
- **THEN** 年龄在 16~80 之间随机，基础寿元为 150

### Requirement: 返回列表且第一个为玩家角色
`GenerateCultivators()` MUST 返回 `List<Npc>`，列表中第一个 NPC 作为玩家控制的角色。

#### Scenario: 第一个 NPC 即玩家角色
- **WHEN** 调用 `GenerateCultivators(500, rng)`
- **THEN** 返回列表的 `[0]` 元素即为玩家角色，可用于后续 `PrintFullInfo()` 展示