# Godot 战斗观察客户端

使用 Godot 4.7.2 .NET 打开根目录 `project.godot`，按 F5 进入真实战斗观察界面。默认场景已设置为 `res://scenes/battle/battle_observer.tscn`。原静态预览保留在 `res://scenes/tests/battle_assets_preview.tscn`，仍可选中后按 F6 单独运行。

观察器使用原控制台默认案例 `console-basic-template`：世界和战斗种子均为 12345，依次装配 `test_npc_a`、`test_npc_b`，上限 5000 Tick。双方真实背包为 5×5，角色数值、格上灵元、卡片点亮、冻结、待发和战报均来自核心模拟。人物立绘仅为客户端展示形象。

## 运行与操作

在仓库根目录执行：

```powershell
dotnet build Xianxia.Client.csproj
# 将 godot 替换为本机 Godot .NET 可执行文件路径。
godot --path . res://scenes/battle/battle_observer.tscn -- --battle-observer
```

- 开场流程结束后停在 **Tick 0，已暂停**。观察模式不推进背景世界。
- **播放**：每秒推进 10 个完整战斗 Tick；蓝白横向光线按双方各自扫描进度向下移动。
- **暂停**：立即停止逻辑时钟，已提交的短动画收尾后静止；继续播放不补跑暂停期间的时间。
- **单步 +1**：暂停时推进一个完整 Tick。充能可令一步扫过多行，仍只增加一个 Tick。
- **重开当前案例（重启观察器）**：启动新的游戏窗口并退出旧窗口，保留完整案例、双种子、参与者顺序、步数配置及数据/核心版本。数据版本变化会停止重放并显示诊断；不会换回另一套默认数据。
- 悬停卡片查看实例 ID、真实占格、点亮需求、冻结及累计使用/生效次数。两张同定义射击共用图片，但状态独立。

属性区显示气血 当前/上限、灵力 当前/上限、SP 占用/角色上限，以及护盾当前值。上限通过核心只读投影取得，不修改原属性缓存；SP 上限不是背包格数。不可用上限显示 ? 并提供诊断。HP 归零后加伤口并恢复 HP 是核心行为，失败以核心结果为准。

光线扫过的受光与真实点亮分开：核心按 Tick 提交终态快照和有序表现事实，客户端据此播放源格灵元飞入、分段底色点亮、通用新卡入包、待发槽内容对撞、头顶伤害和“卡名 · 生效”。卡面不再显示点亮 x/x，悬停仍可查看。待发槽底框与背包卡不移动；伤害来自实际扣血记录。正常结束会播完末步表现，再停留供查看或重开。

## 会话日志

每个游戏进程使用独立的 `user://Logs/observer/<UTC时间>-<进程ID>/` 目录，保存 `input.json`、`core.log`、`records.jsonl`、`presentation-v1.jsonl`，错误时另写 `failure.txt`。实际路径出现在控制台 `[BattleObserver] ready ... logs=...` 和底部状态提示中；当前 Windows 项目位于 `%APPDATA%\Godot\app_userdata\修仙slg\Logs\observer\`。

底部卷轴显示最近两条完整行摘要，悬停可看原文；点击“展开”可滚动查看全部原始战报，点击“收起”返回战斗。完整记录仍在会话文件中。重开采用新进程，是为了隔离核心静态 ID 和监听状态；不反射清理核心私有状态。

## 核心独立与共享验证

- `UnityWorld/UnityWorld.csproj` 输出纯 .NET 8 `UnityWorld.dll`，不引用 Godot。核心仅增加纯观察记录及无副作用属性投影；JSON/Lua、原控制台入口与玩法规则保持不变。
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

`--observer-mode=auto` 使用实际播放时钟；`--observer-restarts=3` 在完成后连续启动三个新观察器进程并携带当前输入。每次运行写 `godot-<模式>-<编号>.jsonl` 和同名 `.jsonl.case.json`，可与共享 CLI 基线严格比较。窗口尺寸可用 Godot 的 `--resolution 1600x900` 参数；默认窗口为 1440×810。

自动操作通过 viewport 注入鼠标移动、按下和释放，经过 Godot GUI 命中检测，覆盖实际按钮回调和坐标路径；它不是操作系统鼠标控制，也不是人工验收。自动播放验证另保存连续 18 个显示帧、光线位置样本及暂停稳定检查，用于检查插值而不是只看逻辑 Tick 截图。各次验证是否通过以输出证据为准，不以入口存在、编译或启动成功代替验收。

## 素材与目录

| 路径 | 内容 |
| --- | --- |
| `assets/art/cards/` | 已确认首批 10 张卡图的项目资源目录 |
| `assets/art/icons/mana/` | 金、木、水、火、土 5 张灵元图标 |
| `assets/art/icons/artifacts/jade_sword.png` | 原有玉剑，复用于长剑，原图保留 |
| `assets/presentation/card_visuals.json` | Card 定义 ID → 图片/裁切/朝向；状态仍按实例 ID 管理 |
| `assets/presentation/mana_visuals.json` | 既有元素 ID → 图标；激活亮度由客户端绘制 |
| `docs/art/card_asset_manifest.json` | 全量 88 个 Card 和独立 5 元素台账、提示词、来源及检查状态 |
| `docs/art/battle_asset_gaps.md` | 其余卡图、UI、动作帧和特效缺口；后续批次待确认 |
| `docs/art/battle_observer_integration.md` | 字段白名单、事件缺口、案例覆盖及详细基线 |
| `docs/art/observer-evidence/` | 本次结构化验证摘要、源码边界与复核记录 |
| `.local-verification/battle-observer/` | 本机完整运行日志、逐 Tick 大文件和画面证据 |

缺图时保留卡名/元素名和真实布局，不隐藏卡或停止战斗。首批范围为 15 张新增图片 + 1 张长剑复用；生成、落盘、加载和格内可读性分别记在台账中，其余 17 种已迁移卡及其他旧卡没有获得自动生成许可。人物仍是静态单帧，蓝白扫描和受光由客户端绘制，并不代表完整角色动作帧已具备。

## 当前验证与保留事项

2026-10-27 原控制台默认案例实际完成：TeamB（苏灵儿）胜，`AllDefeated`，740 Tick。源/输出 Data 各 166 文件且完全相同；原控制台 565 条战斗 Trace 与共享适配器逐条相同。共享适配器的两次新进程、每 Tick 25 次读取、自动多 Tick 推进及完整输入重放均获得 741 帧一致结果。

本轮原有回归合计 487 条断言通过；Godot 烟测通过。最终自动播放、单步及三次重开均与基线完整输入和 741 帧一致；另用不同双种子、案例 ID 和 30 Tick 上限验证重开没有回退默认输入。连续 18 个显示帧证实同 Tick 内插值与双方独立扫描，暂停后保持稳定；1440×810 和 1600×900 均完成画面检查。443 个受保护文件哈希无变化。

最终摘要见 `docs/art/observer-evidence/godot-data-comparisons.json` 和 `visual-verification.json`，画面在同目录的 `screenshots/`。11 种卡和 5 种灵元均通过加载与静态素材图集验收；重伤在默认战斗中未产生，按明确标注的两格素材预览核验，没有改核心数据强行制造战斗画面。

原有 C# 告警、NuGet/NCalcSync 告警、重复 Trigger ID、缺 ExtraElementDefines/Item 及两张主功法 Lua 的“未 return table”提示均保留，详见接入说明。`UnityWorld/.gdignore` 阻止后端被 Godot 导入，后端不需要 `.uid`。正式导出包的数据和 NLua 原生依赖打包尚不在本次验证范围。

旧素材预览曾于 2026-10-14 完成 Godot .NET 运行和 10 张原素材加载验证，截图保留在 `docs/art/battle_assets_preview.png`，来源见 `asset_manifest.json`、`generation_prompts.md`。旧预览的 5×6 示例布局、数值和日志不是本观察器的战斗数据。

## 旧预览视觉组件复用

观察器现复用 `scenes/tests/battle_assets_preview.tscn` 的 Theme、卷轴背包、画像属性框、战报框、待发牌面和人物 Atlas 裁切。`ObserverPreviewSkin` 只提取资源，不把预览节点加入场景树，也不运行其静态示例脚本。原预览文件保留原样。

两侧背包、中央人物、上方待发与底部信息沿用旧构图，真实数据仍是当前五乘五布局。顶部提供播放/暂停/单步/当前案例重开。小格保留名称、分段底色点亮与冻结/使用反馈，悬停查看完整实例信息；属性框显示当前值/真实上限。

新画面见 `docs/art/observer-evidence/screenshots-preview-skin/`；本轮对照结果见 `preview-skin-verification.json`。

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
