## ADDED Requirements

### Requirement: Keyword 索引文件加载
LuaMgr 初始化阶段 SHALL 加载 `Data/LuaScripts/Keywords/Keyword.lua` 索引文件。该文件 SHALL 返回一个 table，key 为 keyword 名称（字符串），value 为对应 Lua 脚本的相对路径名（相对于 Keywords 目录）。LuaMgr SHALL 遍历该 table，加载每个 keyword 脚本并缓存为 `Dictionary<string, LuaTable>` 注册表。

#### Scenario: 正常加载 Keyword.lua
- **WHEN** LuaMgr.Init() 执行且 `Keywords/Keyword.lua` 存在并返回 `{ Passive = "Passive" }`
- **THEN** 注册表中包含 key "Passive"，对应的 LuaTable 为 `Passive.lua` 返回的 table

#### Scenario: Keyword.lua 不存在
- **WHEN** LuaMgr.Init() 执行且 `Keywords/Keyword.lua` 文件不存在
- **THEN** 输出警告日志，注册表为空，不影响其他功能

#### Scenario: keyword 脚本文件不存在
- **WHEN** `Keyword.lua` 中注册了 key "Foo" 指向 "Foo"，但 `Keywords/Foo.lua` 不存在
- **THEN** 输出错误日志，该 keyword 不加入注册表，其余 keyword 正常加载

### Requirement: CombatCard 生命周期遍历 keyword hooks
CombatCard 在 `PreStart`、`Start`、`Tick`、`End` 方法中 SHALL 遍历自身的 `Keywords` 列表，对每个 keyword 查询 LuaMgr 注册表获取对应 LuaTable，并调用 table 中对应的 hook 函数（`OnPreStart`、`OnStart`、`OnTick`、`OnEnd`）。Keyword hooks SHALL 在卡自身 Lua env hooks 之前调用。

#### Scenario: keyword 已注册且有对应 hook
- **WHEN** 一张卡的 Keywords 包含 "Passive"，注册表中有 "Passive"，且 Passive LuaTable 中定义了 `OnPreStart` 函数
- **THEN** CombatCard.PreStart() 中调用 Passive 的 `OnPreStart(card, ctx)`

#### Scenario: keyword 已注册但无对应 hook
- **WHEN** 一张卡的 Keywords 包含 "Passive"，注册表中有 "Passive"，但 Passive LuaTable 中未定义 `OnTick` 函数
- **THEN** CombatCard.Tick() 中静默跳过该 keyword 的 OnTick 调用，不报错

#### Scenario: keyword 未注册
- **WHEN** 一张卡的 Keywords 包含 "UnknownKW"，但注册表中无 "UnknownKW"
- **THEN** 输出错误日志 "Keyword 'UnknownKW' 未注册"

### Requirement: SetPhase API 暴露给 Lua
CombatCard SHALL 提供 `SetPhase(string phaseName)` 方法。该方法 SHALL 将字符串解析为 `CombatCardPhase` 枚举值并设置 `Phase` 字段。解析失败时 SHALL 输出错误日志且不改变当前 Phase。

#### Scenario: 有效的 phase 名称
- **WHEN** Lua 调用 `card:SetPhase("Passive")`
- **THEN** CombatCard.Phase 设为 CombatCardPhase.Passive

#### Scenario: 无效的 phase 名称
- **WHEN** Lua 调用 `card:SetPhase("InvalidPhase")`
- **THEN** 输出错误日志，Phase 保持原值不变
