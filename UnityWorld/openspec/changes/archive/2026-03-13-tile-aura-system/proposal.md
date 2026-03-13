## Why

当前 Tile 只有一个静态的 `BaseAura`，世界地图是均质死水——每块地的五行元气永远是 1.0，没有起伏，没有历史，没有生命。要让"元气场驱动世界运转"这一核心设计真正落地，Tile 需要一套动态的三层元气模型，以及一个感知全局五行收支、以间接方式维持平衡的"天道"系统。

## What Changes

- **BREAKING** `TileAura` 从单一静态值扩展为三层结构：过去层（锚点）、当前层（实时）、未来层（趋势）
- 新增"修正源（TileModifier）"概念：自然地标、后天建筑、NPC 死亡/修炼均以修正源的形式影响 Tile 未来层
- 新增"元气天道（AuraDaoMgr）"全局管理器：持有五行全局账本，感知当前层与过去层的偏差，通过调整世界事件概率权重间接修正失衡
- `PlaneGenerator` 在生成完自然地标后，对所有 Tile 快照当前元气为过去层（原初锚点）
- 新增 `TileSystem` 层：负责每 Tick 将未来层趋势累积到当前层

## Capabilities

### New Capabilities

- `tile-three-layer-aura`：Tile 动态元气模型——Tile 自身只持有当前层（CurrentAura，实时值）和未来层（TileModifier 列表，各修正源叠加的变化趋势）；过去层（原初锚点）由 AuraDaoMgr 统一快照存储，不在 Tile 上冗余
- `modifier-base`：通用修正源基类体系——建立 `ModifierBase` 抽象基类，派生出 `TileModifier`（本次实现）、`NpcModifier`、`CardModifier`（预留扩展）。元气修正数据（AuraData）作为 `TileModifier` 的组成部分，而非独立系统
- `aura-dao`：元气天道——独立的玩法领域，持有全局五行收支账本，计算所有 Tile（当前层 - 过去层）的偏差总和，通过对外暴露"五行失衡权重"影响事件系统概率分配，本身不直接修改任何 Tile 数值；作为高层耦合系统与 Tile、Npc、事件等多个领域交互

### Modified Capabilities

（无现有 spec 需变更）

## Impact

- `Scripts/Game/Domain/Tile/Data/TileAura.cs`：保留并简化，仅作五行数值容器（CurrentAura 复用此类）
- `Scripts/Game/Domain/Tile/Tile.cs`：只持有 CurrentAura + List<TileModifier>，去掉 BaseAura/OriginAura
- `Scripts/Game/Domain/Tile/System/TileSystemAura.cs`：新增，负责 Tick 时将 AuraDelta 累积到 CurrentAura
- `Scripts/Game/Domain/Plane/System/PlaneGenerator.cs`：生成自然地标后执行过去层快照
- 新增 `Scripts/Game/Domain/Modifier/`：放置 `ModifierBase.cs`、`TileModifier.cs`（含 AuraData）、预留 `NpcModifier.cs` / `CardModifier.cs` 占位
- 新增 `Scripts/Game/Domain/AuraDao/AuraDaoMgr.cs`：天道领域，与 Tile/Npc/事件系统耦合
- `Scripts/Game/World/WorldMgr.cs`：注册 AuraDaoMgr，每 Tick 驱动天道计算
