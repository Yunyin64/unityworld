# consume-stack Specification

## Purpose

规定世界侧卡牌消耗品的堆叠数量、容量定义与读取方式，将堆叠语义限制在 Consume 关键词下，保持普通卡牌默认行为不变；本规格不宣称旧消耗品已经完成背包战斗效果迁移。

## Requirements

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

### Requirement: 消耗卡战前校验并在成功主动发动扣一次

Consume SHALL 作为普通 Keyword 保存当前和最大使用次数。战前布局和候补选择 SHALL 拒绝零次数卡。战斗从卡实例复制当前次数。具有主动 Main 的消耗卡每次成功扫描提交扣 1，次数不足时不能发动，失败不扣次数、行额度或蓄灵。条件 SHALL 能比较当前次数与指定值。

#### Scenario: 最后一次可实际发动

- **WHEN** 一张消耗为 1/3 的满灵丹药成功发动
- **THEN** 扣为 0/3，本次效果完整执行。之后再次满足灵元条件也不发动

### Requirement: 到零触发耗尽并按灵活规则离场

消耗卡的剩余份数从正数降至零时 SHALL 派发一次消耗耗尽事件。带灵活的消耗卡 SHALL 在最后一次效果处理结束后离场，释放占格并派发一次离开事件。非灵活消耗卡留在原格，零份时无法再次主动发动。离场时清掉相应的场地占用和监听。

#### Scenario: 待发丹药最后一次仍完成结算

- **WHEN** 带灵活的 1/1 攻击丹药成功发动并进入待发槽
- **THEN** 收到一次消耗耗尽；丹药留场完成这次拼点和后续结算，再释放占格并收到一次离开。

#### Scenario: 战斗结束前仍未配对

- **WHEN** 超时结束战斗时，灵活丹药最后一次发动的拼点仍在等待
- **THEN** 沿用结束规则取消待发，在生成消耗结果前执行一次离开。

### Requirement: 实战按原卡实例提交剩余物品份数

正式战斗 SHALL 按卡实例将物品剩余份数提交一次，包含已离场的卡。零份卡 SHALL 退出备战并保留在持有列表。试战和重复读取结算结果不得改变原卡当前份数。

#### Scenario: 实战留下两份，下一场接着用

- **WHEN** 世界卡有 3 份食物，正式战斗成功食用一次
- **THEN** 正式结算后原卡持有 2 份，再次参战从 2 份开始。

#### Scenario: 灵活丹药的预览与实战

- **WHEN** 一张带灵活的 1/3 丹药在试战中用尽，再以同一张世界卡进行正式战斗并用尽
- **THEN** 试战后仍持有 1 份；实战结算后剩 0 份并退出备战。重复读取战果仍为 0 份。
