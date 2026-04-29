## ADDED Requirements

### Requirement: Attack 构造拼点数据
CombatBaseFunc.Attack(ctx, element, contestType, value) SHALL 从参数构造 ContestData 并放入 caster 的 PendingSlot，而非直接造成伤害。拼点/直击结算由 CombatContestHandler 统一处理。

#### Scenario: Attack 放入代发槽
- **WHEN** 调用 `CombatBaseFunc.Attack(ctx, "Jin", "Zhan", 3)`
- **THEN** 构造 ContestData(Element=Jin, ContestType=Zhan, Value=3) 并设为 caster.PendingSlot

#### Scenario: Attack 不直接造成伤害
- **WHEN** 调用 CombatBaseFunc.Attack()
- **THEN** 不修改任何 NPC 的 HP，伤害由拼点结算后产生

### Requirement: Shield 构造拼点数据
CombatBaseFunc.Shield(ctx, shieldValue) SHALL 从参数构造 ContestData 并放入 caster 的 PendingSlot，与 Attack 同理。

#### Scenario: Shield 放入代发槽
- **WHEN** 调用 `CombatBaseFunc.Shield(ctx, 5)`
- **THEN** 构造 ContestData(ContestType=Shield, Value=5) 并设为 caster.PendingSlot

### Requirement: Block 构造拼点数据
CombatBaseFunc.Block(ctx, blockValue) SHALL 从参数构造 ContestData 并放入 caster 的 PendingSlot，与 Attack 同理。

#### Scenario: Block 放入代发槽
- **WHEN** 调用 `CombatBaseFunc.Block(ctx, 4)`
- **THEN** 构造 ContestData(ContestType=Block, Value=4) 并设为 caster.PendingSlot
