# 探索画面素材

人物、日期和选中地点的摆位参考[玩家标注图](../../openspec/changes/add-exploration-core-ui/references/user-blank-regions.png)。地图铺满 HUD 下方。右下缩放、模式与手动「继续」沿用现有边饰；头像下方、右侧状态与底部行为面板仍留白，可见地形或灰雾。

| 画面 | 来源与制作要求 |
| --- | --- |
| 灰雾 | ImageGen 新绘，[gray_fog.png](../../../assets/art/exploration/backgrounds/gray_fog.png)，1672 × 941，RGB。提示词要点为低对比中性灰、横向薄雾、细微丝绢水洗肌理、柔和漫光。图像只含雾，铺满窗口并居中裁切。 |
| 地形 | ImageGen 独立图集，依照[展示映射](../../../assets/presentation/exploration_visuals.json)的像素矩形取图。统一画风提示词为仙侠舆图、手绘矿物色、低斜俯视、光从左上、低矮树石、地面铺满。平顶六边形和纵向压缩都在运行时完成。 |
| 地点近景 | 使用选中格相同的地形素材和变体，以横向小幅展示。名称、地区、通行数据由游戏填入。 |
| 角色与头像 | 复用 [jade_swordsman.png](../../../assets/art/characters/jade_swordsman.png)，1254 × 1254，原生 RGBA。地图和人物栏分别读取立绘有效区域与脸部裁切，按原比例绘制。外观在探索会话中显式绑定。 |
| 地点、身份纸面 | 复用 [panel_frame.png](../../../assets/art/ui/template_editor/panel_frame.png)，1254 × 1254，RGBA。缩小九宫格边饰以适配浮动小面板。 |
| 日期、命中反馈及相机控件 | 文字和细线由 Godot 绘制，墨绿色配浅铜边。普通周画当期月相，节日显示节名。选择、候选与目的地随行程更新。 |

陆地表现按平野、高地、林木、山系、干旱区和长城制作。水域分别使用河面、湖面、海面、海岸和湿地绘景。河流的出入边取真实水文数据。格子没有中心线时仍保留其水面。

地图人物取原图矩形 `[58,157,1183,991]`，头像取 `[450,260,480,445]`。两种裁切已分别打开核对。纸面源图四边各取 150 像素，按探索专属 14 像素边距显示，保留细铜线和角件。

三张地形图集均为 1536 × 1024、RGB、三行三列。每种地形有一幅主体裁切和两幅略有偏移的局部，按种子、位面和轴坐标固定选择。树木和石块随地面裁切，轮廓收在本格内。

| 地形图集 | 实际覆盖 |
| --- | --- |
| [earth_and_wood.png](../../../assets/art/exploration/terrain/earth_and_wood.png) | 平原、草地、盆地、高山草甸、高原、森林、竹林、雨林、山林。 |
| [mountain_and_wall.png](../../../assets/art/exploration/terrain/mountain_and_wall.png) | 丘陵、黄土丘陵、山地、荒山、喀斯特、丹霞、雪山、火山、长城。 |
| [sand_and_water.png](../../../assets/art/exploration/terrain/sand_and_water.png) | 沙漠、戈壁、干谷、河、湖、海、海岸、湿地。 |

对照实际 26 个地形定义核对了 78 个裁切矩形。缺映射或缺图时，地图保留核心给出的颜色、名称／符号和地块身份，并输出缺项诊断。
