# Proposal

## Why

地标后端已经能生成自然奇观和后天建筑。策划需要能编辑、预览和导出地标，并在 2D 世界探索中检查普通玩家看到的效果。

## What Changes

- 测试首页增加与角色模板编辑器同级的地标编辑器。支持新建、从现有定义复制、编辑完整配置、导入 PNG、预览和导出新定义。
- 地标定义增加可选的 2D 图片逻辑地址。2D 地图与地点详情显示同一张图，历史地标继续支持通用标记。
- 制作十种带独立图片的常用修仙地标，配置到正式世界的已有地区模板。

## Capabilities

### New Capabilities

- `landmark-editor` 提供地标编辑、预览和项目导出。

### Modified Capabilities

- `landmark-system` 支持可选的逻辑图片地址和十种内置内容。
- `exploration-map-view` 根据真实 Tile 地标显示地图图像和地点缩略图。

## Impact

涉及测试首页、Godot 地标编辑器、地图目录、2D 探索绘制、地标 JSON 和外置 `src/landmarks` 图片目录。
