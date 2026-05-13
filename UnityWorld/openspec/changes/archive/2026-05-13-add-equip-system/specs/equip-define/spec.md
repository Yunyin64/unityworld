## ADDED Requirements

### Requirement: EquipDefine 数据结构
系统 SHALL 提供 `EquipDefine` 类（继承 `DefineBase`），定义装备基本形态的静态配置数据。
`EquipDefine` SHALL 包含以下字段（除继承自 `DefineBase` 的 ID、DisplayName、Tags 外）：

| 字段 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| Size | int | 1 | 对应卡牌 Size，表示此装备属性作用于哪个 Size 的卡牌 |
| AttackBase | int | 0 | 攻击基础值（非最终计算值） |
| DefendBase | int | 0 | 防御基础值（非最终计算值） |
| SpeedBase | float | 0 | 速度基础值（非最终值） |
| AmountBase | int | 1 | 数量/耐久基础值（非最终值） |
| FormListBase | List\<string\> | [] | 附带招式卡 ID 列表基础值（引用 CardDefine.ID，非最终值） |

#### Scenario: 完整字段反序列化
- **WHEN** 加载包含所有字段的 JSON 条目
- **THEN** `EquipDefine` 实例的每个属性 SHALL 与 JSON 值一致

#### Scenario: 缺省字段使用默认值
- **WHEN** 加载仅含 `ID` 的最小化 JSON 条目
- **THEN** `Size` SHALL 为 1，`AttackBase` SHALL 为 0，`DefendBase` SHALL 为 0，`SpeedBase` SHALL 为 0，`AmountBase` SHALL 为 1，`FormListBase` SHALL 为空列表

### Requirement: EquipDefineMgr 文件夹加载
系统 SHALL 提供 `EquipDefineMgr` 类（实现 `IDataMgrBase<EquipDefine>`），从 `Data/Equip/` 文件夹下加载所有 `*.json` 文件。
每个 JSON 文件 SHALL 包含 `List<EquipDefine>` 格式的数组，Mgr 将所有文件的条目合并为统一字典。

#### Scenario: 多文件合并加载
- **WHEN** `Data/Equip/` 下有多个 JSON 文件，各含若干 EquipDefine 条目
- **THEN** `EquipDefineMgr.GetAll()` SHALL 返回所有文件中的全部条目

#### Scenario: 重复 ID 跳过
- **WHEN** 不同文件中存在相同 ID 的 EquipDefine
- **THEN** 系统 SHALL 保留先加载的条目，跳过后续重复项，并输出警告日志

#### Scenario: 空文件夹
- **WHEN** `Data/Equip/` 文件夹不存在或为空
- **THEN** `EquipDefineMgr` SHALL 正常初始化，`GetAll()` 返回空集合，并输出警告日志

### Requirement: GameDataMgr 注册
`EquipDefineMgr` SHALL 在 `GameDataMgr` 构造函数中注册，随 `Initialize()` 统一加载。

#### Scenario: 启动时自动加载
- **WHEN** 调用 `GameDataMgr.Initialize()`
- **THEN** `EquipDefineMgr` SHALL 完成加载，可通过 `EquipDefineMgr.Instance` 访问

### Requirement: Data/Equip 数据目录
系统 SHALL 包含 `Data/Equip/` 目录及至少一个 JSON 模板文件，作为装备数据的初始骨架。

#### Scenario: 模板文件可加载
- **WHEN** 使用初始模板文件启动系统
- **THEN** `EquipDefineMgr` SHALL 成功加载模板中的示例条目

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

#### Scenario: 从 Define 创建实例
- **WHEN** 调用 `Equip.FromDefine(EquipDefine define)`
- **THEN** Equip 实例的 `Attack` SHALL 等于 `define.AttackBase`，`Defend` 等于 `define.DefendBase`，`Speed` 等于 `define.SpeedBase`，`Amount` 等于 `define.AmountBase`，`FormList` SHALL 为 `define.FormListBase` 的副本，`Size` 等于 `define.Size`，`DefineId` 等于 `define.ID`，`DisplayName` 等于 `define.DisplayName`

#### Scenario: FormList 独立副本
- **WHEN** 从 Define 创建 Equip 实例后修改 `Equip.FormList`
- **THEN** 原始 `EquipDefine.FormListBase` SHALL 不受影响（深拷贝）
