## Why

战斗中需要支持"光环"类效果：一个 Modifier 的属性修正不只作用于持有者自身，还可以动态作用于其他 Npc 甚至 Card。当前 Modifier 的 `StatModifiers` 只能写入持有者的 StatBlock，无法跨对象影响。同时，现有 `CallLuaHook` 每次调用都通过 `env[hookName] as LuaFunction` 动态查找，存在不必要的重复开销。

本次变更通过两件事解决问题：
1. **统一属性注入机制**：任何对象读属性时遍历所有 Modifier，调用 `OnModifierStat{属性名}` Lua hook，由 Lua 决定"对谁生效、生效多少"。
2. **LuaHooks 预扫描**：Modifier 创建时一次性扫描 Lua env 中的所有函数引用，缓存到 Dictionary，替代运行时动态查找。

## What Changes

- `CombatNpcModifier` 新增 `LuaHooks` 字典（`Dictionary<string, LuaFunction>`），创建时预扫描 Lua env 所有函数。
- `CallLuaHook` 改为查 `LuaHooks` 字典，不再每次访问 env。保留老路径作为开关（AB 测试）。
- `CombatNpc.GetStat(statId)` 新增：裸值 + 遍历全场所有 Modifier 中有 `OnModifierStat{statId}` hook 的，调 Lua 累加返回值。
- `CombatCard` 读属性（如 CD）同样走统一路径：遍历所有 Modifier 中有 `OnModifierStat{属性名}` hook 的，调 Lua 累加。
- 不再需要 AuraTarget 字段、IsAuraTarget hook、内置选择器、CollectAura 方法。
- 传统 Buff（给自己+1攻击）也通过 `OnModifierStatAtk` 实现，Self 不再特殊处理。

## Capabilities

### New Capabilities
- `modifier-stat-hook`: 统一属性注入机制——任何对象读属性时通过 `OnModifierStat{XX}` Lua hook 从全场 Modifier 收集贡献。
- `modifier-lua-cache`: Modifier Lua 函数预扫描缓存——创建时扫描 env，缓存到 LuaHooks 字典，替代运行时动态查找。

### Modified Capabilities
- `modifier-base`: CombatNpcModifier 的 CallLuaHook 改为查预缓存字典，保留老路径开关。

## Impact

- **核心路径变更**: 属性读取统一走 `GetStat()` 包装，遍历 Modifier 的 Lua hook。
- **性能**: 预扫描后，无对应 hook 的 Modifier 通过 `ContainsKey` 跳过，零 Lua 调用开销。有 hook 的才调 Lua。
- **Lua 接口**: Modder 写 `OnModifierStat{XX}(env, caller)` 函数，caller 可以是 Npc 或 Card。
- **向后兼容**: 保留老的 env 动态查找路径作为开关，可 AB 测试。
- **涉及文件**: `CombatNpcModifier.cs`、`CombatNpc*.cs`、`CombatCard*.cs`、`CombatScene.cs`。
