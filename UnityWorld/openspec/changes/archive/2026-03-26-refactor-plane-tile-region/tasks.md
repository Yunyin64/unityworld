## 1. 基础设施

- [x] 1.1 TileAura 增加 CopyFrom/ScaleAdd/Reset 工具方法
- [x] 1.2 TerrainAuraProfile 改为预缓存静态实例（在 TerrainDefineMgr 或单独管理器）

## 2. Tile 元气层重构

- [x] 2.1 Tile 增加 BaseAura 字段（创建时由 TerrainAuraProfile 初始化）
- [x] 2.2 Tile 增加 TargetAura 字段
- [x] 2.3 Tile 增加 _targetDirty 脏标记
- [x] 2.4 TileSystemAura.Tick 重写为 Lerp 趋近逻辑
- [x] 2.5 TileSystemAura 增加重算 TargetAura 的逻辑（Base + Modifiers）

## 3. Plane/NPC 解耦

- [x] 3.1 NpcSystemPosition 增加 _byPlane: Dictionary<int, List<int>> 索引
- [x] 3.2 NpcSystemPosition 增加索引维护逻辑（创建/移动时）
- [x] 3.3 NpcSystemPosition 增加 GetintsByPlane(int planeId) 查询方法
- [x] 3.4 Plane 移除 _npcIds 字段及相关方法
- [x] 3.5 Plane 增加空间查询便利方法（GetNeighborTiles 等）

## 4. Region 元数据下沉

- [x] 4.1 Tile 增加 RegionDefineId: string 字段
- [x] 4.2 Tile 移除或废弃 RegionId 字段
- [x] 4.3 TileMgr 增加 GetTilesByRegionDefineId 方法
- [x] 4.4 PlaneGenerator.Step3 简化，移除 Region 创建逻辑
- [x] 4.5 RegionMgr 标记 [Obsolete]（保留文件）

## 5. 依赖更新与测试

- [x] 5.1 更新 WebAdapter TileDto 使用 RegionDefineId
- [x] 5.2 更新 NpcMgr 使用 NpcSystemPosition.GetintsByPlane
- [x] 5.3 验证元气计算符合预期（Lerp 收敛测试）
- [x] 5.4 验证位面 NPC 查询正确
