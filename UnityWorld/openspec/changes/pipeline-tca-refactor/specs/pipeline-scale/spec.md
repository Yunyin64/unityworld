## ADDED Requirements

### Requirement: ScaleNode 数据结构
系统 SHALL 提供 `ScaleNode` 类，包含 `Query`（string，统计表达式如 "Count"/"Sum"）、`Scope`（可选 Scope 对象，指定统计来源）。支持 JSON 反序列化。

#### Scenario: 计数型 Scale
- **WHEN** ScaleNode.Query == "Count" 且 Scope == (Self, ["IsSpell"], All)
- **THEN** 求值返回自己所有法术卡的数量

#### Scenario: 无 Scope 的 Scale
- **WHEN** ScaleNode.Scope 为 null
- **THEN** 使用管线上下文的默认 Scope（Self）

### Requirement: ScaleNode 求值
系统 SHALL 提供 Scale 求值逻辑：根据 Query 类型对 Scope 选出的目标进行统计，返回 int 值。

#### Scenario: Count 查询
- **WHEN** Query == "Count"，Scope 选出 3 个目标
- **THEN** 返回 3

#### Scenario: 求值结果为 0
- **WHEN** Scope 选出 0 个目标
- **THEN** 返回 0，后续 Action/Aura 的 Value 乘以 0 等于无效果

### Requirement: Scale 倍率应用
Scale 求值结果 SHALL 作为乘数应用于后续 Action/Aura 列表中每个节点的 Value 参数。

#### Scenario: Scale=3，Action.Value=2
- **WHEN** Scale 求值返回 3，Action 节点 Value 为 2
- **THEN** 实际执行效果值为 6（3 × 2）

#### Scenario: 无 Scale 节点
- **WHEN** 管线中 Scale 为 null
- **THEN** 等价于乘数 1，Action/Aura 使用原始 Value
