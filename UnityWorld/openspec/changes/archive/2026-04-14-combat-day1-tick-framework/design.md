## Context

当前战斗系统（CombatScene/CombatNpc）采用回合制顺序出招模型：每个 NPC 持有有序出招表 `DeckSequence`，按 `CurrentDeckIndex` 循环出招，双方轮流行动。此模型过于简单，无法承载新设计的 CD 节奏博弈、待发槽对拼、伤势累积、灵元资源管理等玩法。

本变更是 `combat-system-v3` 五日计划的 Day1，专注于框架级重构。后续 Day2（卡牌数据适配）、Day3（基础卡牌设计）、Day4（流程跑通）、Day5（Log与NPC接通）都依赖本日完成的框架。

现有代码文件：
- `CombatScene.cs`：386行，回合制主循环 `NextTurn()`
- `CombatNpc.cs`：146行，继承 `Npc`，持有出招表
- `CombatResult.cs`：76行，结算数据
- `DamageInfo.cs`：53行，伤害描述（未被引用，预留）
- `Element.cs`：41行，元素类型包装

约束：
- 战斗是离线模拟，`CombatScene` 是纯实例对象，不依赖全局 Tick
- 保持大世界隔离：战斗内变化不直接修改 Npc/StatBlock
- `CombatNpc` 继承自 `Npc`（partial class，含 Soul/Stats/CultivationData 等）
- Day1 部分数据链不完整（ActionDefine 战斗字段在 Day2 才加），需要临时占位

## Goals / Non-Goals

**Goals:**
- 实现 Tick 驱动的战斗主循环，每张卡独立计时器并行推进
- 实现待发槽机制（入槽/溢出直击/双方对拼触发）
- 实现完整的对拼结算规则（数值比较/赢方效果/赢家通吃）
- 实现 SP 溢出判负（每Tick检查）
- 实现 HP 清零→伤势卡生成→塞入卡组→HP恢复50% 的完整链路
- 重构 DamageInfo 为 DamageInfo（ContextBase 因果包模式）
- 新建 CombatCardState 运行时状态追踪
- 提供测试辅助方法，Day1 内部可构造简单战斗验证框架正确性

**Non-Goals:**
- 不接入真实的 ActionDefine 战斗数值（Day2）
- 不实现 Mana 转化/消耗逻辑（Day2），仅占位字段
- 不设计伤势卡模板/Define（Day3），仅硬编码占位
- 不实现效果卡的实际 Effect 执行（Day2），仅日志占位
- 不接入大世界 NPC 属性/卡组（Day5），HP/SP/CardStates 用硬编码
- 不实现战斗 AI 目标切换策略（固定 Target）
- 不实现五行相克/元素加成计算

## Decisions

### 1. DamageInfo → DamageInfo（继承 ContextBase）

- **选择**: 将 DamageInfo 重构为 DamageInfo，继承项目已有的 ContextBase 松散因果包模式，同时提供强类型便捷属性
- **原因**: 对拼/直击/伤势自伤都是"伤害事件"，需要一个统一的因果容器。ContextBase 的 Set/Get 字典模式允许未来任何系统（Trait被动、五行相克、日志）按需读取因果，不需要每次都改 DamageInfo 的字段
- **替代方案**: 保持 DamageInfo 强类型类 → 放弃，因为字段会随系统增长而膨胀，且 BirthContext 已经证明因果包模式在本项目中可行

### 2. CombatCardState 独立于 CardData

- **选择**: 战斗中为每张卡创建独立的 CombatCardState 追踪运行时状态，不修改原始 CardData
- **原因**: 保持大世界 CardData 干净，战斗状态与大世界隔离。CombatCardState 持有 CardData 引用但不修改它
- **替代方案**: 在 CardData 上加战斗临时字段 → 放弃，会污染大世界数据

### 3. CardData 临时战斗字段（占位方案）

- **选择**: Day1 在 CardData 上新增 `ContestValue`/`ContestType`/`PhysicalType`/`CardType` 临时字段，CombatCardState 从中读取拼点数值
- **原因**: Day2 才扩展 ActionDefine 的战斗数值字段（AtkValue/ShieldValue/DefendValue），Day1 数据链不完整。临时字段让 Day1 的框架可以端到端跑通
- **后续回填**: Day2 完成后，CombatCardState 改为从 EffectData→ActionDefine 汇总数值，移除 CardData 上的临时字段
- **替代方案**: Day1 提前做 ActionDefine 扩展 → 放弃，违反逐日推进原则，Day1 应专注框架

### 4. DamageSourceType 枚举区分伤害来源

- **选择**: 新增 `DamageSourceType` 枚举（Contest/DirectHit/Overflow/Injury/Effect），DamageInfo 携带此标记
- **原因**: 日志系统、被动触发都需要知道"这次伤害是怎么来的"，单个枚举比多个 bool 更清晰

### 5. 伤势卡硬编码占位

- **选择**: Day1 的 `CreateInjuryCard()` 返回硬编码的 CardData（固定 Cost/CD/数值），不查 Define
- **原因**: 伤势卡模板 Define 在 Day3 才设计。Day1 只需验证"HP清零→生成伤势卡→塞入卡组→SP溢出判负"的机制链路正确
- **后续回填**: Day3 设计伤势卡模板后，替换为从 Define 查询

### 6. 效果卡结算空实现

- **选择**: Day1 的效果卡（非攻防卡）CD就绪后仅输出日志，不执行实际 Effect
- **原因**: Effect 执行逻辑依赖 Action/Trigger/Condition 体系，Day2 才接入
- **后续回填**: Day2 接入完整的 Effect 执行管线

### 7. CombatScene.PreStart 硬编码初始化

- **选择**: PreStart 阶段的 HP/SP/MP/CardStates 用硬编码或构造参数设置，不读大世界 NPC
- **原因**: 大世界 NPC 接入是 Day5 任务。Day1 需要一个 `SetupTestCombatNpc()` 方法便于内部验证
- **后续回填**: Day5 改为从真实 Npc 读取属性和卡组

## Risks / Trade-offs

- **[临时字段污染 CardData]** Day1 在 CardData 上加的临时字段如果 Day2 忘记移除，会留下技术债 → 在 Day2 任务清单中显式列出"移除 CardData 临时字段"
- **[占位方法签名可能不稳定]** CreateInjuryCard / ResolveEffectCard 等占位方法的参数和返回值在后续接入真实逻辑时可能需要调整 → 接受，Day1 追求框架正确性而非 API 稳定性
- **[CombatNpc 继承 Npc 带来的耦合]** CombatNpc 继承 Npc，但战斗内不应访问 NpcMgr 等全局单例 → Day1 不改继承关系，但战斗内仅通过 CombatNpc 自身字段访问数据
- **[ContextBase 的类型安全]** DamageInfo 继承 ContextBase 的松散字典，Get/Set 无编译期类型检查 → 通过强类型便捷属性包装常用字段，字典仅用于扩展性预留