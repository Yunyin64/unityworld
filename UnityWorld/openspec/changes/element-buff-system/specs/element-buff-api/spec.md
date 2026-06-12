## ADDED Requirements

### Requirement: AddElementBuff API
系统 SHALL 提供 `AddElementBuff` API 函数，注册在 `CombatManaAction.cs`，签名为 `(Domain:String, Element:String, IsDebuff:Bool, Count:Int)`。

循环 Count 次，每次：
- Element == "None" 时：调用 `RandomBaseElementBuff(isDebuff)` 获取随机 ID
- Element 为具体元素名时：从 `ElementType.BaseElementBuff` 字典取对应 buff/debuff ID
- 对 Domain 目标执行 `AddModifier(id, 1)`

#### Scenario: 随机添加正面 Buff
- **WHEN** 调用 `AddElementBuff(ctx)` 且 Element="None", IsDebuff=false, Count=3
- **THEN** 循环 3 次，每次从 5 个正面 buff 中随机选一个叠 1 层

#### Scenario: 指定元素添加负面 Buff
- **WHEN** 调用 `AddElementBuff(ctx)` 且 Element="Huo", IsDebuff=true, Count=2
- **THEN** 对目标执行 `AddModifier("Element_Debuff_Huo", 1)` 两次（叠 2 层）

### Requirement: RemoveElementBuff API
系统 SHALL 提供 `RemoveElementBuff` API 函数，签名为 `(Domain:String, Element:String, IsDebuff:Bool, Count:Int)`。

循环 Count 次，每次：
- 收集目标身上所有匹配的五行 Modifier（正面或负面，由 IsDebuff 决定）
- Element == "None" 时：从已有匹配 Modifier 中随机选一个
- Element 为具体元素名时：选指定元素的 Modifier
- 对选中的 Modifier 执行 `ReduceStack(1)`
- 若目标身上无匹配 Modifier，提前终止循环

#### Scenario: 随机清除负面 Buff
- **WHEN** 调用 `RemoveElementBuff(ctx)` 且 Element="None", IsDebuff=true, Count=5，目标有出血(3层)+石化(2层)
- **THEN** 循环 5 次，每次从 [出血, 石化] 中随机选一个减 1 层，层数归零则由 IsExpired 机制移除

#### Scenario: 目标无匹配 Buff 时提前终止
- **WHEN** 调用 `RemoveElementBuff(ctx)` 且 Count=10，但目标无任何五行负面 Buff
- **THEN** 循环立即终止，不报错
