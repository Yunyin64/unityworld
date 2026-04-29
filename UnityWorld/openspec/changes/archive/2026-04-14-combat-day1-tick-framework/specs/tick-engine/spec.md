## ADDED Requirements

### Requirement: Tick驱动战斗循环
CombatScene SHALL 使用 Tick 驱动模型替代回合制。每次调用 `Tick()` 时，所有存活NPC的所有卡的计时器同步推进1单位。

#### Scenario: 正常Tick推进
- **WHEN** 调用 CombatScene.Tick()
- **THEN** 所有存活NPC的所有活跃卡的 CombatCardState.CurrentCdTick +1

#### Scenario: 已败北NPC跳过
- **WHEN** 某NPC Status != Active
- **THEN** 该NPC的所有卡不再推进计时器

### Requirement: CD就绪分流
每张卡的 CurrentCdTick 达到 Cooldown 值时，根据卡的攻防类型进入不同处理流程。

#### Scenario: 攻防卡CD就绪
- **WHEN** 某张卡 IsAttackDefenseCard() == true 且 CurrentCdTick >= Cooldown
- **THEN** 该卡尝试推入所属NPC的待发槽

#### Scenario: 效果卡CD就绪
- **WHEN** 某张卡 IsAttackDefenseCard() == false 且 CurrentCdTick >= Cooldown
- **THEN** 该卡直接结算效果（⏳Day1占位：仅日志），计时器重置为0

### Requirement: Tick上限结束
战斗 SHALL 在 Tick 数达到 MaxTicks 时强制结束。

#### Scenario: 达到Tick上限
- **WHEN** CurrentTick >= MaxTicks
- **THEN** 战斗结束，EndReason = TurnLimitReached，判为平局

### Requirement: 一方全灭结束
战斗 SHALL 在场上只剩一方存活时结束。

#### Scenario: 某方全灭
- **WHEN** 某一方所有NPC均 Status != Active
- **THEN** 战斗结束，EndReason = AllDefeated，另一方胜利

### Requirement: Target分配
每个NPC在战斗开始时分配固定Target（敌方存活者），所有卡打向同一个Target。

#### Scenario: 初始Target分配
- **WHEN** 战斗PreStart阶段
- **THEN** 每个NPC指向第一个敌方存活者作为Target

#### Scenario: Target被击败后重分配
- **WHEN** 当前Target判负
- **THEN** 自动切换到下一个敌方存活者

### Requirement: Tick方法替代NextTurn
CombatScene SHALL 移除 NextTurn() 方法，新增 Tick() 方法作为战斗主循环入口。

#### Scenario: 标准用法
- **WHEN** 调用方驱动战斗
- **THEN** 在 `while (!scene.IsFinished) scene.Tick()` 循环中推进战斗

### Requirement: MaxTicks/CurrentTick重命名
CombatScene SHALL 将 MaxTurns 改名为 MaxTicks，CurrentTurn 改名为 CurrentTick。

#### Scenario: 属性重命名
- **WHEN** 读取战斗进度
- **THEN** 使用 MaxTicks 和 CurrentTick 属性

### Requirement: PreStart硬编码占位
CombatScene.PreStart SHALL 支持通过硬编码或测试辅助方法设置HP/SP/CardStates，不依赖大世界NPC。

#### Scenario: 测试用初始化
- **WHEN** Day1内部测试
- **THEN** 通过 SetupTestCombatNpc() 设置CombatNpc的HP/SP/CardStates
- **NOTE** ⏳占位，Day5回填为从真实Npc读取