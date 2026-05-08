## ADDED Requirements

### Requirement: CultivationMgr 单例管理器
系统 SHALL 提供 `CultivationMgr`（实现 `IDomainMgrBase`），作为功法运行时管理器。提供静态单例 `Instance`，在 `WorldMgr.Initialize()` 中注册。

#### Scenario: 单例访问
- **WHEN** WorldMgr 初始化完成后
- **THEN** `CultivationMgr.Instance` SHALL 不为 null

### Requirement: NPC 功法持有数据
CultivationMgr SHALL 管理每个 NPC 的修炼运行时数据（`NpcCultivationData`），包含：
- `Path`（PracticePath）：NPC 的道途类型（由核心功法决定）
- `CurrentRealmLevel`（int）：当前境界等级
- `RealmProgress`（int）：当前境界进度累计值
- `CoreCultivationId`（string?）：核心功法的 Define ID
- `GongFaDatas`（List\<GongFa\>）：所有装备功法槽位（含核心）
- `ActiveSlotIndex`（int）：当前正在修炼的功法槽位索引（AI 决策）

#### Scenario: NPC 注册修炼数据
- **WHEN** 调用 `CultivationMgr.Register(int npcId, PracticePath path, string coreCultivationId)`
- **THEN** 系统 SHALL 为该 NPC 创建运行时修炼数据，道途为指定值，核心功法为指定 ID

### Requirement: GongFa 功法槽位
系统 SHALL 提供 `GongFa` 类，表示 NPC 持有的单本功法运行时状态，包含：
- `DefineId`（string）：对应的 CultivationDefine ID
- `CurrentPoint`（int）：当前修炼点数
- `IsCore`（bool）：是否为核心功法
- 可通过 `GetUnlockedPoints()` 查询已解锁的节点列表（CurrentPoint >= Threshold 的节点）

#### Scenario: 节点解锁判定
- **WHEN** 一本功法的 CurrentPoint = 150，且节点阈值分别为 50、100、200、400
- **THEN** `GetUnlockedPoints()` SHALL 返回阈值为 50 和 100 的两个节点

### Requirement: CultivationMgr 查询 API
CultivationMgr SHALL 提供以下查询方法：
- `GetNpcData(int npcId)`：获取 NPC 的修炼运行时数据
- `GetNpcPath(int npcId)`：获取 NPC 的道途类型
- `GetNpcRealmLevel(int npcId)`：获取 NPC 的当前境界等级
- `GetNpcCoreSlot(int npcId)`：获取 NPC 的核心功法槽位

#### Scenario: 查询 NPC 道途
- **WHEN** NPC 已注册且核心功法为灵修类型
- **THEN** `GetNpcPath(npcId)` SHALL 返回 `PracticePath.Ling`

#### Scenario: 查询未注册 NPC
- **WHEN** 查询未注册的 NPC ID
- **THEN** 所有查询方法 SHALL 返回 null 或默认值（PracticePath.None / 0）

### Requirement: GameDataMgr 注册
`GameDataMgr` SHALL 在初始化时注册 `RealmDefineMgr` 和 `CultivationDefineMgr`，加载路径分别为 `Data/Practice/RealmDefines.json` 和 `Data/Practice/CultivationDefines.json`。

#### Scenario: 数据管理器注册
- **WHEN** `GameDataMgr.Initialize()` 执行完成
- **THEN** `RealmDefineMgr.Instance` 和 `CultivationDefineMgr.Instance` SHALL 均不为 null

### Requirement: WorldMgr 注册 CultivationMgr
`WorldMgr.Initialize()` SHALL 创建并注册 `CultivationMgr` 到 `_mgrs` 列表中。

#### Scenario: CultivationMgr 生命周期
- **WHEN** WorldMgr 初始化并调用所有 Mgr 的 `Init()`
- **THEN** CultivationMgr SHALL 完成初始化且 Instance 可访问