## Why

战斗系统当前的机制原子清单（易伤、虚弱、护甲、眩晕、XX强化等）只定义了"做什么"（Action），但缺少**持续效果的容器和生命周期管理**。Action 执行后产生的状态效果——持续多久、能否叠加、如何驱散、被动数值修正如何生效——这些问题没有统一的设计框架。

同时，Modifier 概念已经零散存在于多个系统（TileModifierDefine 用于地块元气修正、功法节点的 Modifier 奖励类型、TCA 体系中"Action 数值可被 buff/debuff 修改"的描述），但缺乏跨系统的统一设计语言。

## What Changes

- 建立 **ModifierDefine 体系**：定义 Modifier 作为一种通用 Define 结构，类似已有的 TileModifierDefine，但面向战斗中的卡牌、NPC、战场三个层级
- 定义 Modifier 的**四大组成部分**：生命周期（A）、数值修正（B）、触发机制（C）、特殊实现（D）
- 明确 **CardModifier / NpcModifier / CombatNpcModifier** 三种宿主类型的适用场景与区分标准
- 明确 Modifier 与现有系统（TCA体系、Action原子、伤势机制）的关系与边界

## Capabilities

### New Capabilities
- `modifier-foundation`: Modifier 基础建设——Define 结构、四大组成部分（生命周期/数值修正/触发机制/特殊实现）、三种宿主类型的设计规则
- `modifier-combat-integration`: Modifier 与战斗系统的集成——Action 如何生产 Modifier、Modifier 在 Tick 循环中的结算时序、与伤势/卡牌系统的边界

### Modified Capabilities

（无已有 spec 需要修改）

## Impact

- **战斗系统设计**：机制原子清单中的状态类效果（易伤、虚弱、护甲、眩晕、XX强化）将获得统一的底层容器
- **TCA 体系**：新增 AddModifier / RemoveModifier 等 Action 类型；Modifier 自身的触发机制（C）复用 TCA 架构
- **NPC 战斗属性**：NPC Stat 清单可能需要扩展，以支持 NpcModifier 的数值修正目标
- **卡牌模型**：CardModifier 需要卡牌实例能持有 Modifier 列表
- **地图系统**：TileModifierDefine 作为已有先例，Modifier 体系应与其保持概念一致性