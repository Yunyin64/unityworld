## 1. 新建 CombatNpcStatBuffFunc.cs

- [x] 1.1 在 `Scripts/Game/Domain/Combat/CombatNpc/` 下新建 `CombatNpcStatBuffFunc.cs`，声明 `partial class CombatNpc`
- [x] 1.2 实现 `AddStatBuff(string statId, ModifierType type, float value, string sourceId = null)` 方法：sourceId 缺省时自动生成，调用 `Stats.AddModifier(statId, new StatModifier(type, value, sourceId))`
- [x] 1.3 实现 `RemoveStatBuff(string sourceId)` 方法：调用 `Stats.RemoveModifiersBySource(sourceId)`

## 2. Lua 友好重载

- [x] 2.1 新增字符串参数重载 `AddStatBuff(string statId, string modifierType, float value, string sourceId = null)`，内部 `Enum.Parse<ModifierType>(modifierType)` 后委托给主方法
