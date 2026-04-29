## Why

当前地图系统（Plane + Tile）只有坐标骨架，40,000个地块完全同质化，缺乏地形差异、区域划分和地标体系。世界无法作为叙事与事件的空间载体运作。

## What Changes

- **新增** `TerrainType` 枚举（7种基础地形：平原/丘陵/山地/河湖/海洋/荒漠/森林），Tile 持有地形类型
- **新增** `TileModifierDefine` 静态数据：元气修正的配置模板（目标偏移量 + 持续时间）
- **新增** `LandMark` 实体 + `LandMarkDefine` 数据：地块上的地标/建筑（自然奇观 or 后天建筑）
- **新增** `LandMarkMgr` 管理所有地标实例
- **新增** `Region` 实体 + `RegionDefine` 数据：连片地块集合，持有地形布局与地标布局
- **新增** `RegionMgr` 管理所有区域实例
- **新增** `TileMgr` 单例：跨位面 Tile 查询（按地形/区域等维度）
- **扩展** `Tile.cs`：增加 `TerrainType`、`RegionId?`、`LandMarkId?` 字段
- **扩展** `PlaneGenerator`：由单步填充扩展为5步世界生成流程（地形生成→区域落地→散布地标→原初快照）
- **扩展** `AuraDaoMgr.TakeSnapshot`：支持忽略 `IsNatural=false` 地标的修正，只记录自然原初态
- **扩展** `GameDataMgr`：注册新增的三个 DefineMgr
- **扩展** `WorldMgr.Initialize`：注册 TileMgr、LandMarkMgr、RegionMgr

## Capabilities

### New Capabilities

- `terrain-type`: 地形枚举定义，及地形与五行浓度的基础对应关系
- `tile-modifier-define`: 元气修正静态配置（TileModifierDefine + TileModifierDefineMgr）
- `landmark-system`: 地标实体与定义（LandMark + LandMarkDefine + LandMarkMgr）
- `region-system`: 区域实体与定义（Region + RegionDefine + RegionMgr）
- `tile-mgr`: 跨位面 Tile 查询管理器
- `world-generation`: 5步世界生成流程（PlaneGenerator 扩展）

### Modified Capabilities

- `plane-generation`: PlaneGenerator.Fill 流程由单步填充改为5步生成入口，原有Fill逻辑保留为Step1

## Impact

- `Scripts/Game/Domain/Object/Tile/Tile.cs`：字段扩展
- `Scripts/Game/Domain/Object/Tile/`：新增 TileMgr.cs、Data/TerrainType.cs
- `Scripts/Game/Domain/Object/LandMark/`：全新目录（LandMark.cs、LandMarkId.cs、LandMarkMgr.cs）
- `Scripts/Game/Domain/Object/Region/`：全新目录（Region.cs、RegionId.cs、RegionMgr.cs）
- `Scripts/Game/Data/Defines/`：新增 TileModifierDefine.cs、LandMarkDefine.cs、RegionDefine.cs
- `Scripts/Game/Data/Mgr/`：新增 TileModifierDefineMgr.cs、LandMarkDefineMgr.cs、RegionDefineMgr.cs
- `Scripts/Game/Domain/Object/Plane/System/PlaneGenerator.cs`：重构为5步流程
- `Scripts/Game/Domain/GamePlay/AuraDao/AuraDaoMgr.cs`：TakeSnapshot 扩展
- `Scripts/Game/Data/GameDataMgr.cs`：注册新 DefineMgr
- `Scripts/Game/World/WorldMgr.cs`：注册新 DomainMgr
