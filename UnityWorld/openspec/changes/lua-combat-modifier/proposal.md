## Why

CombatNpcModifier 已实现 ILuaBindable 接口，拥有 `env` 属性，但当前完全没有被使用——没有 Lua 脚本加载、没有生命周期 Hook 调用、没有 Modifier Tick 衰减逻辑。Modifier 挂上就是死数据。为了让战斗 Modifier（燃烧、中毒、护盾再生等）的行为由 Lua 脚本驱动——与 CombatCard 的 Lua 驱动模式保持一致——需要补全 Modifier 的完整生命周期管线。

## What Changes

- LuaMgr 新增 `LoadModifierScript(defineId)` 方法，按约定路径 `Data/LuaScripts/CombatModifiers/{defineId}.lua` 加载脚本，每次调用返回独立 LuaTable（不缓存）
- CombatNpcModifier 新增 `CallLuaHook<bool>(hookName, npc)` 方法，从 env 取函数并调用
- CombatNpc 新增 partial 文件 `CombatNpcModifierFunc.cs`，包含：
  - `AddModifier()`：查重叠层 + Lua env 加载 + 调用 OnApply/OnStack
  - `ModifierTick()`：遍历 Modifiers、调用 OnTick、衰减 RemainingTime、移除过期 Modifier 并调用 OnRemove
  - `RemoveModifier(id)`：主动移除并调用 OnRemove
- CombatNpc.Buffs 重命名为 Modifiers
- CombatNpc.Tick() 接入 ModifierTick() 调用
- 建立 `Data/LuaScripts/CombatModifiers/` 目录

## Capabilities

### New Capabilities
- `combat-modifier-lua`: CombatNpcModifier 的 Lua 脚本驱动生命周期管线（加载、OnApply、OnTick、OnStack、OnRemove）

### Modified Capabilities
- `modifier-base`: IModifierBase 的叠层规则需要在 spec 层面补充（叠层判定、MaxStack、RefreshOnStack 的行为契约）

## Impact

- `Scripts/Core/Systems/LuaMgr.cs` — 新增 LoadModifierScript 方法
- `Scripts/Game/Domain/Object/Modifier/CombatNpcModifier.cs` — 新增 CallLuaHook
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs` — Tick() 加入 ModifierTick()，Buffs 重命名为 Modifiers
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs` — 新增文件
- `Data/LuaScripts/CombatModifiers/` — 新增目录
