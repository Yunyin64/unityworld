## ADDED Requirements

### Requirement: ManaPool 数据结构
CombatNpc SHALL 持有 ManaPool（Dictionary<string, int>），以元素名称为 key、数量为 value，记录当前拥有的灵元。

#### Scenario: 初始 ManaPool 为空
- **WHEN** CombatNpc 初始化
- **THEN** ManaPool 为空字典，所有元素数量为 0

### Requirement: MP 转化为灵元
CombatManaHandler SHALL 实现 MP→灵元的定期转化逻辑。每隔 N 个 Tick，扣除 CombatNpc 的 A 点 MP，按规则产生若干灵元加入 ManaPool。初始版本使用最简规则（如：固定产生1个无属性灵元）。

#### Scenario: 转化 Tick 到达时执行转化
- **WHEN** 当前 Tick 为转化间隔的倍数（如每 10 Tick）且 MP > 0
- **THEN** 扣除固定 MP，向 ManaPool 添加灵元

#### Scenario: MP 不足时不转化
- **WHEN** 当前 MP 为 0
- **THEN** 不执行转化，ManaPool 不变

### Requirement: 灵元消耗检查
CombatManaHandler SHALL 提供 `CanAffordMana(CombatNpc npc, Dictionary<string, int> manaCost)` 方法，检查 NPC 的 ManaPool 是否满足指定消耗需求。

#### Scenario: ManaPool 充足
- **WHEN** ManaPool 有 {Huo:2}，ManaCost 为 {Huo:1}
- **THEN** CanAffordMana 返回 true

#### Scenario: ManaPool 不足
- **WHEN** ManaPool 有 {Huo:0}，ManaCost 为 {Huo:1}
- **THEN** CanAffordMana 返回 false

### Requirement: 灵元消耗执行
CombatManaHandler SHALL 提供 `ConsumeMana(CombatNpc npc, Dictionary<string, int> manaCost)` 方法，从 ManaPool 中扣除指定灵元。

#### Scenario: 消耗成功
- **WHEN** ManaPool 有 {Huo:2} 并消耗 {Huo:1}
- **THEN** 消耗后 ManaPool 为 {Huo:1}

### Requirement: CombatCardState Mana 满足检查
CombatCardState.IsManaFulfilled SHALL 根据关联 CardData 的 ManaCost 和所属 NPC 的 ManaPool 动态判断，而非始终返回 true。

#### Scenario: 无 ManaCost 的卡始终满足
- **WHEN** CardData.ManaCost 为空字典
- **THEN** IsManaFulfilled 为 true

#### Scenario: 有 ManaCost 且灵元不足
- **WHEN** CardData.ManaCost 为 {Huo:1}，NPC 的 ManaPool 中 Huo 为 0
- **THEN** IsManaFulfilled 为 false

### Requirement: CombatScene.Tick 接入 Mana 转化
CombatScene 的 Tick 循环 SHALL 在推进卡计时器之前调用 CombatManaHandler 的转化逻辑。

#### Scenario: 每 Tick 检查转化
- **WHEN** CombatScene.Tick() 被调用
- **THEN** 对每个存活 NPC 检查是否到达转化时机，是则执行转化