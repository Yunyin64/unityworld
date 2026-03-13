## 1. 基础类型与枚举

- [x] 1.1 在 `Scripts/Game/Data/Enum/EnumTypes.cs` 中添加 `EventScope` 枚举（`Global, Npc, Tile, Plane`），并将 `Em_Event` 枚举标记 `[Obsolete]`
- [x] 1.2 新建 `Scripts/Core/Systems/ScopeKey.cs`，实现 `ScopeKey` struct（含 `Scope`、`Id` 字段、`Global` 静态属性、正确的 `Equals`/`GetHashCode`/`==`/`!=`）
- [x] 1.3 新建 `Scripts/Core/Systems/IEventListener.cs`，定义 `IEventListener` 接口（`void OnEvent(string eventId, ScopeKey scope, object args)`）
- [x] 1.4 在同文件或新建文件中实现 `DelegateEventListener`（包装 `Action<string, ScopeKey, object>`）
- [x] 1.5 新建 `Scripts/Core/Systems/LuaEventListener.cs`，实现 `LuaEventListener : IEventListener` 占位类（`OnEvent` 只输出 `LogMgr.Warn` 并返回）

## 2. EventDefine 数据层

- [x] 2.1 新建 `Scripts/Game/Data/Defines/EventDefine.cs`，继承 `DefineBase`，字段：`EventScope[] Scopes`（JSON 反序列化）
- [x] 2.2 新建 `Scripts/Game/Data/Mgr/EventDefineMgr.cs`，实现 `IDataMgrBase<EventDefine>`，提供 `Get`/`GetAll`/`Contains`，构造函数接收 JSON 路径
- [x] 2.3 新建事件定义 JSON 文件 `StreamingAssets/Data/EventDefines.json`（内含 `NpcMoved`、`NpcDied`、`TileAuraChanged` 等内置事件示例）
- [x] 2.4 在 `WorldMgr.Initialize()` 中注册 `EventDefineMgr`（加入 `_mgrs` 并在首批 `Init()` 前完成加载）

## 3. EventMgr 核心重写

- [x] 3.1 重写 `Scripts/Core/Systems/EventMgr.cs` 内部存储结构为 `Dictionary<string, Dictionary<ScopeKey, List<ListenerEntry>>>`，新增 `ListenerEntry` 内部类（含 `ListenerKey`、`Listener`）
- [x] 3.2 实现新版 `RegisterEvent(string listenerKey, string eventId, ScopeKey scope, IEventListener listener)`，幂等注册，`triggering > 0` 时走延迟队列
- [x] 3.3 实现新版 `RemoveEvent(string eventId, ScopeKey scope, IEventListener listener)`，`triggering > 0` 时走延迟队列
- [x] 3.4 实现 `TriggerEvent(string eventId, object args, params (EventScope scope, string id)[] scopes)`：自动补充 Global 广播、遍历各 scope 监听列表、悬空监听自动清理 + Warn、EventDefine scope 声明校验 Warn
- [x] 3.5 修复所有 `NotImplementedException`：`Begin()` 初始化 Instance、`Tick/Render/Save/Load/Update` 实现为空方法、保留 `Step()` 并标记 `[Obsolete]`
- [x] 3.6 保留 `LuaRegisterEvent`/`LuaUnRegisterEvent` 方法并标记 `[Obsolete]`，待 Lua 集成时替换

## 4. 验证与收尾

- [x] 4.1 在 `WorldMgr` 启动日志中打印已加载的 EventDefine 数量（`LogMgr.Dbg`）
- [x] 4.2 检查全局搜索所有旧 `EventTrigger(Em_Event, ...)` 和 `RegisterEvent(Em_Event, ...)` 调用，确认均已不存在（当前代码库中 `Em_Event` 枚举为空，无存量调用，只需确认）
- [x] 4.3 检查 `EventMgr` 所有 `public` 成员均有 `<summary>` XML 注释