## Why

combat-system-v3 的 Day1~Day3 已完成战斗框架重构、卡牌数据适配和 30 张基础卡牌设计。当前代码可编译，但从未实际运行过一场完整战斗。Day4 的目标是让战斗流程真正跑通——编写测试入口、补齐机制缺口（Condition 选择器、对拼后续结算、Freeze 等）、验证所有子系统联动、并做首轮数值调优。

## What Changes

- 新增 `CombatTestRunner` 静态类：构造测试用 CombatNpc、装入手配卡牌、驱动完整 Tick 循环，输出战斗过程日志
- 补齐 Condition 运行时执行框架：Effect 触发时检查 ConditionId，调用对应选择器逻辑，向 ActionContext.Env 写入选择结果
- 实现 `cond_random_enemy_card_in_cd` 选择器：从敌方 CD 中的卡随机选一张写入 TargetCardId
- 实现 `Freeze` APIFunc Handler：暂停目标卡牌 CD 计时若干 tick
- CombatCardState 新增 frozen 计数器（FrozenTicks），TickCd 时跳过冻结中的卡
- 补齐对拼后续结算：拼完后执行 OnUse Effect 中非拼点 Action，检查 OnContestWin/OnContestLose Trigger
- 补齐 DamageInfo.CreateInjurySelfDamage：从伤势卡 ActionData 读取 SelfDamage 值（确认走 EffectCard 路径即可）
- 首轮数值调参：根据战斗日志调整 CD/攻击/盾/防/HP/SP，确保战斗时长合理

## Capabilities

### New Capabilities
- `combat-condition-selector`: Condition 运行时选择器模式——Condition 不仅返回 bool，还可向 ActionContext 写入选择结果（如 TargetCardId），后续 Action 从 context 读取
- `combat-freeze`: 冻结机制——标记卡牌暂停 CD 计时器若干 tick
- `combat-contest-followup`: 对拼后续结算——拼完后执行非拼点 Action 和 OnContestWin/OnContestLose Trigger

### Modified Capabilities
- `combat-tick-engine`: Tick 主循环中 TickCd 需跳过 frozen 状态的卡；对拼结算后需触发后续 Effect 链
- `combat-card-data`: CombatCardState 新增 FrozenTicks 字段

## Impact

- `Scripts/Game/Domain/Combat/CombatCardState.cs` — 新增 FrozenTicks 字段、TickCd 跳过冻结逻辑
- `Scripts/Game/Domain/Combat/CombatCardFlowHandler.cs` — ResolveEffectCard 中新增 Condition 检查流程
- `Scripts/Game/Domain/Combat/CombatContestHandler.cs` — ResolveContest 后追加 Effect 后续结算
- `Scripts/Game/Domain/!Global/API/Combat/CombatBaseFunc.cs` — 新增 Freeze Handler
- `Scripts/Game/Domain/Combat/` — 新增 `CombatConditionHandler.cs`（Condition 选择器分发）
- `Scripts/Game/Domain/Combat/` — 新增 `CombatTestRunner.cs`（测试入口）
- `Scripts/Game/Domain/Combat/DamageInfo.cs` — CreateInjurySelfDamage 确认/清理占位