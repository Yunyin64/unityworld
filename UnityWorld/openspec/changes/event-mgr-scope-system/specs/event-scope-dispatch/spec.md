## ADDED Requirements

### Requirement: EventScope 枚举定义广播层级
系统 SHALL 提供 `EventScope` 枚举，包含 `Global`、`Npc`、`Tile`、`Plane` 等游戏内有限实体类型作为广播层级。后续按需可扩展新枚举值，不破坏现有逻辑。

#### Scenario: Global scope 接收所有事件
- **WHEN** 任意事件被触发，且存在 Global scope 的监听者
- **THEN** Global scope 下的所有监听者均被调用，无论事件涉及哪个具体实体

#### Scenario: Npc scope 仅接收该 Npc 的事件
- **WHEN** eventId=`"NpcMoved"`，scope=`(Npc, "42")` 的事件被触发
- **THEN** 仅注册在 `(Npc, "42")` 下的监听者被调用，其他 Npc 的监听者不受影响

### Requirement: ScopeKey 唯一标识一个 scope 实例
系统 SHALL 提供 `ScopeKey` 值类型（struct），包含 `EventScope Scope` 和 `string Id` 两个字段。`Global` scope 时 `Id` 为空字符串 `""`。`ScopeKey` SHALL 正确实现值相等（`Equals`/`GetHashCode`），以保证在 Dictionary 中作键时行为正确。

#### Scenario: Global ScopeKey 的标准创建
- **WHEN** 创建 `ScopeKey.Global`
- **THEN** 等价于 `new ScopeKey(EventScope.Global, "")`

#### Scenario: 相同 Scope 和 Id 的两个 ScopeKey 相等
- **WHEN** `new ScopeKey(EventScope.Npc, "42") == new ScopeKey(EventScope.Npc, "42")`
- **THEN** 结果为 `true`，Dictionary 查找正常工作

### Requirement: EventMgr 支持 Scope 分层注册监听
`EventMgr.RegisterEvent` SHALL 接受 `string listenerKey`（调试标识）、`string eventId`、`ScopeKey scope`、`IEventListener listener` 四个参数，将监听者注册到指定 eventId 和 scope 的监听列表中。同一 `(eventId, scope, listener)` 组合重复注册时 SHALL 忽略（幂等）。

#### Scenario: 注册 Npc 局部监听
- **WHEN** `EventMgr.RegisterEvent("npc_moved_listener", "NpcMoved", new ScopeKey(EventScope.Npc, "42"), listener)` 被调用
- **THEN** listener 被加入 `"NpcMoved"` 事件的 `(Npc, "42")` scope 监听列表

#### Scenario: 重复注册同一监听不产生重复调用
- **WHEN** 同一 listener 对同一 `(eventId, scope)` 注册两次
- **THEN** 事件触发时 listener 只被调用一次

### Requirement: EventMgr 支持 TriggerEvent 分层广播
`EventMgr.TriggerEvent` SHALL 接受 `string eventId`、`object args`、以及 `params (EventScope scope, string id)[]` scope 列表，依次向 Global scope 和所有传入 scope 对应的监听者列表广播事件。触发时 SHALL 自动补充 Global scope 广播（无需调用方显式传入）。

#### Scenario: 触发 NpcMoved 同时广播到多个 scope
- **WHEN** `TriggerEvent("NpcMoved", args, (Npc,"42"), (Tile,"3_5"), (Plane,"1"))` 被调用
- **THEN** Global 监听者、`(Npc,"42")` 监听者、`(Tile,"3_5")` 监听者、`(Plane,"1")` 监听者均依次收到回调

#### Scenario: 触发未定义事件时输出 Warn
- **WHEN** `TriggerEvent("UnknownEvent", args)` 被调用，且 `EventDefineMgr` 中不存在该 ID
- **THEN** `LogMgr.Warn` 输出警告，事件不触发

#### Scenario: 调用方未传 EventDefine 声明的 scope 时输出 Warn
- **WHEN** `EventDefine("NpcMoved").Scopes` 包含 `Tile`，但调用方 `TriggerEvent` 未传入任何 Tile scope
- **THEN** `LogMgr.Warn` 提示 "NpcMoved: EventDefine 声明了 Tile scope 但调用方未提供"，事件仍正常触发已有 scope

### Requirement: EventMgr 支持移除监听
`EventMgr.RemoveEvent` SHALL 接受 `string eventId`、`ScopeKey scope`、`IEventListener listener`，从对应监听列表中移除该监听者。若监听者不存在则静默忽略。在事件触发过程中调用 `RemoveEvent` 时，SHALL 延迟到本次触发结束后执行（使用现有 `triggering` 计数器机制）。

#### Scenario: 正常移除监听
- **WHEN** `RemoveEvent("NpcMoved", new ScopeKey(Npc,"42"), listener)` 被调用
- **THEN** 后续触发该事件时 listener 不再被调用

#### Scenario: 触发中移除不崩溃
- **WHEN** 在事件回调内部调用 `RemoveEvent` 移除自身
- **THEN** 当前事件触发正常完成，下一次触发时该 listener 已被移除

### Requirement: EventMgr 悬空监听自动清理
当事件触发时发现某 `ListenerEntry` 的监听者已失效（null 或 Lua 侧对象已销毁），系统 SHALL 自动将其从列表中移除，并通过 `LogMgr.Warn` 输出警告，包含 `listenerKey` 以定位问题来源。

#### Scenario: 发现悬空监听时自动清理并 Warn
- **WHEN** 触发事件时遍历监听列表，发现某 entry 的 `Listener` 为 null
- **THEN** 该 entry 被加入待移除列表，触发结束后自动删除，并输出 `LogMgr.Warn` 含 `listenerKey`

### Requirement: IEventListener 接口预留 Lua 集成扩展点
系统 SHALL 定义 `IEventListener` 接口，包含 `void OnEvent(string eventId, ScopeKey scope, object args)` 方法。C# 侧 SHALL 提供 `DelegateEventListener` 实现类（包装 `Action<string, ScopeKey, object>` 委托）。SHALL 提供空实现的 `LuaEventListener` 占位类，注释标明待 Lua 集成时实现。

#### Scenario: DelegateEventListener 包装委托调用
- **WHEN** 通过 `new DelegateEventListener(myAction)` 注册，事件触发时
- **THEN** `myAction` 被以正确参数调用

#### Scenario: LuaEventListener 占位不崩溃
- **WHEN** `LuaEventListener.OnEvent(...)` 被调用（占位实现）
- **THEN** 方法直接返回，不抛出异常，输出一条 `LogMgr.Warn` 提示"LuaEventListener 尚未实现"
