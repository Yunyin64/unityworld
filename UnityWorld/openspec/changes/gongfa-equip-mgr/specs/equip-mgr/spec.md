## ADDED Requirements

### Requirement: EquipMgr 扁平全局表
系统 SHALL 提供 `EquipMgr` 类（实现 `IDomainMgrBase`），以扁平字典 `Dictionary<int, Equip>` 持有所有运行时 Equip 实例，key 为对应 Card.Id。

EquipMgr SHALL 提供以下核心 API：

| 方法 | 签名 | 说明 |
|------|------|------|
| Add | `Equip Add(int cardId, EquipDefine define)` | 从 Define 创建 Equip 实例并注册，返回实例 |
| Remove | `void Remove(int cardId)` | 从全局表移除 |
| Get | `Equip Get(int cardId)` | 按 cardId 查询，不存在返回 null |
| GetAll | `IEnumerable<Equip> GetAll()` | 遍历所有实例 |

EquipMgr SHALL 通过 `static Instance` 属性提供单例访问。

#### Scenario: Add 创建并注册
- **WHEN** 调用 `EquipMgr.Add(cardId, equipDefine)`
- **THEN** SHALL 返回从 define 创建的 Equip 实例，且 `EquipMgr.Get(cardId)` SHALL 返回同一实例

#### Scenario: Remove 后查询返回 null
- **WHEN** 调用 `EquipMgr.Remove(cardId)`
- **THEN** `EquipMgr.Get(cardId)` SHALL 返回 null

#### Scenario: Get 不存在的 cardId
- **WHEN** 查询一个从未注册的 cardId
- **THEN** `Get` SHALL 返回 null，不抛异常

### Requirement: EquipMgr 生命周期
EquipMgr SHALL 在 `WorldMgr.Initialize()` 中注册到 `_domains` 列表，且排在 CardMgr 之后。
`End()` 时 SHALL 清空内部字典并将 Instance 置为 null。

#### Scenario: 注册顺序
- **WHEN** `WorldMgr.Initialize()` 执行完毕
- **THEN** `EquipMgr.Instance` SHALL 不为 null，且在 `_domains` 列表中位于 CardMgr 之后

#### Scenario: End 清理
- **WHEN** 调用 `EquipMgr.End()`
- **THEN** 内部字典 SHALL 为空，`Instance` SHALL 为 null

### Requirement: Card.IsEquipCard 查询
`Card` 类 SHALL 提供 `IsEquipCard` 属性，通过 `EquipMgr.Instance?.Get(Id) != null` 判断。
`Card` 类 SHALL 新增 `CardEquipData EquipData` 属性（可为 null，null = 非装备卡）。

#### Scenario: 是装备卡
- **WHEN** 某 Card 的 Id 在 EquipMgr 中已注册
- **THEN** `card.IsEquipCard` SHALL 返回 true

#### Scenario: 非装备卡
- **WHEN** 某 Card 的 Id 在 EquipMgr 中未注册
- **THEN** `card.IsEquipCard` SHALL 返回 false

### Requirement: CardEquipData 数据类
系统 SHALL 提供 `CardEquipData` 类（实现 `IDomainDataBase`），位于 `Scripts/Game/Domain/Object/Card/Data/CardEquipData.cs`。

CardEquipData SHALL 持有 `int CardId` 字段（创建时传入），所有便捷方法无需传参。

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| GetEquip() | Equip | 从 EquipMgr 获取装备实例 |

#### Scenario: GetEquip 无参查询
- **WHEN** CardEquipData 的 CardId 在 EquipMgr 中已注册
- **THEN** `GetEquip()` SHALL 返回对应 Equip 实例，无需传 cardId 参数
