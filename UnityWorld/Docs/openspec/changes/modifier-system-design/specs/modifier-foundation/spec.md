## ADDED Requirements

### Requirement: ModifierDefine 是持续效果的标准容器
ModifierDefine SHALL 作为战斗中所有持续效果的统一定义结构。每个 ModifierDefine SHALL 包含四个组成部分：生命周期（A）、数值修正（B）、触发机制（C）、特殊实现（D）。其中 B、C、D 均为可选，但至少须包含 B 或 C 之一。

#### Scenario: 一个只有数值修正的 Modifier
- **WHEN** 定义一个 ModifierDefine，只填写 A（生命周期）和 B（数值修正），C 和 D 留空
- **THEN** 该 Modifier 在存在期间持续提供被动数值修正，无事件触发行为

#### Scenario: 一个同时拥有数值修正和触发机制的 Modifier
- **WHEN** 定义一个 ModifierDefine，同时填写 B（数值修正）和 C（触发机制）
- **THEN** 该 Modifier 在存在期间既提供被动数值修正，也能在事件发生时执行 TCA 效果

---

### Requirement: 三种宿主类型
ModifierDefine SHALL 声明其宿主类型，分为 CardModifier、NpcModifier、CombatNpcModifier 三种。宿主类型决定 Modifier 挂载的目标和可修正的属性范围。

#### Scenario: CardModifier 挂载在特定卡实例上
- **WHEN** 一个 CardModifier 被施加
- **THEN** 该 Modifier SHALL 挂载在指定的卡牌实例上，其数值修正（B）只影响该卡的属性（CD、ActionData 数值等）

#### Scenario: NpcModifier 挂载在 CombatNpc 上
- **WHEN** 一个 NpcModifier 被施加
- **THEN** 该 Modifier SHALL 挂载在指定的 CombatNpc 上，其数值修正（B）影响该 NPC 的 Stat

#### Scenario: CombatNpcModifier 挂载在战场上
- **WHEN** 一个 CombatNpcModifier 被施加
- **THEN** 该 Modifier SHALL 挂载在 CombatScene 上，其数值修正（B）影响战场全局规则参数

---

### Requirement: 宿主类型选择标准
策划在设计 Modifier 时 SHALL 遵循以下判断标准：若修正目标可以抽象为 NPC 的某个 Stat（如"所有火系攻击+1"），SHALL 使用 NpcModifier；若修正目标只能针对特定卡实例（如"上方第1张火系卡攻击+1"），SHALL 使用 CardModifier；若修正影响全局战场规则，SHALL 使用 CombatNpcModifier。

#### Scenario: 全局属性修正使用 NpcModifier
- **WHEN** 设计效果为"该 NPC 所有火系攻击卡拼点+1"
- **THEN** SHALL 设计为 NpcModifier，因为"火系攻击加成"可以抽象为 NPC 的一个 Stat

#### Scenario: 位置相关修正使用 CardModifier
- **WHEN** 设计效果为"卡组上方第1和第2张火系卡的攻击+1"
- **THEN** SHALL 设计为 CardModifier，因为目标由位置决定，无法抽象为全局属性

---

### Requirement: 生命周期（A）管理 Modifier 的存亡
每个 ModifierDefine SHALL 定义生命周期参数，包括层数（Stacks）和/或持续时间（Duration，以 Tick 为单位）。两者可共存，任一归零时 Modifier SHALL 被移除。

#### Scenario: 纯层数生命周期
- **WHEN** 一个 Modifier 定义为 Stacks=3，无 Duration
- **THEN** 该 Modifier 在层数被扣至 0 时移除，无时间限制

#### Scenario: 纯持续时间生命周期
- **WHEN** 一个 Modifier 定义为 Duration=10，无 Stacks
- **THEN** 该 Modifier 在经过 10 个 Tick 后自动移除

#### Scenario: 层数 + 持续时间共存
- **WHEN** 一个 Modifier 定义为 Stacks=3 且 Duration=10
- **THEN** 层数扣至 0 或持续时间到 0，哪个先到就移除

---

### Requirement: 数值修正（B）提供被动效果
ModifierDefine 的数值修正部分 SHALL 描述对宿主 Stat 的修正。只要 Modifier 存在，修正 SHALL 持续生效；Modifier 被移除后，修正 SHALL 立即消失。

#### Scenario: NpcModifier 的数值修正
- **WHEN** 一个 NpcModifier 存在于某 CombatNpc 上，其 B 定义为"所有火系攻击拼点+1"
- **THEN** 该 NPC 的所有火系攻击卡在结算时 SHALL 读取到 +1 的加成

#### Scenario: Modifier 移除后修正消失
- **WHEN** 上述 NpcModifier 被移除
- **THEN** 该 NPC 的火系攻击卡 SHALL 不再获得 +1 加成

---

### Requirement: 触发机制（C）复用 TCA 架构
ModifierDefine 的触发机制部分 SHALL 使用与卡牌相同的 TCA（Trigger + Condition + Action[]）架构。Modifier 的 TCA 可以引用所有已注册的 Action，包括管理自身的 Action（如 RemoveSelf、ModifyStacks）。

#### Scenario: Modifier 响应战斗事件
- **WHEN** 一个 NpcModifier 的 C 定义为 T:OnHit → A:AddModifier("slow", Attacker)
- **THEN** 当该 NPC 被攻击时，SHALL 给攻击者施加一个"减速"Modifier

#### Scenario: Modifier 用 TCA 管理自身生命周期
- **WHEN** 一个 Modifier 的 C 定义为 T:OnHit → C:Self.Stacks>0 → A:RemoveSelf()
- **THEN** 被攻击时若层数大于 0，该 Modifier SHALL 移除自身

---

### Requirement: Modifier 不占卡组空间
Modifier SHALL 独立于卡组之外存在，不占用 NPC 的 SP（卡组空间上限）。这是 Modifier 与伤势卡的核心区别。

#### Scenario: 大量 Modifier 不影响 SP 判定
- **WHEN** 一个 NPC 身上有多个 Modifier，且卡组 Cost 总和 = SP
- **THEN** 该 NPC SHALL 不因 Modifier 的存在而触发 SP 溢出判负