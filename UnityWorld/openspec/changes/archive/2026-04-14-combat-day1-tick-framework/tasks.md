## 1. 枚举与基础类型

- [x] 1.1 EnumTypes.cs — 新增 `CardType` 枚举（ZhaoShi/FaShu/FaBao/DanYao/ZhenFa/ShenTong），含 XML 注释
- [x] 1.2 EnumTypes.cs — 新增 `PhysicalType` 枚举（Zhan/Ci/Da/SheJi），含 XML 注释
- [x] 1.3 EnumTypes.cs — `CombatEndReason` 新增 `SpaceOverflow` 值，加注释"卡组空间溢出判负"
- [x] 1.4 EnumTypes.cs — `CombatantStatus.Defeated` 注释更新为"已被击败（SP溢出等）"
- [x] 1.5 EnumTypes.cs — 新增 `DamageSourceType` 枚举（Contest/DirectHit/Injury/Effect），含 XML 注释

## 2. DamageInfo 重构 DamageInfo

- [x] 2.1 将 DamageInfo.cs 的内容清空，重写为 DamageInfo 类（文件保留，内容替换）
- [x] 2.2 DamageInfo 继承 ContextBase，声明 namespace UnityWorld.Game.Domain.Combat
- [x] 2.3 DamageInfo — 来源因果强类型属性：Source/Target(CombatNpc)、SourceCard/TargetCard(CombatCardState?)、SourceType(DamageSourceType)、Tick(int)
- [x] 2.4 DamageInfo — 数值过程强类型属性：SourceValue/TargetValue/DeltaValue(float)、SourceContestType/TargetContestType(string)、SourceElement(BaseElementType)、SourcePhysicalType(PhysicalType)
- [x] 2.5 DamageInfo — 结算结果强类型属性：FinalDamage/FinalHeal(float)、IsDirectHit/IsWinnerTakesAll/HpZeroed(bool)、InjuryCard(CardData?)
- [x] 2.6 DamageInfo — 静态工厂方法 CreateContest(source, target, sourceCard, targetCard, tick)
- [x] 2.7 DamageInfo — 静态工厂方法 CreateDirectHit(source, target, sourceCard, tick)
- [x] 2.8 DamageInfo — 静态工厂方法 CreateInjurySelfDamage(npc, injuryCard, tick)

## 3. CombatCardState 新类

- [x] 3.1 新建 `Scripts/Game/Domain/Combat/CombatCardState.cs` 文件，namespace Combat
- [x] 3.2 字段：`CardData Card`（readonly，构造函数注入）
- [x] 3.3 字段：`int CurrentCdTick`（当前CD进度，初始0）
- [x] 3.4 字段：`bool IsManaFulfilled`（初始true，⏳Day2接入Mana逻辑）
- [x] 3.5 字段：`bool IsActive`（卡是否可用，初始true）
- [x] 3.6 方法：`bool TickCd()` — CurrentCdTick++，返回 CurrentCdTick >= Card.Cooldown
- [x] 3.7 方法：`void ResetCd()` — CurrentCdTick = 0
- [x] 3.8 方法：`float GetCooldown()` — 返回 Card.Cooldown
- [x] 3.9 ⏳ 方法：`float GetContestValue()` — 占位返回 Card.ContestValue；Day2→从ActionDefine汇总
- [x] 3.10 ⏳ 方法：`string GetContestType()` — 占位返回 Card.ContestType；Day2→从ActionDefine判断
- [x] 3.11 ⏳ 方法：`PhysicalType GetPhysicalType()` — 占位返回 Card.PhysicalType；Day2→从ActionDefine读
- [x] 3.12 ⏳ 方法：`BaseElementType GetElement()` — 占位返回 BaseElementType.None；Day2→从ActionDefine读
- [x] 3.13 方法：`bool IsAttackDefenseCard()` — GetContestType() != ""
- [x] 3.14 构造函数：`CombatCardState(CardData card)`

## 4. CardData 临时战斗字段

- [x] 4.1 ⏳ CardData 新增 `float ContestValue`（默认0），Day2移除
- [x] 4.2 ⏳ CardData 新增 `string ContestType`（默认""），Day2移除
- [x] 4.3 ⏳ CardData 新增 `PhysicalType PhysicalType`（默认Zhan），Day2移除
- [x] 4.4 ⏳ CardData 新增 `CardType CardType`（默认ZhaoShi），Day2→正式字段从CardDefine同步

## 5. CombatNpc 重构 — 移除旧机制

- [x] 5.1 移除 `List<CardData> DeckSequence` 字段
- [x] 5.2 移除 `int CurrentDeckIndex` 属性
- [x] 5.3 移除 `int CycleCount` 属性
- [x] 5.4 移除 `void AdvanceDeckIndex()` 方法
- [x] 5.5 移除 `CardData? CurrentCard` 属性
- [x] 5.6 移除 `void ResetDeckPointer()` 方法

## 6. CombatNpc 重构 — 新增字段与方法

- [x] 6.1 新增 `List<CombatCardState> CardStates` 字段（运行时卡组）
- [x] 6.2 新增 `int Sp` 属性（卡组空间上限）
- [x] 6.3 新增 `float MaxHp` 属性（HP上限，外伤恢复用）
- [x] 6.4 新增 `CombatCardState? PendingSlot` 字段（待发槽，上限1）
- [x] 6.5 新增 `int PendingSlotLimit = 1`（预留可扩展）
- [x] 6.6 ⏳ 新增 `float Mp` 属性（蓝条占位），Day2→接入Mana系统
- [x] 6.7 ⏳ 新增 `Dictionary<BaseElementType, int> ManaPool` 字段（灵元池占位），Day2→接入
- [x] 6.8 方法：`int GetTotalCost()` — 遍历 CardStates 累加 Card.Cost
- [x] 6.9 方法：`bool IsSpOverflow()` — GetTotalCost() > Sp
- [x] 6.10 方法：`void AddCardState(CombatCardState state)` — 运行时加卡（外伤用）
- [x] 6.11 修改 `SnapshotHp(float hp)` — 同时设置 MaxHp = hp
- [x] 6.12 修改 `ApplyDamage(float amount)` — 移除 HP≤0→Defeated 逻辑，改为返回 bool（true=HP清零）
- [x] 6.13 方法：`void InitCardStates(List<CardData> cards)` — 从 CardData 列表构建 CombatCardState 列表
- [x] 6.14 修改 `ToString()` — 反映新字段（CardStates.Count / Sp / PendingSlot）

## 7. CombatScene 重构 — 移除旧机制

- [x] 7.1 移除 `_turnIndex` 字段
- [x] 7.2 移除 `GetNextActor()` 方法
- [x] 7.3 移除 `AdvanceTurnIndex()` 方法
- [x] 7.4 移除 `NextTurn()` 方法（旧主循环）
- [x] 7.5 移除 `ExecuteCardVsCard()` 方法（旧结算）
- [x] 7.6 移除 `BuildDeckSequence()` 方法

## 8. CombatScene 重构 — 重命名与参数调整

- [x] 8.1 `MaxTurns` → `MaxTicks`
- [x] 8.2 `CurrentTurn` → `CurrentTick`
- [x] 8.3 `Init()` 参数 maxTurns → maxTicks
- [x] 8.4 所有日志中的"回合" → "Tick"

## 9. CombatScene 重构 — Tick主循环

- [x] 9.1 新增 `Tick()` 方法骨架（AssertPhase Running、CurrentTick++）
- [x] 9.2 Tick Step1：`TickAllCards()` — 遍历所有存活NPC所有活跃卡，调用 TickCd()
- [x] 9.3 Tick Step2：`CollectReadyCards()` — 收集本Tick CD就绪的卡，返回 List<(CombatNpc, CombatCardState)>
- [x] 9.4 Tick Step3：处理效果卡就绪 — 筛选 !IsAttackDefenseCard()，调用 ResolveEffectCard()，重置CD
- [x] 9.5 Tick Step4：处理攻防卡就绪 — 筛选 IsAttackDefenseCard()，调用 TryPushToPendingSlot()
- [x] 9.6 Tick Step5：`CheckSpOverflow()` — SP溢出检查
- [x] 9.7 Tick Step6：`CheckEndConditions()` — 战斗结束检查（Tick上限 + 一方全灭）

## 10. CombatScene 重构 — PreStart/Start 修改

- [x] 10.1 PreStart：移除 BuildDeckSequence 调用
- [x] 10.2 PreStart：移除按速度排序行动顺序（Tick制不需要）
- [x] 10.3 ⏳ PreStart：HP快照改用硬编码或构造参数，Day5→读真实Npc
- [x] 10.4 ⏳ PreStart：SP初始化硬编码，Day5→读 Npc.GetSpMax()
- [x] 10.5 ⏳ PreStart：CardStates初始化（空列表或测试数据），Day5→读Npc卡组
- [x] 10.6 ⏳ PreStart：Mp/ManaPool初始化占位，Day2→接入Mana系统
- [x] 10.7 PreStart：MaxHp初始化（= HP快照值）
- [x] 10.8 Start：移除出招表校验，改为 CardStates 校验
- [x] 10.9 ⏳ 新增 `SetupTestCombatNpc(CombatNpc npc, List<CardData> cards, float hp, int sp)` 测试辅助方法

## 11. 待发槽机制

- [x] 11.1 新增 `TryPushToPendingSlot(CombatNpc npc, CombatCardState card)` 方法
- [x] 11.2 待发槽空 → 卡入槽（npc.PendingSlot = card）
- [x] 11.3 入槽后调用 `CheckAndTriggerContest(npc)` 检查双方待发槽
- [x] 11.4 待发槽满 → 挤出旧卡，调用 `ExecuteDirectHit(npc, oldCard, target)`
- [x] 11.5 挤出后新卡入槽
- [x] 11.6 新卡入槽后再次调用 `CheckAndTriggerContest(npc)`
- [x] 11.7 新增 `CheckAndTriggerContest(CombatNpc attacker)` — 检查自己和Target PendingSlot，双方都有卡则调用 ResolveContest

## 12. 对拼结算

- [x] 12.1 新增 `ResolveContest(CombatNpc npcA, CombatNpc npcB)` 方法
- [x] 12.2 从双方 PendingSlot 取卡，构建 DamageInfo（CreateContest工厂）
- [x] 12.3 数值比较：确定赢方/输方/平局（比较 GetContestValue）
- [x] 12.4 平局处理：两卡消耗，DamageInfo.FinalDamage = 0
- [x] 12.5 赢方攻击卡：差值→输方 ApplyDamage，DamageInfo.FinalDamage = 差值
- [x] 12.6 赢方盾卡：差值→赢方 ApplyHeal，DamageInfo.FinalHeal = 差值
- [x] 12.7 赢方防卡：差值消失，DamageInfo 记录但无效果
- [x] 12.8 赢家通吃判定：双方攻击卡 + 同PhysicalType + 非SheJi
- [x] 12.9 赢家通吃执行：赢方全额数值伤害（DamageInfo.IsWinnerTakesAll = true）
- [x] 12.10 对拼后清空双方 PendingSlot，重置被消耗卡的CD
- [x] 12.11 对拼后检查 HP清零 → 走伤势流程

## 13. 直击处理

- [x] 13.1 新增 `ExecuteDirectHit(CombatNpc attacker, CombatCardState card, CombatNpc target)` 方法
- [x] 13.2 构建 DamageInfo（CreateDirectHit工厂），FinalDamage = 全额 GetContestValue
- [x] 13.3 对 target 调用 ApplyDamage
- [x] 13.4 直击后检查 HP清零 → 走偏方流程
- [x] 13.5 直击后重置卡的CD

## 14. SP溢出判负

- [x] 14.1 新增 `CheckSpOverflow()` — 遍历所有存活NPC
- [x] 14.2 溢出 → Status = Defeated
- [x] 14.3 溢出 → 日志输出 SP溢出信息
- [x] 14.4 溢出后调用 CheckEndConditions 检查战斗结束

## 15. HP清零→伤势

- [x] 15.1 新增 `HandleHpZero(CombatNpc npc, DamageInfo ctx)` 方法
- [x] 15.2 根据伤害数值映射伤势严重度（⏳简单阈值：<=10轻伤/<=25中伤/>25重伤）
- [x] 15.3 ⏳ 新增 `CreateInjuryCard(int severity)` — 硬编码 CardData（Cost=severity, CD=2, ContestType=""），Day3→从先机卡Define查询
- [x] 15.4 伤势卡包装为 CombatCardState 塞入 npc.CardStates
- [x] 15.5 HP恢复：npc.Hp = npc.MaxHp * 0.5f
- [x] 15.6 更新 DamageInfo：HpZeroed=true, InjuryCard=生成的卡
- [x] 15.7 日志输出伤势产生信息

## 16. 效果卡结算（占位）

- [x] 16.1 ⏳ 新增 `ResolveEffectCard(CombatNpc npc, CombatCardState card)` — 占位：仅日志输出"效果卡[CardDefineId]触发"
- [x] 16.2 效果卡结算后重置CD

## 17. CombatResult 扩展

- [x] 17.1 CombatantResult 新增 `List<CardData> InjuryCards` 字段（初始空列表）
- [x] 17.2 CombatResult `TotalTurns` → `TotalTicks`
- [x] 17.3 EndCombat() 中收集每个NPC的伤势卡写入 CombatantResult.InjuryCards
- [x] 17.4 EndCombat() 支持 SpaceOverflow EndReason

## 18. 父变更 tasks.md 回填追踪

- [x] 18.1 在 `combat-system-v3/tasks.md` Day2 追加回填任务：CombatCardState.GetContestValue/GetContestType/GetPhysicalType/GetElement 改为从 ActionDefine 汇总 ✅ Day2已完成
- [x] 18.2 在 Day2 追加：移除 CardData 临时字段（ContestValue/ContestType/PhysicalType），CardType 保留为正式字段 ✅ Day2已完成
- [x] 18.3 在 Day2 追加：CombatNpc.Mp/ManaPool 接入 Mana 系统 ✅ Day2已完成
- [ ] 18.4 在 Day2 追加：ResolveEffectCard 接入完整 Effect 执行逻辑 → 推迟至 Day4
- [ ] 18.5 在 Day2 追加：CombatScene Tick 中效果卡结算替换占位实现 → 推迟至 Day4
- [ ] 18.6 在 Day3 追加：CreateInjuryCard 从灵药卡模板 Define 查询替换硬编码
- [ ] 18.7 在 Day3 追加：伤势严重度映射从 Define 规则替代硬编码阈值
- [ ] 18.8 在 Day5 追加：CombatScene.PreStart 从真实 Npc 读取 HP/SP/MP/CardStates
- [ ] 18.9 在 Day5 追加：移除 SetupTestCombatNpc 硬编码占位
