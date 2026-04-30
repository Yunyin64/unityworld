## ADDED Requirements

### Requirement: LuaMgr 启动时加载 Init.lua
LuaMgr.Init() 在创建 Lua State 后 SHALL 执行 `Data/LuaScripts/Init.lua`，使其中定义的全局函数（CardBase、Attack、Shield、Block、Heal 等）在后续所有脚本中可见。

#### Scenario: Init.lua 加载成功
- **WHEN** LuaMgr.Init() 被调用
- **THEN** Lua 全局空间中存在 CardBase table 和 Attack/Shield/Block/Heal 函数

#### Scenario: Init.lua 文件缺失
- **WHEN** `Data/LuaScripts/Init.lua` 不存在
- **THEN** LuaMgr 记录错误日志，不阻塞初始化

---

### Requirement: 卡牌脚本加载返回独立 card table
LuaMgr.LoadCardScript(cardId) SHALL 执行 `Data/LuaCards/{cardId}.lua`，捕获脚本的 return 值作为 card table 返回。每次调用 MUST 返回独立的 table 实例，不做缓存。

#### Scenario: 同一 DefineId 多次加载
- **WHEN** 对同一 cardId 调用 LoadCardScript 两次
- **THEN** 返回两个独立的 LuaTable，修改其中一个不影响另一个

#### Scenario: 脚本使用 CardBase 元表
- **WHEN** 脚本中 `setmetatable({}, { __index = CardBase })` 
- **THEN** 正常执行不报错，CardBase 来自 Init.lua 的全局定义

---

### Requirement: CombatCard 实例化时绑定 env
CombatCard.InitializeLuaCards() SHALL 调用 LuaMgr.LoadCardScript(DefineId)，将返回的 card table 赋值给 `this.env`。

#### Scenario: 有 Lua 脚本的卡牌
- **WHEN** `Data/LuaCards/{DefineId}.lua` 存在
- **THEN** `this.env` 为该脚本 return 的 card table

#### Scenario: 无 Lua 脚本的卡牌
- **WHEN** 脚本文件不存在
- **THEN** `this.env` 保持 null，不影响卡牌其他逻辑

---

### Requirement: OnContest 调用 Lua
CombatCard.OnContest() SHALL 构造 APIContext（SourceCard=this, Caster=Owner, Scene=null），从 env 取 OnContest 函数并以 `card:OnContest(ctx)` 方式调用。

#### Scenario: env 有 OnContest 函数
- **WHEN** env["OnContest"] 是 LuaFunction
- **THEN** 以 env 为 self、APIContext 为参数调用该函数

#### Scenario: env 为 null 或无 OnContest
- **WHEN** env 为 null 或 env["OnContest"] 不存在
- **THEN** 跳过 Lua 调用，不报错

---

### Requirement: OnApply 调用 Lua
CombatCard.OnApply() SHALL 构造 APIContext 并调用 env 中的 OnApply 函数，然后将 Phase 设为 Finished。

#### Scenario: Lua OnApply 正常执行
- **WHEN** env["OnApply"] 存在
- **THEN** 调用 Lua 函数后 Phase 变为 Finished

---

### Requirement: Init.lua 包装函数调用 APIMgr
Init.lua 中的全局函数（Attack、Shield 等）SHALL 接收 ctx（C# APIContext 对象）和业务参数，通过 `ctx:Set(key, value)` 填充参数后调用 `APIMgr.Instance:Execute(funcName, ctx)`。

#### Scenario: Lua 调用 Attack
- **WHEN** Lua 侧执行 `Attack(ctx, "Wu", "Da", 2)`
- **THEN** ctx 被设置 Element="Wu", PhysicalType="Da", AttackValue=2，然后 APIMgr.Execute("Attack", ctx) 被调用

---

### Requirement: APIMgr 签名校验支持 APIContext 返回值
APIMgr.ScanHandlers() SHALL 接受返回 APIContext 的方法（不仅限 void），参数类型 SHALL 接受 APIContext 或其基类 ContextBase。

#### Scenario: Attack 方法被注册
- **WHEN** CombatBaseFunc.Attack 签名为 `static APIContext Attack(APIContext ctx)`
- **THEN** 该方法被成功注册到 _handlers 字典中