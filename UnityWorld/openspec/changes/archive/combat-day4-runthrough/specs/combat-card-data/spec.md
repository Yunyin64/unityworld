## MODIFIED Requirements

### Requirement: CombatCardState 新增 FrozenTicks 字段
CombatCardState SHALL 新增 `FrozenTicks` 公有属性（int，默认 0），以及 `Freeze(int ticks)` 方法。FrozenTicks 参与 TickCd 行为变更（详见 combat-tick-engine spec）。

#### Scenario: 新建 CombatCardState 时 FrozenTicks 为 0
- **WHEN** 通过 `new CombatCardState(cardData)` 创建实例
- **THEN** FrozenTicks 初始值为 0

#### Scenario: Freeze 方法设置冻结
- **WHEN** 调用 Freeze(3)
- **THEN** FrozenTicks 被设为 max(当前值, 3)