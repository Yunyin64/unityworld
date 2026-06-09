## Context

当前战斗系统（CombatScene）采用回合制顺序出招模型，NPC 持有有序出招表按索引循环出招。此模型过于简单，无法承载新设计的CD节奏博弈、待发槽对拼、伤势累积、灵元资源管理等玩法。

现有代码：CombatScene.cs / CombatNpc.cs / CombatResult.cs / DamageInfo.cs / Element.cs，以及 Card 体系（CardData / EffectData / CardDefine / ActionDefine 等）。战斗框架需要从"回合驱动"重构为"Tick驱动 + 独立计时器"。

完整设计参见 `Docs/战斗设计.txt`。

## Goals / Non-Goals

**Goals:**
- 实现 Tick 驱动的战斗引擎，每张卡独立计时器并行推进
- 实现待发槽对拼机制（溢出直击、双方有卡立刻对拼）
- 实现伤势系统（HP清零→伤势卡→卡组空间溢出判负）
- 实现基础 Mana 系统框架（蓝条转化灵元）
- 扩展卡牌数据结构支持战斗数值（攻击/盾/防/元素/物理类型）
- 手配30张基础卡验证战斗流程
- 输出可读的战斗Log
- 战斗结算与大世界NPC接通

**Non-Goals:**
- 玩家实时操控战斗（未来预留，本期不做）
- 五行相克加成计算（数值调参阶段再加）
- 完整的 Trait/被动技能 接入战斗（预留接口即可）
- 战斗AI目标切换策略（本期用最简规则：固定Target）
- 战斗动画/表现层（纯逻辑层）

## Decisions

### 1. 战斗驱动从"回合制"改为"Tick制"
- **选择**: 每个Tick推进所有卡的计时器，而非按NPC顺序轮流行动
- **原因**: 新模型的核心是"CD节奏博弈"，每张卡的CD独立跑，回合制无法表达这种并行性
- **替代方案**: 保留回合制但加CD机制 → 放弃，因为回合制的"轮到你才动"与"所有卡同时跑CD"矛盾

### 2. 在现有 ActionDefine 上扩展战斗数值字段
- **选择**: 给 ActionDefine 新增 AtkValue / ShieldValue / DefendValue / Element / PhysicalType 等可选字段
- **原因**: Action 本就是"做什么"的载体，攻击/防御是 Action 的一种，保持一套体系
- **替代方案**: 新建独立的 CombatActionDefine → 放弃，会导致两套 Action 体系并行维护

### 3. CardType 作为风味分类 + 规则差异标记
- **选择**: CardType 是枚举（招式/法术/法宝/丹药/阵法/神通），决定 Mana 消耗模式等规则差异
- **原因**: 卡的实际行为由 List<EffectData> 决定，CardType 是上层分类，不影响 Effect 机制

### 4. 败北判定 = SP 溢出，不是 HP 归零
- **选择**: 每Tick检查卡Cost总和是否超过SP，超过即判负
- **原因**: 核心设计理念"你不是被打死的，是被伤势压垮的"

### 5. 战斗内新建 CombatCardState 追踪每张卡的运行时状态
- **选择**: 不修改 CardData 本身，而是在 CombatNpc 上为每张卡建立 CombatCardState（当前CD进度、Mana是否满足、法宝剩余次数等）
- **原因**: 保持大世界 CardData 干净，战斗状态与大世界隔离

## Risks / Trade-offs

- **[Tick粒度影响平衡]** Tick的时间步长决定CD的精度，太粗会导致同时触发过多，太细会影响性能 → 先用1Tick=1单位时间，后续可调
- **[30张卡的数值平衡]** 首批卡数值靠手感调，跑通后可能需要大量调整 → Day4跑通后用Log数据辅助调参
- **[伤势卡持续到战后]** 需要NPC卡组持久化支持伤势卡的存储和移除 → Day5接通时处理
- **[Mana系统本期仅框架]** 转化规则（什么元素、多少量）暂用简单规则，不深入设计 → 后续迭代