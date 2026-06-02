## 1. Card / CombatCard 基础设施

- [x] 1.1 Card.cs 新增 `int ParentCardId = -1` 字段
- [x] 1.2 CombatCard.CreateFromData 中复制 `card.ParentCardId` 到 CombatCard
- [x] 1.3 CombatCardFunc.cs 新增 `TryPayMana(): bool` 方法（GetCombatManaCost → Owner.TryCostMana，不改 Phase）
- [x] 1.4 CombatCardData.cs 新增 `GetEquipData(): ContextBase` 方法（ParentCardId → EquipMgr.GetById → 构造 ContextBase 填入 Attack/Defend/Speed/Amount/DisplayName）

## 2. NpcCardData 装备管理

- [x] 2.1 NpcCardData.cs 新增 `List<int> EquippedFaBao` 字段 + Clone 中复制
- [x] 2.2 NpcSystemCard.cs 新增 `EquipFaBao(NpcCardData data, int fabaoCardId)` 方法：加入 EquippedFaBao 列表
- [x] 2.3 NpcSystemCard.cs 改造 AssignAllToField：遍历 EquippedFaBao → 取 Equip.FormList → 实例化招式卡 → 设 ParentCardId → 加入 AllCards/AllCardIds/Field（需防重复创建）

## 3. FaBao Lua Keyword

- [x] 3.1 重写 Data/LuaScripts/Keywords/FaBao.lua：定义 `CardBase.UseFabao(card, ctx)` 全局方法（card:TryPayMana() → card:Apply()）
- [x] 3.2 FaBao keyword Apply hook 设置 Phase=Finished
