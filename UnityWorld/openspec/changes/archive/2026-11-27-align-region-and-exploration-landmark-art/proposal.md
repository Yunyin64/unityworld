# Proposal

## Why

区域画布与 2D 探索使用同一批地标图片，但分别决定显示比例、位置和同格排列。作者看到的模板外观因此无法直接预览实际游玩时的地标画面。

## What Changes

- 让地形图模式和 2D 探索共用地图地标画法，直接绘制已有透明 PNG。
- 统一同格多地标的显示、缺图字标和缩放时的数量提示，保留编辑工具的字标模式。

## Capabilities

### Modified Capabilities

- `region-template-editor` 的地形图沿用探索地图的地标画法。
- `exploration-map-view` 的地标图使用同一显示规则。

## Impact

涉及地图地标绘制、探索快照附加地标图片以及编辑器画布。地标定义和模板文件沿用现有数据。
