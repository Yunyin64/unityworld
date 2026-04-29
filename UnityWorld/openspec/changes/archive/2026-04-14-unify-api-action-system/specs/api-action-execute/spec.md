## ADDED Requirements

### Requirement: APIFuncAttribute 函数标记
系统 SHALL 提供 `[APIFunc(funcName, desc)]` Attribute，用于标记静态方法为可执行的 API 函数。funcName 为函数名（全局唯一），desc 为可选描述。

#### Scenario: 标记一个战斗效果函数
- **WHEN** 一个静态方法被标记为 `[APIFunc("Heal", "恢复战斗中HP")]`
- **THEN** APIMgr 初始化时 SHALL 自动扫描并注册该方法为 FuncName="Heal" 的执行函数

#### Scenario: 标记一个大世界效果函数
- **WHEN** 一个静态方法被标记为 `[APIFunc("GiveTrait", "给NPC添加特质")]`
- **THEN** APIMgr 初始化时 SHALL 自动扫描并注册该方法为 FuncName="GiveTrait" 的执行函数

### Requirement: APIMgr 反射扫描注册
APIMgr.Init() SHALL 在 RegisterBuiltinAPIs() 之后执行 ScanHandlers()，扫描当前 Assembly 中所有带 `[APIFunc]` 的静态方法，方法签名 SHALL 为 `static void Xxx(ActionContext ctx)`，并按 FuncName 注册到内部 `_handlers` 字典。

#### Scenario: 启动时自动注册所有标记方法
- **WHEN** APIMgr.Init() 执行
- **THEN** 系统 SHALL 扫描当前 Assembly 中所有带 `[APIFunc]` Attribute 的静态方法，并将它们注册到 `_handlers` 字典中

#### Scenario: 方法签名不匹配时警告跳过
- **WHEN** 一个带 `[APIFunc]` 的方法签名不是 `static void Xxx(ActionContext ctx)`
- **THEN** 系统 SHALL 打印 Warning 日志并跳过该方法，不抛出异常

#### Scenario: 重复 FuncName 注册时警告
- **WHEN** 两个方法使用相同的 FuncName 标记 `[APIFunc]`
- **THEN** 系统 SHALL 打印 Warning 日志，后注册的方法覆盖先前的

### Requirement: ActionContext 统一执行上下文
系统 SHALL 提供 `ActionContext` 类，同时持有 ActionData（Action 参数）和 ContextBase Env（环境信息），作为所有 `[APIFunc]` 标记方法的唯一参数。

#### Scenario: 战斗中构造 ActionContext
- **WHEN** 战斗中需要执行一个效果卡 Action
- **THEN** 调用方 SHALL 构造 ActionContext，将 CombatNpc 作为主体设入 Env["Caster"]，将目标设入 Env["Target"]

#### Scenario: 大世界中构造 ActionContext
- **WHEN** Story 系统需要执行一个效果函数
- **THEN** 调用方 SHALL 构造 ActionContext，将触发主体设入 Env["Subject"]，将 Rng 设入 Env["Rng"]

#### Scenario: 从 ActionContext 读取 Action 参数
- **WHEN** Handler 方法需要读取 Action 的参数（如 HealValue）
- **THEN** 方法 SHALL 通过 `ctx.Action.GetInt("HealValue")` 从 ActionData 获取

#### Scenario: 从 ActionContext 读取环境信息
- **WHEN** Handler 方法需要读取主体对象
- **THEN** 方法 SHALL 通过 `ctx.Get<CombatNpc>("Caster")` 或 `ctx.Get<Npc>("Subject")` 从 Env 获取

### Requirement: APIMgr.Execute 执行能力
APIMgr SHALL 提供 `Execute(string funcName, ActionContext ctx)` 方法，按 FuncName 查找 `_handlers` 字典中注册的执行委托并调用。

#### Scenario: 成功执行已注册函数
- **WHEN** 调用 `APIMgr.Execute("Heal", ctx)`，且 "Heal" 已有注册的 Handler
- **THEN** 系统 SHALL 调用对应的 Handler 方法，传入 ctx

#### Scenario: 执行未注册的函数名
- **WHEN** 调用 `APIMgr.Execute("UnknownFunc", ctx)`
- **THEN** 系统 SHALL 打印 Warning 日志并跳过，不抛出异常

#### Scenario: Handler 执行过程中异常
- **WHEN** Handler 方法内部抛出异常
- **THEN** 系统 SHALL 捕获异常，打印 Warning 日志（含函数名和异常信息），不向上传播

### Requirement: 战斗效果卡接入 APIMgr.Execute
CombatCardFlowHandler.ResolveEffectCard SHALL 遍历效果卡的所有 Effect 中 Trigger 为 OnUse 的 Action，构造 ActionContext 并调用 APIMgr.Execute 执行。拼点类 Action（Attack/Shield/Block）SHALL 跳过，不通过 Execute 执行。

#### Scenario: 效果卡 Heal Action 生效
- **WHEN** 一张效果卡的 EffectData 包含 ActionData{FuncName="Heal", Context={HealValue=5}}，且 CD 就绪
- **THEN** CombatCardFlowHandler SHALL 构造 ActionContext（Caster=当前NPC，Target=目标NPC），调用 APIMgr.Execute("Heal", ctx)，最终 CombatNpc.ApplyHeal(5) 被执行

#### Scenario: 效果卡中的拼点 Action 被跳过
- **WHEN** 一张效果卡的 EffectData 包含 ActionData{FuncName="Attack"}
- **THEN** ResolveEffectCard SHALL 跳过该 Action，不调用 APIMgr.Execute

### Requirement: CombatBaseFunc 战斗域 Handler 集合
系统 SHALL 在 `CombatBaseFunc` 静态类中提供战斗域的 Action Handler 实现，每个方法以 `[APIFunc]` 标记。初始内置 Handler 包括 Heal（恢复HP）、SelfDamage（自伤）。

#### Scenario: CombatBaseFunc.Heal 执行
- **WHEN** APIMgr.Execute("Heal", ctx) 被调用，ctx.Get\<CombatNpc\>("Caster") 为有效 NPC
- **THEN** 该 NPC 的 Hp SHALL 增加 ctx.Action.GetInt("HealValue") 的值

#### Scenario: CombatBaseFunc.SelfDamage 执行
- **WHEN** APIMgr.Execute("SelfDamage", ctx) 被调用
- **THEN** ctx.Get\<CombatNpc\>("Caster") 的 Hp SHALL 减少 ctx.Action.GetInt("DamageValue") 的值
