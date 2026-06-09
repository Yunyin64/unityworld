## ADDED Requirements

### Requirement: CombatCard 持有独立 LuaTable 实例
每张 CombatCard 实例 SHALL 持有自己的 LuaTable 实例，从 LuaMgr 的模板克隆而来。同 DefineId 的多张卡 SHALL 拥有各自独立的 LuaTable，Lua 侧的实例变量（如 self.ChargeCount）MUST NOT 互相污染。

#### Scenario: 重复卡独立状态
- **WHEN** 两张同 DefineId 的 Lua 卡牌存在于同一战斗
- **THEN** 每张卡拥有独立的 LuaTable 实例，一张卡的 Lua 实例变量修改不影响另一张

#### Scenario: 克隆继承模板函数
- **WHEN** 从模板克隆 LuaTable 实例
- **THEN** 实例通过 metatable 继承模板的函数定义，实例只存自己覆盖的值

### Requirement: LuaMgr 模板管理
LuaMgr SHALL 维护按 cardId 索引的模板字典 `_templates`，提供 `LoadCardScript()` 加载脚本为模板，提供 `CloneTemplate(cardId)` 克隆模板返回独立实例。LuaMgr MUST NOT 持有卡牌实例的 LuaTable 引用。

#### Scenario: 加载脚本为模板
- **WHEN** 调用 `LoadCardScript(cardId)` 且脚本文件存在
- **THEN** 脚本在独立环境中执行，返回的 LuaTable 存入 `_templates[cardId]`

#### Scenario: 克隆模板
- **WHEN** 调用 `CloneTemplate(cardId)` 且模板已加载
- **THEN** 返回一个新的 LuaTable，浅拷贝模板键值，metatable __index 指向模板

#### Scenario: 克隆不存在的模板
- **WHEN** 调用 `CloneTemplate(cardId)` 且模板未加载
- **THEN** 返回 null 并记录警告日志

### Requirement: CombatCard Lua 实例方法
CombatCard SHALL 提供 `OnUse(ctx)`、`OnTick(ctx)` 等实例方法，内部直接调用 `_luaTable` 中对应的 Lua 函数。调用时 SHALL 将 `_luaTable` 作为 self 参数传入。非 Lua 卡牌调用这些方法 SHALL 为空操作。

#### Scenario: Lua 卡牌调用 OnUse
- **WHEN** 对 IsLuaCard=true 的 CombatCard 调用 OnUse(ctx)
- **THEN** 执行 `_luaTable["OnUse"](_luaTable, ctx)`，self 为该卡的 LuaTable 实例

#### Scenario: 非 Lua 卡牌调用 OnUse
- **WHEN** 对 IsLuaCard=false 的 CombatCard 调用 OnUse(ctx)
- **THEN** 不执行任何 Lua 逻辑，方法直接返回

### Requirement: CardData 回写覆盖
CombatCard 构造后 SHALL 从 `_luaTable["CardData"]` 读取字段覆盖 C# 侧的 Define 默认值。Lua CardData 优先级高于 Define。CombatCard MUST 拥有自己的 BaseData 副本，回写 MUST NOT 影响大世界原始 Card 数据。

#### Scenario: Lua CardData 覆盖 Define 值
- **WHEN** Lua CardData 定义了 `Cooldown = 5`，Define 中 Cooldown = 3
- **THEN** CombatCard 的 CooldownTicks = 50（5 × 10）

#### Scenario: Lua CardData 缺失时使用 Define 保底
- **WHEN** Lua CardData 未定义 Cooldown
- **THEN** CombatCard 使用 Define 中的 Cooldown 值

#### Scenario: 回写不影响原始 Card
- **WHEN** CardData 回写修改了 BaseData.Size
- **THEN** 大世界原始 Card 的 BaseData.Size 保持不变

### Requirement: CS_Card 注入
CombatCard 构造时 SHALL 将自身引用注入到 `_luaTable["CS_Card"]`，Lua 侧通过 `self.CS_Card` 可直接访问 C# CombatCard 对象的属性和方法。

#### Scenario: Lua 访问 C# 卡牌属性
- **WHEN** Lua 脚本中执行 `self.CS_Card:GetDefineId()`
- **THEN** 返回该 CombatCard 的 DefineId 字符串

### Requirement: OnXxx 分类机制
OnXxx 函数 SHALL 分为两类：实例方法（OnUse/OnTick/OnDraw/OnDiscard 等）由 C# 在特定时机主动调用；Trigger 钩子（OnAfterCardUse/OnAttack/OnTakeDamage 等）通过 EventMgr 事件驱动。CombatCard 构造时 SHALL 扫描 _luaTable 发现 Trigger 钩子并注册到 EventMgr。

#### Scenario: 实例方法直接调用
- **WHEN** 战斗流程需要执行卡牌的 OnUse
- **THEN** C# 直接调用 `card.OnUse(ctx)`，不经过 EventMgr

#### Scenario: Trigger 钩子事件注册
- **WHEN** CombatCard 构造时发现 _luaTable 中有 OnAfterCardUse 函数
- **THEN** 自动注册 LuaEventListener 到 EventMgr 对应事件

### Requirement: 删除 LuaBridge
LuaBridge.cs SHALL 被删除。Lua 脚本 SHALL 直接调用 C# 对象（CombatBaseFunc 等），无需翻译层。LuaMgr 的 RegisterCSharpAPIs SHALL 改为注册 C# 类型到 Lua 全局空间，而非注册 LuaBridge 方法。

#### Scenario: Lua 直接调用 CombatBaseFunc
- **WHEN** Lua 脚本执行 `CombatBaseFunc.Attack(ctx, "Jin", "Zhan", 3)`
- **THEN** 直接调用 C# CombatBaseFunc.Attack 静态方法，无需中间翻译层

### Requirement: LuaMgr 简化
LuaMgr SHALL 删除 CreateContextTable()、CallCardHook() 方法，将 _cardEnvironments 改为 _templates。LuaMgr MUST NOT 负责构造 ctx 或中转 hook 调用。

#### Scenario: LuaMgr 不再中转调用
- **WHEN** 战斗流程需要调用卡牌 Lua 函数
- **THEN** 直接通过 CombatCard 的实例方法调用，不经过 LuaMgr
