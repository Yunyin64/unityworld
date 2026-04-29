## Why

战斗系统重构（combat-system-v3）的 Day1：将战斗框架从"回合制顺序出招"改造为"Tick驱动 + 独立计时器 + 待发槽对拼"。这是整个 v3 重构的地基，后续所有功能（卡牌数据适配、伤势系统、Mana系统、战斗Log、NPC接通）都建立在这个新框架上。必须先完成框架重构，才能推进 Day2~5。

## What Changes

- **BREAKING** 重构 `CombatScene`：移除 `NextTurn()` 回合制主循环，改为 `Tick()` 驱动，每Tick推进所有卡的独立计时器
- **BREAKING** 重构 `CombatNpc`：移除 `DeckSequence`/`CurrentDeckIndex`/`CycleCount` 出招表机制，改为 `List<CombatCardState>` 卡组 + `PendingSlot` 待发槽 + `Sp` 空间上限 + `ManaPool` 占位
- **BREAKING** 重构 `DamageInfo` → `DamageInfo`：继承 `ContextBase`，作为伤害事件的因果上下文包，携带来源/数值/结算结果全流程信息
- 新增 `CombatCardState` 类：每张卡的独立运行时状态（CD计时器、Mana满足、拼点数值提取）
- 新增待发槽机制：上限1张，溢出直击、双方有卡立即对拼
- 新增对拼结算：数值比较 + 赢方效果（攻→伤害/盾→加血/防→消失）+ 赢家通吃
- 新增 SP 溢出判负：每Tick检查 Cost总和 > SP → 判负
- 新增 HP清零→伤势卡生成→塞入卡组→HP恢复50% 流程
- 新增枚举：`CardType`、`PhysicalType`、`DamageSourceType`、`CombatEndReason.SpaceOverflow`
- 更新 `CombatResult`/`CombatantResult`：新增 `InjuryCards` 字段，`TotalTurns` → `TotalTicks`
- `CardData` 新增临时战斗字段（占位，Day2 回填为正式数据链）

## Capabilities

### New Capabilities
- `tick-engine`: Tick驱动战斗主循环 + 独立计时器推进 + 战斗结束条件检查
- `pending-slot`: 待发槽机制（入槽/溢出直击/双方对拼触发）
- `contest-resolve`: 对拼结算规则（数值比较/赢方效果/赢家通吃）
- `damage-context`: DamageInfo 因果上下文（替代 DamageInfo，承载对拼/直击/伤势的完整因果）
- `combat-card-state`: CombatCardState 运行时卡状态（CD追踪/拼点提取/攻防判定）
- `sp-overflow`: SP溢出判负机制（每Tick检查/触发Defeated）
- `injury-basic`: HP清零→伤势卡生成→卡组塞入→HP恢复（占位实现，Day3接入模板）

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/Combat/CombatScene.cs` — 主循环全面重构
- `Scripts/Game/Domain/Combat/CombatNpc.cs` — 数据模型全面重构
- `Scripts/Game/Domain/Combat/DamageInfo.cs` → 重构为 `DamageInfo.cs`
- `Scripts/Game/Domain/Combat/CombatResult.cs` — 新增字段、重命名
- `Scripts/Game/Domain/Combat/CombatCardState.cs` — 新增文件
- `Scripts/Game/Domain/Object/Card/Data/CardData.cs` — 临时扩展战斗字段
- `Scripts/Game/Data/Enum/EnumTypes.cs` — 新增枚举值
- 父变更 `combat-system-v3` 的 `tasks.md` Day2~5 — 需追加占位回填任务