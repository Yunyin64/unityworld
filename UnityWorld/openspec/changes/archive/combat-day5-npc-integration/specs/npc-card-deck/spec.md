## ADDED Requirements

### Requirement: NpcMgr 注册 CardDeckSystem
NpcMgr SHALL 持有 `NpcSystemCardDeck CardDeckSystem` 属性，在 Create() 中注册 NPC 的 CardData，在 Birth() 中调用 CardDeckSystem.OnEntityBorn()。

#### Scenario: NPC 创建后 CardDeckSystem 有数据
- **WHEN** NpcMgr.Create() 创建一个新 NPC
- **THEN** CardDeckSystem 的 _dataTable 中包含该 NPC 的 NpcCardData 实例

### Requirement: NpcSystemCardDeck.Register 存储数据
NpcSystemCardDeck.Register() SHALL 将 NpcCardData 存入 _dataTable，以 NPC ID 为键。

#### Scenario: 注册后可查询
- **WHEN** 调用 Register(npc, data)
- **THEN** 通过 npc.Id 可从 _dataTable 获取该 data

### Requirement: Npc 提供 CardData 访问器
Npc 的 partial class SHALL 提供 `CardData` 属性，返回 NpcMgr.Instance.CardDeckSystem 中对应的数据。

#### Scenario: 访问器返回正确数据
- **WHEN** NPC 已注册 CardData
- **THEN** npc.CardData 返回对应的 NpcCardData 实例

### Requirement: 功法添加时自动发放卡牌
CultivationMgr.AddCultivation() SHALL 在添加功法后，遍历功法定义的 Points 数组，对于 currentPoint >= threshold 且 Type == Card 的节点，调用 CardMgr.InstantiateFromDefine(RefId)，将实例化的卡牌 ID 加入 NPC 的 CardData.CardIds。

#### Scenario: 添加已完成功法获得全部卡牌
- **WHEN** NPC 添加一本 maxPoint=120 的功法，currentPoint=120，功法有 3 个 Card 类型节点
- **THEN** NPC 的 CardData.CardIds 中新增 3 张卡牌

#### Scenario: 功法未解锁的节点不发牌
- **WHEN** NPC 添加功法，currentPoint=50，仅第一个节点（threshold=30）满足
- **THEN** 仅发放 1 张卡牌

#### Scenario: 引用不存在的 CardDefine 时跳过
- **WHEN** 功法节点 refId 指向不存在的 CardDefine
- **THEN** 跳过该节点并打印警告日志，不中断流程

### Requirement: 支持多本功法叠加卡组
AddCultivation SHALL 可多次调用，每次追加新卡牌到同一个 CardData.CardIds 列表，不覆盖已有卡牌。

#### Scenario: 双修两本功法各 3 张卡
- **WHEN** NPC 先添加功法 A（3 张卡），再添加功法 B（3 张卡）
- **THEN** NPC 的 CardData.CardIds 共有 6 张卡牌