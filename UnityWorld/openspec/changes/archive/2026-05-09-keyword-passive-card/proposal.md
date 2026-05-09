## Why

当前战斗卡牌系统中所有 CombatCard 都走统一的 CD 循环（WaitResource → InCD → Ready → InPending → Finished），无法表达"被动卡"——一种不主动出手、没有 CD 机制、通过声明式 keyword 决定行为的卡牌类型。需要引入 Keyword 系统作为基础设施，使卡牌的运行模式可由 Lua 脚本声明式扩展，首个落地的 keyword 为 `Passive`。

## What Changes

- 在 `CombatCardPhase` 枚举中新增 `Passive` 值，被动卡的 Phase 锁定于此，跳过 CD 循环。
- 在 `CardDefine` 和 `CardBaseData` 上新增 `List<string> Keywords` 字段，支持 JSON 定义和运行时访问。
- 在 `LuaMgr` 中新增 Keyword 注册表：启动时加载 `Keywords/Keyword.lua` 索引文件，按映射加载每个 keyword 对应的 Lua 脚本，缓存为 `Dictionary<string, LuaTable>`。
- 在 `CombatCard` 各生命周期节点（PreStart/Start/Tick/OnUse/End）中，遍历卡牌 keywords 列表，对每个 keyword 查询注册表并调用对应 Lua Hook（hook 不存在则静默跳过；keyword 未注册则报错）。
- 新增 `Keywords/Keyword.lua`（索引文件）和 `Keywords/Passive.lua`（Passive keyword 实现），`Passive.lua` 在 `OnPreStart` 中调用 `card:SetPhase("Passive")` 将卡设为被动模式。
- CombatCard.Tick() 中，Phase == Passive 时仅调用 keyword hooks 和卡自身的 `OnPassiveTick`，跳过 CD 循环。

## Capabilities

### New Capabilities
- `keyword-system`: Keyword 注册表基础设施——LuaMgr 加载 Keyword.lua 索引、缓存 keyword Lua 脚本、CombatCard 生命周期中遍历调用 keyword hooks。
- `passive-card`: 被动卡运行模式——CombatCardPhase.Passive、Keywords 数据字段、Passive.lua 实现、Tick 跳过 CD 循环。

### Modified Capabilities
<!-- 无现有 spec 需要修改 -->

## Impact

- **枚举**: `Enum_Combat.cs` — `CombatCardPhase` 新增 `Passive`
- **数据定义**: `CardDefine.cs`、`CardBaseData.cs` — 新增 `Keywords` 字段及 Clone 支持
- **运行时**: `CombatCard.cs` — 生命周期各节点插入 keyword hook 遍历调用
- **Lua 管理**: `LuaMgr.cs` — 新增 Keyword 注册表加载与查询 API
- **Lua 脚本**: 新增 `Keywords/Keyword.lua`、`Keywords/Passive.lua`
- **C# 暴露给 Lua 的 API**: 需要暴露 `SetPhase` 方法供 Lua 调用
