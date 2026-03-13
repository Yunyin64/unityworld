## ADDED Requirements

### Requirement: EventDefine 定义事件元数据
系统 SHALL 提供 `EventDefine` 数据类，继承 `DefineBase`，包含事件的字符串 ID、显示名称和声明广播的 Scope 列表。`EventDefine` SHALL 可通过 JSON 文件加载，允许策划和玩家在不修改 C# 代码的情况下定义新的游戏事件。

#### Scenario: 加载内置事件定义
- **WHEN** 系统启动时 `EventDefineMgr` 初始化
- **THEN** 所有 JSON 中定义的 `EventDefine` 均可通过 `EventDefineMgr.Instance?.Get(id)` 查询

#### Scenario: 查询不存在的事件定义
- **WHEN** 调用 `EventDefineMgr.Instance?.Get("NonExistentEvent")`
- **THEN** 返回 `null`，不抛出异常

#### Scenario: 事件定义包含 Scope 声明
- **WHEN** 读取 `EventDefine` 的 `Scopes` 字段
- **THEN** 返回该事件声明要广播的 `EventScope[]`，如 `[Global, Npc, Tile, Plane]`

### Requirement: EventDefineMgr 支持全量查询与存在性检查
`EventDefineMgr` SHALL 实现 `IDataMgrBase<EventDefine>`，提供 `Get(id)`、`GetAll()`、`Contains(id)` 接口，并在 `GameDataMgr` 或 `WorldMgr` 初始化时注册。

#### Scenario: 全量枚举所有事件定义
- **WHEN** 调用 `EventDefineMgr.Instance?.GetAll()`
- **THEN** 返回所有已加载的 `EventDefine` 集合

#### Scenario: 存在性检查
- **WHEN** 调用 `EventDefineMgr.Instance?.Contains("NpcMoved")`，且该定义存在
- **THEN** 返回 `true`
