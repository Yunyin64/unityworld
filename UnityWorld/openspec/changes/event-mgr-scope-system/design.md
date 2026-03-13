## Context

当前 `EventMgr` 是一个简单的全局枚举事件总线，使用 `Em_Event` 枚举作为键，所有监听者都在同一个全局列表中。游戏需要模拟大量 NPC（主世界数百个），每帧都会产生大量状态变化事件（移动、战斗、交互等）。同时，Modifier 系统需要能动态挂载/卸载对特定对象局部事件的监听，用于实现 Trait 效果（如"每移动一次 +1 阅历"）。此外，为支持策划和玩家通过 Lua 脚本扩展 Trait/Modifier 效果，事件系统需要外部脚本友好。

现有问题：
- 全局广播无法做到"只通知 npc#42 身上的监听者"
- `Em_Event` 枚举无法被外部脚本/策划扩展
- `object[]` 参数造成装箱 GC 压力
- `NotImplementedException` 占位方法会在生命周期调用时崩溃

## Goals / Non-Goals

**Goals:**
- 引入 `EventScope` 枚举分层，支持 `Global / Npc / Tile / Plane` 范围的精准广播
- 用字符串 ID + `EventDefine` JSON 定义替代 `Em_Event` 枚举，允许策划/玩家扩展新事件
- 调用方显式传递 scope 列表，EventMgr 对比 EventDefine 声明进行 Warn 提示
- `RegisterEvent` 支持 `listenerKey` 字符串参数，便于调试时查看 scope 内监听者
- 悬空监听（已销毁对象的监听）自动清理 + Warn
- 预留 `IEventListener` 接口供 Lua 集成（`LuaEventListener` 占位）
- 修复所有 `NotImplementedException`，使生命周期方法正常运行

**Non-Goals:**
- 本次不实现 Lua 实际集成（只预留接口）
- 不实现事件队列/延迟广播（同步触发即可）
- 不实现事件优先级/拦截机制
- 不改动 Modifier 体系内部逻辑（只新增事件挂载能力的基础设施）

## Decisions

### 决策1：事件键用字符串 ID 而非枚举

**选择**：`string eventId`（如 `"NpcMoved"`），配合 `EventDefine` JSON 定义  
**原因**：枚举无法被外部 Lua 脚本或 JSON 配置扩展；字符串 ID 可在运行时动态注册，且对 Lua 天然友好  
**替代方案**：保留枚举 + 注册表映射 → 更繁琐，Lua 侧仍需维护枚举映射

### 决策2：Scope 用枚举而非字符串

**选择**：`enum EventScope { Global, Npc, Tile, Plane, ... }`  
**原因**：游戏内实体类型是有限且固定的（不像事件类型可扩展），枚举提供编译期安全和 switch 分支  
**替代方案**：字符串 scope → 容易拼写错误，无 IDE 提示

### 决策3：调用方显式传 scope 列表，EventMgr 做 Warn 校验

**选择**：方案A —— `TriggerEvent(eventId, args, (Scope.Npc, id), (Scope.Tile, tileId), ...)`  
**原因**：最直接透明，调用侧清楚地表达"这次事件涉及哪些对象"；对比方案C（args 实现 FillScopes 接口），本方案更简单且同样能做 Warn 提示  
**校验逻辑**：EventMgr 对比 `EventDefine.Scopes` 与调用方传入的 scope 列表，发现 EventDefine 声明的 scope 未被传入时 `LogMgr.Warn`

### 决策4：内部存储结构

```
// 核心数据结构
Dictionary<string, Dictionary<ScopeKey, List<ListenerEntry>>>
// eventId  →  scope实例  →  监听者列表

struct ScopeKey {
    EventScope Scope;  // 枚举
    string     Id;     // Global 时为 ""，其他为对象 ID 字符串
}

class ListenerEntry {
    string            ListenerKey;  // 调试用 key
    IEventListener    Listener;     // C# or Lua handler
    WeakReference?    OwnerRef;     // 可选：对象引用用于悬空检测
}
```

### 决策5：IEventListener 接口

```csharp
interface IEventListener {
    void OnEvent(string eventId, ScopeKey scope, object args);
}
```
C# 侧提供 `DelegateEventListener`（包装 `Action<string, ScopeKey, object>`），Lua 侧提供 `LuaEventListener` 占位。

### 决策6：生命周期方法处理

`EventMgr` 不需要 `Tick/Render/Save/Load`，这些方法实现为空方法（不 throw），`Begin()` 中初始化 Instance。`Step()` 旧方法保留但标记 Obsolete。

## Risks / Trade-offs

- **字符串 eventId 的拼写错误** → 触发不存在的事件时 `LogMgr.Warn`，EventDefineMgr 提供 `Contains()` 校验  
- **scope 列表漏传导致静默失效** → EventMgr 对比 EventDefine 声明做 Warn 提示，不静默  
- **ListenerEntry 列表在触发中被修改** → 保留现有 `triggering` 计数器 + `m_TempEvent` 延迟队列机制  
- **object args 装箱 GC** → 本次不解决，高频事件结构体化优化留作后续迭代  
- **LuaEventListener 仅占位** → Lua 集成时需补全，接口已预留

## Migration Plan

1. 新增 `EventDefine.cs`、`EventDefineMgr.cs`、`EventScope` 枚举、`ScopeKey`、`IEventListener`
2. 重写 `EventMgr.cs`（保留文件，不删除）
3. `Em_Event` 枚举保留，加 `[Obsolete]` 标记
4. `GameDataMgr` 或 `WorldMgr` 注册 `EventDefineMgr`
5. 现有调用方（当前为空，`Em_Event` 枚举也为空）无需迁移

## Open Questions

- `EventScope` 后续是否需要 `Faction`、`Combat` 等新 scope？ → 按需添加枚举值即可，无破坏性变更
- `WeakReference` 悬空检测是否过重？ → 初期可不做 WeakRef，改为"RemoveEvent 时主动清理"约定
