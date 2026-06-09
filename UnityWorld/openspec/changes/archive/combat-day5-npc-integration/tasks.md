## 1. Phase 0：属性基础设施

- [x] 1.1 修改 `NpcCultivationData` 中 `Properties` 初始值：从 `BaseProperty.Zero` 改为全 10（QiXue=10, TiPo=10, QiGan=10, LingJi=10, ShenShi=10, WuXing=10, JiYuan=10, MeiLi=10）
- [x] 1.2 在 `NpcCultivationData` 中新增 `RecalcCombatStats()` 方法：`HpMax = Properties.QiXue`, `MpMax = Properties.QiGan * 3`, `SpMax = Properties.ShenShi`
- [x] 1.3 在 `NpcCultivationData` 中新增 `CalcAffinityFromSoul(SoulData soul)` 方法：水=FI+FE, 火=NI+NE, 金=TI+TE, 木=SI+SE, 土=MI+ME
- [x] 1.4 在 `NpcSystemCultivation.OnEntityBorn` 中调用 `RecalcCombatStats()` + `CalcAffinityFromSoul(npc.Soul)`，保证 NPC 出生后即有正确的战斗三维和五行亲和
- [x] 1.5 验证：在 NPC 创建后打印 `HpMax/MpMax/SpMax` 和五行亲和，确认公式正确（凡人基准：HpMax=10, MpMax=30, SpMax=10，五行亲和由 Soul 决定不全为 0）

## 2. Phase 1：卡组基础设施

- [x] 2.1 在 `NpcMgr` 中添加 `public NpcSystemCardDeck CardDeckSystem { get; } = new();` 属性
- [x] 2.2 在 `NpcMgr.Create()` 中为新 NPC 创建 `NpcCardData` 实例，调用 `CardDeckSystem.Register(npc, data)` 注册
- [x] 2.3 实装 `NpcSystemCardDeck.Register()`：将 data 存入 `_dataTable[npc.Id]`
- [x] 2.4 在 `NpcMgr` 中添加 `GetCardData(int npcId)` 公开查询方法，从 CardDeckSystem._dataTable 获取
- [x] 2.5 在 `Npc.cs` 的 partial class 中添加 `CardData` 属性访问器：`NpcMgr.Instance?.GetCardData(Id)`
- [x] 2.6 修改 `CultivationMgr.AddCultivation()`：添加功法后，遍历功法定义的 Points 数组，对 `currentPoint >= threshold` 且 `Type == Card` 的节点，调用 `CardMgr.InstantiateFromDefine(RefId)` 并将 `card.Id` 加入 `npc.CardData.CardIds`；RefId 找不到时打印警告跳过
- [x] 2.7 在 `NpcMgr.Birth()` 中添加 `CardDeckSystem.OnEntityBorn(ctx)` 调用
- [ ] 2.8 验证：创建 NPC + 添加 2 本 realmLevel=1 功法（如 `ling_flame_heart` + `ling_golden_blade`）→ 确认 `CardData.CardIds` 有 6 张卡牌

## 3. Phase 2：战斗接通

- [x] 3.1 修改 `CombatScene.Init()`：接受大世界 `Npc` 实例列表而非纯 id+team，内部保存 Npc 引用
- [x] 3.2 修改 `CombatScene.PreStart()`：从 `npc.CultivationData` 读取 HP=HpMax, SP=SpMax, MP=MpMax，替换硬编码
- [x] 3.3 修改 `CombatScene.PreStart()`：从 `npc.CardData.CardIds` 构建 CombatCardState 列表（CardIds → `CardMgr.Get(id)` → 构造 CombatCardState），替换硬编码
- [x] 3.4 修改 `DoManaConvert`：读取 `npc.CultivationData.Affinity` 的五行权重，每点 MP 按权重比例随机抽取元素类型分配到 ManaPool
- [x] 3.5 新增伤势卡回写逻辑：战斗结束后，遍历每个 CombatNpc 的 CardStates，识别新增的伤势卡（CardType == Wound 且不在战前 CardIds 中），将其 ID 追加到对应大世界 `npc.CardData.CardIds`
- [x] 3.6 在 `WorldMgr` 中新增 `RunCombat(Npc npcA, Npc npcB)` 静态方法：构造 CombatScene → Init → PreStart → Start → Tick 循环 → 伤势回写 → ExportLog → Cleanup；若任一 NPC 卡组为空则打印错误并跳过
- [x] 3.7 移除 `CombatScene.SetupTestCombatNpc()` 方法及所有调用

## 4. Phase 3：CombatTestRunner 改造

- [x] 4.1 改造 `CombatTestRunner.RunBasicTest()`：不再自行随机抽卡，改为从 `NpcMgr` 获取或创建两个真实 NPC
- [x] 4.2 在 RunBasicTest 中为测试 NPC 各添加 2 本 realmLevel=1 功法（如 TeamA: `wu_tiger_fist` + `wu_stone_body`，TeamB: `ling_flame_heart` + `hun_frost_mind`）
- [x] 4.3 调用 `WorldMgr.RunCombat(npcA, npcB)` 执行战斗
- [x] 4.4 战斗结束后打印双方 NPC 的 CardData.CardIds 变化（战前卡数 vs 战后卡数），验证伤势卡已回写

## 5. Phase 4：战斗 Log 增强

- [x] 5.1 在 `CombatLogger` 中新增 `LogCombatSummary()` 方法：输出胜负方、总 Tick 数、每个 NPC 的最终 HP/SP/存活卡数/伤势卡数
- [x] 5.2 在战斗结束流程中调用 `LogCombatSummary()`
- [ ] 5.3 验证：运行 CombatTestRunner，确认日志输出完整、结构化、可读
