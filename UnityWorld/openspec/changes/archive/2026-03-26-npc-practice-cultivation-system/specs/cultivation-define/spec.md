## ADDED Requirements

### Requirement: CultivationDefine 数据结构
系统 SHALL 提供 `CultivationDefine` 类（继承 `DefineBase`），定义功法的静态数据模板。每条记录包含：
- `Desc`（string）：功法描述
- `PathType`（PracticePath）：所属道途类型
- `RealmLevel`（int）：适应境界等级（标识功法设计的目标境界，不是硬性限制）
- `MaxPoint`（int）：修炼点数上限
- `Completeness`（float, 0~1）：完整度，影响节点分布密度
- `Points`（CultivationPointDefine[]）：节点序列
- `CoreEffect`（CultivationCoreEffect）：装备为核心功法时激活的核心效果
- `Tags`（string[]）：与 Tag 系统关联的标签

#### Scenario: CultivationDefine 字段完整性
- **WHEN** 加载一条 CultivationDefine 记录
- **THEN** 该记录 SHALL 包含 ID、DisplayName、Desc、PathType、RealmLevel、MaxPoint、Completeness、Points、CoreEffect、Tags 字段

### Requirement: CultivationPointDefine 节点结构
系统 SHALL 提供 `CultivationPointDefine` 类，描述功法中单个修炼节点。每个节点包含：
- `Threshold`（int）：解锁所需的修炼点数阈值
- `Type`（CultivationPointType）：奖励类型（Card / BehaviorCard / Modifier / Story）
- `RefId`（string）：引用对应定义的 ID

#### Scenario: 节点按阈值排序
- **WHEN** 读取一本功法的 Points 列表
- **THEN** 节点 SHALL 按 Threshold 升序排列

#### Scenario: 节点类型引用
- **WHEN** 节点 Type 为 Card 且 RefId 为 "card_huoqiushu"
- **THEN** 该节点表示在修炼进度达到 Threshold 时，NPC 解锁 CardDefine ID 为 "card_huoqiushu" 的战斗卡牌

### Requirement: CultivationCoreEffect 核心效果结构
系统 SHALL 提供 `CultivationCoreEffect` 类，描述功法核心效果的数据占位。本次仅定义结构，不实现运行时执行。包含：
- `EffectId`（string）：效果标识
- `Desc`（string）：效果描述文本

#### Scenario: 核心效果数据占位
- **WHEN** 读取一本功法的 CoreEffect
- **THEN** SHALL 能获取 EffectId 和 Desc 字段，即使效果运行时未实现

### Requirement: CultivationDefineMgr 加载与查询
系统 SHALL 提供 `CultivationDefineMgr`（实现 `IDataMgrBase<CultivationDefine>`），从 `Data/Practice/CultivationDefines.json` 加载数据，提供以下查询能力：
- `Get(string id)`：按 ID 查询单条功法定义
- `GetAll()`：全量查询
- `GetByPath(PracticePath path)`：按道途类型查询所有功法
- `GetByPathAndRealm(PracticePath path, int realmLevel)`：按道途 + 境界等级查询

#### Scenario: 按道途查询功法
- **WHEN** 调用 `GetByPath(PracticePath.Ling)`
- **THEN** 系统 SHALL 返回所有 PathType=Ling 的 CultivationDefine

#### Scenario: 按道途和境界查询功法
- **WHEN** 调用 `GetByPathAndRealm(PracticePath.Wu, 1)`
- **THEN** 系统 SHALL 返回所有 PathType=Wu 且 RealmLevel=1 的 CultivationDefine

### Requirement: CultivationDefines.json 示例数据
系统 SHALL 提供至少灵修、武修、魂修各 1 本手配示例功法，每本功法至少包含 3 个节点。

#### Scenario: 示例功法覆盖度
- **WHEN** 加载 CultivationDefines.json
- **THEN** 至少 SHALL 包含 Ling、Wu、Hun 三个道途的各 1 本功法，每本至少 3 个 CultivationPointDefine 节点