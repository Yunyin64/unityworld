## ADDED Requirements

### Requirement: PreStart 从真实 NPC 读取战斗数据
CombatScene.PreStart() SHALL 从大世界 Npc 实例读取：
- HP ← npc.CultivationData.HpMax
- SP ← npc.CultivationData.SpMax
- MP ← npc.CultivationData.MpMax
- 卡组 ← npc.CardData.CardIds → CardMgr.Get(id) → CombatCardState

不再使用任何硬编码默认值。

#### Scenario: 凡人 NPC 进入战斗
- **WHEN** 八大属性均为 10 的 NPC 进入战斗
- **THEN** CombatNpc 的 MaxHp=10, Sp=10, Mp=30

#### Scenario: NPC 卡组完整加载
- **WHEN** NPC 的 CardData.CardIds 有 6 张卡
- **THEN** CombatNpc 的 CardStates 列表有 6 个 CombatCardState

### Requirement: ManaPool 按五行亲和权重随机分配
CombatScene.PreStart() 或 DoManaConvert 中，ManaPool 的初始化 SHALL 使用 NPC 的 ElementalAffinity 作为权重，每点 MP 按 `概率(X) = Affinity.X / Sum(All)` 随机抽取元素类型。

#### Scenario: 偏火亲和的 NPC
- **WHEN** NPC 五行亲和为 Shui=50, Huo=150, Jin=50, Mu=50, Tu=50（火占比 43%）
- **THEN** ManaPool 中火元素数量占比接近 43%

### Requirement: 伤势卡回写到大世界卡组
战斗结束后，CombatScene SHALL 识别战斗中新增的伤势卡（CardType == Wound），将其实例 ID 追加到对应 NPC 的 CardData.CardIds。

#### Scenario: 战后获得伤势
- **WHEN** 战斗中 NPC 被打出 2 张伤势卡
- **THEN** 战斗结束后 NPC 的 CardData.CardIds 中新增这 2 张伤势卡的 ID

#### Scenario: 伤势卡持久化
- **WHEN** NPC 上次战斗获得伤势卡后再次进入战斗
- **THEN** CombatNpc 的 CardStates 中包含上次战斗的伤势卡

### Requirement: WorldMgr.RunCombat 便捷方法
WorldMgr SHALL 提供 `RunCombat(Npc npcA, Npc npcB)` 静态方法，串联完整流程：构造 CombatScene → Init → PreStart → Start → Tick 循环 → 伤势回写 → Cleanup。

#### Scenario: 一键发起战斗
- **WHEN** 调用 WorldMgr.RunCombat(npcA, npcB)
- **THEN** 完整战斗流程执行完毕，返回后双方 NPC 状态已更新（伤势卡已回写）

#### Scenario: 卡组为空时报错
- **WHEN** 某 NPC 的 CardData.CardIds 为空
- **THEN** RunCombat 打印错误日志并跳过战斗，不抛异常

### Requirement: 移除 SetupTestCombatNpc 硬编码
CombatScene SHALL 移除 SetupTestCombatNpc 方法及所有相关调用，所有战斗数据从真实 NPC 读取。

#### Scenario: 硬编码方法不存在
- **WHEN** 代码库中搜索 SetupTestCombatNpc
- **THEN** 无任何匹配

### Requirement: CombatTestRunner 使用真实 NPC
CombatTestRunner SHALL 改造为：从 NpcMgr 获取或创建两个真实 NPC，为其添加功法获得卡组，然后调用 WorldMgr.RunCombat 发起战斗。

#### Scenario: 端到端测试
- **WHEN** 运行 CombatTestRunner.RunBasicTest()
- **THEN** 使用两个有属性有卡组的真实 NPC 完成完整战斗

### Requirement: 战斗结果摘要日志
CombatLogger SHALL 在战斗结束时输出结构化摘要，包含：胜负方、总 Tick 数、每个 NPC 的最终状态（HP/SP/存活卡牌数/伤势卡数）。

#### Scenario: 战斗结束后输出摘要
- **WHEN** 战斗结束
- **THEN** 日志中包含胜负方名称、总 Tick 数、各 NPC 最终 HP/SP