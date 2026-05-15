## ADDED Requirements

### Requirement: ExpirePolicy 枚举定义
`IModifierBase` 接口 SHALL 包含 `ExpirePolicy` 属性，类型为 `ExpirePolicy` 枚举。枚举值 SHALL 包含：`Never`、`TimeBased`、`StackBased`、`TimeOrStack`、`TriggerBased`。

#### Scenario: 枚举值完整
- **WHEN** 引擎代码或 Lua 读取任意 Modifier 的 ExpirePolicy
- **THEN** 返回值 MUST 是 Never / TimeBased / StackBased / TimeOrStack / TriggerBased 之一

### Requirement: RemoveTriggerId 字段定义
`IModifierBase` 接口 SHALL 包含 `RemoveTriggerId` 字符串属性，引用 `TriggerDefine.ID`。值为 null 或空字符串表示不响应任何触发器事件。

#### Scenario: RemoveTriggerId 引用有效 TriggerDefine
- **WHEN** Modifier 的 RemoveTriggerId 为非空字符串
- **THEN** 该值 MUST 对应一个已加载的 TriggerDefine.ID

#### Scenario: RemoveTriggerId 为空
- **WHEN** Modifier 的 RemoveTriggerId 为 null 或空字符串
- **THEN** 该 Modifier 不响应任何触发器广播，不影响其他过期判定

### Requirement: IsExpired 统一判定
`IModifierBaseExt.IsExpired()` 扩展方法 SHALL 基于 `ExpirePolicy` 做统一判定：
- `Never`：始终返回 false
- `TimeBased`：当 `Duration > 0 && RemainingTime <= 0` 时返回 true
- `StackBased`：当 `CurrentStack <= 0` 时返回 true
- `TimeOrStack`：TimeBased 或 StackBased 任一满足时返回 true
- `TriggerBased`：始终返回 false（由事件广播直接移除，不靠轮询）

#### Scenario: TimeBased 过期
- **WHEN** Modifier 的 ExpirePolicy 为 TimeBased，Duration 为 30，RemainingTime 递减到 0
- **THEN** IsExpired() 返回 true

#### Scenario: TimeBased 永久不过期
- **WHEN** Modifier 的 ExpirePolicy 为 TimeBased，Duration 为 -1
- **THEN** IsExpired() 始终返回 false

#### Scenario: StackBased 过期
- **WHEN** Modifier 的 ExpirePolicy 为 StackBased，CurrentStack 被减到 0
- **THEN** IsExpired() 返回 true

#### Scenario: TimeOrStack 时间先到
- **WHEN** Modifier 的 ExpirePolicy 为 TimeOrStack，RemainingTime 递减到 0 但 CurrentStack > 0
- **THEN** IsExpired() 返回 true

#### Scenario: TimeOrStack 层数先空
- **WHEN** Modifier 的 ExpirePolicy 为 TimeOrStack，CurrentStack 减到 0 但 RemainingTime > 0
- **THEN** IsExpired() 返回 true

#### Scenario: TriggerBased 不靠轮询
- **WHEN** Modifier 的 ExpirePolicy 为 TriggerBased
- **THEN** IsExpired() 始终返回 false

#### Scenario: Never 永不过期
- **WHEN** Modifier 的 ExpirePolicy 为 Never
- **THEN** IsExpired() 始终返回 false

### Requirement: ReduceStack 扩展方法
`IModifierBaseExt` SHALL 提供 `ReduceStack(this IModifierBase self, int count = 1)` 扩展方法。CurrentStack 减少后 MUST NOT 低于 0。

#### Scenario: 正常减层
- **WHEN** CurrentStack 为 3，调用 ReduceStack(1)
- **THEN** CurrentStack 变为 2

#### Scenario: 减层不低于零
- **WHEN** CurrentStack 为 1，调用 ReduceStack(5)
- **THEN** CurrentStack 变为 0（不为负数）

### Requirement: AddStack 扩展方法
`IModifierBaseExt` SHALL 提供 `AddStack(this IModifierBase self, int count = 1)` 扩展方法。当 `MaxStack > 0` 时 CurrentStack MUST NOT 超过 MaxStack。当 `MaxStack == 0` 时无上限。当 `RefreshOnStack` 为 true 且 `Duration > 0` 时 SHALL 重置 `RemainingTime = Duration`。

#### Scenario: 正常加层
- **WHEN** CurrentStack 为 2，MaxStack 为 5，调用 AddStack(1)
- **THEN** CurrentStack 变为 3

#### Scenario: 加层受 MaxStack 限制
- **WHEN** CurrentStack 为 4，MaxStack 为 5，调用 AddStack(3)
- **THEN** CurrentStack 变为 5

#### Scenario: MaxStack 为零无上限
- **WHEN** CurrentStack 为 99，MaxStack 为 0，调用 AddStack(1)
- **THEN** CurrentStack 变为 100

#### Scenario: RefreshOnStack 刷新时间
- **WHEN** RefreshOnStack 为 true，Duration 为 30，RemainingTime 为 10，调用 AddStack(1)
- **THEN** RemainingTime 重置为 30

### Requirement: 触发器广播移除机制
当引擎在事件点广播 triggerId 时，SHALL 遍历目标实体的所有 Modifier：
- 若 `modifier.RemoveTriggerId == triggerId` 且 `ExpirePolicy == TriggerBased`：直接标记移除
- 若 `modifier.RemoveTriggerId == triggerId` 且 `ExpirePolicy != TriggerBased`：调用 `ReduceStack(1)`

#### Scenario: TriggerBased 直接移除
- **WHEN** CardModifier 的 ExpirePolicy 为 TriggerBased，RemoveTriggerId 为 "OnUse"，卡牌使用时广播 "OnUse"
- **THEN** 该 CardModifier 被直接移除

#### Scenario: StackBased 配合触发器减层
- **WHEN** CombatNpcModifier 的 ExpirePolicy 为 StackBased，RemoveTriggerId 为 "OnHit"，CurrentStack 为 3，NPC 受击时广播 "OnHit"
- **THEN** CurrentStack 减为 2，Modifier 未被移除

#### Scenario: StackBased 触发器减至零过期
- **WHEN** CombatNpcModifier 的 ExpirePolicy 为 StackBased，RemoveTriggerId 为 "OnHit"，CurrentStack 为 1，NPC 受击时广播 "OnHit"
- **THEN** CurrentStack 减为 0，Modifier 在过期检查时被移除

### Requirement: 子类不得遮蔽 IsExpired
所有 `IModifierBase` 实现类 MUST NOT 声明自定义的 `IsExpired` 字段或属性。过期判定 MUST 统一通过 `IModifierBaseExt.IsExpired()` 扩展方法。

#### Scenario: TileModifier 无遮蔽
- **WHEN** TileModifier 类编译完成
- **THEN** 类中不存在名为 IsExpired 的字段或属性声明

#### Scenario: CardModifier 无遮蔽
- **WHEN** CardModifier 类编译完成
- **THEN** 类中不存在名为 isExpired 或 IsExpired 的字段或属性声明

### Requirement: Define 层支持 ExpirePolicy 和 RemoveTriggerId 配置
`CombatNpcModifierDefine` 和 `NpcModifierDefine` SHALL 包含 `ExpirePolicy` 和 `RemoveTriggerId` 字段，支持 JSON 配置。工厂方法创建 Modifier 实例时 SHALL 将 Define 的值赋给实例。

#### Scenario: Define 配置传递到实例
- **WHEN** CombatNpcModifierDefine 配置 ExpirePolicy 为 "StackBased"，RemoveTriggerId 为 "OnHit"
- **THEN** 通过 CreateModifier 创建的 CombatNpcModifier 实例的 ExpirePolicy 为 StackBased，RemoveTriggerId 为 "OnHit"

#### Scenario: Define 默认值
- **WHEN** CombatNpcModifierDefine 未配置 ExpirePolicy 和 RemoveTriggerId
- **THEN** ExpirePolicy 默认为 TimeBased，RemoveTriggerId 默认为 null 或空字符串
