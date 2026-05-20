## 1. Scope 扩展 + 战斗事件注册/触发框架

- [x] 1.1 `EnumTypes.cs` 中 `Scope` 新增 `CombatNpc` 枚举值
- [x] 1.2 `EventDefines.json` 新增战斗域事件定义：`CombatAttack`（scope: CombatNpc）、`CombatHitBody`（scope: CombatNpc）、`CombatContestWin`（scope: CombatNpc）、`CombatContestLose`（scope: CombatNpc）、`CombatDominate`（scope: CombatNpc）、`CombatDominated`（scope: CombatNpc）、`CombatBurn`（scope: CombatNpc）、`CombatDeath`（scope: CombatNpc）。eventId 直接复用 TriggerDefine 的 ID（如 `trigger_on_hit_body`）
- [x] 1.3 CombatScene 新增 `RegisterCardTriggerListeners()` 方法：遍历所有 CombatNpc 的所有 CardState 的所有 Effect，对于 TriggerId 非空且非 "trigger_on_use" 的 Effect，用 `EventMgr.RegisterEvent` 注册监听（scope = `new ScopeKey(Scope.CombatNpc, npc.Id.ToString())`），Handler 为执行该 Effect 的 Action 链（经 Condition 门控）
- [x] 1.4 CombatScene.Start() 中调用 `RegisterCardTriggerListeners()`
- [x] 1.5 CombatScene.End() 或战斗结束时清理所有注册的 CombatNpc scope 监听（遍历注册列表调用 RemoveEvent）
- [x] 1.6 在 CombatContestHandler 的对拼结算中，在适当时机调用 `EventMgr.TriggerEvent("trigger_on_contest_win", result, (Scope.CombatNpc, winnerId))`，同理 contest_lose / dominate / dominated
- [x] 1.7 在对拼结算中攻击伤害打到 HP 时，调用 `EventMgr.TriggerEvent("trigger_on_hit_body", damageCtx, (Scope.CombatNpc, attackerId))`
- [x] 1.8 在对拼发生时（或攻防卡入槽时），调用 `EventMgr.TriggerEvent("trigger_on_attack", ..., (Scope.CombatNpc, attackerId))`

## 2. CombatCardState 冻结 + 速率机制

- [x] 2.1 CombatCardState 新增 `FrozenTicks` 属性（int，默认 0）和 `Freeze(int ticks)` 方法（取 max）
- [x] 2.2 CombatCardState 新增 `SlowStacks` 和 `HasteStacks` 属性（int，默认 0），表示减速/加速叠层
- [x] 2.3 修改 `TickCd()`：FrozenTicks > 0 时递减 FrozenTicks 且不递增 CurrentCdTick；否则按速率公式递增（Haste: +1×(1+10%×HasteStacks)，Slow: +1×(1-10×SlowStacks/(100+10×SlowStacks))），递增后向下取整写入 CurrentCdTick。⚠️ 注意 CurrentCdTick 是 int，考虑用 float 累加器 `_cdAccumulator` 来保留精度
- [x] 2.4 CombatCardState 新增 `ResetSpeedModifiers()` 方法：将 SlowStacks/HasteStacks 归零。在卡牌 CD 满（使用后）自动调用

## 3. CombatNpc Buff 基础设施

- [x] 3.1 新建 `CombatBuff.cs`（与 CombatCardState.cs 同级）：简单数据类，字段包括 `BuffId`(string)、`Stacks`(int)、`RemainingDuration`(float，-1 表示永久)
- [x] 3.2 CombatNpc 新增 `List<CombatBuff> Buffs` 属性（默认空列表）
- [x] 3.3 CombatNpc 新增 `AddBuff(string buffId, int stacks, float duration = -1)` 方法：已存在同 buffId 的 Buff 则叠加层数（duration 取最大值），不存在则新增
- [x] 3.4 CombatNpc 新增 `RemoveBuff(string buffId, int stacks = int.MaxValue)` 方法：减少层数，归零时移除
- [x] 3.5 CombatNpc 新增 `GetBuffStacks(string buffId)` 方法：返回 0 表示无
- [x] 3.6 CombatNpc 新增 `TickBuffs(float dt)` 方法：遍历 Buffs，有 duration 的递减，过期移除；处理 Burn 效果（每 Tick 造成 Stacks 点自伤 + 层数-1）
- [x] 3.7 CombatScene.Tick 中在合适位置（如 Tick 开头或结尾）调用每个 CombatNpc 的 TickBuffs

## 4. CombatCardState Buff 基础设施

- [x] 4.1 CombatCardState 新增 `Dictionary<string, int> CardBuffs` 属性（默认空字典），key=BuffId, value=层数/值
- [x] 4.2 CombatCardState 新增 `AddCardBuff(string buffId, int value)` 和 `ConsumeCardBuff(string buffId)` 方法
- [x] 4.3 CombatManaHandler 在检查灵元消耗时，先检查 CardBuff 中是否有 "FreeManaCost"：若有则跳过灵元消耗并消耗该 Buff

## 5. CombatConditionHandler 选择器框架

- [x] 5.1 新建 `CombatConditionHandler.cs`（与 CombatManaHandler 同级），构造函数接收 CombatScene 和 Rng
- [x] 5.2 实现 `bool Evaluate(string conditionId, CombatNpc caster, CombatCardState? currentCard, ContextBase env)` 主方法，对 conditionId 做 switch 分发
- [x] 5.3 实现 `cond_none` / 空字符串分支：直接返回 true
- [x] 5.4 实现 `cond_random_enemy_card_in_cd`：从 caster.Target 的 CardStates 中筛选 IsActive 且 CurrentCdTick < Cooldown 的卡，随机选一张写入 env["TargetCardId"]，无可选卡返回 false
- [x] 5.5 实现 `cond_card_above`：在 caster.CardStates 中找到 currentCard 的索引，选索引-1 的卡写入 env["TargetCardId"]，无上方卡返回 false
- [x] 5.6 实现 `cond_card_self`：将 currentCard 的 DefineId 写入 env["TargetCardId"]
- [x] 5.7 实现 `cond_card_all_self`：将 caster 所有 IsActive 的 CardState 的 DefineId 列表写入 env["TargetCardIds"]（List\<string\>），后续 Action 需遍历执行
- [x] 5.8 实现 `cond_target_self`：将 caster.Id.ToString() 写入 env["Targetint"]
- [x] 5.9 实现 `cond_target_enemy`：将 caster.Target.Id.ToString() 写入 env["Targetint"]
- [x] 5.10 实现 `cond_enemy_card_top`：取 caster.Target.CardStates 中第一张 IsActive 的卡的 DefineId 写入 env["TargetCardId"]，无卡返回 false
- [x] 5.11 default 分支：LogMgr.Warn 输出未知 conditionId，返回 false

## 6. Condition 接入 Effect 执行流程

- [x] 6.1 CombatScene 新增 `_conditionHandler` 字段，InitializeHandlers 中初始化，暴露 internal 属性
- [x] 6.2 修改 `CombatCardFlowHandler.ResolveEffectCard`：遍历 Effect 时，在执行 Action 前检查 ConditionId，非空且非 "cond_none" 时调用 Evaluate，返回 false 则跳过该 Effect
- [x] 6.3 Evaluate 返回 true 时，将 env 中的选择结果传递到每个 Action 的 ActionContext.Env 中
- [x] 6.4 对于 `cond_card_all_self` 返回的 TargetCardIds 列表，需要对 Action 循环执行（每张目标卡执行一次，每次将对应的 DefineId 写入 TargetCardId）

## 7. APIFunc Handler 全量实现

- [x] 7.1 `[APIFunc("Freeze")]`：从 ctx.Env 读 TargetCardId（优先 Env，Action.Context 为空串时从 Env 取），在 caster.Target.CardStates 中按 DefineId 查找，调用 Freeze(freezeTick)
- [x] 7.2 `[APIFunc("Charge")]`：同理取 TargetCardId，在 caster.CardStates（self）或 target.CardStates 中查找（取决于卡），增加其 CurrentCdTick（不超过 Cooldown）
- [x] 7.3 `[APIFunc("Convert")]`：从 caster.ManaPool 中取指定 Element 的灵元（"Any" 时按任意顺序取），数量不超过 MaxAmount，转化回 caster.Mp（1:1）
- [x] 7.4 `[APIFunc("Draw")]`：从 caster.Mp 中扣除 Amount，按 caster 当前卡牌的元素类型分配灵元到 ManaPool（⚠️ 决策点：Draw 的灵元归属什么元素？暂按均分或按卡牌 ManaCost 中的元素类型）
- [x] 7.5 `[APIFunc("ArmorBreak")]`：从 caster.Target 身上的 Shield 状态（⚠️ 决策点：Shield 值存在哪里？需要 CombatNpc 新增 ShieldValue 属性）中减去 BreakValue
- [x] 7.6 `[APIFunc("AddNpcBuff")]`：从 Env 取 Targetint，从 CombatScene 查找目标 CombatNpc，调用 AddBuff(buffId, stacks, duration)
- [x] 7.7 `[APIFunc("AddCardBuff")]`：从 Env 取 TargetCardId，在对应 NPC 的 CardStates 中查找目标卡，调用 AddCardBuff(buffId, value)
- [x] 7.8 `[APIFunc("Slow")]`：从 Env 取 TargetCardId，在 caster.Target.CardStates 中查找，增加 SlowStacks += X
- [x] 7.9 `[APIFunc("Haste")]`：从 Env 取 TargetCardId，在对应 NPC 的 CardStates 中查找，增加 HasteStacks += X
- [x] 7.10 `[APIFunc("RemoveWound")]`：在 caster.CardStates 中找 Card.CardType 为伤势且 Size 匹配 SizeList 的卡，随机移除一张
- [x] 7.11 `[APIFunc("Displace")]`：从 Env 取 TargetCardId，在目标 NPC 的 CardStates 列表中将其移到 Position 指定的位置（"Bottom" = 列表末尾，"Top" = 列表开头）

## 8. 对拼后续结算（OnUse 非拼点 Action）

- [x] 8.1 CombatContestHandler.ResolveContest 结束后（清空 PendingSlot 之前），遍历双方 PendingSlot.SourceCard.Card.Effects
- [x] 8.2 对 TriggerId 为 "trigger_on_use" 或空的 Effect，提取其中 FuncName 不是 Attack/Shield/Block 的 Action，经 Condition 门控后执行
- [x] 8.3 ⚠️ 需要 CombatConditionHandler 引用，通过 CombatScene 暴露 internal 属性获取

## 9. Shield 机制 + Buff 对伤害计算的接入

- [x] 9.1 CombatNpc 新增 `ShieldValue`（float，默认 0）属性：表示当前护盾值
- [x] 9.2 对拼结算中 Shield 赢了后的溢出治疗，需要同时把 ShieldValue 设为盾值（或累加）
- [x] 9.3 对拼结算中攻击伤害先扣 ShieldValue 再扣 HP
- [x] 9.4 ArmorBreak 消除 ShieldValue
- [x] 9.5 CombatContestHandler.ResolveContest 中，攻击伤害计算后、ApplyDamage 之前，查询 loser 的 Buff 层做修正：Vulnerable → 伤害 × (1 + 10%×stacks)，Weakness → 伤害 × (1 - 10×stacks/(100+10×stacks))，Armor → 先扣 ShieldValue
- [x] 9.6 同理 ExecuteDirectHit 中也要做 Buff 修正
- [x] 9.7 ⚠️ 决策点：Vulnerable/Weakness 的修正公式是否与 Slow/Haste 对称？当前 Slow 是 `1 - 10X/(100+10X)`，Haste 是 `1 + 10%X`。建议统一为：增益 `1 + 10%X`，减益 `1 - 10X/(100+10X)`（递减公式，不会归零）

## 10. DamageInfo.CreateInjurySelfDamage 清理

- [x] 10.1 确认伤势卡通过 EffectCard 路径生效：检查 Wound.json 的 EffectIds 对应 SelfDamage Action
- [x] 10.2 DamageInfo.CreateInjurySelfDamage 占位注释改为 `// 已通过 EffectCard 路径实现`

## 11. 战斗日志系统（CombatLogger）

- [x] 11.1 新建 `CombatLogger.cs`（与 CombatScene.cs 同级）：持有 `StringBuilder _sb`，提供 `Log(string msg)` 方法追加带时间戳的行（格式：`[Tick{CurrentTick,3}] {msg}`），同时 `Console.WriteLine` 输出
- [x] 11.2 CombatLogger 提供 `LogSeparator(string title)` 方法：输出分隔线（如 `═══ Tick 5 ═══`）
- [x] 11.3 CombatLogger 提供 `LogNpcSnapshot(CombatNpc npc)` 方法：输出 NPC 当前战斗数据快照（HP/MaxHp、ShieldValue、SP/Sp、Mp、ManaPool 内容、Buffs 列表、CardStates 简表[DefineId CD/Cooldown IsActive]）
- [x] 11.4 CombatLogger 提供 `LogDeckInfo(CombatNpc npc)` 方法：输出卡组初始信息（NPC ID、阵营、HP、SP、卡牌列表[DefineId Size Cooldown ManaCost]）
- [x] 11.5 CombatLogger 提供 `ExportToFile(string filePath)` 方法：将 `_sb` 内容写入指定 txt 文件（`File.WriteAllText`），路径默认为 `CombatLogs/combat_{timestamp}.txt`
- [x] 11.6 CombatLogger 提供 `Clear()` 方法：清空 _sb
- [x] 11.7 CombatScene 新增 `_logger` 字段（CombatLogger 实例），暴露 internal 属性 `Logger` 供 Handler 访问
- [x] 11.8 修改 `Log(string msg)` → 改为调用 `_logger.Log(msg)`，保持所有现有 Handler 的调用不变
- [x] 11.9 CombatScene.Init 中：`_logger = new CombatLogger()`，输出战斗初始化信息
- [x] 11.10 CombatScene.PreStart 中：对每个 CombatNpc 调用 `_logger.LogDeckInfo(npc)`，输出双方卡组信息
- [x] 11.11 CombatScene.Tick 中：开头调用 `_logger.LogSeparator($"Tick {CurrentTick}")`；每 5 Tick 或关键事件后调用 `_logger.LogNpcSnapshot(npc)` 输出快照
- [x] 11.12 战斗结束时（SpilloverHandler.SetResult 后）：调用 `_logger.LogNpcSnapshot` 输出最终状态，然后 `_logger.ExportToFile()` 自动导出
- [x] 11.13 各 Handler 中已有的 `Log(...)` 调用无需修改（已通过 11.8 统一重定向到 Logger）
- [x] 11.14 新增的关键行为节点补充日志：卡牌 CD 就绪时、Buff 添加/移除时、Condition 评估结果时、EventMgr TriggerEvent 时

## 12. 测试入口与验证

- [x] 12.1 新建 `CombatTestRunner.cs`，静态方法 `RunBasicTest()`：构造 2 个 CombatNpc（TeamA / TeamB），各装 3~5 张不同类型的卡（包含攻击/防御/辅助/效果卡，覆盖 Freeze/Charge/Heal/Slow 等机制）
- [x] 12.2 使用 CardMgr.InstantiateFromDefine 从真实 JSON Define 实例化卡牌
- [x] 12.3 调用 CombatScene 完整流程：Init → PreStart → Setup → Start → while(!IsFinished) Tick → GetResult
- [x] 12.4 验证 Tick 循环（CD 递增 + Frozen 跳过 + Slow/Haste 速率调整）
- [x] 12.5 验证待发槽入槽 / 溢出直击 / 对拼
- [x] 12.6 验证对拼结算（攻vs攻、攻vs盾、攻vs防）+ Shield 机制
- [x] 12.7 验证对拼后续 OnUse 非拼点 Action + ContestWin/Lose Trigger
- [x] 12.8 验证 HP清零 → 伤势卡生成 → SP溢出判负
- [x] 12.9 验证 Mana 转化 → 灵元消耗 → 法术卡启动
- [x] 12.10 验证效果卡直接结算（Condition 门控 + Freeze/Charge/Convert/Draw 等）
- [x] 12.11 验证 Buff 系统（AddNpcBuff Burn 每 tick 伤害、Vulnerable/Weakness 增减伤）
- [x] 12.12 验证 trigger_on_hit_body 和 trigger_on_attack 的 EventMgr 触发

## 13. 数值首轮调参

- [x] 13.1 运行测试，观察战斗日志中 Tick 数、HP 变化趋势、伤势卡生成频率
