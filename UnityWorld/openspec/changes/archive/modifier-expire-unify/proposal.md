## Why

当前三种运行时 Modifier（CombatNpcModifier、NpcModifier、CardModifier）的过期/移除判定各自为政：TileModifier 的 IsExpired 字段遮蔽了扩展方法导致永远不过期；CardModifier 的 isExpired 逻辑反了；NpcModifier 完全没有过期驱动；基线扩展方法不考虑层数。需要统一过期策略、引入触发器驱动的移除时机，并修复所有已知 bug。

## What Changes

- **BREAKING** 在 `IModifierBase` 接口新增 `ExpirePolicy` 枚举字段和 `RemoveTriggerId` 字符串字段
- 新增 `ExpirePolicy` 枚举（Never / TimeBased / StackBased / TimeOrStack / TriggerBased）
- 重写 `IModifierBaseExt.IsExpired()` 扩展方法，基于 `ExpirePolicy` 做统一判定
- 新增 `ReduceStack(int)` / `AddStack(int)` 扩展方法供引擎和 Lua 调用
- 删除 `CardModifier.isExpired` 自定义属性和 `StackReduceType` 枚举
- 删除 `TileModifier.IsExpired` 字段（不再遮蔽扩展方法）（TileModifier 本次不改造，仅修此 bug）
- 各 Modifier 子类（CombatNpcModifier、NpcModifier、CardModifier）实现新接口字段
- 各 Define（CombatNpcModifierDefine、NpcModifierDefine）新增 `ExpirePolicy` 和 `RemoveTriggerId` 配置
- `CombatNpcModifierFunc.ModifierTick()` 改用统一 `IsExpired()` 判定
- `CombatNpcModifierFunc.GetAllModifiers()` 语义修正（当前只返回过期的，应返回未过期的）
- `RemoveTriggerId` 引用已有的 `TriggerDefine.ID`，复用触发器数据体系

## Capabilities

### New Capabilities
- `modifier-expire-policy`: 统一的 Modifier 过期策略枚举与判定逻辑，包括 ExpirePolicy、RemoveTriggerId、IsExpired 统一判定、ReduceStack/AddStack 操作方法

### Modified Capabilities
（无已有 spec 需要修改）

## Impact

- `Scripts/Game/Domain/Object/Modifier/ModifierBase.cs` — 接口扩展 + 扩展方法重写
- `Scripts/Game/Domain/Object/Modifier/CardModifier.cs` — 删除自定义 isExpired 和 StackReduceType 枚举
- `Scripts/Game/Domain/Object/Modifier/CombatNpcModifier.cs` — 新增字段
- `Scripts/Game/Domain/Object/Modifier/NpcModifier.cs` — 新增字段
- `Scripts/Game/Domain/Object/Modifier/TileModifier.cs` — 仅删除 IsExpired 字段
- `Scripts/Game/Data/Defines/Modifier/CombatModifierDefine.cs` — 新增配置字段
- `Scripts/Game/Data/Defines/Modifier/NpcModifierDefine.cs` — 新增配置字段
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs` — ModifierTick 和 GetAllModifiers 修复
- `Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs` — Apply 中广播 RemoveTrigger
