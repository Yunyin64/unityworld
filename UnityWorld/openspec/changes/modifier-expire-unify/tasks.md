## 1. 枚举与接口层

- [ ] 1.1 在 `ModifierBase.cs` 中新增 `ExpirePolicy` 枚举（Never / TimeBased / StackBased / TimeOrStack / TriggerBased）
- [ ] 1.2 在 `IModifierBase` 接口中新增 `ExpirePolicy ExpirePolicy { get; set; }` 属性
- [ ] 1.3 在 `IModifierBase` 接口中新增 `string RemoveTriggerId { get; set; }` 属性
- [ ] 1.4 重写 `IModifierBaseExt.IsExpired()` 扩展方法，基于 ExpirePolicy switch 做统一判定
- [ ] 1.5 在 `IModifierBaseExt` 中新增 `ReduceStack(this IModifierBase self, int count = 1)` 扩展方法
- [ ] 1.6 在 `IModifierBaseExt` 中新增 `AddStack(this IModifierBase self, int count = 1)` 扩展方法（含 MaxStack 限制和 RefreshOnStack 逻辑）

## 2. 子类实现新接口字段

- [ ] 2.1 `CombatNpcModifier` 添加 `ExpirePolicy` 和 `RemoveTriggerId` 属性实现
- [ ] 2.2 `NpcModifier` 添加 `ExpirePolicy` 和 `RemoveTriggerId` 属性实现
- [ ] 2.3 `CardModifier` 添加 `ExpirePolicy` 和 `RemoveTriggerId` 属性实现；删除自定义 `isExpired` 属性；删除 `StackReduceType` 枚举和字段
- [ ] 2.4 `TileModifier` 添加 `ExpirePolicy` 和 `RemoveTriggerId` 属性实现；删除 `public bool IsExpired = false` 字段

## 3. Define 层配置

- [ ] 3.1 `CombatNpcModifierDefine` 新增 `ExpirePolicy` 和 `RemoveTriggerId` JSON 字段（ExpirePolicy 默认 TimeBased，RemoveTriggerId 默认空）
- [ ] 3.2 `NpcModifierDefine` 新增 `ExpirePolicy` 和 `RemoveTriggerId` JSON 字段
- [ ] 3.3 `CombatNpcModifier.CreateModifier()` 工厂方法中将 Define 的 ExpirePolicy 和 RemoveTriggerId 赋值给实例
- [ ] 3.4 `NpcModifierDefine.CreateModifier()` 工厂方法中将 Define 的 ExpirePolicy 和 RemoveTriggerId 赋值给实例

## 4. Bug 修复与逻辑统一

- [ ] 4.1 `CombatNpcModifierFunc.ModifierTick()` 中将内联的 `mod.RemainingTime <= 0` 判定替换为 `mod.IsExpired()`；PerTick 减层逻辑删除（层数变化由外部驱动）
- [ ] 4.2 `CombatNpcModifierFunc.GetAllModifiers()` 修正过滤条件为 `!m.IsExpired()`（返回未过期的 Modifier）
- [ ] 4.3 `CombatNpcModifierFunc.StackModifier()` 中叠层逻辑改用 `AddStack()` 扩展方法

## 5. 触发器广播集成

- [ ] 5.1 在 `CombatNpcModifierFunc` 中新增 `BroadcastRemoveTrigger(string triggerId)` 方法，遍历 Modifiers 处理 RemoveTriggerId 匹配逻辑
- [ ] 5.2 在 `CombatCard.OnApply()` 中调用 Owner 的 `BroadcastRemoveTrigger("OnUse")` 广播卡牌使用事件
- [ ] 5.3 在 `CombatNpc.ApplyDamage()` 中调用 `BroadcastRemoveTrigger("OnHit")` 广播受击事件
