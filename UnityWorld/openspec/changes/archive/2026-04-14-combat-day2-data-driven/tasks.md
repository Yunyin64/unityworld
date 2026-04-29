## 1. Data 层：ActionDefine 扩展

- [x] 1.1 ActionDefine.cs 新增 `FuncName`（string，JsonPropertyName "funcName"，默认 ""）和 `Params`（List<object>，JsonPropertyName "params"，默认空列表）字段
- [x] 1.2 验证 ActionDefines.json 加载：带 funcName/params 的条目能正确反序列化，不带的条目字段为默认值

## 2. Data 层：CardDefine 扩展

- [x] 2.1 CardDefine.cs 新增 `CardType`（string，JsonPropertyName "cardType"，默认 "ZhaoShi"）字段
- [x] 2.2 CardDefine.cs 新增 `ManaCost`（Dictionary<string,int>，JsonPropertyName "manaCost"，默认空字典）字段

## 3. API 层：APIMgr 签名对齐

- [x] 3.1 APIMgr.RegisterBuiltinAPIs 中移除 Defend 签名，替换为 Shield 和 Block 两个独立签名：Shield(Element:String, PhysicalType:String, ShieldValue:Int)、Block(Element:String, PhysicalType:String, BlockValue:Int)
- [x] 3.2 验证 APIMgr.Get("Shield") 和 APIMgr.Get("Block") 返回正确签名，APIMgr.Get("Defend") 返回 null

## 4. Domain 层：新增 ActionData 运行时类

- [x] 4.1 新建 `Scripts/Game/Domain/Object/Card/Data/ActionData.cs`：字段 DefineId(string)、FuncName(string)、Context(ContextBase)
- [x] 4.2 ActionData 提供静态工厂方法 `FromDefine(ActionDefine define)`：调用 APIMgr.ParseToContext 将 define.Params 解析为 Context；若 APIMgr 未注册该 funcName 则 Context 为空 ContextBase 并 LogWarn
- [x] 4.3 ActionData 提供便捷取值方法：`GetFloat(string key, float defaultVal)` → 从 Context 读取、`GetString(string key)` → 从 Context 读取

## 5. Domain 层：EffectData 结构性变更

- [x] 5.1 EffectData.cs 新增 `Actions`（List<ActionData>）字段，替代 `ActionIds`
- [x] 5.2 EffectData.cs 保留 `ActionIds` 属性改为只读计算属性：`get => Actions.Select(a => a.DefineId).ToList()`
- [x] 5.3 EffectData 提供 `InitActions(List<ActionDefine> defines)` 方法：遍历 ActionDefine 列表，调用 ActionData.FromDefine 构造实例填入 Actions

## 6. Domain 层：CardData 字段变更

- [x] 6.1 CardData.cs 新增 `ManaCost`（Dictionary<string,int>，默认空字典）字段
- [x] 6.2 CardData.cs 移除临时占位字段：`ContestValue`、`ContestType`、`PhysicalType`（三个标记为 ⏳Day2移除 的字段）
- [x] 6.3 编译修复：移除 ContestValue/ContestType/PhysicalType 后，所有引用这三个字段的地方编译报错，逐一修复（主要在 CombatCardState）

## 7. Domain 层：新增 ContestData 临时结构

- [x] 7.1 新建 `Scripts/Game/Domain/Combat/ContestData.cs`：字段 ContestType(string)、ContestValue(float)、Element(BaseElementType)、PhysicalType(PhysicalType)、SourceCard(CombatCardState)、OwnerNpc(CombatNpc)
- [x] 7.2 ContestData 提供静态工厂方法 `FromActionData(ActionData action, CombatCardState card, CombatNpc owner)`：从 ActionData.Context 提取数值填充各字段；FuncName→ContestType 映射（"Attack"→"Attack"，"Shield"→"Shield"，"Block"→"Block"），对应 value key 为 "AttackValue"/"ShieldValue"/"BlockValue"

## 8. 战斗引擎：CombatCardState 重写拼点逻辑

- [x] 8.1 CombatCardState 新增 `BuildContestData(CombatNpc owner)` 方法：遍历 Card.Effects 的所有 ActionData，找到第一个 FuncName 为 Attack/Shield/Block 的 ActionData，调用 ContestData.FromActionData 构造返回；无则返回 null
- [x] 8.2 CombatCardState.IsAttackDefenseCard() 重写：改为调用 BuildContestData(null) != null 或直接遍历 Effects 的 Actions 检查 FuncName
- [x] 8.3 CombatCardState 移除旧方法：`GetContestValue()`、`GetContestType()`、`GetPhysicalType()`、`GetElement()`（功能已被 ContestData 替代）

## 9. 战斗引擎：PendingSlot 类型变更

- [x] 9.1 CombatNpc.PendingSlot 类型从 `CombatCardState?` 改为 `ContestData?`
- [x] 9.2 CombatSlotHandler.TryPushToPendingSlot：入槽前调用 `card.BuildContestData(npc)` 构造 ContestData，将 ContestData 放入 PendingSlot（而非 card 本身）
- [x] 9.3 SlotPushResult 中的 `OverflowedCard` 类型考虑：保持为 CombatCardState（因为直击需要完整卡信息），同时新增 `OverflowedContestData` 字段存挤出的 ContestData

## 10. 战斗引擎：对拼结算适配 ContestData

- [x] 10.1 CombatContestHandler.ResolveContest 重写：从 npcA.PendingSlot（ContestData）和 npcB.PendingSlot（ContestData）读取 ContestType/ContestValue/PhysicalType/Element，替换原来的 cardA.GetXxx() 调用
- [x] 10.2 CombatContestHandler.ExecuteDirectHit 重写：接受 ContestData 参数而非 CombatCardState，从 ContestData 读取伤害数值
- [x] 10.3 DamageInfo.CreateContest 签名变更：参数从 CombatCardState 改为 ContestData（或同时接受两者），从 ContestData 读取 SourceValue/SourceContestType/SourceElement/SourcePhysicalType
- [x] 10.4 DamageInfo.CreateDirectHit 签名变更：参数从 CombatCardState 改为 ContestData
- [x] 10.5 CombatScene.ProcessSlotResults 适配：直击和对拼调用链中传递 ContestData 而非 CombatCardState
- [x] 10.6 ClearPendingSlotsAndResetCd 方法适配：PendingSlot 清空后，通过 ContestData.SourceCard 拿到原卡重置 CD

## 11. Mana 系统框架

- [x] 11.1 CombatNpc.ManaPool 类型从 `Dictionary<BaseElementType, int>` 改为 `Dictionary<string, int>`（与 JSON ManaCost 格式一致）
- [x] 11.2 新建 `Scripts/Game/Domain/Combat/CombatManaHandler.cs`：持有 CombatScene 引用
- [x] 11.3 CombatManaHandler 实现 `TickManaConvert(int currentTick)`：每隔 N Tick（初始值 10），对每个存活 NPC 检查 Mp > 0，扣除固定 Mp（初始值 10），产生 1 个无属性灵元加入 ManaPool
- [x] 11.4 CombatManaHandler 实现 `CanAffordMana(CombatNpc npc, Dictionary<string,int> manaCost)`：检查 npc.ManaPool 中每个 key 的数量 >= manaCost 对应值
- [x] 11.5 CombatManaHandler 实现 `ConsumeMana(CombatNpc npc, Dictionary<string,int> manaCost)`：从 npc.ManaPool 中扣除
- [x] 11.6 CombatScene.InitializeHandlers 中创建 CombatManaHandler 实例
- [x] 11.7 CombatScene.Tick() 在 Step 1（TickAllCards）之前新增 Step 0：调用 _manaHandler.TickManaConvert(CurrentTick)
- [x] 11.8 CombatScene.PreStart() 中 ManaPool 初始化改为 `new Dictionary<string, int>()`

## 12. Mana 与 CardState 联动

- [x] 12.1 CombatCardState.IsManaFulfilled 改为动态计算：若 Card.ManaCost 为空则 true；否则需持有 CombatNpc 引用（或在检查时传入 NPC）来判断 CanAffordMana
- [x] 12.2 CombatCardFlowHandler.CollectReadyCards 中 Mana 检查适配：在 CD 就绪后调用 ManaHandler.CanAffordMana 判断
- [x] 12.3 CombatCardFlowHandler 处理攻防卡就绪时，CD 满且 Mana 满足 → 消耗 Mana → 入槽/直接结算

## 13. CardSystemGenerate 适配新格式

- [x] 13.1 CardSystemGenerate.BuildEffectData 方法适配：生成 EffectData 时调用 EffectData.InitActions（传入选出的 ActionDefine 列表）构造 ActionData 实例
- [x] 13.2 CardSystemGenerate.ExportToCardTemp 方法适配：EffectDefine 导出时从 EffectData.Actions 提取 ActionIds（通过只读属性）

## 14. JSON 数据更新

- [x] 14.1 Data/ActionDefines.json：为已有 Action 条目添加 funcName 和 params 字段。例如攻击类 Action 添加 `"funcName":"Attack","params":["Huo","SheJi",3]`
- [x] 14.2 Data/CardDefines.json：为已有 Card 条目添加 cardType 和 manaCost 字段
- [x] 14.3 Data/EffectDefines.json：检查 actionIds 引用的 Action 是否都有 funcName，若有缺失则补充
- [x] 14.4 bin/Debug/net8.0/Data/ 下的 JSON 文件同步更新（或确认构建自动复制）

## 15. 编译验证与全局扫描

- [x] 15.1 全量编译通过：确保所有 .cs 文件无编译错误
- [x] 15.2 全局搜索 `ContestValue`、`ContestType`（CardData 旧字段）：确认无残留引用
- [x] 15.3 全局搜索 `GetContestValue`、`GetContestType`、`GetPhysicalType`、`GetElement`（CombatCardState 旧方法）：确认无残留引用
- [x] 15.4 全局搜索 `"Defend"`（APIMgr 旧签名）：确认无残留引用（除注释外）
- [x] 15.5 WorldMgr.Initialize 中确认 APIMgr 注册顺序在 CardMgr/ActionData 构造之前

## 16. 🔄 Day2 完成后需回填到后续 Day 的事项

> 以下任务是 Day 2 数据驱动改造产生的"连锁反应"，需要在 Day 3-5 中处理。
> Day 2 实现时遇到这些点只需留占位/TODO 注释，不要在 Day 2 中实现。

### → 回填到 Day 3（基础30张卡牌设计）

- [ ] 16.1 【Day3】CombatSpilloverHandler.CreateInjuryCard 适配新结构：移除对 ContestType/ContestValue 的赋值，伤势卡的自伤效果改为包含 SelfDamage(N) 的 ActionData 实例（通过 EffectData.Actions）
- [ ] 16.2 【Day3】设计伤势卡 Define 时使用新的 funcName+params 格式，医学界 Action 为 `{"funcName":"SelfDamage","params":[N]}`
- [ ] 16.3 【Day3】30 张卡的 ActionDefines.json 全部使用 funcName+params 格式
- [ ] 16.4 【Day3】30 张卡的 CardDefines.json 全部填写 cardType + manaCost 字段

### → 回填到 Day 4（战斗流程跑通）

- [ ] 16.5 【Day4】实现 Action 执行器（ActionResolver）：至少能执行 Heal(N)→实际恢复 HP、SelfDamage(N)→实际自伤，使效果卡和伤势卡真正生效
- [ ] 16.6 【Day4】CombatCardFlowHandler.ResolveEffectCard 接入 Action 执行器：效果卡 CD 满时遍历 EffectData.Actions 调用执行器，替换当前的纯日志占位
- [ ] 16.7 【Day4】实现对拼后续结算：拼完后执行 OnUse Effect 中非拼点的 Action（如 AddPoison），检查 OnContestWin/OnContestLose Trigger 的 Effect
- [ ] 16.8 【Day4】DamageInfo.CreateInjurySelfDamage 接入 ActionData：从伤势卡的 ActionData 中读取 SelfDamage 值，替换当前的 ⏳ 占位

### → 回填到 Day 5（战斗Log与NPC接通）

- [ ] 16.9 【Day5】CombatScene.PreStart 中 Mp 从真实 NPC Stat 读取，替换硬编码 100f
- [ ] 16.10 【Day5】CombatScene.PreStart 中 ManaPool 初始化规则：根据 NPC 道途/功法决定初始灵元类型和数量
- [ ] 16.11 【Day5】移除 SetupTestCombatNpc 方法，PreStart 直接从真实 NPC 读取 HP/SP/MP/CardStates