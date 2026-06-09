## ADDED Requirements

### Requirement: Tick驱动战斗循环
CombatScene SHALL 使用 Tick 驱动模型替代回合制。每次调用 Tick() 时，所有存活NPC的所有卡的计时器同步推进1单位。

#### Scenario: 正常Tick推进
- **WHEN** 调用 CombatScene.Tick()
- **THEN** 所有存活NPC的所有卡的CD计时器+1

#### Scenario: 已败北NPC跳过
- **WHEN** 某NPC已判负（Status != Active）
- **THEN** 该NPC的所有卡不再推进计时器

### Requirement: 独立计时器CD触发
每张卡拥有独立的CD计时器。当计时器达到卡的Cooldown值时，该卡触发就绪，根据卡的类型进入不同处理流程。

#### Scenario: 攻防卡CD就绪
- **WHEN** 某张包含攻击/盾/防数值的卡计时器达到Cooldown
- **THEN** 该卡尝试推入所属NPC的待发槽

#### Scenario: 效果卡CD就绪
- **WHEN** 某张不含攻击/盾/防数值的卡计时器达到Cooldown
- **THEN** 该卡直接结算效果，计时器重置为0

### Requirement: 待发槽机制
每个NPC拥有一个待发槽，默认上限1张。攻防卡CD就绪后推入待发槽。

#### Scenario: 待发槽为空时入槽
- **WHEN** 攻防卡就绪且待发槽为空
- **THEN** 卡进入待发槽，等待对手出卡

#### Scenario: 待发槽已满时溢出
- **WHEN** 攻防卡就绪但待发槽已满（达到上限）
- **THEN** 待发槽中最早的卡被挤出，直击对方本体（全额攻击数值伤害），新卡进入待发槽

#### Scenario: 双方待发槽都有卡时对拼
- **WHEN** 一方卡进入待发槽后，检测到双方（自己和Target）待发槽都有卡
- **THEN** 立刻触发对拼结算，双方待发槽的卡各消耗一张

### Requirement: 对拼结算规则
对拼时比较双方卡的拼点数值（攻击值/盾值/防值），数值高者为赢方。

#### Scenario: 普通对拼赢方为攻击卡
- **WHEN** 攻击卡赢得对拼
- **THEN** 差值作为伤害施加到输方NPC

#### Scenario: 赢方为盾卡
- **WHEN** 盾卡赢得对拼
- **THEN** 差值加入己方NPC的血条（HP增加）

#### Scenario: 赢方为防卡
- **WHEN** 防卡赢得对拼
- **THEN** 差值消失，无额外效果（纯减伤）

#### Scenario: 数值相同平局
- **WHEN** 双方拼点数值相同
- **THEN** 两卡均消耗，无伤害产生

#### Scenario: 同物理类型攻击对打（赢家通吃）
- **WHEN** 双方均为攻击卡且物理类型相同（斩vs斩/刺vs刺/打vs打，射击除外）
- **THEN** 赢家造成全额数值伤害（非差值），输家承受全额伤害

#### Scenario: 射击类型不触发赢家通吃
- **WHEN** 任一方为射击类型
- **THEN** 按普通差值规则结算，不触发赢家通吃

### Requirement: SP溢出判负
每Tick检查所有存活NPC的卡组空间占用。卡Cost总和超过SP时判负。

#### Scenario: 正常状态
- **WHEN** NPC卡组Cost总和 ≤ SP
- **THEN** 继续战斗

#### Scenario: 溢出判负
- **WHEN** NPC卡组Cost总和 > SP（通常因伤势卡塞入导致）
- **THEN** 该NPC立即判负，Status设为Defeated，EndReason为SpaceOverflow

### Requirement: Target分配
每个NPC在战斗开始时分配固定Target（敌方存活者），所有卡打向同一个Target。

#### Scenario: 初始Target分配
- **WHEN** 战斗PreStart阶段
- **THEN** 每个NPC指向第一个敌方存活者作为Target

#### Scenario: Target被击败后重分配
- **WHEN** 当前Target判负
- **THEN** 自动切换到下一个敌方存活者

### Requirement: 战斗结束条件
战斗在以下条件下结束：SP溢出判负（场上只剩一方）、Tick上限到达。

#### Scenario: 一方全灭
- **WHEN** 某一方所有NPC均已判负
- **THEN** 战斗结束，另一方胜利

#### Scenario: Tick上限
- **WHEN** 总Tick数达到上限
- **THEN** 战斗结束，判为平局