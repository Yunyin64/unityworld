## 1. PhysicalAffinity struct

- [ ] 1.1 创建 `Scripts/Core/Base/PhysicalAffinity.cs`，实现 struct 包装 Dictionary<string,int>，提供 Get/Set/Has/Keys/Clone/ToString
- [ ] 1.2 添加 JSON 序列化支持（System.Text.Json 兼容，直接作为 Dictionary 序列化）

## 2. CardBaseData ConsumeStack

- [ ] 2.1 在 `CardBaseData` 中新增 `ConsumeStack` 字段（int, 默认 1）
- [ ] 2.2 在 `CardDefine` 中新增 `ConsumeStack` JSON 字段（int, 默认 1）
- [ ] 2.3 在 `CardMgr.InstantiateFromDefine` 中将 CardDefine.ConsumeStack 赋值到 CardBaseData.ConsumeStack
- [ ] 2.4 在 `CardBaseData.Log()` 中输出 ConsumeStack
- [ ] 2.5 在 `CardBaseData.Clone()` 中包含 ConsumeStack

## 3. ItemDefine 静态数据

- [ ] 3.1 创建 `Scripts/Game/Data/Defines/ItemDefine.cs`（继承 DefineBase，字段：Desc/Value/UseEffect/Tags/ElementalAffinity/PhysicalAffinity/Entries）
- [ ] 3.2 创建 `Scripts/Game/Data/Mgr/ItemDefineMgr.cs`（IDataMgrBase，加载 Data/Item/）
- [ ] 3.3 在 `GameDataMgr` 中注册 `ItemDefineMgr`
- [ ] 3.4 创建 `Data/Item/` 目录并放入示例 JSON 文件

## 4. Item 运行时实体

- [ ] 4.1 创建 `Scripts/Game/Domain/Object/Item/Item.cs`（GameEntityBase, IFormDefine<ItemDefine>，字段：Id/DefineId/DisplayName/Value/ElementalAffinity/PhysicalAffinity/Entries，含 FromDefine 静态方法和 LogAllInfo）
- [ ] 4.2 创建 `Scripts/Game/Domain/Object/Item/ItemMgr.cs`（DomainMgrBase<Item>, ISoulBase，单例模式，与 EquipMgr 平行）
- [ ] 4.3 在 `WorldMgr.Initialize()` 中注册 `ItemMgr`

## 5. Card-Item 桥接

- [ ] 5.1 创建 `Scripts/Game/Domain/Object/Card/Data/CardItemData.cs`（IDomainDataBase，CardId 字段，GetItem() 方法，Log()，partial class Card 扩展 IsItemCard/ItemData）
- [ ] 5.2 创建 `Scripts/Game/Domain/Object/Card/Systems/CardSystemItem.cs`（CardSystemBase<CardItemData>）
- [ ] 5.3 在 `CardMgr` 中添加 `ItemSystem` 属性并在 InstantiateFromDefine 中注册 CardItemData
