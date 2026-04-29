## 1. 地形枚举与基础 Aura 映射

- [x] 1.1 在 `Scripts/Game/Domain/Object/Tile/Data/` 新建 `TerrainType.cs`，定义 7 种地形枚举（Plain/Hill/Mountain/RiverLake/Ocean/Desert/Forest）
- [x] 1.2 在同目录新建 `TerrainAuraProfile.cs`，提供静态方法 `Get(TerrainType)` 返回对应 `TileAura` 基础浓度值
- [x] 1.3 扩展 `Tile.cs`：增加 `TerrainType Terrain` 字段（默认 Plain）、`RegionId? RegionId` 字段、`LandMarkId? LandMarkId` 字段

## 2. TileModifierDefine 数据层

- [x] 2.1 新建 `Scripts/Game/Data/Defines/TileModifierDefine.cs`（继承 DefineBase，字段：AuraData、Duration）
- [x] 2.2 在 `TileModifierDefine` 中增加 `CreateModifier(string sourceId)` 工厂方法，实例化 `TileModifier`
- [x] 2.3 新建 `Scripts/Game/Data/Mgr/TileModifierDefineMgr.cs`（实现 `IDataMgrBase<TileModifierDefine>`，JSON 加载）
- [x] 2.4 在 `GameDataMgr` 构造函数中注册 `TileModifierDefineMgr`（数据文件：`TileModifierDefines.json`）

## 3. LandMark 系统

- [x] 3.1 新建 `Scripts/Game/Domain/Object/LandMark/Data/LandMarkId.cs`（struct，含 int Value，实现 IEquatable）
- [x] 3.2 新建 `Scripts/Game/Domain/Object/LandMark/Data/LandMarkIdGenerator.cs`（静态自增 ID 生成器）
- [x] 3.3 新建 `Scripts/Game/Data/Defines/LandMarkDefine.cs`（继承 DefineBase，含 IsNatural/IsSingleton/SpawnWeight/Tags/PlacementTerrains/PlacementExtraTerrains/ModifierDefineIds）
- [x] 3.4 新建 `Scripts/Game/Data/Mgr/LandMarkDefineMgr.cs`（实现 `IDataMgrBase<LandMarkDefine>`，JSON 加载）
- [x] 3.5 在 `GameDataMgr` 中注册 `LandMarkDefineMgr`（数据文件：`LandMarkDefines.json`）
- [x] 3.6 新建 `Scripts/Game/Domain/Object/LandMark/LandMark.cs`（字段：Id/DefineId/Position/PlaneId/IsActive）
- [x] 3.7 新建 `Scripts/Game/Domain/Object/LandMark/LandMarkMgr.cs`（实现 IDomainMgrBase，单例，提供 Place/Remove/GetById/GetByPlane/GetNaturalLandMarks）
- [x] 3.8 `LandMarkMgr.Place`：实例化 LandMark，从 ModifierDefineIds 创建 TileModifier 并挂到 Tile，写 Tile.LandMarkId
- [x] 3.9 `LandMarkMgr.Remove`：清理 Tile 上对应 SourceId 的 Modifier，重置 Tile.LandMarkId = null，置 IsActive = false
- [x] 3.10 Singleton 约束：Place 前检查该 DefineId 是否已存在实例，IsSingleton=true 时若已存在则返回 null

## 4. Region 系统

- [x] 4.1 新建 `Scripts/Game/Domain/Object/Region/Data/RegionId.cs`（struct，含 int Value，实现 IEquatable）
- [x] 4.2 新建 `Scripts/Game/Domain/Object/Region/Data/RegionIdGenerator.cs`（静态自增 ID 生成器）
- [x] 4.3 新建 `Scripts/Game/Data/Defines/RegionDefine.cs`（继承 DefineBase，含 IsGuaranteed/MaxCount/SpawnWeight/PlacementTerrains/Width/Height/TerrainLayout/LandMarkLayout/ExtraTerrainIds/AuraProfile/Tags）
- [x] 4.4 `RegionDefine` 的 TerrainLayout 和 LandMarkLayout 键使用 `(int dq, int dr)` 的 ValueTuple，JSON 序列化时使用字符串 `"dq,dr"` 格式并提供自定义转换
- [x] 4.5 新建 `Scripts/Game/Data/Mgr/RegionDefineMgr.cs`（实现 `IDataMgrBase<RegionDefine>`，JSON 加载）
- [x] 4.6 在 `GameDataMgr` 中注册 `RegionDefineMgr`（数据文件：`RegionDefines.json`）
- [x] 4.7 新建 `Scripts/Game/Domain/Object/Region/Region.cs`（字段：Id/DefineId/Center/PlaneId/OwnedTiles/LandMarkIds）
- [x] 4.8 新建 `Scripts/Game/Domain/Object/Region/RegionMgr.cs`（实现 IDomainMgrBase，单例，提供 GetById/GetByPlane/GetRegionOfTile）

## 5. TileMgr

- [x] 5.1 新建 `Scripts/Game/Domain/Object/Tile/TileMgr.cs`（实现 IDomainMgrBase，单例）
- [x] 5.2 实现 `GetTile(planeId, tileId)`：委托 `PlaneMgr.Instance?.GetPlaneById(planeId)?.GetTile(tileId)`
- [x] 5.3 实现 `GetTilesByTerrain(planeId, terrain)`：遍历位面所有 Tile 按地形筛选
- [x] 5.4 实现 `GetTilesByRegion(regionId)`：通过 `RegionMgr.Instance?.GetById(regionId)?.OwnedTiles` 获取坐标，再逐一 GetTile
- [x] 5.5 实现 `GetTilesWithNoRegion(planeId)`：遍历位面所有 Tile，过滤 `RegionId == null`

## 6. AuraDaoMgr 扩展

- [x] 6.1 在 `AuraDaoMgr` 中新增 `TakeNaturalSnapshot(Plane plane, LandMarkMgr landMarkMgr)` 方法
- [x] 6.2 该方法遍历 Tile 时，计算「自然态元气」= CurrentAura 减去所有 IsNatural=false 地标挂载的 Modifier 的 AuraData，以此值记录快照
- [x] 6.3 原有 `TakeSnapshot(Plane plane)` 方法保留，不影响现有调用路径

## 7. PlaneGenerator 重构（5步流程）

- [x] 7.1 在 `PlaneGenerator` 中新增静态方法 `Generate(Plane plane, Rng rng, RegionMgr?, LandMarkMgr?, AuraDaoMgr?)`
- [x] 7.2 Step2 TerrainGen：实现简单分区地形分配（如 Rng 随机分配或噪声简化版），调用 `TerrainAuraProfile.Get` 写入 `Tile.CurrentAura`
- [x] 7.3 Step3 RegionPlace：从 `RegionDefineMgr` 取所有 Define，先遍历 IsGuaranteed=true 的，再按权重随机排序其余的；每个 Region 用 `Plane.OffsetToAxial` 计算长方形范围，检查重叠，落地写 RegionId 并调用 LandMarkMgr 生成 LandMarkLayout 中的地标
- [x] 7.4 Step4 LandMarkPlace：遍历 GetTilesWithNoRegion，对每个满足 PlacementTerrains/PlacementExtraTerrains 条件的无主 Tile，按 SpawnWeight 权重随机决定是否生成地标（每个 LandMarkDefine 独立尝试）
- [x] 7.5 Step5 Snapshot：若 auraDaoMgr 和 landMarkMgr 均不为 null，调用 `auraDaoMgr.TakeNaturalSnapshot(plane, landMarkMgr)`；否则退化调用原有 `TakeSnapshot`
- [x] 7.6 修改 `PlaneMgr.CreatePlane`：将 `PlaneGenerator.Fill(plane, AuraDaoMgr)` 替换为 `PlaneGenerator.Generate(plane, _rng, RegionMgr.Instance, LandMarkMgr.Instance, AuraDaoMgr)`
- [x] 7.7 `PlaneMgr` 增加 `Rng _rng` 字段，在构造函数或 Init 中初始化（可从 WorldMgr 传入 seed）

## 8. WorldMgr 注册新 Mgr

- [x] 8.1 在 `WorldMgr.Initialize()` 中，在 PlaneMgr 之前注册 `LandMarkMgr`、`RegionMgr`、`TileMgr`（保证初始化顺序：LandMarkMgr → RegionMgr → PlaneMgr → TileMgr）
- [x] 8.2 确认 `PlaneMgr` 初始化时 `RegionMgr.Instance` 和 `LandMarkMgr.Instance` 已不为 null

## 9. 示例 JSON 数据文件

- [x] 9.1 新建 `Data/TileModifierDefines.json`，添加 2~3 个示例修正定义（如灵气涌泉/火山灼热）
- [x] 9.2 新建 `Data/LandMarkDefines.json`，添加 2~3 个示例地标（含 1 个自然奇观、1 个后天建筑）
- [x] 9.3 新建 `Data/RegionDefines.json`，添加 1 个示例区域（含地形布局和地标布局）
