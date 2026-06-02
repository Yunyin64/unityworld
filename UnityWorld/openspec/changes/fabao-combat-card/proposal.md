## Why

法宝卡（FaBao）在战斗中缺乏完整的运行逻辑。当前 Equip 系统有 FormList（招式卡列表）但从未消费；法宝卡作为触发式被动卡没有统一的 UseFabao 入口；招式卡无法动态引用所属法宝的武器数值。需要将这三块串联起来形成闭环。

## What Changes

- FaBao.lua Keyword 脚本中实现 `CardBase.UseFabao(card, ctx)` 全局方法（检查灵元 → 成功则 Apply）
- Card 层新增 `ParentCardId` 字段（大世界即设置），CombatCard.CreateFromData 继承
- Card/CombatCard 新增 `GetEquipData(): ContextBase` 方法，招式卡通过它获取所属装备的全部数值
- NpcCardData 新增 `EquippedFaBao` 列表，记录已装备的法宝卡 Id（纯标记）
- NpcSystemCard 新增 `EquipFaBao` 方法：仅将法宝卡 Id 加入 EquippedFaBao
- NpcSystemCard 改造 `AssignAllToField`：分配卡组时识别已装备法宝 → 实例化 FormList 招式卡 → 设 ParentCardId → 入 Field
- CombatCard 新增 `TryPayMana(): bool`（无副作用纯扣费）

## Capabilities

### New Capabilities
- `fabao-trigger`: 法宝卡触发式行为 — CardBase.UseFabao 全局入口 + FaBao keyword 驱动
- `weapon-stat-bridge`: 招式卡动态武器数值桥接 — Card.ParentCardId + GetEquipData(): ContextBase
- `fabao-equip`: 法宝装备管理 — EquipFaBao 纯标记 + AssignAllToField 时实例化招式卡

### Modified Capabilities
<!-- 无既有 spec 需修改 -->

## Impact

- `Data/LuaScripts/Keywords/FaBao.lua` — Keyword 脚本重写
- `Scripts/Game/Domain/Object/Card/Card.cs` — +ParentCardId
- `Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs` — CreateFromData 继承 ParentCardId
- `Scripts/Game/Domain/Combat/CombatCard/CombatCardFunc.cs` — +TryPayMana
- `Scripts/Game/Domain/Combat/CombatCard/CombatCardData.cs` — +GetEquipData
- `Scripts/Game/Domain/Object/Npc/Data/NpcCardData.cs` — +EquippedFaBao
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCard.cs` — +EquipFaBao + 改造 AssignAllToField
