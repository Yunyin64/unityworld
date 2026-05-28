## ADDED Requirements

### Requirement: AuraPipeline 数据结构
系统 SHALL 提供 `AuraPipeline` 类，包含：
- `Condition`（ConditionNode，可选）
- `Scale`（ScaleNode，可选）
- `Auras`（List\<AuraNode\>，至少一个）

不包含 Trigger 字段。支持 JSON 反序列化。

#### Scenario: 无条件 Aura
- **WHEN** JSON 为 `{"auras": [{"funcId": "AttackBoost", "value": 2}]}`
- **THEN** 反序列化后 Condition 为 null，Scale 为 null，Auras 含 1 项，表示无条件持续生效

#### Scenario: 有条件有倍率的 Aura
- **WHEN** JSON 包含 condition + scale + auras
- **THEN** 三段全部正确反序列化

### Requirement: AuraNode 数据结构
系统 SHALL 提供 `AuraNode` 类，包含 `FuncId`（string，持续效果函数标识）、`Value`（float，基础数值参数）、`Scope`（可选 Scope，指定对谁生效）和 `Requires`（List\<string\>，目标前置要求）。

#### Scenario: 自身属性增强 Aura
- **WHEN** AuraNode.FuncId == "AttackBoost"，Value == 2，Scope 为 null
- **THEN** 默认对自己持续提供 +2 攻击力

#### Scenario: 针对目标的 Aura
- **WHEN** AuraNode.Scope == (Enemy, [], All)
- **THEN** 对所有敌方持续生效

### Requirement: AuraPipeline 持续生效逻辑
系统 SHALL 实现 AuraPipeline 的持续检测：
1. 如有 Condition，每 Tick 求值
2. 条件满足 → 如有 Scale 求值得到 N → Aura 效果按 Value × N 生效
3. 条件不满足 → Aura 效果移除

#### Scenario: 条件满足时生效
- **WHEN** AuraPipeline 的 Condition 求值为 true，Scale 返回 3，Aura.Value 为 1
- **THEN** 实际持续效果值为 3

#### Scenario: 条件从满足变为不满足
- **WHEN** 上一 Tick Condition 为 true（Aura 生效中），本 Tick Condition 变为 false
- **THEN** 系统 SHALL 移除该 Aura 的效果

#### Scenario: 无 Condition 的 Aura
- **WHEN** AuraPipeline.Condition 为 null
- **THEN** Aura 始终生效（只要卡牌/来源存在）

### Requirement: AuraPipeline 与 Modifier 系统集成
AuraPipeline 生效时 SHALL 通过现有 Modifier 系统挂载效果；失效时通过 Modifier 系统摘除。

#### Scenario: Aura 挂载为 Modifier
- **WHEN** AuraPipeline 首次生效
- **THEN** 系统创建对应 Modifier 实例挂载到目标上

#### Scenario: Aura 失效时移除 Modifier
- **WHEN** AuraPipeline 条件不满足或来源卡消失
- **THEN** 系统 SHALL 移除对应 Modifier 实例
