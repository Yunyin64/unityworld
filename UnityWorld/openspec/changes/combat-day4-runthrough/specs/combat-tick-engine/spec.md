## MODIFIED Requirements

### Requirement: TickCd 跳过冻结卡牌
CombatCardState.TickCd() SHALL 在 FrozenTicks > 0 时不递增 CurrentCdTick，改为递减 FrozenTicks。原有 TickCd 行为在 FrozenTicks == 0 时保持不变。

#### Scenario: 冻结中的卡 TickCd 不增 CD
- **WHEN** CombatCardState.FrozenTicks = 2，调用 TickCd()
- **THEN** CurrentCdTick 不变，FrozenTicks 变为 1，返回 false

#### Scenario: 未冻结的卡 TickCd 正常
- **WHEN** CombatCardState.FrozenTicks = 0，调用 TickCd()
- **THEN** CurrentCdTick 递增 1，返回是否已就绪

### Requirement: 对拼结算后触发后续 Effect 链
CombatScene.ProcessSlotResults SHALL 在对拼结算完成后，通过 CombatContestHandler 触发双方卡牌的后续 Effect（OnUse 非拼点 Action + OnContestWin/OnContestLose Trigger）。

#### Scenario: 对拼结算完成后执行后续
- **WHEN** 对拼结算返回 ContestResult（非平局）
- **THEN** 赢家卡的 OnContestWin Effect 被执行，输家卡的 OnContestLose Effect 被执行，双方 OnUse 非拼点 Action 被执行