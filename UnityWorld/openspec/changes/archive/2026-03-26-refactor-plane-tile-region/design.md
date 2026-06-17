## Context

空间领域对象（Plane/Tile/Region）是游戏世界的基础架构。当前实现存在三个核心问题：

1. **元气计算错误**：`TileSystemAura` 直接累积 `CurrentAura`，导致无限增长，违背设计文档的"目标趋近"模型
2. **Plane 职责膨胀**：Plane 维护 `_npcIds` 列表，与 NpcSystemPosition 形成数据冗余
3. **Region 对象轻量**：Region 运行时对象仅承载静态标签，但需要完整生命周期管理

### 约束
- 不删除 .cs 文件（规则约束）
- 保持 API 向后兼容（WebAdapter 依赖）
- 保持可复现随机性（使用 Rng）

## Goals / Non-Goals

**Goals:**
- 对齐元气三层模型设计：Base → Target → Current Lerp
- 消除 Plane 与 NpcSystemPosition 的数据冗余
- 简化 Region 为纯静态元数据
- 减少热路径 GC 压力

**Non-Goals:**
- 不重构 PlaneGenerator 的 Lilypad/InnerWorld 生成逻辑
- 不改变位面拓扑结构（网格尺寸、连通性）
- 不引入新的数据持久化格式

## Decisions

### D1: 元气三层模型

**选择**：Tile 维护三层元气
```
BaseAura     ← TerrainAuraProfile 定义，创建时确定
TargetAura   ← BaseAura + Modifiers，脏标记计算
CurrentAura  ← Lerp(Current, Target, speed * dt)
```

**替换方案**：
- ❌ 保持线性累积 → 与设计文档冲突，无限增长
- ❌ 只保留 Current + Target → 无法区分地形基础贡献

**脏标记策略**：
- Modifier 添加/移除时标记 `_targetDirty = true`
- Tick 时若脏则重算 TargetAura，否则直接 Lerp

### D2: NpcSystemPosition 索引

**选择**：在 NpcSystemPosition 中增加 `_byPlane: Dictionary<int, List<int>>`

**替换方案**：
- ❌ 在 Plane 中维护，NpcSystemPosition 同步 → 数据重复，一致性风险
- ❌ 每次遍历全部 NPC → O(N) 查询，性能不可接受

**索引维护**：
- NPC 创建/移动时更新索引
- 跨位面移动：Remove(oldPlane) → Add(newPlane)

### D3: Region 元数据下沉

**选择**：Tile 直接存储 `RegionDefineId: string`

**替换方案**：
- ❌ 保持 Region 运行时对象 → 管理复杂度高，价值低
- ❌ 用 RegionId (int) → 需要维护映射表，增加间接层

**查询方式**：
- `TileMgr.GetTilesByRegionDefineId(string regionDefineId)`
- 直接遍历 Tile，按 `RegionDefineId` 过滤

### D4: TileAura 工具方法

**选择**：在 TileAura 中添加实例方法
```csharp
public void CopyFrom(TileAura other);
public void ScaleAdd(TileAura other, float scale);
public void Reset();
```

**替换方案**：
- ❌ 静态工具类 → 增加类型，调用繁琐
- ❌ 运算符重载 → 产生 GC（每次 new TileAura）

### D5: TerrainAuraProfile 预缓存

**选择**：在 TerrainDefineMgr 中预创建 Profile 实例缓存

**替换方案**：
- ❌ 每次 new TileAura() → 热路径 GC 压力
- ❌ 结构体 → 需要重构 TileAura，影响面大

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| TargetAura 重算开销 | 脏标记优化，仅 Modifier 变化时重算 |
| _byPlane 索引一致性 | NpcSystemPosition 是位置唯一权威，集中更新 |
| RegionDefineId 字符串比较 | 位面内 Tile 数量有限，可接受 |
| 废弃代码残留 | 保留文件但标记 [Obsolete]，后续集中清理 |

## Migration Plan

1. **Phase 1: 基础设施**
   - TileAura 工具方法
   - TerrainAuraProfile 预缓存

2. **Phase 2: Tile/元气重构**
   - Tile 增加 BaseAura/TargetAura
   - TileSystemAura 重写

3. **Phase 3: Plane/NPC 解耦**
   - NpcSystemPosition 加索引
   - Plane 移除 _npcIds

4. **Phase 4: Region 废弃**
   - Tile 改用 RegionDefineId
   - PlaneGenerator 简化

**回滚策略**：每个 Phase 独立提交，可单独回滚
