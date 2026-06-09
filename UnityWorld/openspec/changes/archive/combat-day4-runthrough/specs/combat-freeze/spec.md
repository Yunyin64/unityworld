## ADDED Requirements

### Requirement: CombatCardState 冻结状态
CombatCardState SHALL 新增 `FrozenTicks` 属性（int，默认 0）。
- FrozenTicks > 0 时，TickCd() SHALL 不递增 CurrentCdTick，而是递减 FrozenTicks
- FrozenTicks == 0 时，TickCd() 恢复正常递增行为
- SHALL 提供 `Freeze(int ticks)` 方法，将 FrozenTicks 设置为指定值（叠加取最大值）

#### Scenario: 卡牌被冻结 3 tick
- **WHEN** FrozenTicks 被设为 3，连续调用 3 次 TickCd()
- **THEN** CurrentCdTick 不变，FrozenTicks 依次变为 2、1、0

#### Scenario: 冻结结束后恢复正常
- **WHEN** FrozenTicks 降为 0 后再调用 TickCd()
- **THEN** CurrentCdTick 正常递增

#### Scenario: 冻结叠加取最大值
- **WHEN** FrozenTicks 当前为 2，再次 Freeze(5)
- **THEN** FrozenTicks 变为 5（取最大值）

### Requirement: Freeze APIFunc Handler
CombatBaseFunc SHALL 新增 `[APIFunc("Freeze")]` 标记的静态方法：
- 从 ActionContext.Env 读取 "Caster"（CombatNpc）
- 从 ActionContext.Action.Context 读取 "TargetCardId"（string）和 "FreezeTick"（int）
- 在 Caster 的 Target 的 CardStates 中，找到 DefineId == TargetCardId 的卡牌
- 调用该卡牌的 Freeze(FreezeTick)
- 若找不到目标卡牌，输出警告日志并跳过

#### Scenario: 成功冻结目标卡牌
- **WHEN** ActionContext 中 TargetCardId = "card_form_jian_zhan"，FreezeTick = 3，敌方有该卡
- **THEN** 该卡的 FrozenTicks 设为 3

#### Scenario: 目标卡牌不存在
- **WHEN** TargetCardId 指向一张敌方不存在的卡
- **THEN** 输出警告日志，不崩溃