## Context

当前战斗卡组为单一 `CardDeck: List<CombatCard>`，所有卡全部参与 Tick/CD/SP 计算。大世界层 `NpcCardData` 只有扁平的 `AllCards` 列表，没有"部署位置"概念。

需要引入 Reserve 池——一个静默的候补区，卡在其中不运转、不占 SP。通过 Deploy/Recall 显式操作在两池间流转。

## Goals / Non-Goals

**Goals:**
- 大世界层为 Npc 的每张卡增加 Field/Reserve 分配
- 战斗层实现 Reserve 静默池，与现有 CardDeck（Field）并存
- Deploy/Recall 作为 Action API 暴露，支持 Lua hook
- SP 计算仅统计 Field
- 保持 AllCards 逻辑不变，Field/Reserve 是引用层

**Non-Goals:**
- 不设计 Reserve 的资格判定规则（留给后续玩法层）
- 不实现 UI/显示层
- 不修改现有 CardDeck 的命名（代码内 CardDeck = Field 语义，不做 rename）
- 不改变 AllCards 的任何现有逻辑

## Decisions

### 1. 数据存储：List<int> 引用而非独立集合

Field/Reserve 存 cardId（int），引用 AllCards 中的 Card 实例。

**理由：** AllCards 是所有权，Field/Reserve 是分配视图。一张卡只存在于 AllCards 一份，避免数据冗余和同步问题。

**替代方案：** 存 Card 引用 → 需要保证 AllCards 增删时同步清理两个列表，复杂度高。

### 2. 战斗层 Reserve 存 CombatCard 实例（非 int）

`CombatNpc.Reserve: List<CombatCard>` 直接持有实例。

**理由：** 战斗内 CombatCard 需要被 Deploy 时保留部分状态（如 Modifier、Lua env）。持有实例可直接移动，无需重新 CreateFromData。

### 3. Deploy/Recall 默认 CD 清零

Deploy 后卡从 Waiting 开始走流程；Recall 时清零 CD 进度。

**理由：** 探索讨论中确定。Lua hook（OnDeploy/OnRecall）可覆盖此行为，Keyword 也可自定义。

### 4. Deploy/Recall 作为 Action API

在 `CombatBaseAction` 同级新增 Deploy/Recall action，通过 APIContext 指定目标卡。

**理由：** 与现有 Action 体系一致（CombatBaseAction/CombatCDAction/CombatManaAction），不需要新发明机制。

### 5. InitDeck 分流而非重写

`InitDeck()` 增加按 `NpcCardData.Field` / `NpcCardData.Reserve` 分流的逻辑。若 Field/Reserve 都为空（旧数据兼容），fallback 到现有行为（全部进 CardDeck）。

**理由：** 向后兼容，旧存档/旧 NPC 数据不会崩。

## Risks / Trade-offs

- **[数据一致性]** Field/Reserve 的 cardId 必须是 AllCards 中存在的 id → 需要在 GainCard/移除卡时同步清理。→ *缓解：NpcSystemCard 的 Remove 操作中清理两个列表。*

- **[战斗中卡数量变化]** 伤势卡（Wound）战中生成时默认进 CardDeck（Field），不进 Reserve。这是正确行为但需明确。→ *缓解：AddCombatCard 现有行为不变，默认进 CardDeck。*

- **[Fallback 分支]** 旧数据 Field/Reserve 为空时全部进 CardDeck，可能导致新旧行为不一致（旧NPC没有 Reserve 概念）。→ *可接受：渐进迁移。*

- **[Deploy 打断]** Deploy 时如果 SP 已满？→ *设计选择：不做框架层限制，SP 溢出 = Defeated 是现有机制自然处理。Action 层可自行判断前置条件。*
