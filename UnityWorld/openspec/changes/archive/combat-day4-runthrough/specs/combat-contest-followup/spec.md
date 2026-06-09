## ADDED Requirements

### Requirement: 对拼后执行 OnUse 非拼点 Action
CombatContestHandler.ResolveContest SHALL 在对拼数值结算完成后，遍历双方 PendingSlot 来源卡的所有 Effect：
- 对于 TriggerId 为 "trigger_on_use" 或空的 Effect，执行其中非拼点类的 Action（FuncName 不是 "Attack"/"Shield"/"Block"）
- Action 执行前 SHALL 先通过 CombatConditionHandler 检查 Condition
- ActionContext.Env 中 SHALL 包含 Caster（卡牌所有者）、Target（对方）、Scene

#### Scenario: 攻击卡附带毒效果
- **WHEN** 攻击卡的 OnUse Effect 中包含 Attack(5) 和 AddPoison(2)
- **THEN** 对拼结算后，Attack 走拼点路径，AddPoison 作为后续 Action 执行

#### Scenario: 盾卡无额外效果
- **WHEN** 盾卡的 OnUse Effect 只有 Shield(3)
- **THEN** 对拼结算后无额外 Action 执行

### Requirement: ContestWin Trigger 执行
CombatContestHandler SHALL 在对拼结算后，对赢家的 PendingSlot 来源卡检查是否有 TriggerId 为 "trigger_on_contest_win" 的 Effect：
- 若有，SHALL 执行该 Effect 的所有 Action（经 Condition 门控）
- ActionContext.Env 中 SHALL 包含对拼结果信息

#### Scenario: 赢家有 ContestWin 效果
- **WHEN** 赢家卡牌包含 trigger_on_contest_win Effect 带 Heal(2)
- **THEN** 赢家额外恢复 2 HP

#### Scenario: 赢家无 ContestWin 效果
- **WHEN** 赢家卡牌无 trigger_on_contest_win Effect
- **THEN** 无额外效果

### Requirement: ContestLose Trigger 执行
CombatContestHandler SHALL 在对拼结算后，对输家的 PendingSlot 来源卡检查是否有 TriggerId 为 "trigger_on_contest_lose" 的 Effect：
- 若有，SHALL 执行该 Effect 的所有 Action（经 Condition 门控）

#### Scenario: 输家有 ContestLose 效果
- **WHEN** 输家卡牌包含 trigger_on_contest_lose Effect
- **THEN** 执行该 Effect 的 Action

### Requirement: 平局时不触发 Win/Lose Trigger
对拼结果为平局时，SHALL 不触发任何 ContestWin 或 ContestLose 的 Effect。

#### Scenario: 平局无后续 Trigger
- **WHEN** 对拼结果为平局（数值相等）
- **THEN** 不执行 ContestWin/ContestLose Effect