## ADDED Requirements

### Requirement: LuaMgr 单例生命周期
LuaMgr SHALL 作为 IDomainMgrBase 在 WorldMgr._mgrs 中注册，在 Init() 时创建 NLua.Lua State，在 End() 时 Dispose State 并置 Instance=null。

#### Scenario: 正常初始化
- **WHEN** WorldMgr.Initialize() 执行
- **THEN** LuaMgr.Instance 不为 null，Lua State 已创建

#### Scenario: 正常销毁
- **WHEN** WorldMgr 调用 End()
- **THEN** LuaMgr.Instance 为 null，Lua State 已 Dispose

### Requirement: C# API 注册到 Lua 全局空间
LuaMgr.Init() SHALL 将所有标记了 `[APIFunc]` 的 CombatBaseFunc 静态方法注册为 Lua 全局函数，函数名与 FuncName 一致（如 Attack、Charge、Heal）。

#### Scenario: Lua 调用 Attack
- **WHEN** Lua 脚本执行 `Attack(ctx, "None", "Zhan", 3)`
- **THEN** 实际调用 CombatBaseFunc.Attack 方法，参数正确传递

#### Scenario: 所有 APIFunc 可用
- **WHEN** LuaMgr 初始化完成后
- **THEN** Lua 全局空间中存在与 CombatBaseFunc 中所有 `[APIFunc]` 方法同名的函数

### Requirement: Lua 卡牌脚本加载
LuaMgr SHALL 提供 `LoadCardScript(string cardId)` 方法，从 `Data/LuaCards/{cardId}.lua` 加载脚本到独立环境，返回该环境的引用。

#### Scenario: 加载已存在的脚本
- **WHEN** 调用 LoadCardScript("card_jin_whirlwind") 且文件存在
- **THEN** 返回非 null 环境引用，脚本中定义的函数可被调用

#### Scenario: 加载不存在的脚本
- **WHEN** 调用 LoadCardScript("card_nonexist") 且文件不存在
- **THEN** 返回 null，LogMgr.Warn 输出警告

### Requirement: Hook 函数自动发现
LuaMgr 加载卡牌脚本后 SHALL 扫描环境中所有 `OnXxx` 命名的函数，返回发现的函数名列表。

#### Scenario: 发现 OnUse 和 OnAttack
- **WHEN** 加载的 .lua 中定义了 `function CombatCard:OnUse(ctx)` 和 `function CombatCard:OnAttack(ctx)`
- **THEN** 返回的函数名列表包含 ["OnUse", "OnAttack"]

#### Scenario: 无 Hook 函数
- **WHEN** 加载的 .lua 中没有任何 `OnXxx` 函数
- **THEN** 返回空列表

### Requirement: EventMgr 自动注册
对于发现的每个非 OnUse 的 Hook 函数，框架 SHALL 自动创建 LuaEventListener 并注册到 EventMgr，使用内置函数名→EventId 映射表。

#### Scenario: OnAttack 自动注册
- **WHEN** 卡牌 .lua 包含 OnAttack 函数
- **THEN** 一个 LuaEventListener 被注册到 EventMgr，eventId="trigger_on_attack"，scope 为该 CombatNpc

#### Scenario: OnUse 不注册
- **WHEN** 卡牌 .lua 只包含 OnUse 函数
- **THEN** 不向 EventMgr 注册任何监听

### Requirement: LuaEventListener 实现
LuaEventListener.OnEvent() SHALL 调用对应的 Lua 函数，传入构造好的 context table。执行异常时 LogMgr.Err 并跳过。

#### Scenario: 事件正常触发
- **WHEN** EventMgr 触发 trigger_on_attack 且对应 LuaEventListener 被调用
- **THEN** Lua 中的 OnAttack 函数被执行

#### Scenario: Lua 执行异常
- **WHEN** Lua 函数执行抛出异常
- **THEN** LogMgr.Err 记录错误，战斗继续（不崩溃）