# Godot 探索、模板编辑与战斗观察客户端

项目文档统一放在 [UnityWorld/Docs](UnityWorld/Docs/File.md)，素材台账位于其中的 `art` 目录。

## 测试首页与可视布局

用 Godot .NET 打开仓库根的 `project.godot`，按 **F5** 进入测试首页。可以选择「3D 世界探索」「2D 地图测试」或「角色模板编辑器」。点工具里的「返回首页」可以继续测试另一项。地图返回会结束本次行程，模板有未保存修改时可以保存、放弃或取消。

主游戏窗口、独立试战窗口和重开后的观察器默认以 **1920×1080** 打开。首页的「界面缩放」可选 75%、100%、125%、150%；各工具也可用 **Ctrl + 加号**（或等号）、**Ctrl + 减号**调整，**Ctrl + 0** 恢复 100%。设置自动保存，新开的窗口沿用该倍率。

改窗口分辨率只改变可用空间。比如 100% 时，240 像素宽的控件从 1920×1080 换到 1280×720 后仍宽 240 像素；手动调到 125% 后才宽 300 像素。角落控件按各自窗口边缘定位。窄窗口可滚动查看放不下的内容，地图自己的加减号仍控制地图视野。

在 2D 编辑器中打开对应的场景即可直接检查和调整布局。保存后按 **F6** 调试当前页。

用 F5 调试时，如果浮窗四周出现黑边，在浮窗右上角的 **⋮ → 嵌入式窗口尺寸** 里选择 **伸展以适应**。Godot 将这个选项保存在当前电脑的项目编辑器设置里。右上角显示实际游戏分辨率，拖动浮窗后会跟着变。

| 场景 | 可以直接调整的区域 |
| --- | --- |
| [3D 世界探索](scenes/exploration/exploration_world_3d.tscn) | 立体地图、环境光、镜头、地点详情和行程操作 |
| [2D 地图测试](scenes/exploration/exploration_view.tscn) | 地图、人物名牌、日期、地点详情、缩放和行程操作 |
| [角色模板编辑器](scenes/templates/character_template_editor.tscn) | 模板库、背包装配、卡库、详情和试战按钮 |
| [战斗观察器](scenes/battle/battle_observer.tscn) | 两侧储物阵、人物、Buff、待发飞牌、播放控制和战报 |
| [卡牌详情](scenes/ui/CardTip.tscn) · [Buff 详情](scenes/ui/BuffTip.tscn) | 供各窗口悬浮层实例化的图片、标题、文本和宽度 |

角色模板编辑器的主场景负责摆放各区域。修改区域内部布局时，直接打开 [模板库](scenes/templates/template_library.tscn)、[背包装配区](scenes/templates/template_loadout.tscn) 或 [卡库](scenes/templates/template_card_library.tscn)。例如，卡库的搜索框和关键词列表都在卡库子场景里。

**场景拆分检查豁免**　以后盘点客户端 `.tscn` 的大小、行数或节点数时，跳过 [战斗素材预览](scenes/tests/battle_assets_preview.tscn)。它用于查看原始素材，观察器仍通过固定节点路径读取其中的皮肤资源。

动态栏位在编辑器中显示容量样文，如「这里显示七个字」。正式内容由 C# 绑定。移动背包请选中背包面板或它的子场景，卡面绘制、拖放和悬浮会使用它的新位置。卡牌和 Buff 的排版分别在各自的提示场景中调整。

## 3D 世界探索

从首页打开「3D 世界探索」，也可以在编辑器里打开 [三维探索场景](scenes/exploration/exploration_world_3d.tscn) 后按 F6。左键选择地块，合法邻格可以再点一次或点「启程」。右键拖动旋转，中键拖动平移，滚轮缩放。真实世界使用当前探索视野、日期及逐周行程。

「地形样板」打开一张 357 格的海陆组合，可切换格线，点选任一格查看邻近地形。「山门近景」切到同一张地图上的透视镜头，可看暖色侧光、栖霞山门的岩石台基和瓦坡，以及山谷里的体积雾。「返回全景」恢复正交地图。样板期间真实行程暂停。

### 相邻地形怎么接

| 位置 | 建模规则 | 样板例子 |
| --- | --- | --- |
| 格心 | 地形标签、海拔和水文控制起伏、底色与植被密度 | 平原保留开阔地，雪山形成显著主峰 |
| 两格之间 | 共享格边顶点并向两侧格心插值。邻山抬高接缝，水岸按地面与水面交线裁剪 | 两山之间保留山脊。滨海平地压低成浅滩，邻海的陡坡保留岩面 |
| 三格共角 | 三个格心共同计算顶点和法线 | 山地、林地、海岸交会时，坡面与水线在同一处收口 |
| 林缘与河道 | 植被密度随邻域渐变，河道沿核心记录的相连水文边成形 | 树木从密林渐稀，河道跨格后接到河口 |

逻辑格继续用现有轴坐标与距离。三维画面按六边形生成三角网格，绘制时合批，选格时从镜头对实际陆地与水面三角形求交。最外圈使用一圈邻格来算接缝。移动后，重叠的地块可从相同位置重新生成相同网格。

《文明 VII》环境美术提到他们参考了微缩场景，并在高处读图和近处分辨材质之间取舍。参考图里的跨格山脊与自然海岸可以用共享边、角及岸线裁剪实现，公开教程也给出了这类连接的构造。当前实现来源于这些通用做法和项目的实际地图结构。 [Firaxis 环境美术访谈](https://www.hookedgamers.com/features/2024/10/23/the_art_of_civilization_vii_-_an_interview.html) · [六边形坡面与三格共角](https://catlikecoding.com/unity/tutorials/hex-map/part-3/) · [海岸网格和浪花](https://catlikecoding.com/unity/tutorials/hex-map/part-8/)

岩壁用三平面投影和物理材质贴图，针叶与阔叶沿立体枝条分布。Forward+ 用于水面屏幕空间反射、接触阴影和近景体积雾。样板里的建筑体量、山道、远山背景和灯光为本项目制作。贴图的原始资产和处理方式见 [3D 探索素材来源](assets/art/exploration/terrain3d/SOURCES.md)。 [Godot 的屏幕空间效果](https://docs.godotengine.org/en/stable/tutorials/3d/environment_and_post_processing.html) · [Godot 的体积雾着色器](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/fog_shader.html)

### 描边

右上角「描边」控制效果。独立视口用同一套作画相机和网格输出山体、树冠、建筑等类别，屏幕合成结合主视口深度识别遮挡轮廓，用法线识别表面折线。因此可单独为檐口和台阶保留短线，也可把相互叠住的树叶收成较轻的树冠轮廓。远山的笔触随距离淡出。 [屏幕空间几何边缘](https://developer.nvidia.com/gpugems/gpugems2/part-ii-shading-lighting-and-shadows/chapter-15-blueprint-rendering-and-sketchy) · [Epic 的对象遮罩](https://dev.epicgames.com/documentation/unreal-engine/post-process-materials-in-unreal-engine?lang=en-US)

「MapCamera / OutlinePass」的 ShaderMaterial 里可分别调山体、建筑和植被的目标线宽。900 像素画高下默认是 3.35、1.85、0.78 像素。轮廓粗细随类别、美术设置与画面尺度变化，内部折线使用单独的强度。分类缓冲会复用叶片的透明裁切和风动，关闭描边也会停止绘制分类缓冲。 [《罪恶装备 Xrd》美术技术分享](https://media.gdcvault.com/gdc2015/presentations/Motomura_Junya_GuiltyGearXrd.pdf) · [稀疏线条如何传达形体](https://gfx.cs.princeton.edu/pubs/DeCarlo_2003_SCF/)

### Tile 覆盖

开发种子 12345 的真实位面生成了目录里的 26 种 Tile。已逐项检查它们能转换为显示参数，并验证同坐标换类型、改海拔和邻格变成水域会使新快照触发重建。真实探索在读取结算快照后更新地形。

地形样板使用了其中 10 种定义。真实地图按标签、颜色、海拔、温度和水文组合模型。绝界长城还使用通用地表，竹海与雨林仍共用通用树型，河道侧翼按中心线生成窄水道。26 种 Tile 的独立造型及完整水文范围需要单独验收。

## 2D 探索试玩

用 Godot .NET 打开项目，选中 [exploration_view.tscn](scenes/exploration/exploration_view.tscn) 后按 **F6**。命令行也可从仓库根目录启动：

```powershell
dotnet build Xianxia.Client.csproj
godot --path . res://scenes/exploration/exploration_view.tscn -- --exploration
```

先点格子预览地形和路程，再点同一个合法邻格确认启程。启程立即经过一周。默认使用**自动**模式，此后每隔片刻继续已确认的这段路，每次只走一周。抵达、遇阻、结算失败或进度不再增加时停止。抵达后等你选择下一格。

右下角放缩放和自动／手动切换。手动模式会多出「继续」，途中点一次才再过一周。两种模式都允许途中切换；点地图只查看地点，当前行程的目的地仍由出发时的选择决定。滚轮也可缩放，中键可拖图。改变窗口宽高后，日期仍在顶部中央，人物和地点面板贴左，操作组贴右下。[探索素材清单](UnityWorld/Docs/art/exploration_asset_manifest.md)

例如，默认速度每周 50，相邻距离 100。确认首周后是新月，人物留在原格，进度 50/100。自动模式稍后走完第二周并停下；若在首周切到手动，则等你按「继续」才到达。第二周是上弦，人物从原格走到目标格，镜头随行。

## 模板编辑与战斗观察

在测试首页选择「角色模板编辑器」。左侧区分只读的“项目模板”和可编辑的“我的模板”，复制后可调整属性、摆放已有 Card、保存个人草稿并进入原战斗观察界面试战。

编辑器快速使用、个人库位置、六项人工检查及当前验证边界见 [模板编辑器快速开始](UnityWorld/Docs/template-editor/QUICKSTART.md)。卡牌 Keyword 筛选由实际目录生成，多选为“同时满足”；缺图卡牌显示名称，仍可编辑。编辑器只加载定义，试战启动独立游戏窗口；关闭试战后可继续编辑，未保存内容不会被试战隐式保存。

原战斗观察场景仍是 `res://scenes/battle/battle_observer.tscn`，可用下方命令直接运行，或在编辑器顶部点击“战斗观察”。原静态素材预览保留在 `res://scenes/tests/battle_assets_preview.tscn`，可选中后按 F6 单独运行。以下战斗操作与历史验证记录继续保留。

观察器使用原控制台默认案例 `console-basic-template`：世界和战斗种子均为 12345，依次装配 `test_npc_a`、`test_npc_b`，上限 5000 Tick。双方真实背包为 5×5，角色数值、格上灵元、卡片点亮、冻结、待发和战报均来自核心模拟。人物立绘仅为客户端展示形象。

## 运行与操作

在仓库根目录执行：

```powershell
dotnet build Xianxia.Client.csproj
# 将 godot 替换为本机 Godot .NET 可执行文件路径。
godot --path . res://scenes/templates/character_template_editor.tscn -- --template-editor
# 直接运行原战斗观察器：
godot --path . res://scenes/battle/battle_observer.tscn -- --battle-observer
```

- 开场流程结束后停在 **Tick 0，已暂停**。观察模式不推进背景世界。
- **播放**：每秒推进 10 个完整战斗 Tick；蓝白横向光线按双方各自扫描进度向下移动。
- **暂停**：立即停止逻辑时钟，已提交的短动画收尾后静止；继续播放不补跑暂停期间的时间。
- **单步 +1**：暂停时推进一个完整 Tick。充能可令一步扫过多行，仍只增加一个 Tick。
- **重开当前案例**：在独立观察器里重启该战斗窗口，在当前窗口里只更换后台模拟进程，保留完整案例、双种子、参与者顺序、步数配置及数据/核心版本。数据版本变化会停止重放并显示诊断；不会换回另一套默认数据。
- 悬停卡片查看实例 ID、真实占格、点亮需求、冻结及累计使用/生效次数。两张同定义射击共用图片，但状态独立。

属性区显示气血 当前/上限、灵力 当前/上限、SP 占用/角色上限，以及护盾当前值。上限通过核心只读投影取得，不修改原属性缓存；SP 上限不是背包格数。不可用上限显示 ? 并提供诊断。HP 归零后加伤口并恢复 HP 是核心行为，失败以核心结果为准。

核心按 Tick 提交终态快照和有序表现事实，客户端据此播放灵元飞入、点亮、新卡入包、待发飞牌和头顶结算。待发卡从储物阵的实际占格飞到人物身后，拼点时沿角色外侧展开。伤害来自实际扣血记录。正常结束会播完末步表现，再停留供查看或重开。

## 会话日志

每个游戏进程使用独立的 `user://Logs/observer/<UTC时间>-<进程ID>/` 目录，保存 `input.json`、`core.log`、`records.jsonl`、`presentation-v1.jsonl`，错误时另写 `failure.txt`。实际路径出现在控制台 `[BattleObserver] ready ... logs=...` 和底部状态提示中；当前 Windows 项目位于 `%APPDATA%\Godot\app_userdata\修仙slg\Logs\observer\`。

底部卷轴显示最近两条完整行摘要，悬停可看原文；点击“展开”可滚动查看全部原始战报，点击“收起”返回战斗。完整记录仍在会话文件中。重开采用新进程，是为了隔离核心静态 ID 和监听状态；不反射清理核心私有状态。

## 核心独立与共享验证

- `UnityWorld/UnityWorld.csproj` 输出纯 .NET 8 `UnityWorld.dll`，不引用 Godot。核心提供观察记录与无副作用属性投影，直接消费模板进行装配；模板配置检查和 HP/MP/SP 预览由客户端 `scripts/client/templates/TemplateEditorValidator.cs` 提供，正式 JSON/Lua 仍由项目内容提供。
- `Xianxia.Client.csproj` 通过 ProjectReference 引用核心，客户端代码位于 `scripts/client/`。
- `scripts/client/battle/` 为纯 C# 观察会话/值快照；`assets/testing/battle_cases.json` 保存共享案例输入。Godot 和 `tools/client-observer/` 编译同一份适配代码，不复制 NPC 数值或卡位。
- 原 `UnityWorld/Console/UnityWorld.Console.csproj` 保留原测试入口作为兼容基线，没有改成读取新案例清单。
- Godot 编辑器直接加载 `UnityWorld/Data`；原控制台加载构建输出目录 Data。验证比较实际 JSON/Lua 文件集与 SHA-256，不只比较模板 ID 和种子。

以下命令分别创建全新的验证进程，并比较完整输入、Tick 0 到结束的状态与原始 Trace：

```powershell
dotnet run --project UnityWorld/Console/UnityWorld.Console.csproj
dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=. --output=.local-verification/battle-observer/check-a.jsonl --no-event-observer
dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=. --output=.local-verification/battle-observer/check-b.jsonl --observe-repeat=25 --mode=play
dotnet run --project tools/client-observer/ClientObserver.csproj -- compare .local-verification/battle-observer/check-a.jsonl .local-verification/battle-observer/check-b.jsonl
```

输出配套 `.case.json` 保存完整输入，`.identity.json` 保存源文件指纹，`.records.jsonl` 保存观察记录。Compare 要求双方都有 `.case.json`，差异会输出第一处字段路径；成功示例为 `OBSERVER_MATCH frames=741`。指定 `--case-input=<某次.jsonl.case.json>` 可在新进程重放保存的完整案例；只检查前几步时可加 `--stop-tick=20`。

保留的核心加载烟测：

```powershell
dotnet run --project UnityWorld/Console/UnityWorld.Console.csproj -- --core-smoke-test
godot --headless --path . -- --core-smoke-test
```

两个宿主加载真实世界并推进 3 Tick，成功输出 `CORE_SMOKE_OK seed=12345 ticks=3 tiles=40000`。烟测模式优先于观察模式，测试结束会退出。

## Godot 自动验证入口

有画面的验证会保存初始/扫描/伤口候选时点/结果截图及逐 Tick JSONL：

```powershell
$verifyDir = Join-Path (Get-Location) '.local-verification/battle-observer/godot-check'
godot --path . res://scenes/battle/battle_observer.tscn -- --battle-observer "--observer-verify=$verifyDir" --observer-mode=step
```

`--observer-mode=auto` 使用实际播放时钟；`--observer-restarts=3` 在完成后连续启动三个新观察器进程并携带当前输入。每次运行写 `godot-<模式>-<编号>.jsonl` 和同名 `.jsonl.case.json`，可与共享 CLI 基线严格比较。窗口尺寸可用 Godot 的 `--resolution 1600x900` 参数；默认窗口为 1920×1080。

自动操作通过 viewport 注入鼠标移动、按下和释放，经过 Godot GUI 命中检测，覆盖实际按钮回调和坐标路径；它不是操作系统鼠标控制，也不是人工验收。自动播放验证另保存连续 18 个显示帧、光线位置样本及暂停稳定检查，用于检查插值而不是只看逻辑 Tick 截图。各次验证是否通过以输出证据为准，不以入口存在、编译或启动成功代替验收。

## 素材与目录

| 路径 | 内容 |
| --- | --- |
| `assets/art/cards/` | 已确认首批 10 张卡图的项目资源目录 |
| `assets/art/icons/mana/` | 金、木、水、火、土 5 张灵元图标 |
| `assets/art/icons/artifacts/jade_sword.png` | 原有玉剑，复用于长剑，原图保留 |
| `assets/presentation/card_visuals.json` | Card 定义 ID → 图片/裁切/朝向；状态仍按实例 ID 管理 |
| `assets/presentation/mana_visuals.json` | 既有元素 ID → 图标；激活亮度由客户端绘制 |
| `UnityWorld/Docs/art/card_asset_manifest.json` | 全量 88 个 Card 和独立 5 元素台账、提示词、来源及检查状态 |
| `UnityWorld/Docs/art/battle_asset_gaps.md` | 其余卡图、UI、动作帧和特效缺口；后续批次待确认 |
| `UnityWorld/Docs/art/battle_observer_integration.md` | 字段白名单、事件缺口、案例覆盖及详细基线 |
| `UnityWorld/Docs/art/observer-evidence/` | 本次结构化验证摘要、源码边界与复核记录 |
| `.local-verification/battle-observer/` | 本机完整运行日志、逐 Tick 大文件和画面证据 |

缺图时保留卡名/元素名和真实布局，不隐藏卡或停止战斗。首批范围为 15 张新增图片 + 1 张长剑复用；生成、落盘、加载和格内可读性分别记在台账中，其余 17 种已迁移卡及其他旧卡没有获得自动生成许可。人物仍是静态单帧，蓝白扫描和受光由客户端绘制，并不代表完整角色动作帧已具备。

## 当前验证与保留事项

2026-10-27 原控制台默认案例实际完成：TeamB（苏灵儿）胜，`AllDefeated`，740 Tick。源/输出 Data 各 166 文件且完全相同；原控制台 565 条战斗 Trace 与共享适配器逐条相同。共享适配器的两次新进程、每 Tick 25 次读取、自动多 Tick 推进及完整输入重放均获得 741 帧一致结果。

本轮原有回归合计 487 条断言通过；Godot 烟测通过。最终自动播放、单步及三次重开均与基线完整输入和 741 帧一致；另用不同双种子、案例 ID 和 30 Tick 上限验证重开没有回退默认输入。连续 18 个显示帧证实同 Tick 内插值与双方独立扫描，暂停后保持稳定；1440×810 和 1600×900 均完成画面检查。443 个受保护文件哈希无变化。

最终摘要见 `UnityWorld/Docs/art/observer-evidence/godot-data-comparisons.json` 和 `visual-verification.json`，画面在同目录的 `screenshots/`。11 种卡和 5 种灵元均通过加载与静态素材图集验收；重伤在默认战斗中未产生，按明确标注的两格素材预览核验，没有改核心数据强行制造战斗画面。

原有 C# 告警、NuGet/NCalcSync 告警、重复 Trigger ID、缺 ExtraElementDefines/Item 及两张主功法 Lua 的“未 return table”提示均保留，详见接入说明。`UnityWorld/.gdignore` 阻止后端被 Godot 导入，后端不需要 `.uid`。正式导出包的数据和 NLua 原生依赖打包尚不在本次验证范围。

旧素材预览曾于 2026-10-14 完成 Godot .NET 运行和 10 张原素材加载验证，截图保留在 `UnityWorld/Docs/art/battle_assets_preview.png`，来源见 `asset_manifest.json`、`generation_prompts.md`。旧预览的 5×6 示例布局、数值和日志不是本观察器的战斗数据。

## 旧预览视觉组件复用

观察器现复用 `scenes/tests/battle_assets_preview.tscn` 的 Theme、卷轴背包、画像属性框、战报框、待发牌面和人物 Atlas 裁切。`ObserverPreviewSkin` 只提取资源，不把预览节点加入场景树，也不运行其静态示例脚本。原预览文件保留原样。

两侧背包、中央人物、上方待发与底部信息沿用旧构图，真实数据仍是当前五乘五布局。顶部提供播放/暂停/单步/当前案例重开。小格保留名称、分段底色点亮与冻结/使用反馈，悬停查看完整实例信息；属性框显示当前值/真实上限。

新画面见 `UnityWorld/Docs/art/observer-evidence/screenshots-preview-skin/`；本轮对照结果见 `preview-skin-verification.json`。

## 2026-10-31 战斗表现扩展

实现说明及当前验证证据见 `tools/client-observer/PRESENTATION.md`；核心事件、纯属性读取、UI 组件分别见同目录 `CORE_PRESENTATION.md`、`STAT_PROJECTION.md`、`UI_PRESENTATION.md`。上文 2026-10-27 数量与文件哈希是历史首版证据。

新增验证入口：

```powershell
dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=. --check-presentation-core
dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=. --verify-stat-projection
dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=. --check-presentation-player
```

`--observer-no-animations` 关闭客户端动画但继续显示完整状态。Godot 的 `--observer-presentation-fixture=<绝对目录>` 运行明确标注的纯值视觉案例，覆盖两格水卡半亮、混色冻结、通用入包和双槽平局等；不替代真实战斗验证。

用户实测修正：扫描光线独立跟随最新逻辑进度；跨 Tick 的无关动画并行，生效名称与伤害淡出不阻塞新事件。同一多格卡的底色与图片合并连续绘制，长剑不再被内部格线切断。
