## ADDED Requirements

### Requirement: ActionPipeline 数据结构
系统 SHALL 提供 `ActionPipeline` 类，包含：
- `Trigger`（TriggerNode，必有）
- `Condition`（ConditionNode，可选）
- `Scale`（ScaleNode，可选）
- `Actions`（List\<ActionNode\>，至少一个）

支持 JSON 反序列化。

#### Scenario: 最简 ActionPipeline
- **WHEN** JSON 为 `{"trigger": {"eventId": "OnPlay"}, "actions": [{"funcId": "Damage", "value": 3}]}`
- **THEN** 反序列化后 Trigger 有值，Condition 为 null，Scale 为 null，Actions 含 1 项

#### Scenario: 完整四段 ActionPipeline
- **WHEN** JSON 包含 trigger + condition + scale + actions
- **THEN** 四段全部正确反序列化，各自带独立 Scope

### Requirement: TriggerNode 数据结构
系统 SHALL 提供 `TriggerNode` 类，包含 `EventId`（string，事件类型标识）和 `Scope`（可选 Scope，指定监听谁的事件）。

#### Scenario: 监听自己的事件
- **WHEN** TriggerNode.EventId == "OnCharge"，Scope == (Self, ["IsSpell"])
- **THEN** 仅当自己的法术卡触发充能事件时激活管线

#### Scenario: 监听全局事件
- **WHEN** TriggerNode.Scope 为 null 或 Owner == Any
- **THEN** 任意来源的该事件都会激活管线

### Requirement: ConditionNode 数据结构
系统 SHALL 提供 `ConditionNode` 类，包含 `Check`（string，条件表达式标识）、`Scope`（可选 Scope，指定检查谁）和 `ParamValues`（可选参数值列表）。

#### Scenario: 条件通过
- **WHEN** ConditionNode.Check == "HasTag" 且 Scope 目标存在指定 Tag
- **THEN** 管线继续执行后续节点

#### Scenario: 条件不通过
- **WHEN** ConditionNode 求值返回 false
- **THEN** 管线中断，不执行后续 Scale 和 Actions

### Requirement: ActionNode 数据结构
系统 SHALL 提供 `ActionNode` 类，包含 `FuncId`（string，动作函数标识）、`Value`（float，基础数值参数）、`Scope`（可选 Scope，指定对谁执行）和 `Requires`（List\<string\>，目标前置要求）。

#### Scenario: 有目标的 Action
- **WHEN** ActionNode.FuncId == "Charge"，Scope == (Self, ["HasCD"], Random(1))
- **THEN** 对自己随机一张有 CD 的卡执行充能

#### Scenario: 无 Scope 的 Action
- **WHEN** ActionNode.Scope 为 null，FuncId == "Draw"
- **THEN** 默认对 Self（自己）执行抽牌

### Requirement: ActionPipeline 执行流程
系统 SHALL 按以下顺序执行 ActionPipeline：
1. 事件匹配 Trigger（Event + Scope）
2. 如有 Condition，求值，为 false 则中断
3. 如有 Scale，求值得到倍率 N
4. 遍历 Actions 列表，逐个执行（Value × N）

#### Scenario: 完整执行流程
- **WHEN** 事件匹配 Trigger，Condition 通过，Scale 返回 2，Actions 有一个 Damage(3)
- **THEN** 实际造成 6 点伤害

#### Scenario: Trigger 不匹配
- **WHEN** 发生的事件与 Trigger.EventId 不同
- **THEN** 管线不激活，不执行任何逻辑

#### Scenario: Actions 列表多个
- **WHEN** Actions 包含 [Damage(2), Draw(1)]
- **THEN** 按顺序执行：先造成伤害，再抽牌，Scale 倍率同时应用于两者
