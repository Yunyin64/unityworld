## 1. 大世界数据层

- [x] 1.1 NpcCardData 新增 `Field: List<int>` 和 `Reserve: List<int>` 字段，补 Clone/Log
- [x] 1.2 Npc partial class 新增便捷访问器：`GetField()`, `GetReserve()`, `AssignToField(cardId)`, `AssignToReserve(cardId)`, `UnassignCard(cardId)`
- [x] 1.3 NpcSystemCard 中 GainCard 后不自动分配（保持未分配状态），新增分配/取消分配方法
- [x] 1.4 NpcSystemCard 中卡被移除时同步清理 Field/Reserve 列表

## 2. 大世界 SP 计算

- [x] 2.1 修改 `GetAllCardSize()` → 只统计 Field 中卡的 Size（或新增 `GetFieldSize()`）
- [x] 2.2 确认其他引用 GetAllCardSize 的地方兼容新逻辑

## 3. 战斗层 Reserve 池

- [x] 3.1 CombatNpcCardFunc.cs 新增 `Reserve: List<CombatCard>` 字段和 `GetReserve()` 访问器
- [x] 3.2 实现 `Deploy(CombatCard card)` — 从 Reserve 移入 CardDeck，CD 清零，Phase=Waiting，调用 card.CallLua("OnDeploy")
- [x] 3.3 实现 `Recall(CombatCard card)` — 从 CardDeck 移入 Reserve，CD 清零，调用 card.CallLua("OnRecall")
- [x] 3.4 Deploy/Recall 边界处理：卡不在预期池中时输出警告日志并 return

## 4. 战斗初始化分流

- [x] 4.1 修改 `InitDeck()` — 按 NpcCardData.Field 加载到 CardDeck，按 NpcCardData.Reserve 加载到 Reserve
- [x] 4.2 Fallback 兼容：若 Field 和 Reserve 均为空，保持原逻辑（AllCards 全进 CardDeck）
- [x] 4.3 Reserve 中的 CombatCard 执行 PreStart（初始化 Lua env）但不执行 Start（不启动 CD 循环）

## 5. Action API 暴露

- [x] 5.1 新增 `CombatReserveAction.cs`（或在现有 Action 文件中追加），注册 Deploy/Recall 两个 Action 函数
- [x] 5.2 Action 签名：接收 APIContext + 目标卡标识（cardId 或 index），调用 CombatNpc.Deploy/Recall
