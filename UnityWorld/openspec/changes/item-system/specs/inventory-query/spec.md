## ADDED Requirements

### Requirement: NpcInventoryData 改为查询接口
`NpcInventoryData` SHALL 移除 `List<string> ItemIds` 字段，改为提供查询方法从 NPC 的 CardData 中筛选所有 Item 卡。

#### Scenario: 获取 NPC 所有 Item 卡
- **WHEN** 调用 NpcInventoryData 的查询方法
- **THEN** 返回该 NPC 的 AllCards 中所有 `IsItemCard == true` 的 Card 列表

#### Scenario: NPC 没有 Item 卡
- **WHEN** NPC 不持有任何 Item 卡
- **THEN** 查询返回空列表

### Requirement: NpcSystemInventory 保留为背包预留
`NpcSystemInventory` SHALL 保留作为 NpcMgr 的子系统，OnTick 暂为空实现，为未来背包玩法逻辑预留扩展点。

#### Scenario: 系统存在但无副作用
- **WHEN** NpcSystemInventory.OnTick 被调用
- **THEN** 不产生任何副作用，不报错
