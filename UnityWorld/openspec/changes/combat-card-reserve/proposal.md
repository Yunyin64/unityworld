## Why

战斗卡组目前是扁平结构——所有卡一股脑全部加载到运转池，全部占SP、全部参与CD循环。这导致：
1. 无法表达"战斗中可能用到但现在不需要"的卡牌状态
2. SP管理缺乏战术深度——玩家/AI没有主动调度卡位的操作空间
3. 大世界也缺少"哪些卡上阵、哪些卡候补"的分配概念

引入 Reserve（候补池）机制，使卡牌有"运转/候补"两种部署状态，通过 Deploy/Recall 操作在两池之间流转，增加战术层次。

## What Changes

- 大世界 `NpcCardData` 新增 `Field: List<int>` 和 `Reserve: List<int>` 两个 cardId 列表，表示卡的部署分配
- 战斗 `CombatNpc` 新增 `Reserve: List<CombatCard>` 静默池，不Tick、不占SP
- 新增 `Deploy(card)` / `Recall(card)` 操作（Reserve ↔ Field），附带 Lua hook `OnDeploy` / `OnRecall`
- 战斗初始化 `InitDeck()` 改为按 Field/Reserve 分流加载
- SP 计算只统计 Field（运转池）中的卡
- Deploy/Recall 作为 API Action 或 Keyword 驱动的显式操作暴露

## Capabilities

### New Capabilities
- `card-reserve`: 战斗卡牌候补池（Reserve）机制——卡的双池分配、Deploy/Recall 流转、SP隔离、Lua hook

### Modified Capabilities
<!-- 无现有 spec 需要修改 -->

## Impact

- `Scripts/Game/Domain/Object/Npc/Data/NpcCardData.cs` — 新增 Field/Reserve 字段
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCard.cs` — 分配/查询接口
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcCardFunc.cs` — 新增 Reserve 列表、Deploy/Recall 方法
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs` — InitDeck 分流逻辑
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs` — GetSp 保持只算 CardDeck
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs` — PreStart/Tick 不遍历 Reserve
- API Action 层 — 新增 Deploy/Recall action 函数
- `Scripts/Game/Domain/Combat/Enum_Combat.cs` — 可能新增相关枚举
