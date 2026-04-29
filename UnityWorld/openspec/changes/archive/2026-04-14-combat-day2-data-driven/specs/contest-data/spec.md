## ADDED Requirements

### Requirement: ContestData 临时拼点数据结构
系统 SHALL 提供 ContestData 类，封装一次拼点的数据快照。字段包括：
- ContestType（string）：拼点类型，为 "Attack"/"Shield"/"Block" 之一
- ContestValue（float）：拼点数值（AttackValue/ShieldValue/BlockValue）
- Element（BaseElementType）：元素类型
- PhysicalType（PhysicalType）：物理攻击类型
- SourceCard（CombatCardState）：来源卡引用
- OwnerNpc（CombatNpc）：所属 NPC 引用

#### Scenario: 从 Attack ActionData 构造 ContestData
- **WHEN** ActionData 的 FuncName 为 "Attack"，Context 包含 {Element="Huo", PhysicalType="SheJi", AttackValue=3}
- **THEN** ContestData 的 ContestType="Attack"，ContestValue=3，Element=Huo，PhysicalType=SheJi

#### Scenario: 从 Shield ActionData 构造 ContestData
- **WHEN** ActionData 的 FuncName 为 "Shield"，Context 包含 {Element="Shui", PhysicalType="Da", ShieldValue=4}
- **THEN** ContestData 的 ContestType="Shield"，ContestValue=4，Element=Shui，PhysicalType=Da

### Requirement: ContestData 从 CombatCardState 提取
CombatCardState SHALL 提供 `BuildContestData()` 方法，遍历关联 CardData 的所有 EffectData 的所有 ActionData，找到第一个 FuncName 为 Attack/Shield/Block 的 ActionData，构造 ContestData 返回。若无拼点 Action 则返回 null。

#### Scenario: 卡含单个 Attack Action
- **WHEN** CardData 有一个 EffectData，其 ActionData 列表中有一个 FuncName="Attack" 的 ActionData
- **THEN** BuildContestData() 返回非空 ContestData，类型为 "Attack"

#### Scenario: 纯效果卡无拼点 Action
- **WHEN** CardData 的所有 EffectData 中没有 FuncName 为 Attack/Shield/Block 的 ActionData
- **THEN** BuildContestData() 返回 null

#### Scenario: 多个拼点 Action 取第一个
- **WHEN** CardData 有 EffectData 包含 [Attack(3), Shield(2)] 两个拼点 Action
- **THEN** BuildContestData() 返回第一个（Attack），多骰子预留未来支持

### Requirement: 待发槽存放 ContestData
CombatNpc 的 PendingSlot SHALL 改为存放 ContestData（而非 CombatCardState）。对拼结算时直接从 ContestData 读取数值和类型。

#### Scenario: 攻防卡 CD 满进入待发槽
- **WHEN** 一张攻防卡 CD 满，BuildContestData() 返回非空
- **THEN** 构造的 ContestData 被放入 PendingSlot