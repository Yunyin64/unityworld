# Proposal

## Why

Region 模板由不规则六边形地块组合而成。手写相对坐标时，很难看出区域轮廓、地形变化和格子上已有的引用。

## What Changes

- 在 Godot 测试首页增加区域模板编辑器，提供模板切换、新建、复制、保存、重新读取与可恢复归档。
- 左侧编辑 RegionDefine，中间预览和编辑实际占地，右侧按地形标签、水体类型、名称或 ID 查找可拖入画布的 Tile。
- 为 RegionDefine 补充可选描述，保存时使用正式地图配置加载与校验流程。

## Capabilities

### New Capabilities

- `region-template-editor` 提供区域模板管理、属性编辑、六边形画布及地块库。

### Modified Capabilities

- `region-system` 的静态区域定义支持可选描述。

## Impact

涉及 Godot 测试首页、地图配置入口及 `Data/RegionDefines`。区域描述沿用版本 2 配置。
