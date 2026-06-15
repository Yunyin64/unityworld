## ADDED Requirements

### Requirement: CardBaseData 新增 Stack 字段
`CardBaseData` SHALL 新增 `Stack`（int，默认 0）和 `StackMax`（int，默认 0）字段，表示世界侧消耗品堆叠数量及上限。

#### Scenario: 默认值不影响现有卡
- **WHEN** 一张没有 Consume keyword 的卡被创建
- **THEN** Stack = 0，StackMax = 0，不影响任何现有逻辑

### Requirement: Stack 仅在 Consume keyword 下生效
Stack 机制 SHALL 仅在卡牌拥有 "Consume" keyword 时具有语义意义。无 Consume keyword 的卡 Stack 值无意义。

#### Scenario: Consume 卡的堆叠
- **WHEN** 一张 Keywords 包含 "Consume" 的卡，Stack = 100，StackMax = 999
- **THEN** 该卡表示持有 100 个该消耗品，最多堆叠 999

### Requirement: Card 提供 Stack 便捷访问器
Card 类 SHALL 提供 `GetStack()` 和 `GetStackMax()` 方法，从 BaseData 获取对应值。

#### Scenario: 通过 Card 访问 Stack
- **WHEN** 调用 `card.GetStack()`
- **THEN** 返回 BaseData.Stack 的当前值

### Requirement: CardDefine 支持 StackMax 定义
CardDefine 的 JSON SHALL 支持 `StackMax` 字段，实例化 Card 时写入 CardBaseData.StackMax。

#### Scenario: 从 Define 实例化带 StackMax 的卡
- **WHEN** CardDefine JSON 中 StackMax = 999
- **THEN** 实例化后 Card.BaseData.StackMax = 999
