# Design

## Context

`LandMarkDefine` 负责定义，Tile 持有实例 ID。测试首页挂载独立 Godot 场景。现有 `ClientAssets` 从安装根和 Mod 的 `src` 解码并共享纹理。

## Goals / Non-Goals

交付从编辑和图片预览，到导出正式配置，再到正常世界 2D 绘制的完整链路。地标仍覆盖自然奇观和建筑，沿用一格一个地标、创世和元气规则。施工、经营、存档、3D 模型沿用当前边界。

## Decisions

1. 编辑页由首页打开，左栏列出现有定义，中栏滚动编辑（基础、生成、五行、固定位置），右栏显示图片和导出状态。已有定义可复制为新草稿；退出和切换草稿会确认未导出修改。
2. 新定义以安全的新 ID 导出。目标为当前开发项目或安装的 `Data/LandMarkDefines/<ID>.json` 和 `src/landmarks/<ID>.png`。序列化整个 SchemaVersion 2 定义到单元素数组，写前检查引用、坐标、唯一性和重名；先暂存，再提交图片和 JSON，失败回收本次创建的输出。原有定义和资源禁止覆盖。导出后刷新共享图片缓存，下次开地图读取新定义。
3. 图片引用为可选 `ImagePath`，使用已有资源链的相对 PNG 地址。缺图、旧定义和坏图保留通用标记或地形缩略图。地图图案以格心为锚点，有上限并保持比例；地点预览共用图片。
4. 新内容使用已有必出地区的相对布局，并检查对应 Tile 类型和原有地标位。十种地标为藏经阁、炼丹房、灵药圃、试剑台、聚灵阵坛、云渡码头、闭关洞府、灵瀑、雷纹竹林和云涌灵泉。

## Risks / Trade-offs

- 绝对坐标可能使具体种子的地图候选失败。明确校验静态冲突，固定坐标导出再试跑默认世界种子。
- 地图需要允许没有图的 Mod 内容掉级绘制，导出器对主动导入的图片做完整解码验证。
- 两个文件跨目录发布时 JSON 最后提交，使生成器只读到完整结果。输出占用由独占写入保护，回滚只处理本次创建的文件。

## 内置地标与美术输入

十张 PNG 由内置 ImageGen 分别生成并保存于 `src/landmarks`。使用统一提示词，替换其中的主体描述。

> Stylized concept art, single original PNG sprite for a Chinese xianxia exploration game's 2D hex map, [subject]. Slightly elevated three-quarter view, camera looks down 35 degrees. One centered readable silhouette, compact base, all edges within the frame, object fills about 82% of a square. Hand-painted mineral colors and soft ink-dark edges, subtly textured, tonal jade, amber, muted natural pigments, coherent warm diffuse light from upper left. Fine detail is subordinate to large forms readable on a tiny 60-pixel map. Alpha transparent background with no square scenery, no colored backing, no horizon, no text, no border, no characters, no watermark. Keep only this isolated place or structure and its compact ground contact.

| 地标 | 模板与相对坐标 | 主体 |
| --- | --- | --- |
| 藏经阁 | 太行 `(-2,-2)` | a tall three-story Chinese timber scripture library, dark jade sweeping tiled roofs, vermilion doors, and warm paper windows |
| 炼丹房 | 平原大湖 `(7,1)` | a compact round-roofed Chinese alchemy workshop with a bronze three-legged furnace and a tiny curling ribbon of blue-gray steam |
| 灵药圃 | 湖群湿地 `(2,3)` | a small orderly herb garden with three stepped jade-green planting beds, low bamboo rails, and one bright flowering spirit herb |
| 试剑台 | 太行 `(1,1)` | a weathered square stone dueling platform with a single upright bronze training sword and two short stone pillars |
| 聚灵阵坛 | 太行 `(-2,3)` | a circular tiered ritual stone altar, carved concentric rings and four low standing stones with restrained teal light |
| 云渡码头 | 平原大湖 `(1,2)` | a traditional wooden ferry landing with a roofed end shelter, tied narrow river skiff, and a short blue water fringe directly under its base |
| 闭关洞府 | 狭长山间湖 `(4,1)` | an angular mossy rock outcrop with a deep cave opening framed by two simple hand-cut stone pillars and a closed wooden gate within |
| 灵瀑 | 狭长山间湖 `(14,6)` | a short luminous aquamarine waterfall pouring over an isolated dark angular rock outcrop into a small pool |
| 雷纹竹林 | 秦岭 `(-2,-2)` | a dense compact stand of dark teal bamboo, distinctive pale gold lightning-shaped scars along three visible stems |
| 云涌灵泉 | 祁连 `(-1,2)` | a turquoise natural hot spring in a ring of dark stone with small white curls of steam and one patch of moss |
