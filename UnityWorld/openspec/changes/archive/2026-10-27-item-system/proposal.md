## Why

修仙游戏缺少"物品"概念——灵石、丹药、矿石、酒杯等物质实体无法被表达。现有 Card 系统已有 Equip 和 GongFa 两种身份数据面，但没有 Item 面。同时战斗侧已有 ConsumeStack 消耗机制，但世界侧缺少对应的堆叠存储。需要补全 Item 作为 Card 的第三种身份数据面，形成完整的物品-背包-交易基础。

## What Changes

- 新增 `ItemDefine` 静态定义层（Value、Element、物理词条等模板数据）
- 新增 `ItemDefineMgr` 数据管理器（加载 JSON、提供查询）
- 新增 `Item` 运行时实例（可变状态：价值、词条、充能等，具体字段后续扩展）
- 新增 `ItemMgr` 全局管理器（扁平表 cardId → Item，与 EquipMgr/GongFaMgr 同构）
- 新增 `CardItemData` + `CardSystemItem`（Card 上的 Item 身份壳子）
- `CardBaseData` 新增 `Stack` / `StackMax` 字段（通用消耗堆叠机制，Consume keyword 时生效）
- `NpcInventoryData` / `NpcSystemInventory` 改为纯查询接口（筛出 NPC 所有 Item 卡，为背包玩法预留）

## Capabilities

### New Capabilities
- `item-define`: Item 静态定义与数据加载（ItemDefine + ItemDefineMgr）
- `item-runtime`: Item 运行时实例与管理器（Item + ItemMgr + CardItemData + CardSystemItem）
- `consume-stack`: Card 通用消耗堆叠机制（CardBaseData.Stack/StackMax，世界侧与战斗侧联动）
- `inventory-query`: NPC 背包查询接口（NpcInventoryData 改造为 Item 卡筛选视图）

### Modified Capabilities


## Impact

- `Scripts/Game/Data/Defines/ItemDefine.cs` — 新增
- `Scripts/Game/Data/Mgr/ItemDefineMgr.cs` — 新增
- `Scripts/Game/Domain/Object/Item/Item.cs` — 新增
- `Scripts/Game/Domain/Object/Item/ItemMgr.cs` — 新增
- `Scripts/Game/Domain/Object/Card/Data/CardItemData.cs` — 新增
- `Scripts/Game/Domain/Object/Card/Systems/CardSystemItem.cs` — 新增
- `Scripts/Game/Domain/Object/Card/Data/CardBaseData.cs` — 修改（加 Stack/StackMax）
- `Scripts/Game/Domain/Object/Npc/Data/NpcInventoryData.cs` — 改造
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemInventory.cs` — 改造
- `Scripts/Game/Domain/Object/Card/CardMgr.cs` — 注册 CardSystemItem
- `Scripts/Game/World/WorldMgr.cs` — 注册 ItemMgr
- `Data/Item/` — 新增 JSON 目录
