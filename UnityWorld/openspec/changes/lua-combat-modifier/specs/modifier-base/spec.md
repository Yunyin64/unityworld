## MODIFIED Requirements

### Requirement: NpcModifier 和 CardModifier 预留为空扩展点
系统 SHALL 提供 `NpcModifier` 和 `CardModifier` 继承自 `IModifierBase` 的类占位，用于标明扩展意图。NpcModifier SHALL 实现 `ILuaBindable` 接口，持有 `env` 属性供未来 Lua 化使用。CardModifier 本期不实现 Lua 绑定。

#### Scenario: NpcModifier 可被实例化而不报错
- **WHEN** 实例化 NpcModifier
- **THEN** 不抛出异常，IsExpired 行为与基类一致

#### Scenario: CardModifier 可被实例化而不报错
- **WHEN** 实例化 CardModifier
- **THEN** 不抛出异常，IsExpired 行为与基类一致

## ADDED Requirements

### Requirement: IModifierBase 叠层行为契约
IModifierBase 的叠层字段 SHALL 遵循以下行为契约：
- `MaxStack`：最大叠加层数。1 表示不叠加（重复添加时刷新而非叠层），0 表示无上限。
- `CurrentStack`：当前层数，首次添加时为 1。
- `RefreshOnStack`：为 true 时，叠层操作 SHALL 将 RemainingTime 重置为 Duration。

#### Scenario: MaxStack=1 不叠加
- **WHEN** MaxStack=1 且已有 1 层，再次 AddBuff
- **THEN** CurrentStack 保持 1，若 RefreshOnStack=true 则刷新 RemainingTime

#### Scenario: MaxStack=0 无上限叠加
- **WHEN** MaxStack=0 且 CurrentStack=999，再次 AddBuff
- **THEN** CurrentStack 变为 1000，无上限

#### Scenario: RefreshOnStack=false 不刷新时间
- **WHEN** RefreshOnStack=false，触发叠层
- **THEN** CurrentStack 增加，RemainingTime 不变
