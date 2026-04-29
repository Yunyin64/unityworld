## ADDED Requirements

### Requirement: ExtraBehaviorDefine 数据定义
系统 SHALL 提供 `ExtraBehaviorDefine`，继承 `DefineBase`，位于 `Data/Defines/`，包含 Desc（string，描述）和 Tags（List\<string\>，语义标签）字段，用于数据驱动定义行为变体。

#### Scenario: 通过 ID 查询 ExtraBehaviorDefine
- **WHEN** 调用 ExtraBehaviorDefineMgr.Instance?.Get("fire_meditation")
- **THEN** SHALL 返回对应的 ExtraBehaviorDefine 实例，包含配置的 Desc 和 Tags

#### Scenario: Define 的 ID 即为 BehaviorId
- **WHEN** ExtraBehaviorDefine.ID 为 "fire_meditation"
- **THEN** 使用该 ID 创建的 ExtraBehavior 实例的 BehaviorId SHALL 为 "fire_meditation"

### Requirement: ExtraBehaviorDefineMgr 加载器
系统 SHALL 提供 `ExtraBehaviorDefineMgr`，实现 `IDataMgrBase<ExtraBehaviorDefine>`，从 JSON 文件加载行为拓展定义，在 `GameDataMgr` 中注册。

#### Scenario: 加载 JSON 数据
- **WHEN** GameDataMgr 初始化
- **THEN** ExtraBehaviorDefineMgr SHALL 从指定路径加载所有 ExtraBehaviorDefine

#### Scenario: 通过 GetAll 获取全部定义
- **WHEN** 调用 ExtraBehaviorDefineMgr.Instance?.GetAll()
- **THEN** SHALL 返回所有已加载的 ExtraBehaviorDefine

### Requirement: GameDataMgr 注册 ExtraBehaviorDefineMgr
系统 SHALL 在 GameDataMgr 构造函数中注册 ExtraBehaviorDefineMgr，加载路径为 ExtraBehaviorDefines JSON 文件。

#### Scenario: 注册加载
- **WHEN** GameDataMgr 构造函数执行
- **THEN** SHALL 将 ExtraBehaviorDefineMgr 加入 _datamgrs 列表

### Requirement: 空 JSON 数据模板
系统 SHALL 在数据目录下提供 ExtraBehaviorDefines 的空 JSON 文件模板，包含空数组结构。

#### Scenario: 空模板结构
- **WHEN** 读取 ExtraBehaviorDefines JSON 文件
- **THEN** SHALL 为合法的 JSON 数组格式，可被 ExtraBehaviorDefineMgr 正确解析