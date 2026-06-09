## ADDED Requirements

### Requirement: ActionDefine 参数化格式
ActionDefine SHALL 使用参数化 Params 数组，每项包含 Name（参数名）、Type（String/Int/Float）、Value（可选值列表）、Score（各选值对应分数）。

#### Scenario: action_atk 参数化定义
- **WHEN** 定义 action_atk
- **THEN** 其 Params 包含 Element(5选1)、PhysicalType(4选1)、AttackValue(5选1)，每个值有对应 Score

#### Scenario: Score 累加计算
- **WHEN** 随机造卡选中 Element="Jin"(Score=0)、PhysicalType="Zhan"(Score=0)、AttackValue=3(Score=3)
- **THEN** 该 Action 的总分 = 0+0+3 = 3

### Requirement: ConditionDefine 参数化格式
ConditionDefine SHALL 使用与 ActionDefine 相同的参数化 Params 结构，支持条件模板的参数可选值与评分。

#### Scenario: cond_target_element 参数化
- **WHEN** 定义条件"目标为某元素"
- **THEN** Params 包含 TargetElement(5选1)，各值有对应 Score

### Requirement: TriggerDefine 保持现有格式
TriggerDefine SHALL 保持现有格式不变（ID/DisplayName/Desc/Score/Tags/ConflictTags/Weight），因为 Trigger 不需要参数化。

#### Scenario: trigger_on_attack 格式不变
- **WHEN** 读取 TriggerDefine
- **THEN** 格式与当前一致，包含 Tags 用于匹配

### Requirement: Tag 匹配造卡
造卡系统 SHALL 通过 Tag 交集筛选可用的 T/C/A 模板：输入 TagList → 匹配 Tags 含交集的 Trigger/Condition/Action → 组合。

#### Scenario: Tag 匹配 Action
- **WHEN** 输入 Tags=["金","攻击"]
- **THEN** action_atk 被匹配到（其 Tags 包含"攻击"）

#### Scenario: ConflictTags 排除
- **WHEN** 已选 Trigger 的 ConflictTags 包含"防御"
- **THEN** Tags 含"防御"的 Action 不被选入

### Requirement: Lua 代码生成
每个 ActionDefine/ConditionDefine SHALL 包含 LuaTemplate 字段（可选），用于将选定参数填入模板生成 Lua 代码片段。

#### Scenario: action_atk 生成 Lua
- **WHEN** 选定 Element="Jin"、PhysicalType="Zhan"、AttackValue=3
- **THEN** 生成代码片段 `Attack(ctx, "Jin", "Zhan", 3)`

#### Scenario: 无 LuaTemplate 时使用默认模板
- **WHEN** ActionDefine 未定义 LuaTemplate
- **THEN** 使用 `{FuncName}(ctx, {Param1}, {Param2}, ...)` 的默认生成规则

### Requirement: 分数约束造卡
造卡系统 SHALL 支持给定目标分数/稀有度，通过搜索 T×C×A 参数组合找到满足约束的方案。

#### Scenario: 目标分数为 5
- **WHEN** 指定 targetScore=5
- **THEN** 搜索出 Action 参数组合使总分接近 5（允许±1 偏差）