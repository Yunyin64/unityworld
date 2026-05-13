## MODIFIED Requirements

### Requirement: Equip 运行时实例
系统 SHALL 提供 `Equip` 类（实现 `IFormDefine<EquipDefine>`），位于 `Scripts/Game/Domain/Object/Equip/Equip.cs`。
`Equip` 是 `EquipDefine` 的实例化载体——Define 是模板（Base 值），Equip 是最终生效的运行时对象。

`Equip` SHALL 包含以下字段：

| 字段 | 类型 | 初始值来源 | 说明 |
|---|---|---|---|
| DefineId | string | 构造传入 | 关联 EquipDefine（实现 IFormDefine） |
| DisplayName | string | 构造传入 | 显示名（实现 IFormDefine） |
| Size | int | EquipDefine.Size | 作用于哪个 Size 的卡牌 |
| Attack | int | EquipDefine.AttackBase | 攻击值（最终生效值） |
| Defend | int | EquipDefine.DefendBase | 防御值（最终生效值） |
| Speed | float | EquipDefine.SpeedBase | 速度（最终生效值） |
| Amount | int | EquipDefine.AmountBase | 数量/耐久（最终生效值） |
| FormList | List\<string\> | EquipDefine.FormListBase | 招式卡列表（最终生效值） |

Equip 实例 SHALL 由 `EquipMgr.Add(cardId, EquipDefine)` 统一创建和管理，不再直接调用 `Equip.FromDefine`。
`Equip.FromDefine` 静态方法 SHALL 保留但仅供 EquipMgr 内部使用。

Equip 实例不持有独立 ID，其身份由所挂载的 Card.Id 决定。

#### Scenario: 从 Define 创建实例（通过 EquipMgr）
- **WHEN** 调用 `EquipMgr.Add(cardId, equipDefine)`
- **THEN** 返回的 Equip 实例的 `Attack` SHALL 等于 `define.AttackBase`，`Defend` 等于 `define.DefendBase`，`Speed` 等于 `define.SpeedBase`，`Amount` 等于 `define.AmountBase`，`FormList` SHALL 为 `define.FormListBase` 的副本

#### Scenario: FormList 独立副本
- **WHEN** 从 Define 创建 Equip 实例后修改 `Equip.FormList`
- **THEN** 原始 `EquipDefine.FormListBase` SHALL 不受影响（深拷贝）
