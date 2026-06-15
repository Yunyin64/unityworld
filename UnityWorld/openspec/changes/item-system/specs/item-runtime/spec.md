## ADDED Requirements

### Requirement: Item 运行时实例
系统 SHALL 提供 `Item` 类继承 `GameEntityBase` 并实现 `IFormDefine<ItemDefine>`。Item 实例 SHALL 包含：`Id`（int，= 所属 Card.Id）、`DefineId`（string，指向 ItemDefine.ID）、`DisplayName`（string）。后续可扩展可变字段。

#### Scenario: 从 ItemDefine 创建 Item 实例
- **WHEN** 调用 `ItemMgr.Instance.InstantiateFromDefine(cardId, define)`
- **THEN** 返回 Item 实例，Id = cardId，DefineId = define.ID，DisplayName = define.DisplayName

### Requirement: ItemMgr 全局管理器
系统 SHALL 提供 `ItemMgr` 继承 `DomainMgrBase<Item>` 并实现 `ISoulBase`，维护全局扁平表（cardId → Item）。提供 Add/Remove/GetById/GetAll 操作。

#### Scenario: 注册与查询 Item
- **WHEN** 通过 `ItemMgr.Instance.Add(37, item)` 注册后调用 `GetById(37)`
- **THEN** 返回同一 Item 实例

#### Scenario: ItemMgr 在 WorldMgr 中注册
- **WHEN** WorldMgr.Initialize() 执行
- **THEN** ItemMgr.Instance 非 null

### Requirement: CardItemData 壳子
系统 SHALL 提供 `CardItemData` 实现 `IDomainDataBase`，包含 `CardId` 字段，提供 `GetItem()` 方法通过 `ItemMgr.Instance.GetById(CardId)` 获取运行时实例。

#### Scenario: Card 访问 Item 数据
- **WHEN** 一张 Item 卡调用 `card.GetItem()`
- **THEN** 返回 ItemMgr 中对应的 Item 实例

#### Scenario: 非 Item 卡访问
- **WHEN** 一张非 Item 卡调用 `card.GetItem()`
- **THEN** 返回 null

### Requirement: CardSystemItem 子系统
系统 SHALL 提供 `CardSystemItem` 继承 `CardSystemBase<CardItemData>`，注册到 `CardMgr` 中作为 Item 数据面的子系统管理器。

#### Scenario: CardMgr 包含 ItemSystem
- **WHEN** CardMgr 初始化完成
- **THEN** `CardMgr.Instance.ItemSystem` 非 null

### Requirement: Card 便捷访问器
Card 类 SHALL 提供 `IsItemCard` 属性（判断 ItemMgr 中是否存在对应实例）和 `GetItem()` 方法。

#### Scenario: 判断卡牌是否为 Item 卡
- **WHEN** 一张卡有对应的 Item 实例在 ItemMgr 中
- **THEN** `card.IsItemCard` 返回 true
