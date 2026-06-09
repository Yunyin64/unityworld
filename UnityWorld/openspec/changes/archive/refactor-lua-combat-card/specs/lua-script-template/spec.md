## ADDED Requirements

### Requirement: Lua 卡牌脚本标准模板
每张 Lua 卡牌脚本 SHALL 遵循固定模板结构：继承元表区、数据表区、OnXxx 函数区。脚本 SHALL 返回一个继承自 CardBase 的 LuaTable。

#### Scenario: 标准模板结构
- **WHEN** 编写一张新的 Lua 卡牌脚本
- **THEN** 脚本包含以下固定结构：
  - `local card = setmetatable({}, { __index = CardBase })` — 继承元表
  - `card.CardData = { ... }` — 数据表
  - `card.Keywords = { ... }` — 关键字表
  - `function card:OnXxx(ctx) ... end` — 各种 OnXxx 函数
  - `return card` — 返回实例

### Requirement: CardBase 全局基表
LuaMgr 初始化时 SHALL 在 Lua 全局空间创建 CardBase 表，并设置 `setmetatable(CardBase, { __index = _G })` 使其继承全局 API。所有卡牌脚本通过 `setmetatable({}, { __index = CardBase })` 继承此基表。

#### Scenario: CardBase 继承全局 API
- **WHEN** Lua 卡牌脚本中调用 `CombatBaseFunc.Attack()`
- **THEN** 通过 CardBase → _G 的 metatable 链找到全局注册的 C# 函数

#### Scenario: CardBase 提供默认 OnXxx
- **WHEN** 卡牌脚本未定义某个 OnXxx 函数（如 OnTick）
- **THEN** 可在 CardBase 中提供默认空实现，实例通过 metatable 继承

### Requirement: CardData 字段规范
CardData 表 SHALL 支持以下字段，均为可选（缺失时使用 Define 保底值）：
- `Size` (number) — 卡槽占用
- `Cooldown` (number) — 冷却时间（秒）
- `CardType` (string) — 卡牌类型枚举名
- `ManaCost` (table) — 灵力消耗

#### Scenario: CardData 完整定义
- **WHEN** Lua 脚本定义 `card.CardData = { Size=2, Cooldown=8, CardType="FaShu", ManaCost={} }`
- **THEN** 所有字段回写覆盖 CombatCard 的 Define 默认值

#### Scenario: CardData 部分定义
- **WHEN** Lua 脚本定义 `card.CardData = { Size=2 }`
- **THEN** Size 使用 Lua 值 2，其余字段使用 Define 保底值

### Requirement: OnXxx 函数签名规范
所有 OnXxx 函数 SHALL 使用冒号语法定义 `function card:OnXxx(ctx)`，其中 self 为卡牌的 LuaTable 实例，ctx 为 C# 传入的上下文对象（ActionContext/ContextBase）。Lua 侧通过 `self.CS_Card` 访问 C# CombatCard 对象。

#### Scenario: OnUse 标准签名
- **WHEN** 定义 `function card:OnUse(ctx)` 并被 C# 调用
- **THEN** self 为该卡的 LuaTable 实例，ctx 为 C# ActionContext 对象

#### Scenario: Lua 中访问 C# 卡牌
- **WHEN** Lua 脚本执行 `self.CS_Card:GetDefineId()`
- **THEN** 返回该卡牌的 DefineId 字符串
