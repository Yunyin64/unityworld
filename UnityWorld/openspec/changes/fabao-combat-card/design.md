## Context

当前战斗系统中，Card 已有完整的 CD 循环（Waiting→InCD→Ready→Use→Finished）和 Keyword 驱动机制。法宝卡设定为 CD=0 的被动卡（自动获得 Passive keyword），但缺乏触发式行为入口和招式卡数值桥接。

现有关系：EquipDefine → Equip 运行时实例 → CardEquipData 桥接 → Card.IsEquipCard。Equip 上已有 FormList（招式卡 DefineId 列表）和 Attack/Defend/Speed 数值字段，但从未被消费。

关键区分：**装备法宝**（标记绑定关系）与**分配卡入 Field**（AssignAllToField）是两个独立动作。

## Goals / Non-Goals

**Goals:**
- 法宝卡通过 Lua hook 触发 → `CardBase.UseFabao(card, ctx)` → 检查灵元 → Apply
- 招式卡通过 ParentCardId + `GetEquipData(): ContextBase` 动态取武器数值
- 装备法宝 = 纯标记；AssignAllToField 时才实例化 FormList 招式卡并设 ParentCardId
- 全部逻辑 Mod 友好：触发条件由各法宝 Lua 自行决定

**Non-Goals:**
- Amount（弹药）机制本次不处理
- Consume（消耗）机制本次不处理
- 法宝耐久与战后回写本次不处理
- NpcEquipData 拆分本次不做

## Decisions

### 1. UseFabao 写在 Lua CardBase 全局方法

**选择**: `CardBase.UseFabao(card, ctx)` 写在 FaBao.lua 中  
**理由**: 全 Lua 逻辑 Mod 可覆写；C# 侧只提供 `TryPayMana(): bool` 原子操作  
**替代方案**: C# 写 UseFabao → 不够灵活，Modder 无法改触发后行为

### 2. ParentCardId 放在 Card 层（非 CombatCard）

**选择**: Card.cs 新增 `int ParentCardId = -1`，大世界 AssignAllToField 时设置，CreateFromData 继承  
**理由**: 大世界分配阶段即可确定归属关系，战斗初始化无需额外逻辑  
**替代方案**: 仅 CombatCard 持有 → InitDeck 需额外匹配逻辑，复杂

### 3. GetEquipData 返回 ContextBase

**选择**: `GetEquipData(): ContextBase`，填入 Attack/Defend/Speed/Amount/DisplayName  
**理由**: 通用 key-value 容器，Lua 友好（GetValue/GetObject），不硬编码字段  
**替代方案**: 三个 GetWeaponXxx 方法 → 每加字段都要改 C#，不灵活

### 4. EquippedFaBao 暂存 NpcCardData

**选择**: NpcCardData 加 `List<int> EquippedFaBao`  
**理由**: 快速跑通，NpcCardData 已管理卡组相关数据  
**替代方案**: 新建 NpcEquipData → 结构更优但本次过度设计

### 5. 装备与分配两步分离

**选择**: `EquipFaBao()` 仅标记 EquippedFaBao 列表；`AssignAllToField()` 时识别已装备法宝 → 实例化招式卡 → 设 ParentCardId → 入 Field  
**理由**: 装备是持久状态绑定，分配是卡组运转调度，职责分离  
**替代方案**: 装备时直接实例化招式卡入卡组 → 卸装时还要清理，状态耦合

## Risks / Trade-offs

- [多法宝共享招式卡] 若两把法宝 FormList 含同一 DefineId → 每个法宝各自实例化独立的招式卡，互不影响
- [AssignAllToField 重复调用] 需防重复创建招式卡 → 检查是否已有同 ParentCardId 的招式卡存在
- [Lua 跨桥性能] UseFabao 每次触发走 Lua → C# TryPayMana → Lua Apply → 触发频率有限，可接受
- [NpcCardData 膨胀] 后续可能还有其他装备类型 → 明确标注为临时方案，后续拆 NpcEquipData
