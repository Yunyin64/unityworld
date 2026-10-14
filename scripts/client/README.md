# Godot 客户端 C#

此目录只放 Godot 客户端的显示、输入和引擎接入代码。

当前 `AssetPreview.cs` 只检查测试场景中的贴图是否加载，并提供可选截图参数。没有战斗、卡牌、背包或角色数值逻辑。

根目录 `Xianxia.Client.csproj` 只编译 `scripts/client/**/*.cs`，不引用、不编译、不写入 `UnityWorld/`。

场景布局、贴图引用、文字和示例资源条保存在 `scenes/tests/battle_assets_preview.tscn`，可直接在 Godot 编辑器中调整。
