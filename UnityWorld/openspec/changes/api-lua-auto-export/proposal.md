## Why

手写 Lua 包装函数（Action.lua / Condition.lua 等）与 C# 侧 `[APIFunc]` Attribute 存在重复声明——每次新增或修改 API 都需要同步维护两处，容易遗漏导致运行时报错。通过运行时反射自动导出 Lua 包装层，消除这一同步成本。

## What Changes

- `APIMgr` 新增 `ExportLua()` 方法，在 `Init()` 末尾调用
- 按 `APIType`（Action / Condition / Contest / Aura）分别生成对应的 `.lua` 文件到 `Data/LuaScripts/`
- 生成逻辑根据参数类型自动选择 `tonumber()` 包装或直接传值
- Condition 类型自动追加 `return ctx:GetObject("Result")`
- 可选参数生成 `if x then ... end` 守卫
- 现有手写的 `Action.lua`、`Condition.lua` 被自动生成版本替代

## Capabilities

### New Capabilities
- `lua-wrapper-export`: APIMgr 运行时按 APIType 自动生成 Lua 包装函数文件

### Modified Capabilities

（无）

## Impact

- 修改文件：`Scripts/Game/Domain/!Global/API/APIMgr.cs`
- 覆盖文件：`Data/LuaScripts/Action.lua`、`Data/LuaScripts/Condition.lua`、`Data/LuaScripts/Contest.lua`、`Data/LuaScripts/Aura.lua`
- `Init.lua` 中的 `require` 列表可能需要更新（如新增 Contest.lua）
- 不影响卡牌脚本（card_xxx.lua）和 Keywords/ 目录
