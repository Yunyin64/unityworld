# Godot 客户端与独立 C# 核心库

用 Godot 4.7.2 .NET 打开根目录 `project.godot`，按 F5 运行。
主场景已设置为 `res://scenes/tests/battle_assets_preview.tscn`。
主场景仍是素材展示，数值和日志是示例文本，没有背包交互。自动加载的 `GameBootstrap` 会从 `UnityWorld.dll` 初始化真实世界，并在物理帧驱动 `WorldMgr.Tick()`；展示界面尚未绑定核心战斗结果。

## 核心库与启动方式

- `UnityWorld/UnityWorld.csproj`：纯 .NET 8 类库，输出 `UnityWorld.dll`，不引用 Godot。
- `UnityWorld/Console/UnityWorld.Console.csproj`：独立控制台宿主，链接原有 `Program.cs` 和测试代码，不复制或移动现有 C# 文件。
- `Xianxia.Client.csproj`：Godot 宿主，通过 `ProjectReference` 引用核心库；修改后端后正常构建即可自动更新 DLL，无须手动复制。
- 核心的 `Data/**/*.json`、`Data/**/*.lua` 自动复制到各宿主输出目录。Godot 编辑器运行时直接读取 `UnityWorld/Data`，发布版约定读取可执行文件旁的 `Data` 目录，由宿主把路径传入核心；退出时调用 `WorldMgr.End()`。
- `UnityWorld/.gdignore` 阻止 Godot 扫描整个后端目录，不影响 .NET 编译。后端不需要 `.uid`；客户端脚本的 `.uid` 仍需提交。
- `bin/`、`obj/`、`.godot/` 不提交。这里验证的是本机 .NET / Godot 运行；正式 Godot 导出包仍需另行验证数据文件和 NLua 原生依赖的打包。

在仓库根目录执行：

```powershell
dotnet build UnityWorld/UnityWorld.sln
dotnet run --project UnityWorld/Console/UnityWorld.Console.csproj
dotnet build Xianxia.Client.csproj
```

不跑完整战斗的核心接入检查：

```powershell
dotnet run --project UnityWorld/Console/UnityWorld.Console.csproj -- --core-smoke-test
# 将 godot 替换为本机 Godot .NET 可执行文件路径：
godot --headless --path . -- --core-smoke-test
```

两个宿主都以固定种子初始化、加载实际数据并推进 3 个 Tick，成功时输出 `CORE_SMOKE_OK`。

## DLL 拆分验证（2026-10-14）

- 核心/控制台解决方案及 Godot 项目编译通过，0 个错误；原有 C# 警告与 NCalcSync 依赖警告仍存在。
- 控制台、Godot headless 均输出 `CORE_SMOKE_OK seed=12345 ticks=3 tiles=40000`，实际加载 Lua 初始化文件和 12 个 Keyword。
- 控制台原有战斗流程完成；Godot 正常渲染主场景、加载 10 张素材并成功截图。
- 重新导入 Godot 后，`UnityWorld/` 下 `.uid` 数量仍为 0；客户端 `.uid` 正常保留。
- 数据里原有的重复 Trigger ID、缺少 ExtraElementDefines/Item 提示仍保留，本次没有修改玩法数据。
- API 文档/Lua 包装导出改为开发工具显式调用，正常启动不再写入旧电脑的 `F:\Openclaw` 路径。`ExportLua(outputDir)` 可用于重新生成包装文件。
- 本地检查日志、截图及清理前的 UID 备份放在根目录 `.local-verification/`，不进入 Git。

## 目录

```text
unityworld/
├── UnityWorld/                引擎无关核心 DLL、数据和独立 Console 宿主
├── assets/
│   └── art/
│       ├── backgrounds/       青岚山背景 × 1
│       ├── characters/        透明底示例人物 × 2
│       ├── ui/                背包、战斗记录、属性面板 × 3
│       └── icons/artifacts/   玉剑、葫芦、火符、铜铃 × 4
├── scenes/tests/              可编辑的素材加载测试场景
├── scripts/client/            Godot 启动适配、素材加载检查与可选截图
├── docs/art/                  素材清单、生成提示词、实际运行截图
├── Xianxia.Client.csproj      编译客户端并引用核心项目
└── project.godot              Godot 工程入口
```

## 素材与显示

- 共 10 张独立 PNG：背景不透明，其余 9 张保留透明通道。
- 左右面板复用同一份素材；物品图标、人物、背景均可独立替换。
- 网格由 60 个独立 Panel 控件组成，每侧 5 列 × 6 行。占格底色和物品贴图也分别独立。
- 文字是 Label，红蓝黄条是 ProgressBar；不是整张 UI 图片。
- 人物为静态单帧，不包含动画；面板为完整底图，未制作九宫格切片。
- 场景中的 AtlasTexture 只调整透明留白和头像显示区域，原始 PNG 不受影响。
- 基准布局为 1600 × 900，默认窗口为 1440 × 810。使用项目的 canvas_items 缩放。
- 中文使用系统字体回退（微软雅黑、Noto Sans CJK SC、Noto Sans SC）。发行时建议再指定可随游戏分发的字体。
- 场景树中 `Hud` 下放置 UI，`Combatants` 下放置人物。均可直接在编辑器中调整。

## 原素材预览验证记录（拆分前）

已完成 `dotnet build Xianxia.Client.csproj`：0 个警告、0 个错误。
实际使用 Godot 4.7.2 .NET、D3D12 Forward+ 运行根目录主场景，日志确认 10 张素材全部加载。
实际截图见 `docs/art/battle_assets_preview.png`。
对 `UnityWorld/` 的 1639 个原有文件做了前后 SHA-256 校验，没有新增、删除或内容变化。

生成方式为内置 imagegen，提示词见 `docs/art/generation_prompts.md`，路径/尺寸/透明信息见 `docs/art/asset_manifest.json`。
