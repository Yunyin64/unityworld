## ADDED Requirements

### Requirement: OnModifierStat{XX} 统一属性注入 hook
任何战斗对象（CombatNpc 或 CombatCard）读取属性时，SHALL 遍历场上所有存活 Npc 的所有 Modifier，对每个 Modifier 检查其 `LuaHooks` 中是否存在 `OnModifierStat{属性名}` 函数。存在则调用该函数，签名为 `OnModifierStat{XX}(env, caller)` → 返回 float 修正值。0 表示不生效。所有命中 Modifier 的返回值累加为光环贡献。

#### Scenario: Modifier 有对应 hook 时生效
- **WHEN** Modifier 的 LuaHooks 包含 `OnModifierStatAtk`，且该函数对 caller 返回 1
- **THEN** caller 的 GetStat("Atk") 比裸值多 1

#### Scenario: Modifier 无对应 hook 时跳过
- **WHEN** Modifier 的 LuaHooks 不包含 `OnModifierStatAtk`
- **THEN** 该 Modifier 对 caller 的 Atk 无贡献，不产生 Lua 调用

#### Scenario: 多个 Modifier 同时生效时累加
- **WHEN** 两个 Modifier 各有 `OnModifierStatDef`，分别返回 1 和 2
- **THEN** caller 的 GetStat("Def") 比裸值多 3

#### Scenario: 跨类型影响——Npc 的 Modifier 影响 Card 属性
- **WHEN** Npc 的 Modifier 定义了 `OnModifierStatCD`，caller 是一张 CombatCard
- **THEN** 该 Card 的 GetStat("CD") 包含此 Modifier 的贡献

### Requirement: GameEntityBase.GetStat 虚方法
`GameEntityBase` SHALL 新增 `virtual float GetStat(string statId)` 方法，默认实现为 `return Stats.Get(statId)`。战斗子类（CombatNpc、CombatCard）override 此方法，在裸值基础上叠加全场 Modifier 的 `OnModifierStat{statId}` hook 贡献。

#### Scenario: 非战斗实体 GetStat 等于 Stats.Get
- **WHEN** 一个非战斗的 GameEntityBase 子类调用 `GetStat("Atk")`
- **THEN** 返回值等于 `Stats.Get("Atk")`，无 Modifier 遍历

#### Scenario: CombatNpc.GetStat 包含 Modifier 贡献
- **WHEN** 场上有 Modifier 的 `OnModifierStatAtk` 对本 CombatNpc 返回 2
- **THEN** `npc.GetStat("Atk")` == `npc.Stats.Get("Atk")` + 2

#### Scenario: CombatCard.GetStat 包含 Modifier 贡献
- **WHEN** 场上有 Modifier 的 `OnModifierStatCD` 对本 CombatCard 返回 -1
- **THEN** `card.GetStat("CD")` 比基础 CD 少 1

#### Scenario: 无 hook 时 GetStat 等于 Stats.Get
- **WHEN** 场上没有任何 Modifier 定义 `OnModifierStatAtk`
- **THEN** `npc.GetStat("Atk")` == `npc.Stats.Get("Atk")`

### Requirement: ICombatEntity 新增 CombatScene 引用
`ICombatEntity` SHALL 新增 `CombatScene Scene { get; set; }` 属性。`CombatScene.Init` 中为所有战斗实体赋值。战斗实体通过此引用访问 `CollectModifierStat`。

#### Scenario: CombatNpc 可通过 ICombatEntity.Scene 访问 CombatScene
- **WHEN** CombatScene.Init 完成后
- **THEN** 每个 CombatNpc 的 Scene 不为 null

#### Scenario: CombatCard 可通过 Owner.Scene 访问 CombatScene
- **WHEN** CombatScene.Init 完成后，CombatCard 的 Owner 已设置
- **THEN** `card.Owner.Scene` 不为 null（Card 通过 Owner 间接访问）

### Requirement: hook 内读属性使用裸值避免递归
`OnModifierStat{XX}` Lua 函数内读取属性 MUST 使用 `Stats:Get()`（裸值），不得使用 `GetStat()`（含 hook 贡献），以避免无限递归。

#### Scenario: 攻击最高的人+1攻击不产生递归
- **WHEN** `OnModifierStatAtk` 内部通过 `npc.Stats:Get("Atk")` 比较各队友攻击值
- **THEN** 不触发 `GetStat`，不产生递归

### Requirement: CombatScene 提供遍历全场 Modifier 的能力
`CombatScene` SHALL 提供 `CollectModifierStat(object caller, string statId)` 方法，遍历所有存活 Npc 的所有 Modifier，对 `LuaHooks` 中包含 `OnModifierStat{statId}` 的 Modifier 调用该 hook，累加返回值。

#### Scenario: 遍历全场累加贡献
- **WHEN** 场上 3 个 Npc 各有 1 个 Modifier，其中 2 个定义了 `OnModifierStatAtk`，分别返回 1 和 3
- **THEN** `CollectModifierStat(targetNpc, "Atk")` 返回 4
