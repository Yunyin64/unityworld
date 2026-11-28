## ADDED Requirements

### Requirement: 2D 地图辨识同格多地标

同一 Tile 含有多个地标时，2D 地图 SHALL 显示主地标图形和额外数量，地点详情 SHALL 可查看同格其他地标名称。

#### Scenario: 选中带多个地标的地块
- **WHEN** 一个 Tile 包含主地标和两个附加地标
- **THEN** 地图出现主图标与 `+2`，地点详情可查看两个附加地标名称
