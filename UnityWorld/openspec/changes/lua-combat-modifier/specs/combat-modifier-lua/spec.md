## ADDED Requirements

### Requirement: LuaMgr 提供 Modifier 脚本加载方法
LuaMgr SHALL 提供 `LoadModifierScript(string defineId)` 方法。该方法 SHALL 在 `Data/LuaScripts/CombatModifiers/{defineId}.lua` 路径加载 Lua 脚本，每次调用 SHALL 执行 DoFile 返回全新的独立 LuaTable（不缓存）。脚本不存在时 SHALL 返回 null 且不报错。

#### Scenario: 正常加载 Modifier 脚本
- **WHEN** 调用 `LuaMgr.LoadModifierScript("Burn")` 且 `CombatModifiers/Burn.lua` 存在并 return table
- **THEN** 返回一个 LuaTable 实例，包含脚本定义的所有函数

#### Scenario: 脚本不存在时返回 null
- **WHEN** 调用 `LuaMgr.LoadModifierScript("NonExist")` 且对应文件不存在
- **THEN** 返回 null，不抛出异常，不输出错误日志

#### Scenario: 多次加载同 ID 返回独立实例
- **WHEN** 连续两次调用 `LuaMgr.LoadModifierScript("Burn")`
- **THEN** 返回两个不同引用的 LuaTable，修改其中一个不影响另一个

### Requirement: CombatNpcModifier 支持 Lua Hook 调用
CombatNpcModifier SHALL 提供 `CallLuaHook(string hookName, CombatNpc npc)` 方法。该方法 SHALL 从 `env` 中取出名为 hookName 的 LuaFunction 并以 `(mod, npc)` 参数调用。env 为 null 或 hook 函数不存在时 SHALL 静默跳过。Lua 执行异常时 SHALL 捕获并输出错误日志，不中断战斗流程。

#### Scenario: env 存在且 hook 存在
- **WHEN** modifier.env 包含 `OnTick` 函数，调用 `CallLuaHook("OnTick", npc)`
- **THEN** 执行 `env.OnTick(mod, npc)`

#### Scenario: env 为 null（纯数值 Modifier）
- **WHEN** modifier.env 为 null，调用 `CallLuaHook("OnTick", npc)`
- **THEN** 静默跳过，不报错

#### Scenario: hook 函数不存在
- **WHEN** modifier.env 存在但不包含 `OnStack` 函数，调用 `CallLuaHook("OnStack", npc)`
- **THEN** 静默跳过，不报错

#### Scenario: Lua 执行异常
- **WHEN** hook 函数执行时抛出 Lua 异常
- **THEN** 捕获异常，输出错误日志，战斗继续正常运行

### Requirement: AddModifier 支持 Lua 加载与叠层
CombatNpc.AddModifier(defineId, stacks) SHALL 执行以下流程：
1. 查找 Modifiers 列表中是否已有同 DefineId 的 Modifier
2. 若无：从 Define 创建实例 → 通过 LuaMgr 加载 Lua env → 调用 OnApply hook → 加入 Modifiers 列表
3. 若有：执行叠层逻辑（CurrentStack += stacks，受 MaxStack 限制；RefreshOnStack 时重置 RemainingTime）→ 调用 OnStack hook

#### Scenario: 首次添加 Modifier 触发 OnApply
- **WHEN** CombatNpc 的 Modifiers 中没有 DefineId="Burn" 的 Modifier，调用 AddModifier("Burn")
- **THEN** 创建 CombatNpcModifier 实例，加载 Burn.lua 为 env，调用 env.OnApply(mod, npc)，加入 Modifiers

#### Scenario: 首次添加无 Lua 脚本的纯数值 Modifier
- **WHEN** 调用 AddModifier("AtkUp") 且 AtkUp.lua 不存在
- **THEN** 创建 CombatNpcModifier 实例，env 为 null，正常加入 Modifiers，StatModifiers 正常生效

#### Scenario: 重复添加同 DefineId 触发叠层
- **WHEN** Modifiers 中已有 DefineId="Poison" 且 CurrentStack=1、MaxStack=5，调用 AddModifier("Poison", 2)
- **THEN** CurrentStack 变为 3，调用 env.OnStack(mod, npc)，不创建新实例

#### Scenario: 叠层达到 MaxStack 上限
- **WHEN** Modifiers 中已有 DefineId="Burn" 且 CurrentStack=4、MaxStack=5，调用 AddModifier("Burn", 3)
- **THEN** CurrentStack 变为 5（不超过 MaxStack），调用 env.OnStack

#### Scenario: MaxStack=0 表示无上限
- **WHEN** MaxStack=0 且 CurrentStack=100，调用 AddModifier 再叠 50 层
- **THEN** CurrentStack 变为 150

#### Scenario: RefreshOnStack 刷新持续时间
- **WHEN** 已有 Modifier 的 RefreshOnStack=true、Duration=100、RemainingTime=30，触发叠层
- **THEN** RemainingTime 重置为 100

### Requirement: ModifierTick 每战斗 Tick 驱动 Modifier 生命周期
CombatNpc SHALL 在每个战斗 Tick 中调用 ModifierTick()。ModifierTick SHALL 遍历 Modifiers 列表，对每个 Modifier 调用 OnTick hook，然后对有限时 Modifier（Duration > 0）衰减 RemainingTime。过期 Modifier SHALL 调用 OnRemove hook 后从列表移除。遍历中 SHALL 使用 toRemove 临时列表收集待移除 Modifier，遍历结束后批量移除。

#### Scenario: 正常 Tick 调用 OnTick
- **WHEN** Modifiers 中有一个 env 包含 OnTick 的 Modifier，执行 ModifierTick()
- **THEN** 调用 env.OnTick(mod, npc)

#### Scenario: 有限时 Modifier 衰减
- **WHEN** Duration=10、RemainingTime=3 的 Modifier 经过一次 ModifierTick()
- **THEN** RemainingTime 变为 2

#### Scenario: Modifier 过期后调用 OnRemove 并移除
- **WHEN** RemainingTime=1 的 Modifier 经过 ModifierTick()，RemainingTime 衰减到 0
- **THEN** 调用 env.OnRemove(mod, npc)，该 Modifier 从 Modifiers 列表中移除

#### Scenario: 永久 Modifier 不衰减
- **WHEN** Duration=-1 的 Modifier 经过 ModifierTick()
- **THEN** RemainingTime 不变，Modifier 不被移除

#### Scenario: 不直接在遍历中修改集合
- **WHEN** ModifierTick 遍历时有 2 个 Modifier 过期
- **THEN** 先收集到 toRemove 列表，遍历结束后批量调用 OnRemove 并移除

### Requirement: RemoveModifier 支持主动移除
CombatNpc SHALL 提供 `RemoveModifier(string defineId)` 方法，查找 Modifiers 中 DefineId 匹配的 Modifier，调用 OnRemove hook 后移除。未找到时静默跳过。

#### Scenario: 主动移除已有 Modifier
- **WHEN** Modifiers 中有 DefineId="Shield" 的 Modifier，调用 RemoveModifier("Shield")
- **THEN** 调用 env.OnRemove(mod, npc)，从 Modifiers 列表中移除

#### Scenario: 移除不存在的 Modifier
- **WHEN** Modifiers 中没有 DefineId="Shield"，调用 RemoveModifier("Shield")
- **THEN** 无任何操作，不报错

### Requirement: CombatNpc.Tick 接入 ModifierTick
CombatNpc.Tick() SHALL 在每个战斗 Tick 中调用 ModifierTick()，使 Modifier 生命周期与战斗主循环同步。

#### Scenario: Tick 中 ModifierTick 被调用
- **WHEN** CombatNpc.Tick() 执行
- **THEN** ModifierTick() 被调用，所有 Modifier 的 OnTick 被执行

### Requirement: CombatModifiers Lua 脚本约定
`Data/LuaScripts/CombatModifiers/` 目录下的 Lua 脚本 SHALL return 一个 table，可包含以下 hook 函数（均为可选）：
- `OnApply(mod, npc)` — 首次挂载
- `OnTick(mod, npc)` — 每战斗 Tick
- `OnStack(mod, npc)` — 叠层时
- `OnRemove(mod, npc)` — 移除时

#### Scenario: 最小有效 Modifier 脚本
- **WHEN** Lua 脚本仅定义 OnApply，不定义其他 hook
- **THEN** OnApply 正常调用，其他 hook 静默跳过

#### Scenario: 空 table 脚本
- **WHEN** Lua 脚本 return 空 table（无任何 hook）
- **THEN** env 正常赋值，所有 hook 调用静默跳过，Modifier 仅靠 StatModifiers 生效
