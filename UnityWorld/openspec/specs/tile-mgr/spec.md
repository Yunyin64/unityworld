## ADDED Requirements

### Requirement: TileMgr 单例跨位面查询
系统 SHALL 提供 `TileMgr`（实现 `IDomainMgrBase`），单例，作为全局 Tile 查询入口，内部通过 `PlaneMgr.Instance` 访问各位面数据。

提供以下查询方法：
- `GetTile(planeId, tileId)`：跨位面按坐标获取 Tile
- `GetTilesByTerrain(planeId, terrain)`：获取某位面指定地形的所有 Tile
- `GetTilesByRegion(regionId)`：获取指定区域的所有 Tile（通过 `Region.OwnedTiles`）
- `GetTilesWithNoRegion(planeId)`：获取某位面所有无区域归属的 Tile

#### Scenario: 跨位面 Tile 查询
- **WHEN** 调用 `TileMgr.Instance?.GetTile(planeId, tileId)`
- **THEN** 返回对应 Tile，不存在（位面不存在或坐标无效）返回 null

#### Scenario: 按地形筛选 Tile
- **WHEN** 调用 `TileMgr.Instance?.GetTilesByTerrain(mainPlaneId, TerrainType.Mountain)`
- **THEN** 返回主世界所有 `Terrain == Mountain` 的 Tile 集合

#### Scenario: 按区域筛选 Tile
- **WHEN** 调用 `TileMgr.Instance?.GetTilesByRegion(regionId)`
- **THEN** 返回该区域 `OwnedTiles` 对应的所有 Tile 实例

#### Scenario: 查询无主 Tile
- **WHEN** 调用 `TileMgr.Instance?.GetTilesWithNoRegion(planeId)`
- **THEN** 返回该位面所有 `RegionId == null` 的 Tile 集合

### Requirement: TileMgr 在 WorldMgr 中注册
`TileMgr` SHALL 在 `WorldMgr.Initialize()` 中作为 `IDomainMgrBase` 注册到 `_mgrs` 列表，生命周期与其他领域 Mgr 一致。

#### Scenario: TileMgr 初始化后可访问
- **WHEN** `WorldMgr.Initialize()` 完成
- **THEN** `TileMgr.Instance != null`，可正常查询 Tile
