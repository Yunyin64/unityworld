## ADDED Requirements

### Requirement: LandMarkId 唯一标识
系统 SHALL 提供 `LandMarkId` 值类型（struct），用于唯一标识运行时地标实例，支持 `==`、`!=` 比较和字典键。

#### Scenario: LandMarkId 相等性
- **WHEN** 两个 `LandMarkId` 由同一整数值构造
- **THEN** `a == b` 为 true，`a.GetHashCode() == b.GetHashCode()`

### Requirement: LandMarkDefine 静态配置
系统 SHALL 提供 `LandMarkDefine`（继承 `DefineBase`），字段包括：
- `IsNatural: bool`：true = 自然奇观（计入原初快照），false = 后天建筑
- `IsSingleton: bool`：全图唯一
- `SpawnWeight: float`：散布生成权重（0 = 不参与散布随机）
- `Tags: List<string>`：叙事标签，供事件/NPC 匹配
- `PlacementTerrains: List<TerrainType>`：允许生成的地形（空 = 不限）
- `PlacementExtraTerrains: List<string>`：要求的拓展地形标签 ID（空 = 不限）
- `ModifierDefineIds: List<string>`：该地标对地块施加的元气修正 Define ID 列表

#### Scenario: 自然奇观配置
- **WHEN** 一个 `LandMarkDefine` 的 `IsNatural = true`
- **THEN** 原初快照步骤会将此地标的 Modifier 纳入快照计算

#### Scenario: 生成条件过滤
- **WHEN** 地标 Define 配置 `PlacementTerrains = [Mountain]`
- **THEN** 该地标只能生成在 `TerrainType.Mountain` 的地块上

### Requirement: LandMark 运行时实体
系统 SHALL 提供 `LandMark` 类，含：
- `Id: LandMarkId`
- `DefineId: string`
- `Position: TileId`（所在地块坐标）
- `PlaneId: PlaneId`（所在位面）
- `IsActive: bool`（是否有效，销毁后置 false）

#### Scenario: LandMark 生成后关联地块
- **WHEN** `LandMarkMgr` 在某地块上生成一个 LandMark
- **THEN** `LandMark.Position == tile.Id`，且 `tile.LandMarkId == landmark.Id`

### Requirement: LandMarkMgr 管理所有地标实例
系统 SHALL 提供 `LandMarkMgr`（实现 `IDomainMgrBase`），单例，功能：
- `Place(plane, tile, defineId)`：在指定地块生成地标，挂载 Modifier 到 Tile，写入 `Tile.LandMarkId`
- `Remove(id)`：移除地标，清理 Tile 上对应 Modifier，重置 `Tile.LandMarkId = null`
- `GetById(id)`：按 ID 查询地标
- `GetByPlane(planeId)`：获取某位面所有地标
- `GetNaturalLandMarks(planeId)`：获取某位面所有 `IsNatural=true` 的地标

#### Scenario: 生成地标时 Modifier 挂载
- **WHEN** 调用 `LandMarkMgr.Place(plane, tile, "holy_mountain")`，Define 中有 ModifierDefineIds
- **THEN** 每个 ModifierDefine 被实例化为 `TileModifier` 并加入 `tile.Modifiers`

#### Scenario: 移除地标时清理 Modifier
- **WHEN** 调用 `LandMarkMgr.Remove(landmarkId)`
- **THEN** 对应 Tile 上由该 LandMark 挂载的所有 Modifier 被移除，`Tile.LandMarkId = null`

#### Scenario: Singleton 地标全图唯一
- **WHEN** 某 `LandMarkDefine.IsSingleton = true`，且已生成一个实例
- **THEN** 再次尝试生成时，`LandMarkMgr.Place` 返回 null，不创建第二个

### Requirement: LandMarkDefineMgr 数据加载
系统 SHALL 提供 `LandMarkDefineMgr`（实现 `IDataMgrBase<LandMarkDefine>`），从 JSON 加载，在 `GameDataMgr` 中注册。

#### Scenario: 按 ID 查询地标定义
- **WHEN** 调用 `LandMarkDefineMgr.Instance?.Get("volcanic_pillar")`
- **THEN** 返回对应 Define，不存在返回 null
