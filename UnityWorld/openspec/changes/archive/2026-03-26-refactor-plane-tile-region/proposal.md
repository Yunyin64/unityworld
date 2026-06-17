## Why

当前 Plane/Tile/Region 的实现与设计文档存在三处核心偏差，导致代码难以维护且与游戏设计意图不一致：

1. **元气模型偏差**：设计文档要求"目标趋近"模型（Current → Target Lerp），但代码实现为"线性累积"导致无限增长
2. **Plane 职责膨胀**：Plane 承担了 NPC 管理，违反空间层次的纯粹性
3. **Region 对象冗余**：Region 运行时对象只是静态标签载体，但引入了额外的管理复杂度

## What Changes

### 元气系统重构
- Tile 增加 `BaseAura`（地形基础）和 `TargetAura`（脏标记计算的目标态）
- `TileSystemAura.Tick()` 改为 Lerp 趋近逻辑
- `Modifier` 影响 `TargetAura` 而非直接累积 `CurrentAura`
- TileAura 补充 `CopyFrom`/`ScaleAdd`/`Reset` 工具方法
- TerrainAuraProfile 改为预缓存静态实例

### Plane 瘦身
- **BREAKING** 移除 `Plane._npcIds` 及相关方法
- 将"某位面有哪些 NPC"查询移至 NpcSystemPosition 的 `_byPlane` 索引
- Plane 增加 `GetNeighborTiles(TileId)` 等空间查询便利方法

### Region 对象移除
- **BREAKING** 废弃 Region.cs 运行时实体（代码保留但不使用）
- **BREAKING** 废弃 RegionMgr.cs（代码保留但不使用）
- Tile 用 `RegionDefineId: string` 替代 `RegionId: int?`
- PlaneGenerator.Step3 不再创建 Region 对象，只执行地形/元气/地标覆盖

### TileMgr 增强
- 承担跨位面 Tile 查询的聚合入口职责
- 新增按 RegionDefineId 查询、统计类查询方法

## Capabilities

### New Capabilities
- `aura-target-model`: 元气目标趋近模型，Tile 的三层元气结构（Base/Target/Current）
- `plane-spatial-query`: 位面空间查询便利方法（邻居、范围等）

### Modified Capabilities
- `plane-core`: 移除 NPC 管理，纯粹作为空间容器
- `tile-core`: 增加 BaseAura、TargetAura，RegionDefineId 替代 RegionId
- `tile-aura-system`: 改为 Lerp 趋近逻辑

## Impact

### 直接影响文件
- `Scripts/Game/Domain/Object/Tile/Tile.cs` - 增加 BaseAura, TargetAura, RegionDefineId
- `Scripts/Game/Domain/Object/Tile/Data/TileAura.cs` - 增加工具方法
- `Scripts/Game/Domain/Object/Tile/Data/TerrainAuraProfile.cs` - 改为预缓存
- `Scripts/Game/Domain/Object/Tile/System/TileSystemAura.cs` - 重写 Tick 逻辑
- `Scripts/Game/Domain/Object/Tile/TileMgr.cs` - 增加查询方法
- `Scripts/Game/Domain/Object/Plane/Plane.cs` - 移除 NPC 管理，增加空间查询
- `Scripts/Game/Domain/Object/Plane/PlaneMgr.cs` - 简化 NPC 相关逻辑
- `Scripts/Game/Domain/Object/Plane/System/PlaneGenerator.cs` - 简化 Step3
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemPosition.cs` - 加 _byPlane 索引

### 保留但废弃
- `Scripts/Game/Domain/Object/Region/Region.cs`
- `Scripts/Game/Domain/Object/Region/RegionMgr.cs`

### 依赖关系
- NpcMgr 需改用 NpcSystemPosition 查询位面归属
- WebAdapter 的 TileDto 需改用 RegionDefineId
