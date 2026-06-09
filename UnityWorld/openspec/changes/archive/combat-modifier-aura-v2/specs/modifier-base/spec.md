## MODIFIED Requirements

### Requirement: ModifierBase 是所有修正源的抽象基类
系统 SHALL 提供 `ModifierBase` 抽象基类，定义所有修正源的公共属性：唯一 ID、来源标识（SourceId）、持续时间（Duration，-1 表示永久）、剩余时间（RemainingTime）。所有具体修正类型 MUST 继承此基类。

#### Scenario: 有限时修正源可追踪剩余时间
- **WHEN** 创建一个 Duration = 10 的 ModifierBase 子类实例
- **THEN** RemainingTime 初始为 10，IsExpired 返回 false

#### Scenario: 永久修正源永不过期
- **WHEN** 创建一个 Duration = -1 的 ModifierBase 子类实例
- **THEN** IsExpired 始终返回 false，无论经过多少 Tick

#### Scenario: 有限时修正源倒计时归零后标记过期
- **WHEN** RemainingTime 减少至 0 或以下
- **THEN** IsExpired 返回 true

## ADDED Requirements

### Requirement: CombatNpcModifier.CallLuaHook 迁移至 ILuaBindable 层
`CombatNpcModifier` 现有的 `CallLuaHook` 方法 SHALL 迁移到 `ILuaBindable` 层（通过接口默认实现或基类）。迁移后 `CombatNpcModifier` 删除自身的 `CallLuaHook` 实现，改为调用 `ILuaBindable` 层的统一实现。`CombatNpcModifierFunc` 等现有调用方无需修改签名，行为不变。

#### Scenario: 迁移后现有 hook 调用行为不变
- **WHEN** `CombatNpcModifierFunc.ModifierTick` 调用 `mod.CallLuaHook<bool>("OnTick", this)`
- **THEN** 行为与迁移前完全一致

#### Scenario: CombatCard 也可使用 CallLuaHook
- **WHEN** `CombatCard` 实现 `ILuaBindable`，调用 `CallLuaHook<bool>("OnUse", ...)`
- **THEN** 通过 `ILuaBindable` 层的统一实现调用 Lua 函数
