## ADDED Requirements

### Requirement: 多文件目录扫描
TagDefineMgr SHALL 支持扫描指定目录下的所有 JSON 文件并合并加载。

- 扫描 `<baseDir>/**/*.json` 模式的所有文件
- 支持嵌套子目录
- 只处理 `.json` 扩展名的文件

#### Scenario: 扫描多级目录
- **WHEN** 目录结构为 `Data/Tag/Core/ElementTag.json` 和 `Data/Tag/Virtual/StatusTag.json`
- **THEN** 两个文件都被加载

#### Scenario: 忽略非 JSON 文件
- **WHEN** 目录中存在 `.txt` 或其他非 JSON 文件
- **THEN** 忽略这些文件，只加载 .json 文件

### Requirement: 每文件多 Tag 数组格式
每个 JSON 文件 SHALL 包含一个 TagDefine 数组。

- 文件格式：`[ {TagDefine1}, {TagDefine2}, ... ]`
- 每个元素须符合 TagDefine 结构
- 空文件（空数组 `[]`）是合法的

#### Scenario: 加载包含多个 Tag 的文件
- **WHEN** `ElementTag.json` 包含 `[{id:"fire"...}, {id:"water"...}]`
- **THEN** fire 和 water 两个 Tag 都被加载

#### Scenario: 加载空文件
- **WHEN** 某个 JSON 文件为 `[]`
- **THEN** 不报错，继续处理其他文件

### Requirement: ID 重复处理
当多个文件定义相同 ID 的 Tag 时，系统 SHALL 采用后加载覆盖策略。

- 后加载的 Tag 覆盖先加载的
- 输出警告日志记录重复情况
- 不中断加载流程

#### Scenario: ID 重复时覆盖
- **WHEN** 文件 A 定义 `{id:"fire", desc:"火焰"}`，文件 B 定义 `{id:"fire", desc:"燃烧"}`
- **THEN** 最终 fire 的 desc 为"火焰"或"燃烧"（取决于加载顺序），并输出警告

### Requirement: 加载错误容错
单个文件加载失败 SHALL 不影响其他文件的加载。

- 记录错误日志
- 继续处理剩余文件
- 返回的 Tag 集合不包含加载失败的文件内容

#### Scenario: 文件格式错误
- **WHEN** 某个 JSON 文件格式不正确（如语法错误）
- **THEN** 输出错误日志，跳过该文件，继续加载其他文件

#### Scenario: 文件不存在
- **WHEN** 指定的目录不存在
- **THEN** 不报错，返回空集合

### Requirement: 初始化时机
TagDefineMgr 的多文件加载 SHALL 在 `Init()` 方法中执行。

- 构造函数只接收基础路径
- `Init()` 执行实际的文件扫描和加载
- 加载完成后构建层级索引缓存

#### Scenario: 构造时指定目录
- **WHEN** `new TagDefineMgr("Data/Tag")` 且调用 `Init()`
- **THEN** 扫描 Data/Tag 下的所有 JSON 文件

### Requirement: 层级索引构建
加载完成后，TagDefineMgr SHALL 自动构建层级相关的索引缓存。

- `_childrenMap`：父→子节点映射
- `_depthCache`：Tag→Depth 映射
- 构建时检测循环引用和孤儿 Tag

#### Scenario: 构建子节点索引
- **WHEN** 加载完成后
- **THEN** `_childrenMap["element"]` 包含 ["fire", "water", "jin", "mu", "tu"]

#### Scenario: 检测循环引用
- **WHEN** 存在 A→B→C→A 的循环
- **THEN** 输出警告，循环中的 Tag Depth 设为 0

### Requirement: 兼容旧的查询接口
TagDefineMgr SHALL 继续支持现有的 `IDataMgrBase<TagDefine>` 接口方法。

- `Get(string id)`：按 ID 查询
- `GetAll()`：返回所有 Tag
- `Contains(string id)`：存在性检查

#### Scenario: 使用旧接口查询
- **WHEN** 调用 `Get("fire")`
- **THEN** 正常返回 fire 的 TagDefine

#### Scenario: 使用旧接口遍历
- **WHEN** 调用 `GetAll()`
- **THEN** 返回所有加载的 Tag（合并后）
