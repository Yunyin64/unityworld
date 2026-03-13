## Why

当前 `EventMgr` 只支持全局广播，无法高效支撑大规模 NPC 模拟场景（每帧数百个 NPC 状态变化）。同时游戏玩法需要 Modifier 能动态订阅对象局部事件（如"该 NPC 每移动一次 +1 阅历"），全局广播模式会导致每次触发都遍历所有监听者，性能与扩展性均不可接受。另外，为支持策划和玩家通过 Lua 脚本自定义 Trait/Modifier 效果，事件系统需要对外部脚本友好。

## What Changes

- **BREAKING** 移除 `Em_Event` 枚举作为事件键，改用字符串 ID（`EventDefine.ID`）驱动事件系统
- 新增 `EventDefine` 数据定义类，支持 JSON 加载，可由策划/玩家扩展
- 新增 `EventDefineMgr`，负责加载和查询所有 EventDefine
- 新增 `EventScope` 枚举（`Global / Npc / Tile / Plane` 等游戏内有限实体层级）
- `EventMgr` 内部存储改为 `Dict<eventId, Dict<ScopeKey, List<IEventListener>>>`，支持 Scope 分层广播
- 新增 `ScopeKey` 结构体（`EventScope 枚举 + string ID`）
- `EventMgr.EventTrigger` 保持命名，但签名改为接收 `string eventId`、`object args`、及 `params (EventScope, string)[]` scope 列表
- `EventMgr.RegisterEvent` 增加第一个参数 `string listenerKey`（便于调试查看 scope 内监听者）
- 新增对"EventDefine 声明的 Scope 但调用方未传"的 `LogMgr.Warn` 提示机制
- 悬空监听（对象已销毁但监听未移除）自动清理 + `LogMgr.Warn`
- Lua 集成预留接口：`IEventListener` 接口，`LuaEventListener` 包装类占位

## Capabilities

### New Capabilities
- `event-define`: EventDefine 静态数据定义 + EventDefineMgr 加载查询
- `event-scope-dispatch`: EventMgr Scope 分层注册/广播/清理核心机制

### Modified Capabilities
- 无（EventMgr 为全新重写，旧枚举体系废弃）

## Impact

- `Scripts/Core/Systems/EventMgr.cs`：核心重写
- `Scripts/Game/Data/Enum/EnumTypes.cs`：`Em_Event` 枚举保留但标记废弃（不删除文件）
- `Scripts/Game/Data/Defines/EventDefine.cs`：新建
- `Scripts/Game/Data/Mgr/EventDefineMgr.cs`：新建
- `Scripts/Game/World/WorldMgr.cs`：注册 EventDefineMgr
- 所有调用 `EventTrigger` / `RegisterEvent` 的位置：调用签名变更
- Lua 集成层（预留接口，暂不实现）
