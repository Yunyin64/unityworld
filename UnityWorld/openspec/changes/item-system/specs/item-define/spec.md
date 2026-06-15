## ADDED Requirements

### Requirement: ItemDefine 静态定义结构
系统 SHALL 提供 `ItemDefine` 类继承 `DefineBase`，包含物品的静态模板数据。初始骨架 SHALL 至少包含 `ID`（string）和 `DisplayName`（string）字段，后续可扩展 Value、Element、物理词条等。

#### Scenario: ItemDefine 最小字段
- **WHEN** 加载一条 ItemDefine JSON
- **THEN** 实例包含 ID 和 DisplayName 两个非空字段

### Requirement: ItemDefineMgr 数据管理器
系统 SHALL 提供 `ItemDefineMgr` 继承 `DefineMgrBase<ItemDefine>`，负责从 `Data/Item/` 路径加载 JSON 文件并提供 `Get(string id)` 查询。

#### Scenario: 通过 ID 查询 ItemDefine
- **WHEN** 调用 `ItemDefineMgr.Instance.Get("lingshi")`
- **THEN** 返回对应的 ItemDefine 实例

#### Scenario: 查询不存在的 ID
- **WHEN** 调用 `ItemDefineMgr.Instance.Get("not_exist")`
- **THEN** 返回 null

### Requirement: ItemDefine JSON 存放位置
ItemDefine 的 JSON 数据文件 SHALL 存放在 `Data/Item/` 目录下，与其他 Define 数据目录平级。

#### Scenario: 数据目录存在
- **WHEN** 项目初始化完成
- **THEN** `Data/Item/` 目录存在且可被 ItemDefineMgr 扫描
