# Godot 客户端 C#

此目录放 Godot 显示/输入、宿主模式与客户端非 UI 服务。根目录 `Xianxia.Client.csproj` 编译 `scripts/client/**/*.cs`，通过 ProjectReference 引用纯 .NET 核心 `UnityWorld/UnityWorld.csproj`；核心不依赖 Godot。

当前默认入口是 `scenes/templates/character_template_editor.tscn`。使用 Godot .NET 打开项目按 F5，或在仓库根目录执行：

```powershell
dotnet build Xianxia.Client.csproj
godot --path . res://scenes/templates/character_template_editor.tscn -- --template-editor
```

将 `godot` 替换为本机 Godot .NET 可执行文件路径。实际操作、个人文件位置和人工检查见 [模板编辑器快速开始](../../docs/template-editor/QUICKSTART.md)，完整观察器说明见 [根目录 README](../../README_GODOT.md)。

| 目录／文件 | 职责 |
| --- | --- |
| `GameBootstrap.cs` | 明确识别 editor / observer / world / smoke 模式；编辑器不创建或推进世界，观察器独立拥有模拟会话 |
| `templates/` | 只读内容目录、客户端模板配置检查与 HP/MP/SP 预览、草稿文档与稳定卡实例身份、转换与保存、动态筛选、冻结试战输入与独立进程启动 |
| `ui/templates/` | 模板列表、属性/卡牌详情、背包拖放、动态 Keyword 筛选、素材与操作对话框 |
| `battle/` | 观察案例、真实模拟会话、值快照和表现事实；读取个人模板快照与旧项目模板案例 |
| `ui/` 中的 `BattleObserverView` 等 | 已有双背包战斗展示、播放/暂停/单步、日志、重开与表现层 |
| `AssetPreview.cs` | 原静态素材预览辅助；不再代表整个客户端的能力 |

项目模板只读，复制、新建和导入的内容存入个人库；正式导出使用标准 NPC JSON 数组，并禁止从编辑器写入项目 `UnityWorld/Data/`。卡牌、功法和装备组合来自已有内容，图片缺失时显示名称，不影响内容可用性。Keyword 选项从卡牌显式 `Keywords` 动态去重，多选采用 AND，不枚举运行时派生项。

试战复用 `scenes/battle/battle_observer.tscn`，启动独立观察器窗口并保留编辑器。直接启动旧默认战斗仍可用：

```powershell
godot --path . res://scenes/battle/battle_observer.tscn -- --battle-observer
```

数据协议见 [data-format.md](../../docs/template-editor/data-format.md)，冻结快照和进程参数见 [trial-input.md](../../docs/template-editor/trial-input.md)，核心字段支持范围见 [backend-gap-check.md](../../docs/template-editor/backend-gap-check.md)。
