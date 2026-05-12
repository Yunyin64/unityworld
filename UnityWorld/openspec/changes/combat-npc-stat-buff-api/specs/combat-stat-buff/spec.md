## ADDED Requirements

### Requirement: CombatNpc 可通过 AddStatBuff 直接施加永久属性修正
CombatNpc SHALL 提供 `AddStatBuff(string statId, ModifierType type, float value, string sourceId = null)` 方法，将一个 StatModifier 写入战斗 StatBlock。该修正永久存活直到战斗结束或被显式移除。

#### Scenario: 施加 Flat 修正
- **WHEN** 调用 `combatNpc.AddStatBuff("HpMax", ModifierType.Flat, 10, "card_heal")`
- **THEN** `combatNpc.Stats.Get("HpMax")` 的返回值增加 10

#### Scenario: 施加 Percent 修正
- **WHEN** 调用 `combatNpc.AddStatBuff("Atk", ModifierType.Percent, 0.3f, "buff_rage")`
- **THEN** `combatNpc.Stats.Get("Atk")` 的返回值在基础值上乘以 1.3

#### Scenario: sourceId 缺省时自动生成
- **WHEN** 调用 `combatNpc.AddStatBuff("Def", ModifierType.Flat, 5)` 不传 sourceId
- **THEN** 修正成功写入，使用自动生成的唯一 sourceId

### Requirement: CombatNpc 可通过 RemoveStatBuff 按来源移除修正
CombatNpc SHALL 提供 `RemoveStatBuff(string sourceId)` 方法，移除所有来源为指定 sourceId 的属性修正。

#### Scenario: 精准移除已施加的修正
- **WHEN** 先调用 `AddStatBuff("HpMax", Flat, 10, "card_heal")`，再调用 `RemoveStatBuff("card_heal")`
- **THEN** `combatNpc.Stats.Get("HpMax")` 恢复到施加前的值

#### Scenario: 移除不存在的 sourceId 时静默通过
- **WHEN** 调用 `RemoveStatBuff("not_exist")`
- **THEN** 不抛异常，无副作用

### Requirement: Lua 脚本可调用 AddStatBuff 和 RemoveStatBuff
CombatNpc SHALL 对 Lua 暴露 AddStatBuff 方法，支持字符串形式的 ModifierType 参数（"Flat"/"Percent"/"Override"/"ClampMax"/"ClampMin"）。

#### Scenario: Lua 中施加属性修正
- **WHEN** Lua 脚本执行 `npc:AddStatBuff("HpMax", "Flat", 1)`
- **THEN** 等效于 C# 调用 `npc.AddStatBuff("HpMax", ModifierType.Flat, 1f)`

#### Scenario: Lua 中带 sourceId 施加并移除
- **WHEN** Lua 脚本执行 `npc:AddStatBuff("Def", "Flat", 5, "my_buff")`，随后执行 `npc:RemoveStatBuff("my_buff")`
- **THEN** Def 修正被完整撤销
