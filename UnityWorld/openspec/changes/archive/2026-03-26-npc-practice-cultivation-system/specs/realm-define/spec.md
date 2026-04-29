## ADDED Requirements

### Requirement: RealmDefine 数据结构
系统 SHALL 提供 `RealmDefine` 类（继承 `DefineBase`），定义境界的静态数据。每条记录包含：
- `Type`（PracticePath）：所属道途类型
- `Level`（int）：同一道途内的境界先后顺序（1=最低境界）
- `ProgressRequired`（int）：从上一境界突破到此境界所需的境界进度总量
- `LifespanBonus`（float）：突破到此境界后获得的额外寿元加成
- `Tags`（string[]）：与 Tag 系统关联的标签

#### Scenario: RealmDefine 字段完整性
- **WHEN** 加载一条 RealmDefine 记录
- **THEN** 该记录 SHALL 包含 ID、DisplayName（继承自 DefineBase）、Type、Level、ProgressRequired、LifespanBonus、Tags 字段

### Requirement: RealmDefine 每道途独立序列
每个 `PracticePath` 类型 SHALL 拥有独立的境界序列，境界名称和属性各不相同。不同道途的相同 Level 不要求名称或数值对齐。

#### Scenario: 灵修与武修境界不同
- **WHEN** 查询 PracticePath.Ling 的 Level=1 境界
- **THEN** 返回的 DisplayName SHALL 为灵修特有的名称（如"练气"），而非武修的名称（如"锻体"）

#### Scenario: 按道途查询境界序列
- **WHEN** 查询指定 PracticePath 的所有境界
- **THEN** 系统 SHALL 返回该道途下按 Level 升序排列的完整境界列表

### Requirement: RealmDefineMgr 加载与查询
系统 SHALL 提供 `RealmDefineMgr`（实现 `IDataMgrBase<RealmDefine>`），从 `Data/Practice/RealmDefines.json` 加载数据，提供以下查询能力：
- `Get(string id)`：按 ID 查询单条境界定义
- `GetAll()`：全量查询
- `GetByPath(PracticePath path)`：按道途类型查询该道途的所有境界（按 Level 排序）
- `GetByPathAndLevel(PracticePath path, int level)`：按道途 + 等级精确查询

#### Scenario: 按道途加载境界
- **WHEN** 调用 `GetByPath(PracticePath.Ling)`
- **THEN** 系统 SHALL 返回所有 Type=Ling 的 RealmDefine，按 Level 升序排列

#### Scenario: 精确查询境界
- **WHEN** 调用 `GetByPathAndLevel(PracticePath.Wu, 2)`
- **THEN** 系统 SHALL 返回 Type=Wu 且 Level=2 的唯一 RealmDefine，不存在则返回 null

### Requirement: RealmDefines.json 示例数据
系统 SHALL 提供至少灵修（Ling）、武修（Wu）、魂修（Hun）三个道途的境界示例数据，每个道途至少 3 个境界等级。

#### Scenario: 示例数据覆盖度
- **WHEN** 加载 RealmDefines.json
- **THEN** 至少 SHALL 包含 Ling、Wu、Hun 三个道途的境界定义，每道途至少 3 个 Level