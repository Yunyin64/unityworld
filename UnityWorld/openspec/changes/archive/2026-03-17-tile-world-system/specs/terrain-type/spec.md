## ADDED Requirements

### Requirement: 定义基础地形枚举
系统 SHALL 提供 `TerrainType` 枚举，包含 7 种固定基础地形：`Plain`（平原）、`Hill`（丘陵）、`Mountain`（山地）、`RiverLake`（河湖）、`Ocean`（海洋）、`Desert`（荒漠）、`Forest`（森林）。

#### Scenario: 枚举值完备
- **WHEN** 代码引用 `TerrainType`
- **THEN** 可以枚举出恰好 7 种值，不含 None/Unknown 等占位值

### Requirement: 地形与五行浓度基础对应
系统 SHALL 提供静态映射，将每种 `TerrainType` 映射到一组基础 `TileAura` 偏向值，作为 Step2 地形生成时写入地块的初始浓度依据。

| 地形 | 金 | 木 | 水 | 火 | 土 |
|------|----|----|----|----|-----|
| Plain   | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Hill    | 1.0 | 0.8 | 0.8 | 0.8 | 1.5 |
| Mountain| 2.0 | 0.5 | 0.8 | 0.8 | 1.2 |
| RiverLake| 0.8 | 1.2 | 2.5 | 0.5 | 0.8 |
| Ocean   | 0.5 | 0.8 | 4.0 | 0.3 | 0.5 |
| Desert  | 1.0 | 0.3 | 0.3 | 2.5 | 1.5 |
| Forest  | 0.8 | 2.5 | 1.2 | 0.5 | 0.8 |

#### Scenario: 获取地形基础元气
- **WHEN** 调用 `TerrainAuraProfile.Get(TerrainType.Mountain)`
- **THEN** 返回金=2.0、木=0.5 等对应的 TileAura 数值

### Requirement: Tile 持有地形类型
`Tile` SHALL 有 `TerrainType Terrain` 字段，默认值为 `TerrainType.Plain`。

#### Scenario: Tile 创建后地形默认值
- **WHEN** 创建一个新 `Tile`
- **THEN** `Tile.Terrain == TerrainType.Plain`
