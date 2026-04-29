## Context

当前地图系统（`Plane` + `Tile`）已有六边形坐标系（Axial + odd-r offset）、`TileModifier`/`TileAura` 元气体系、`AuraDaoMgr` 原初快照机制。

痛点：`PlaneGenerator.Fill()` 只做坐标填充，生成的 40,000 个 Tile 完全同质，没有地形、没有区域、没有地标，世界是一张白纸。

需要在不破坏现有 `TileModifier` / `AuraDaoMgr` 流程的前提下，为地图注入"内容"。

## Goals / Non-Goals

**Goals:**
- 给 Tile 加 `TerrainType`、`RegionId?`、`LandMarkId?` 三个字段
- 建立 `LandMark` 实体 + `LandMarkDefine` 配置体系
- 建立 `Region` 实体 + `RegionDefine` 配置体系（长方形占位，含地形/地标布局）
- 建立 `TileMgr` 跨位面查询单例
- 将 `PlaneGenerator` 扩展为 5 步流程
- 调整 `AuraDaoMgr.TakeSnapshot` 支持仅记录自然奇观后的原初态

**Non-Goals:**
- 地形噪声算法的具体实现（Step2 先用简单规则占位，后续替换）
- NPC 寻路与地形可通行性（字段预留，逻辑不实现）
- 后天建筑的运行时建造/拆除流程（LandMarkMgr 提供接口，具体触发由上层系统负责）
- Unity 渲染层适配（UnityAdapter 不在本次范围内）

## Decisions

### D1：Region 占位采用「长方形 + Axial 相对偏移」

**选择**：RegionDefine 用 `Width × Height`（Offset 坐标系）描述占用范围，内部 `TerrainLayout` / `LandMarkLayout` 用 `(dq, dr)` Axial 相对偏移描述每个地块/地标的位置。

**理由**：
- 长方形边界简单，生成时碰撞检测 O(1)
- Axial 偏移与 TileId 直接相加得到世界坐标，无需额外转换
- 策划配置可视化友好（宽高 + 相对坐标）

**备选**：自由形状多边形——拒绝，配置复杂度高，边缘计算困难。

---

### D2：LandMark 的元气影响走现有 TileModifier 体系

**选择**：`LandMarkDefine` 持有 `List<string> ModifierDefineIds`，生成时从 `TileModifierDefineMgr` 取出 `TileModifierDefine`，实例化为 `TileModifier` 挂在对应 Tile 上。

**理由**：
- 复用现有 `TileSystemAura.Tick` 的 Modifier 累积逻辑，零新增热路径代码
- `TileModifier` 已有 Duration（永久/有限）机制，天然支持临时地标效果
- `AuraDaoMgr` 快照时机已经明确，不需要另建通道

**备选**：LandMark 直接修改 `BaseAura`——拒绝，失去运行时动态性，且与原初快照哲学冲突（建筑建造后不能追溯修改原初）。

---

### D3：原初快照时机 = Step5（LandMarkPlace 之后，过滤非自然地标）

**选择**：`PlaneGenerator` Step5 专门调用 `AuraDaoMgr.TakeNaturalSnapshot(plane, landMarkMgr)`，快照时临时排除 `IsNatural=false` 的地标 Modifier，记录纯自然态五行浓度。

**理由**：
- 符合设计哲学："矿脉叙事上本来就在这里"，自然奇观是原初的一部分
- 后天建筑（宗门、工坊）造成的元气扰动才是天道感知的对象
- 通过 `LandMarkMgr` 查找 `IsNatural=true` 的地标集合，算法明确

**备选A**：Step2 后拍快照（纯地形态）——拒绝，会把灵山矿脉算作"后天改变"，不符合设计哲学。

**备选B**：两层快照——拒绝，增加 `AuraDaoMgr` 复杂度，当前阶段过度设计。

---

### D4：Tile 持有 `RegionId?` 和 `LandMarkId?`（单向引用）

**选择**：Tile 直接持有可空的 `RegionId` 和 `LandMarkId`，Region / LandMark 也单向持有其管辖的 TileId 集合。即双向引用。

**理由**：
- Tile 需要快速查"我在哪个区域"（O(1)），不能靠遍历所有 Region
- TileMgr 的跨位面查询需要按 Region/Terrain 筛选 Tile，Region 持有 TileId 集合可直接返回
- 内存代价可接受：主世界 40,000 Tile，额外 2 个可空结构体字段开销极小

---

### D5：`TileMgr` 负责跨位面 Tile 查询，不替代 `Plane.GetTile`

**选择**：`TileMgr` 是全局查询入口，内部维护 `Dict<PlaneId, Plane>` 引用（通过 `PlaneMgr.Instance`）。`Plane.GetTile` 继续作为位面内部查询接口。

**理由**：
- 不破坏现有 `Plane` 的封装
- `TileMgr` 提供按地形、按区域的批量查询，是上层系统（事件/NPC）的便利接口
- 生命周期与 `PlaneMgr` 对齐，在 `WorldMgr.Initialize` 中注册

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| Step2 地形噪声算法缺失 | 世界地形均质，验证困难 | 先用简单的随机/分区规则，后续替换为 Simplex 噪声 |
| Region 长方形重叠 | 两个区域争夺同一格 | 生成时检测中心点范围，重叠则跳过（log warning），先放 Guaranteed 的 |
| TileModifierDefine 与现有 TileModifier 语义混淆 | AuraData 的"目标偏移量"含义需明确注释 | 在类文档和字段注释中强调"目标偏移量不是每秒速率" |
| 主世界 40k Tile 遍历性能 | Step3~5 遍历开销 | 仅在生成时调用一次，非 Tick 热路径，可接受 |
| LandMarkMgr 全量遍历查询 | 按位面/区域查询 LandMark 时 O(n) | 当前地标数量有限（百量级），暂用线性查询，后续可加索引 |
