## Why

卡牌系统的 Lua 脚本执行链路当前是断的：`CombatCard` 已有 `env` 字段和 `OnContest`/`OnApply` 占位，`LuaMgr` 已有脚本加载和 Hook 调用接口，但两者之间没有真正连通。需要跑通完整的 Lua 调用管线，让卡牌脚本可以定义战斗行为。

## What Changes

- `LuaMgr.Init()` 增加加载 `Data/LuaScripts/Init.lua` 的步骤，注册全局函数（Attack、Shield 等包装）和 CardBase 元表
- `LuaMgr.LoadCardScript()` 改为不缓存，每次执行脚本返回独立 card table（避免多实例污染）
- `CombatCard.OnContest()` / `OnApply()` 构造 `APIContext` 并调用 Lua 函数
- `CombatCard.InitializeLuaCards()` 简化为加载脚本 → `env = return 值`
- `APIMgr.ScanHandlers()` 修复签名校验 bug（支持返回 `APIContext` + 参数类型 `APIContext`）
- `Data/LuaScripts/Init.lua` 新建，提供 Attack/Shield/Block/Heal 等 Lua 包装函数
- `Data/LuaCards/card_form_quan_da.lua` 补全 OnContest 实际逻辑

## Capabilities

### New Capabilities
- `lua-card-runtime`: 卡牌 Lua 脚本运行时管线——从加载、实例化到 OnContest/OnApply 的完整调用链路

### Modified Capabilities


## Impact

- `Scripts/Core/Systems/LuaMgr.cs` — 加载 Init.lua、去掉缓存、简化 LoadCardScript
- `Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs` — OnContest/OnApply 填入 Lua 调用
- `Scripts/Game/Domain/Combat/CombatCard/CombatCardFunc.cs` — InitializeLuaCards 简化
- `Scripts/Game/Domain/!Global/API/APIMgr.cs` — ScanHandlers 签名校验修复
- `Data/LuaScripts/Init.lua` — 新文件
- `Data/LuaCards/card_form_quan_da.lua` — 补全逻辑