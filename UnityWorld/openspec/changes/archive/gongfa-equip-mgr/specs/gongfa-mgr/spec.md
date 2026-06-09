## ADDED Requirements

### Requirement: GongFaMgr 扁平全局表
系统 SHALL 提供 `GongFaMgr` 类（实现 `IDomainMgrBase`），以扁平字典 `Dictionary<int, GongFa>` 持有所有运行时 GongFa 实例，key 为对应 Card.Id。

GongFaMgr SHALL 提供以下核心 API：

| 方法 | 签名 | 说明 |
|------|------|------|
| Add | `void Add(int cardId, GongFa gongFa)` | 将 GongFa 实例注册到全局表 |
| Remove | `void Remove(int cardId)` | 从全局表移除 |
| Get | `GongFa Get(int cardId)` | 按 cardId 查询，不存在返回 null |
| GetAll | `IEnumerable<GongFa> GetAll()` | 遍历所有实例 |
| GetAllWithId | `IEnumerable<KeyValuePair<int,GongFa>> GetAllWithId()` | 遍历所有实例（含 cardId） |

GongFaMgr SHALL 通过 `static Instance` 属性提供单例访问。

#### Scenario: Add 注册并 Get 查询
- **WHEN** 调用 `GongFaMgr.Add(cardId, gongFa)`
- **THEN** `GongFaMgr.Get(cardId)` SHALL 返回该 gongFa 实例

#### Scenario: Remove 后查询返回 null
- **WHEN** 调用 `GongFaMgr.Remove(cardId)`
- **THEN** `GongFaMgr.Get(cardId)` SHALL 返回 null

#### Scenario: Get 不存在的 cardId
- **WHEN** 查询一个从未注册的 cardId
- **THEN** `Get` SHALL 返回 null，不抛异常

#### Scenario: GetAll 遍历
- **WHEN** 全局表中有 N 个 GongFa 实例
- **THEN** `GetAll()` SHALL 返回恰好 N 个实例

### Requirement: GongFaMgr 生命周期
GongFaMgr SHALL 在 `WorldMgr.Initialize()` 中注册到 `_domains` 列表，且排在 CardMgr 之后。
`End()` 时 SHALL 清空内部字典并将 Instance 置为 null。

#### Scenario: 注册顺序
- **WHEN** `WorldMgr.Initialize()` 执行完毕
- **THEN** `GongFaMgr.Instance` SHALL 不为 null，且在 `_domains` 列表中位于 CardMgr 之后

#### Scenario: End 清理
- **WHEN** 调用 `GongFaMgr.End()`
- **THEN** 内部字典 SHALL 为空，`Instance` SHALL 为 null

### Requirement: NpcGongFaData 改为 CardId 索引
`NpcGongFaData` SHALL 将 `List<GongFa> AllSlots` 改为 `List<int> AllSlotCardIds`，将 `List<GongFa> ActiveSlots` 改为 `List<int> ActiveSlotCardIds`。

NpcGongFaData SHALL 提供便捷查询方法：
- `GetAllGongFa()` → 通过 AllSlotCardIds 从 GongFaMgr 获取 GongFa 实例列表
- `GetActiveGongFa()` → 通过 ActiveSlotCardIds 从 GongFaMgr 获取 GongFa 实例列表

#### Scenario: AllSlotCardIds 索引与 GongFaMgr 一致
- **WHEN** 通过 CultivationMgr 为 NPC 添加一个功法
- **THEN** 该功法卡的 cardId SHALL 同时存在于 `NpcGongFaData.AllSlotCardIds` 和 `GongFaMgr._table` 中

#### Scenario: GetAllGongFa 便捷查询
- **WHEN** `AllSlotCardIds` 包含 [cardId1, cardId2]
- **THEN** `GetAllGongFa()` SHALL 返回 [GongFaMgr.Get(cardId1), GongFaMgr.Get(cardId2)]，过滤掉 null

### Requirement: NpcPraticeData 改存 CardId
`NpcPraticeData.NowGongFaData`（类型 `GongFa`）SHALL 改为 `NowGongFaCardId`（类型 `int`，默认 -1 表示无）。

NpcPraticeData SHALL 提供便捷方法 `GetNowGongFa()` → 通过 `GongFaMgr.Get(NowGongFaCardId)` 获取实例。

#### Scenario: NowGongFaCardId 默认值
- **WHEN** NpcPraticeData 初始化
- **THEN** `NowGongFaCardId` SHALL 为 -1

#### Scenario: GetNowGongFa 查询
- **WHEN** `NowGongFaCardId` 为有效 cardId
- **THEN** `GetNowGongFa()` SHALL 返回 `GongFaMgr.Get(NowGongFaCardId)` 的结果

#### Scenario: GetNowGongFa 无功法
- **WHEN** `NowGongFaCardId` 为 -1
- **THEN** `GetNowGongFa()` SHALL 返回 null

### Requirement: Card partial 改造
`Card` 类 SHALL 移除原有的 `GongFa GongFaData` 属性（来自 GongFa.cs 的 partial）。
`Card` 类 SHALL 新增 `CardGongFaData GongFaData` 属性（可为 null，null = 非功法卡）。
`IsGongFaCard` SHALL 改为 `GongFaMgr.Instance?.Get(Id) != null`。

#### Scenario: IsGongFaCard 查询
- **WHEN** 某 Card 的 Id 在 GongFaMgr 中已注册
- **THEN** `card.IsGongFaCard` SHALL 返回 true

#### Scenario: 非功法卡
- **WHEN** 某 Card 的 Id 在 GongFaMgr 中未注册
- **THEN** `card.IsGongFaCard` SHALL 返回 false

### Requirement: CardGongFaData 数据类
系统 SHALL 提供 `CardGongFaData` 类（实现 `IDomainDataBase`），位于 `Scripts/Game/Domain/Object/Card/Data/CardGongFaData.cs`。

CardGongFaData SHALL 持有 `int CardId` 字段（创建时传入），所有便捷方法无需传参。

CardGongFaData SHALL 提供以下便捷方法：

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| GetGongFa() | GongFa | 从 GongFaMgr 获取功法实例 |
| GetUnlockedPoints() | List\<CultivationPointDefine\> | 委托到 GongFa.GetUnlockedPoints() |
| GetNextPoint() | CultivationPointDefine | 委托到 GongFa.GetNextPoint() |
| IsComplete() | bool | 委托到 GongFa.IsComplete() |

#### Scenario: GetGongFa 无参查询
- **WHEN** CardGongFaData 的 CardId 在 GongFaMgr 中已注册
- **THEN** `GetGongFa()` SHALL 返回对应 GongFa 实例，无需传 cardId 参数

#### Scenario: 便捷方法委托
- **WHEN** 调用 `cardGongFaData.IsComplete()`
- **THEN** SHALL 等价于 `GongFaMgr.Get(CardId)?.IsComplete() ?? false`

### Requirement: CultivationMgr 适配 GongFaMgr
`CultivationMgr.AddCultivation` SHALL 改为：创建 GongFa 实例后调用 `GongFaMgr.Add(card.Id, gongFa)` 并同步 `NpcGongFaData.AllSlotCardIds.Add(card.Id)`。

`CultivationMgr.RemoveCultivation` SHALL 改为：先 `GongFaMgr.Remove(cardId)` 再同步 `NpcGongFaData.AllSlotCardIds.Remove(cardId)`。

#### Scenario: 添加功法
- **WHEN** 调用 `AddCultivation(npc, defineId)`
- **THEN** GongFaMgr 全局表 SHALL 包含新 GongFa 实例，NpcGongFaData.AllSlotCardIds SHALL 包含对应 cardId

#### Scenario: 移除功法
- **WHEN** 调用 `RemoveCultivation(npc, cardId)`
- **THEN** GongFaMgr 全局表 SHALL 不再包含该 cardId，NpcGongFaData.AllSlotCardIds SHALL 不再包含该 cardId
