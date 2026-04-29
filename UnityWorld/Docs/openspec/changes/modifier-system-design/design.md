## Context

战斗系统采用 Tick 驱动 + 卡牌自动循环的模式，战斗中无玩家实时操作。当前卡牌的行为完全由 TCA（Trigger-Condition-Action）体系驱动，Action 是最小执行单元。

现状问题：Action 执行后产生的持续效果（如"给敌方挂上易伤""给己方火系卡+1攻击"）没有统一的容器。机制原子清单中描述了这些效果的语义，但缺少"效果挂在哪里、怎么持续、怎么消亡"的设计。

已有先例：TileModifierDefine 用于地块元气修正，是 Modifier 概念在世界层面的成功实践。功法系统的 CultivationPointType 枚举中也有 Modifier 作为节点奖励类型。本设计将这一概念统一并扩展到战斗层面。

## Goals / Non-Goals

**Goals:**
- 建立 ModifierDefine 作为通用的持续效果容器，支持三种宿主：Card、NPC、Combat
- 定义 Modifier 的四大组成部分（生命周期 / 数值修正 / 触发机制 / 特殊实现），职责清晰互不越界
- 触发机制（C）完全复用 TCA 架构，策划工具链统一
- 与现有伤势机制和平共存——伤势卡是"占 SP 的惩罚卡"，Modifier 是"不占 SP 的状态容器"，二者不替代

**Non-Goals:**
- 不设计大世界/非战斗场景的 Modifier（功法 Modifier、地块 Modifier 已有各自方案）
- 不定义具体的 Modifier 数值（如"护甲减多少伤"），那是策划填表的事
- 不讨论 Modifier 的程序实现架构（数据结构、类继承等），那是代码层的事

## Decisions

### 决策1：三种宿主类型的划分标准

**CardModifier** — 挂在某张特定卡实例上
- 适用场景：修正目标是特定卡实例，无法抽象为宿主的全局属性
- 典型案例："上方第1和第2张火系卡的攻击+1"——目标由位置决定，只能定点挂载

**NpcModifier** — 挂在某个 CombatNpc 上
- 适用场景：修正目标可以抽象为 NPC 的某个 Stat / 全局属性
- 典型案例："该 NPC 所有火系攻击卡拼点+1"——这是一个可查询的属性，所有火系卡结算时去读它

**CombatNpcModifier** — 挂在整场战斗上
- 适用场景：修正影响战场全局规则，不归属于某个 NPC
- 典型案例：环境效果（"此战场所有火系伤害+2"）、天象、地形战斗效果

**判断标准：能抽象成宿主的 Stat → NpcModifier；只能改特定卡实例 → CardModifier；影响全局 → CombatNpcModifier。**

### 决策2：四大组成部分的职责边界

```
A 生命周期 ── 我能存在多久
   • Stacks（层数）
   • Duration（Tick 数）
   • 两者可共存：3层且持续10Tick，谁先归零谁先杀死我
   • 所有"花式移除条件"不在这里处理，交给 C（触发机制）自己用 TCA 实现
     例如"被打时如果层数>0则移除自身" = T:OnHit C:Self.Stacks>0 A:RemoveSelf()

B 数值修正 ── 我静静存在就能改变什么（被动效果）
   • 对宿主的 Stat 做修正
   • CardModifier：改卡的 CD、改 ActionData 中的数值
   • NpcModifier：改 NPC 的基础属性、战斗属性
   • CombatNpcModifier：改全局规则参数
   • 只要 Modifier 存在就持续生效，移除后修正消失

C 触发机制 ── 我能在事件中做什么（主动效果）
   • 完全复用 TCA：Trigger + Condition + Action[]
   • 典型："被攻击时给攻击者施加减速"
   • 也用于管理自身生命周期（扣层、移除自身等）

D 特殊实现 ── Lua 硬编码扩展
   • 策划无感，为复杂逻辑预留的程序出口
```

**B 和 C 的设计直觉：**
- B = 只要我在就一直生效，不需要事件触发（"受到伤害-1"、"所有火系卡攻击+1"、"CD速率-50%"）
- C = 需要某个事件发生才触发（"被攻击时给攻击者减速"、"每3Tick对自身造成1点伤害"、"对拼赢时回1血"）
- 一个 Modifier 可以同时拥有 B 和 C

### 决策3：Modifier 与现有机制原子的映射

现有机制原子清单中的状态类效果，在 Modifier 体系下的表达：

| 原有概念 | Modifier 类型 | A 生命周期 | B 数值修正 | C 触发机制 |
|---------|--------------|-----------|-----------|-----------|
| 护甲N | NpcModifier | N层 | 受到伤害-1（每次消耗1层） | — |
| 易伤X | NpcModifier | 1次触发后消失 | — | OnHit → 伤害+X，然后 RemoveSelf |
| 虚弱X | NpcModifier | 1次出手后消失 | 下一张待发卡拼点-X | 出手后 RemoveSelf |
| 眩晕 | NpcModifier | 持续X Tick | 所有卡CD推进速率=0 | — |
| XX强化1 | NpcModifier | 看设计 | 对应元素攻击卡拼点+1 | — |
| 中毒N | NpcModifier | N层 | — | 每Y Tick → SelfDamage(1)，消耗1层 |

### 决策4：Action 是 Modifier 的生产者

卡牌通过 TCA Action 来创建 Modifier：
- 新增 Action：`AddModifier(ModifierId, Target)` — 在目标身上实例化一个 Modifier
- 新增 Action：`RemoveModifier(ModifierId, Target)` — 移除目标身上的指定 Modifier
- 新增 Action：`ModifyStacks(ModifierId, Target, Delta)` — 增减目标身上某 Modifier 的层数

Modifier 自身的 TCA（C部分）也可以调用这些 Action，实现 Modifier 之间的联动。

### 决策5：Modifier 不占卡组空间

这是 Modifier 与伤势卡的核心区别：
- **伤势卡** = 塞入卡组、占 SP、参与 CD 循环的惩罚机制。它的"占空间"本身就是设计目的。
- **Modifier** = 独立于卡组之外的状态容器，不占 SP。

两者和平共存，各司其职。

## Risks / Trade-offs

- **[Modifier 数量膨胀]** → 当大量 Modifier 同时存在时，每 Tick 的结算复杂度上升。缓解：这是程序优化问题，不是设计问题；设计层面通过合理的层数/持续时间控制总量。
- **[B 和 C 的边界模糊]** → 某些效果可以用 B（被动数值修正）也可以用 C（OnTick 触发）来实现，比如"每秒回1血"。缓解：策划用哪个都行，效果等价；约定"持续性数值修正用 B，周期性事件用 C"作为风格指南。
- **[与伤势系统的潜在混淆]** → "中毒"用 Modifier 实现而非伤势卡，但两者都造成持续伤害。缓解：设计身份清晰不同——伤势卡是"战后也存在的永久惩罚"，Modifier 是"战斗内的临时状态"。