## 1. 建立 ModifierBase 体系

- [x] 1.1 新建目录 `Scripts/Game/Domain/Modifier/`
- [x] 1.2 新建 `ModifierBase.cs`：抽象类，含 Id（string）、SourceId（string）、Duration（float，-1 永久）、RemainingTime（float）、IsExpired 属性（Duration == -1 时永不过期，否则 RemainingTime <= 0 时为 true）
- [x] 1.3 新建 `TileModifier.cs`：继承 ModifierBase，持有 `AuraData`（类型复用 `TileAura`，语义为每 Tick 变化量）
- [x] 1.4 新建 `NpcModifier.cs`：继承 ModifierBase，空类占位，暂无额外字段
- [x] 1.5 新建 `CardModifier.cs`：继承 ModifierBase，空类占位，暂无额外字段

## 2. 改造 Tile

- [x] 2.1 在 `Tile.cs` 中将 `BaseAura` 字段替换为 `CurrentAura`（类型仍为 `TileAura`）
- [x] 2.2 在 `Tile.cs` 中新增 `List<TileModifier> Modifiers` 字段，构造时初始化为空列表
- [x] 2.3 在 `Tile.cs` 中新增 `AddModifier(TileModifier)` 方法
- [x] 2.4 在 `Tile.cs` 中新增 `RemoveModifier(TileModifier)` 方法

## 3. 修复 BaseAura 的破坏性变更

- [x] 3.1 全局 grep `BaseAura`，找出所有调用点（预计：`Plane.cs`、`PlaneGenerator.cs` 等）
- [x] 3.2 修改 `Plane.cs` 中的 `GetEffectiveAura()`：改为直接返回 `tile.CurrentAura`（PlaneAuraBonus 暂保留叠加逻辑）
- [x] 3.3 修改 `PlaneGenerator.cs` 中所有对 `BaseAura` 的赋值，改为对 `CurrentAura` 的赋值（PlaneGenerator 无 BaseAura 使用，无需修改）
- [x] 3.4 确认编译无报错（待 WorldMgr 接入后统一验证）

## 4. 建立 AuraDaoMgr

- [x] 4.1 新建目录 `Scripts/Game/Domain/AuraDao/`
- [x] 4.2 新建 `AuraDaoMgr.cs`：持有 `Dictionary<TileId, TileAura> _originSnapshot`（私有只读快照）
- [x] 4.3 实现 `TakeSnapshot(Plane plane)`：遍历 plane 所有 Tile，深拷贝 CurrentAura 存入 `_originSnapshot`
- [x] 4.4 实现 `GetOrigin(TileId)`：返回快照中对应 TileAura，不存在时返回 null
- [x] 4.5 实现 `CalculateBalance()`：遍历已注册 Plane 所有 Tile，累加 `CurrentAura - OriginAura` 差值，返回五行各自净偏差
- [x] 4.6 实现 `GetImbalanceWeight(BaseElementType)`：根据 CalculateBalance 结果返回对应元素失衡程度（缓存本 Tick 计算结果）
- [x] 4.7 实现 `OnTick(float deltaTime)`：刷新失衡权重缓存

## 5. 建立 TileSystemAura

- [x] 5.1 新建 `Scripts/Game/Domain/Tile/System/TileSystemAura.cs`
- [x] 5.2 实现 `Tick(Plane plane, float deltaTime)`：遍历所有 Tile，对每个 Modifier 将 AuraData 按 deltaTime 比例累积到 CurrentAura，同时递减 RemainingTime
- [x] 5.3 在 Tick 中清理 IsExpired == true 的 Modifier（遍历结束后统一移除，避免遍历中修改集合）

## 6. 接入 PlaneGenerator 快照

- [x] 6.1 在 `PlaneGenerator.Fill()` 末尾添加对 `AuraDaoMgr.TakeSnapshot(plane)` 的调用
- [x] 6.2 确认快照在自然地标生成完毕后、任何后天 Modifier 添加前触发

## 7. 接入 WorldMgr 驱动

- [x] 7.1 在 `WorldMgr.cs` 中持有 `AuraDaoMgr` 实例并初始化
- [x] 7.2 在 `WorldMgr.cs` 中持有 `TileSystemAura` 实例并初始化
- [x] 7.3 在 `WorldMgr.OnTick()` 中先调用 `TileSystemAura.Tick(plane, deltaTime)`，再调用 `AuraDaoMgr.OnTick(deltaTime)`

## 8. 验证

- 不用验证
