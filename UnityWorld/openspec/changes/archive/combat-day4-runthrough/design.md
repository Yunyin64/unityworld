## Context

combat-system-v3 Day1~Day3 已搭建完成：
- Tick 驱动引擎（CombatScene.Tick）+ 6 个 Handler（Targeting/CardFlow/Slot/Contest/Spillover/Mana）
- CombatCardState 独立计时器、ContestData 快照、待发槽机制、对拼结算、伤势卡生成
- APIMgr 反射扫描 + CombatBaseFunc 实现了 Heal/SelfDamage/ReduceMana
- 30 张卡牌 JSON 数据（FormBase/HuoCardBase 等 + Wound）

当前缺口：
1. **从未跑过一场战斗** — 没有测试入口，所有逻辑未经验证
2. **Condition 无运行时执行** — EffectData.ConditionId 有值但无处检查
3. **对拼只做数值比较** — 拼完后不执行 OnUse 非拼点 Action，也不检查 ContestWin/ContestLose Trigger
4. **Freeze 机制未实现** — APIMgr 注册了签名，但无 [APIFunc] Handler，CombatCardState 也没有 frozen 状态
5. **DamageInfo.CreateInjurySelfDamage 仍是占位** — 不过伤势卡实际走的是 EffectCard 路径（CD 满→SelfDamage Action），CreateInjurySelfDamage 可能已不需要

## Goals / Non-Goals

**Goals:**
- 让一场 2v2 或 1v1 战斗能从 Init→PreStart→Start→Tick 循环→结果，完整跑通
- Condition 选择器模式可用，至少支持 `cond_random_enemy_card_in_cd` 和 `cond_card_above`
- Freeze 机制可用（冻结卡牌暂停 CD）
- 对拼后续结算可用（执行非拼点 Action + ContestWin/ContestLose）
- 战斗日志可读，能辅助判断数值是否合理
- 首轮数值粗调，确保战斗不会 1 Tick 秒杀也不会 100 Tick 还不结束

**Non-Goals:**
- 不接入大世界 NPC（Day5 做）
- 不写单元测试框架（用一个可运行的静态方法即可）
- 不做完整的 Condition 系统（只实现 Day4 需要的几个选择器）
- ~~不做 Slow/Haste/Charge 等其他 APIFunc 的实现~~ → **已改为全量实现所有 30 张卡引用的 Handler**

## Decisions

### 0. Trigger 运行时分发走 EventMgr，不手写 if-else

- **选择**: 战斗内的被动 Trigger（trigger_on_hit_body / trigger_on_attack / trigger_on_contest_win 等）通过现有 EventMgr 的 RegisterEvent + TriggerEvent 机制实现
- **原因**: EventMgr 已提供完整的 Scope 分层广播、触发中保护、悬空清理、延迟操作等基础设施。在 Scope 枚举中新增 `CombatNpc` 值，用 CombatNpc 的 ID 做 scope，战斗内事件与大世界完全隔离
- **流程**: CombatScene.Start() 遍历所有 CardState 的 Effect，对非 OnUse 的 Trigger 注册监听；战斗流程中在对应时机调用 TriggerEvent；CombatScene.End() 清理所有监听
- **替代方案**: 在 ContestHandler 里 if (triggerId == "xxx") 手写分发 → 放弃，不可扩展且破坏关注点分离
- **`trigger_on_use` 不走 EventMgr**：OnUse 是"CD 满→直接执行"的同步路径，保持 ResolveEffectCard/ResolveContest 直接调用

### 1. Condition 执行器用 switch 硬编码，不走反射

- **选择**: 新建 `CombatConditionHandler`，对 ConditionId 做 switch 分发，返回 bool + 向 Env 写入选择结果
- **原因**: Condition 数量有限（当前 JSON 只有 10 个），硬编码最直接、最可调试。反射扫描适合 Action（数量多、签名统一），不适合 Condition（每个逻辑差异大）
- **替代方案**: 类似 APIMgr 用 [ConditionFunc] attribute 反射注册 → 放弃，过度工程化

### 2. CombatConditionHandler 作为 CombatScene 的 Handler 组件（非全局单例）

- **选择**: 与 CombatManaHandler 同级，由 CombatScene 持有
- **原因**: Condition 的执行需要访问 CombatScene 的 Combatants（如查找敌方卡牌），放在 Handler 层最自然

### 3. Frozen 状态放在 CombatCardState 上，用 int 计数器

- **选择**: `CombatCardState.FrozenTicks`，每 Tick 递减，>0 时 TickCd 跳过
- **原因**: 简单明了，与现有 CurrentCdTick 并列，无需引入状态机或 buff 系统
- **替代方案**: 用 buff 系统管理 → 放弃，本期不引入 buff 系统

### 4. 对拼后续结算放在 CombatContestHandler 内部

- **选择**: ResolveContest 结束后，自己遍历赢家/输家的 PendingSlot 来源卡的 Effects，执行后续 Action
- **原因**: 对拼后续是对拼的延续，逻辑内聚。不需要回到 CombatScene 再调度

### 5. CreateInjurySelfDamage 不需要实质改动

- **选择**: 确认伤势卡通过 EffectCard 路径生效（CardState 加入卡组→CD 满→ProcessEffectCards→SelfDamage），CreateInjurySelfDamage 保留但标记 `// 已通过 EffectCard 路径实现`
- **原因**: 伤势卡的 EffectDefine 里已经配了 `action_self_dmg_1`，CD 满时自动走 ResolveEffectCard → APIMgr.Execute("SelfDamage")。DamageInfo.CreateInjurySelfDamage 是早期设计遗留，当前路径不经过它

### 6. 测试入口用静态方法 CombatTestRunner.RunBasicTest()

- **选择**: 新文件 `CombatTestRunner.cs`，静态方法，硬编码构造 2 个 CombatNpc 各装 3~5 张卡
- **原因**: 最快能跑起来，不依赖大世界初始化。后续 Day5 会删掉这个 Runner

### 7. ResolveEffectCard 中的 Condition 检查流程

- **选择**: 在执行 Action 前，先检查 EffectData.ConditionId。如果非空，调用 CombatConditionHandler.Evaluate → 返回 false 则跳过整个 Effect → 返回 true 则 Env 中已写入选择结果，后续 Action 从中读取
- **原因**: Condition 是 Effect 级别的门控，一个 Effect 的所有 Action 共享同一个 Condition 结果

## Risks / Trade-offs

- **[Condition 硬编码不可扩展]** 每加一个 Condition 要改 switch → 可接受，数量少；后续如果 Condition 爆发增长再考虑反射
- **[对拼后续结算可能递归]** ContestWin 的 Action 如果又触发对拼→无限递归 → 本期 ContestWin/ContestLose 的 Effect 不允许含拼点类 Action，由数据约束保证
- **[数值首调靠人工]** 没有自动化平衡工具 → 可接受，先跑通，后续用日志统计
- **[测试覆盖不足]** 只有一个手动测试入口 → 可接受，Day4 目标是跑通不是测试覆盖