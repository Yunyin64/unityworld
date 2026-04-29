## ADDED Requirements

### Requirement: DamageInfo继承ContextBase
DamageInfo SHALL 继承 ContextBase，同时提供强类型便捷属性访问常用字段。

#### Scenario: 因果包模式
- **WHEN** 创建 DamageInfo
- **THEN** 可通过 Set/Get 字典存取任意扩展信息
- **AND** 可通过强类型属性访问 Source/Target/FinalDamage 等核心字段

### Requirement: DamageInfo来源因果字段
DamageInfo SHALL 携带伤害事件的来源因果信息。

#### Scenario: 来源NPC
- **WHEN** 构建 DamageInfo
- **THEN** 包含 Source（施害方 CombatNpc）和 Target（受害方 CombatNpc）

#### Scenario: 来源卡牌
- **WHEN** 由卡牌触发的伤害
- **THEN** 包含 SourceCard（施害方 CombatCardState）和 TargetCard（受害方 CombatCardState，对拼时有值）

#### Scenario: 伤害来源类型
- **WHEN** 构建 DamageInfo
- **THEN** 包含 DamageSourceType 标记（Contest/DirectHit/Injury/Effect）

#### Scenario: 触发Tick
- **WHEN** 构建 DamageInfo
- **THEN** 包含 Tick（伤害发生的Tick序号）

### Requirement: DamageInfo数值过程字段
DamageInfo SHALL 记录数值比较的中间过程。

#### Scenario: 对拼数值
- **WHEN** 对拼产生 DamageInfo
- **THEN** 包含 SourceValue/TargetValue（双方拼点值）和 DeltaValue（差值）

#### Scenario: 卡牌类型信息
- **WHEN** 对拼产生 DamageInfo
- **THEN** 包含 SourceContestType/TargetContestType（Atk/Shield/Defend）

#### Scenario: 元素和物理类型
- **WHEN** 对拼产生 DamageInfo
- **THEN** 包含 SourceElement 和 SourcePhysicalType

### Requirement: DamageInfo结算结果字段
DamageInfo SHALL 记录最终结算结果。

#### Scenario: 伤害结果
- **WHEN** 结算完成
- **THEN** FinalDamage 记录最终伤害值（0表示无伤害）

#### Scenario: 治疗结果
- **WHEN** 盾卡赢得对拼
- **THEN** FinalHeal 记录治疗值

#### Scenario: 直击标记
- **WHEN** 待发槽溢出直击
- **THEN** IsDirectHit = true

#### Scenario: 赢家通吃标记
- **WHEN** 同物理类型攻击对打赢家通吃触发
- **THEN** IsWinnerTakesAll = true

#### Scenario: HP清零标记
- **WHEN** 伤害导致目标HP清零
- **THEN** HpZeroed = true，InjuryCard 记录生成的伤势卡

### Requirement: DamageInfo工厂方法
DamageInfo SHALL 提供静态工厂方法简化常见场景的构建。

#### Scenario: 对拼场景
- **WHEN** 需要创建对拼的 DamageInfo
- **THEN** 使用 DamageInfo.CreateContest(source, target, sourceCard, targetCard, tick) 构建

#### Scenario: 直击场景
- **WHEN** 需要创建直击的 DamageInfo
- **THEN** 使用 DamageInfo.CreateDirectHit(source, target, sourceCard, tick) 构建

#### Scenario: 伤势自伤场景
- **WHEN** 伤势卡触发自伤
- **THEN** 使用 DamageInfo.CreateInjurySelfDamage(npc, injuryCard, tick) 构建

### Requirement: DamageInfo文件重构
DamageInfo.cs SHALL 被重构为 DamageInfo.cs，旧的 DamageInfo 类移除。

#### Scenario: 文件替换
- **WHEN** 重构完成
- **THEN** DamageInfo.cs 内容被 DamageInfo 替换，不再存在 DamageInfo 类