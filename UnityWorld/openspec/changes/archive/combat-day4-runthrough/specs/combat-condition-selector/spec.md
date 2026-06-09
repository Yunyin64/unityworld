## ADDED Requirements

### Requirement: Condition 选择器执行框架
CombatConditionHandler SHALL 提供 `Evaluate(conditionId, caster, scene, env)` 方法，接收 ConditionId 字符串，根据 ID 分发到对应的硬编码逻辑。
- 返回 bool 表示条件是否满足
- 条件满足时 SHALL 向传入的 ContextBase env 中写入选择结果（如 "TargetCardId"、"Targetint"、"TargetCardIds"）
- 空字符串或 "cond_none" SHALL 始终返回 true 且不写入任何选择结果
- 未识别的 ConditionId SHALL 返回 false 并输出警告日志

#### Scenario: 空条件始终通过
- **WHEN** ConditionId 为空字符串或 "cond_none"
- **THEN** Evaluate 返回 true，env 中不新增任何 key

#### Scenario: 未知条件拒绝
- **WHEN** ConditionId 为一个未在 switch 中实现的值（如 "cond_unknown"）
- **THEN** Evaluate 返回 false，输出警告日志

### Requirement: cond_random_enemy_card_in_cd 实现
CombatConditionHandler SHALL 实现 `cond_random_enemy_card_in_cd` 条件：
- 从 caster 的 Target（敌方 CombatNpc）的 CardStates 中筛选 IsActive 且 CurrentCdTick < Cooldown 的卡
- 从筛选结果中用 Rng 随机选一张
- 将选中卡的 CardData.DefineId 写入 env 的 "TargetCardId" key
- 若无可选卡牌则返回 false

#### Scenario: 敌方有 CD 中的卡
- **WHEN** 敌方有 2 张卡在 CD 中
- **THEN** Evaluate 返回 true，env["TargetCardId"] 为其中一张的 DefineId

#### Scenario: 敌方无 CD 中的卡
- **WHEN** 敌方所有卡的 CD 都已满或无卡
- **THEN** Evaluate 返回 false，不写入 TargetCardId

### Requirement: cond_card_above 实现
CombatConditionHandler SHALL 实现 `cond_card_above` 条件：
- 在 caster 的 CardStates 列表中，找到当前卡牌的索引位置
- 选择索引 - 1 位置的卡牌（上方卡牌）
- 将该卡的 CardData.DefineId 写入 env 的 "TargetCardId" key
- 若已是第一张（无上方卡）则返回 false

#### Scenario: 卡牌上方有卡
- **WHEN** 当前卡牌在 CardStates 中索引为 2（存在索引 1 的卡）
- **THEN** Evaluate 返回 true，env["TargetCardId"] 为索引 1 处卡牌的 DefineId

#### Scenario: 卡牌已在最上方
- **WHEN** 当前卡牌在 CardStates 中索引为 0
- **THEN** Evaluate 返回 false

### Requirement: Effect 执行时 Condition 门控
CombatCardFlowHandler.ResolveEffectCard SHALL 在执行 Effect 的 Actions 之前检查 ConditionId：
- 若 ConditionId 非空且非 "cond_none"，调用 CombatConditionHandler.Evaluate
- 若 Evaluate 返回 false，跳过该 Effect 的所有 Actions
- 若 Evaluate 返回 true，env 中的选择结果 SHALL 传递给后续 Action 的 ActionContext.Env

#### Scenario: 有条件且条件满足
- **WHEN** Effect 的 ConditionId = "cond_card_above" 且卡牌上方有卡
- **THEN** 执行该 Effect 的所有 Action，ActionContext.Env 中包含 TargetCardId

#### Scenario: 有条件但条件不满足
- **WHEN** Effect 的 ConditionId = "cond_random_enemy_card_in_cd" 且敌方无 CD 中的卡
- **THEN** 跳过该 Effect 的所有 Action