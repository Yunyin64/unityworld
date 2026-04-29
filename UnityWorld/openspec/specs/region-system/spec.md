## ADDED Requirements

### Requirement: RegionId 唯一标识
系统 SHALL 提供 `RegionId` 值类型（struct），唯一标识运行时区域实例，支持 `==`、`!=` 和字典键。

#### Scenario: RegionId 相等性
- **WHEN** 两个 `RegionId` 由同一整数值构造
- **THEN** `a == b` 为 true

### Requirement: RegionDefine 静态配置
系统 SHALL 提供 `RegionDefine`（继承 `DefineBase`），字段包括：

**生成规则：**
- `IsGuaranteed: bool`：必然生成（世界宿命地点，优先放置）
- `MaxCount: int`：全图最多实例数（-1 = 不限）
- `SpawnWeight: float`：随机放置权重
- `PlacementTerrains: List<TerrainType>`：中心点允许落在的地形（空 = 不限）

**占用形状：**
- `Width: int` / `Height: int`：区域占用的长方形格数（Offset 坐标系）

**内容布局（相对中心点的 Axial 偏移）：**
- `TerrainLayout: Dictionary<(int dq, int dr), TerrainType>`：相对坐标→地形覆盖
- `LandMarkLayout: Dictionary<(int dq, int dr), string>`：相对坐标→LandMarkDefine ID（固定位置，必然生成）

**区域属性：**
- `ExtraTerrainIds: List<string>`：区域持有的拓展地形语义标签
- `AuraProfile: TileAura`：区域整体元气倾向（叠加到区域内所有地块）
- `Tags: List<string>`：叙事标签

#### Scenario: 必然生成区域优先放置
- **WHEN** 世界生成时存在 `IsGuaranteed = true` 的 RegionDefine
- **THEN** 该区域在所有随机区域放置之前先行落地

#### Scenario: 长方形超出地图边界
- **WHEN** 区域中心点落在地图边缘，长方形有部分超出边界
- **THEN** 超出边界的格子被忽略，区域仍然生成（不强制要求完整长方形）

#### Scenario: 区域地形布局覆盖
- **WHEN** 一个 Region 的 `TerrainLayout` 中配置了 `(0,0) → Mountain`
- **THEN** 区域落地后，中心点所在地块的 `Terrain` 被覆盖为 `Mountain`

#### Scenario: 区域地标固定生成
- **WHEN** 一个 Region 的 `LandMarkLayout` 中配置了 `(0,0) → "volcanic_pillar"`
- **THEN** 区域落地后，中心点地块上必然生成该地标，不受地标 SpawnWeight 影响

### Requirement: Region 运行时实体
系统 SHALL 提供 `Region` 类，含：
- `Id: RegionId`
- `DefineId: string`
- `Center: TileId`（中心点 Axial 坐标）
- `PlaneId: PlaneId`
- `OwnedTiles: HashSet<TileId>`（区域拥有的所有地块坐标）
- `LandMarkIds: List<LandMarkId>`（区域内生成的地标 ID）

#### Scenario: 区域拥有的 Tile 被标记
- **WHEN** Region 落地后
- **THEN** `OwnedTiles` 中每个 TileId 对应的 `Tile.RegionId == region.Id`

### Requirement: RegionMgr 管理所有区域实例
系统 SHALL 提供 `RegionMgr`（实现 `IDomainMgrBase`），单例，功能：
- `GetById(id)`：按 ID 查询区域
- `GetByPlane(planeId)`：获取某位面所有区域
- `GetRegionOfTile(tileId)`：通过 Tile 的 RegionId 反查区域

#### Scenario: 跨位面区域查询
- **WHEN** 调用 `RegionMgr.Instance?.GetByPlane(planeId)`
- **THEN** 返回该位面上所有已落地的 Region 列表

#### Scenario: 区域数量上限
- **WHEN** 一个 `RegionDefine.MaxCount = 3`，世界生成时尝试放置第 4 个
- **THEN** 第 4 次放置被跳过，全图该类型区域不超过 3 个

### Requirement: RegionDefineMgr 数据加载
系统 SHALL 提供 `RegionDefineMgr`（实现 `IDataMgrBase<RegionDefine>`），从 JSON 加载，在 `GameDataMgr` 中注册。

#### Scenario: 按 ID 查询区域定义
- **WHEN** 调用 `RegionDefineMgr.Instance?.Get("volcanic_belt")`
- **THEN** 返回对应 Define，不存在返回 null
