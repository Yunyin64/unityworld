## ADDED Requirements

### Requirement: 战斗过程日志
CombatScene SHALL 输出完整的战斗过程日志，记录每个关键事件。

#### Scenario: Tick级事件记录
- **WHEN** 每个Tick中发生卡CD就绪、入槽、溢出、对拼等事件
- **THEN** 输出带Tick编号的日志行，格式清晰可读

#### Scenario: 对拼结算日志
- **WHEN** 两张卡对拼结算
- **THEN** 日志记录：双方卡名/数值/类型、赢方、伤害/加血数值、是否赢家通吃

#### Scenario: 伤势产生日志
- **WHEN** HP清零产生伤势卡
- **THEN** 日志记录：NPC、伤害数值、伤势卡名称/Cost、当前卡组空间占用

#### Scenario: 判负日志
- **WHEN** NPC因SP溢出判负
- **THEN** 日志记录：NPC、卡组Cost总和、SP上限

### Requirement: 战斗结果摘要
战斗结束后SHALL输出结果摘要日志。

#### Scenario: 结果摘要
- **WHEN** 战斗结束
- **THEN** 输出：胜负方、总Tick数、每个NPC的最终HP/SP/伤势数量、结束原因

### Requirement: 战斗结算与NPC接通
CombatResult SHALL 包含足够信息供调用方回写大世界NPC状态。

#### Scenario: HP损耗回写
- **WHEN** 战斗结算
- **THEN** CombatantResult包含HpLost，调用方可据此修改NPC的StatBlock

#### Scenario: 伤势卡回写
- **WHEN** 战斗中NPC获得了伤势卡
- **THEN** CombatantResult包含伤势卡列表(InjuryCards)，调用方将其写入NPC持久卡组

#### Scenario: 从NPC读取战斗属性
- **WHEN** 战斗PreStart阶段构建CombatNpc
- **THEN** 从大世界NPC读取体魄→HP、神识→SP、蓝条→MP、现有卡组