## 1. Data 层 — ItemDefine

- [x] 1.1 创建 `Scripts/Game/Data/Defines/ItemDefine.cs`：继承 DefineBase，包含 ID、DisplayName 字段（骨架）
- [x] 1.2 创建 `Scripts/Game/Data/Mgr/ItemDefineMgr.cs`：继承 DefineMgrBase<ItemDefine>，构造函数接收 path，设置 Instance 单例
- [x] 1.3 在 GameDataMgr 中注册 ItemDefineMgr，加载路径为 "Item"
- [x] 1.4 确保 `Data/Item/` 目录存在

## 2. Domain 层 — Item + ItemMgr

- [x] 2.1 创建 `Scripts/Game/Domain/Object/Item/Item.cs`：继承 GameEntityBase，实现 IFormDefine<ItemDefine>，包含 Id、DefineId、DisplayName 字段
- [x] 2.2 创建 `Scripts/Game/Domain/Object/Item/ItemMgr.cs`：继承 DomainMgrBase<Item>，实现 ISoulBase，提供 InstantiateFromDefine 方法
- [x] 2.3 在 WorldMgr.Initialize() 中注册 ItemMgr

## 3. Card 挂载层 — CardItemData + CardSystemItem

- [x] 3.1 创建 `Scripts/Game/Domain/Object/Card/Data/CardItemData.cs`：实现 IDomainDataBase，包含 CardId，提供 GetItem() 方法
- [x] 3.2 在 CardItemData.cs 中扩展 partial Card：添加 IsItemCard 属性和 GetItem() 方法
- [x] 3.3 创建 `Scripts/Game/Domain/Object/Card/Systems/CardSystemItem.cs`：继承 CardSystemBase<CardItemData>
- [x] 3.4 在 CardMgr 中注册 CardSystemItem（添加 ItemSystem 属性并在初始化时创建）

## 4. 通用消耗堆叠 — CardBaseData.Stack

- [x] 4.1 在 CardBaseData 中添加 Stack（int，默认0）和 StackMax（int，默认0）字段
- [x] 4.2 在 CardBaseData.Clone() 中确保 Stack/StackMax 被正确复制（值类型，MemberwiseClone 已覆盖）
- [x] 4.3 在 Card partial 中添加 GetStack() 和 GetStackMax() 便捷访问器
- [x] 4.4 确认 CardDefine 加载流程支持 StackMax 字段写入 CardBaseData

## 5. NpcInventoryData 改造

- [x] 5.1 移除 NpcInventoryData 中的 `List<string> ItemIds` 字段
- [x] 5.2 添加查询方法：通过 NpcId 从 NpcCardData 中筛选 IsItemCard 的 Card 列表
- [x] 5.3 修复 Clone() 实现（当前抛 NotImplementedException）
- [x] 5.4 补全 Log() 输出
