## ADDED Requirements

### Requirement: Action 是 Modifier 的生产者
卡牌和 Modifier 的 TCA 体系 SHALL 通过以下 Action 来管理 Modifier 的创建与移除：
- `AddModifier(ModifierId, Target)` — 在目标身上实例化一个 Modifier
- `RemoveModifier(ModifierId, Target)` — 移除目标身上的指定 Modifier
- `ModifyStacks(ModifierId, Target, Delta)` — 增减目标身上某 Modifier 的层数

#### Scenario: 卡牌通过 Action 施加 Modifier
- **WHEN** 一张卡的 TCA 定义为 T:OnUse → A:AddModifier("armor_3", Self)
- **THEN** 卡牌使用时 SHALL 在自身 NPC 上实例化一个 Id 为 "armor_3" 的 Modifier

#### Scenario: 卡牌通过 Action 移除 Modifier
- **WHEN** 一张"驱散"卡的 TCA 定义为 T:OnUse → A:RemoveModifier("poison", Enemy)
- **THEN** 卡牌使用时 SHALL 移除敌方身上 Id 为 "poison" 的 Modifier

#### Scenario: Modifier 的 TCA 调用 Modifier Action
- **WHEN** 一个 Modifier 的 C 部分定义为 T:OnTick(3) → A:ModifyStacks(Self, -1)
- **THEN** 每 3 Tick 该 Modifier SHALL 自行扣减 1 层

---

### Requirement: 现有状态类机制原子统一为 Modifier
机制原子清单中的以下状态类效果 SHALL 使用 Modifier 体系重新表达：护甲、易伤、虚弱、眩晕、XX强化、中毒。原有的 Action（如 AddPoison、BuffAtk）SHALL 改为调用 AddModifier 的语义等价形式。

#### Scenario: 中毒效果改用 Modifier 表达
- **WHEN** 策划设计"给目标添加2层中毒"效果
- **THEN** SHALL 使用 AddModifier("poison_2", Enemy)，其中 "poison_2" 是一个 NpcModifier，A=2层，C=每Y Tick → SelfDamage(1) 并扣1层

#### Scenario: 护甲效果改用 Modifier 表达
- **WHEN** 策划设计"给自身+3护甲"效果
- **THEN** SHALL 使用 AddModifier("armor_3", Self)，其中 "armor_3" 是一个 NpcModifier，A=3层，B=受到伤害-1（每次触发消耗1层）

---

### Requirement: Modifier 与伤势系统的边界
Modifier SHALL 不替代伤势卡机制。伤势卡是塞入卡组、占 SP、参与 CD 循环的永久惩罚；Modifier 是独立于卡组之外、不占 SP、战斗内的临时状态。两者设计身份不同，和平共存。

#### Scenario: 中毒（Modifier）vs 毒伤（伤势卡）
- **WHEN** NPC 身上同时有"中毒"Modifier 和"毒伤"伤势卡
- **THEN** 中毒 Modifier 通过自身 TCA 造成伤害且不占 SP；毒伤伤势卡在卡组中占 SP 并参与 CD 循环造成 SelfDamage。两者独立结算，互不干扰

---

### Requirement: Modifier 在 Tick 循环中的结算时序
每个 Tick 中，Modifier 的结算 SHALL 发生在卡牌 CD 推进之前。具体顺序为：①Modifier 的持续时间倒计时 → ②Modifier 的 OnTick 类 TCA 触发 → ③移除到期的 Modifier → ④卡牌 CD 推进（此时数值修正 B 已生效）。

#### Scenario: 眩晕 Modifier 在 CD 推进前生效
- **WHEN** 一个眩晕 Modifier（B=CD推进速率为0）存在于某 NPC 上
- **THEN** 该 Tick 中，先处理 Modifier（确认眩晕仍存在），再推进卡牌 CD 时 SHALL 读取到速率为 0，因此卡牌 CD 不推进

#### Scenario: Modifier 到期后同 Tick 内数值修正消失
- **WHEN** 一个持续时间为 10 的 Modifier 在第 10 Tick 到期并被移除
- **THEN** 第 10 Tick 的卡牌 CD 推进阶段 SHALL 不再受该 Modifier 的数值修正影响