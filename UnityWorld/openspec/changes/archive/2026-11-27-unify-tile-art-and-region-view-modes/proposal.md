# Proposal

## Why

地图定义预留了 `AssetRef`，而 2D 探索后来按 Tile ID 维护图集映射。两条路径分开后，区域编辑器无法显示已有的 26 种地形美术。

## What Changes

- 让内置 Tile 显式引用现有探索图集。探索地图、地点缩略图与区域编辑器沿用同一套取图规则和纹理缓存。
- 区域编辑器提供「地形图」和「字标」显示模式，切换同时作用于画布和地块库。
- 允许 Tile 引用内容包 `src` 中的单图。旧定义省略 `AssetRef` 时继续按 Tile ID 查现有图集，缺图时使用已有诊断色和字标。

## Capabilities

### New Capabilities

本次调整现有能力。

### Modified Capabilities

- `exploration-visual-assets` 统一 Tile 的正式图片引用、已有图集和缺图回退。
- `region-template-editor` 提供地形图与字标的显示切换。

## Impact

涉及 26 个内置 Tile 定义、探索地图资源配置、共享纹理缓存、2D 探索和区域模板编辑器。
