## Why

游戏需要物品系统。物品（Item）是独立实体，与 Equip 平级，本质也是一张 Card。
玩家打开卡组界面看到所有 Card（功法/物品/装备），物品通过 Tags 分类，无需 ItemType enum。
材料类物品需要五行亲和（ElementalAffinity）与物理属性亲和（PhysicalAffinity）来支撑炼丹/炼器玩法。
PhysicalAffinity 是一个新 struct，作为开放属性容器。

## What Changes

- 新增 `Item` 运行时实体（GameEntityBase, IFormDefine<ItemDefine>），与 Equip 平行
- 新增 `ItemDefine` 静态数据定义 + `ItemDefineMgr` 加载器
- 新增 `ItemMgr`（DomainMgrBase<Item>）管理所有 Item 实例
- 新增 `CardItemData` + `CardSystemItem`，在 Card 层桥接 Item
- 新增 `PhysicalAffinity` struct（Dictionary<string,int> 包装，Mod 友好开放 key）
- Card 通用层 `CardBaseData` 新增 `ConsumeStack` 字段（堆叠机制，物品/装备/战斗卡共享）
- `CardMgr` 注册新的 ItemSystem 子系统
- `GameDataMgr` 注册 `ItemDefineMgr`
- `WorldMgr` 注册 `ItemMgr`

## Capabilities

### New Capabilities
- `item-entity`: Item 运行时实体与 ItemMgr 管理器
- `item-define`: ItemDefine 静态数据定义与加载
- `card-item-bridge`: Card 层对 Item 的桥接（CardItemData / CardSystemItem）
- `physical-affinity`: PhysicalAffinity struct，通用物理属性亲和容器
- `card-consume-stack`: CardBaseData.ConsumeStack 堆叠机制

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/Object/Item/` — 新文件：Item.cs, ItemMgr.cs
- `Scripts/Game/Domain/Object/Card/Data/` — 新文件：CardItemData.cs
- `Scripts/Game/Domain/Object/Card/Systems/` — 新文件：CardSystemItem.cs
- `Scripts/Game/Domain/Object/Card/Data/CardBaseData.cs` — 新增 ConsumeStack 字段
- `Scripts/Game/Domain/Object/Card/CardMgr.cs` — 注册 ItemSystem
- `Scripts/Game/Data/Defines/` — 新文件：ItemDefine.cs
- `Scripts/Game/Data/Mgr/` — 新文件：ItemDefineMgr.cs
- `Scripts/Game/Data/GameDataMgr.cs` — 注册 ItemDefineMgr
- `Scripts/Game/World/WorldMgr.cs` — 注册 ItemMgr
- `Scripts/Core/Base/` 或 `Scripts/Game/Data/` — 新文件：PhysicalAffinity.cs
- `Data/` — 新 JSON 数据目录：Item/
