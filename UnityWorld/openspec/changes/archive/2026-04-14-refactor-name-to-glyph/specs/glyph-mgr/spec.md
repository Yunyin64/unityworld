## ADDED Requirements

### Requirement: GlyphMgr 单例生命周期
GlyphMgr SHALL 实现 `IGameplayMgrBase` 和 `ISoulBase` 接口，通过构造函数接收 seed 参数初始化 `SoulData`，并在构造时设置 `Instance` 静态单例。`End()` 时 SHALL 将 `Instance` 置为 null。

#### Scenario: GlyphMgr 正常初始化
- **WHEN** `WorldMgr.Initialize()` 创建 `new GlyphMgr(seed)` 并加入 `_gameplays` 列表
- **THEN** `GlyphMgr.Instance` 不为 null，可被其他系统访问

#### Scenario: GlyphMgr 销毁清理
- **WHEN** `GlyphMgr.End()` 被调用
- **THEN** `GlyphMgr.Instance` 被置为 null

### Requirement: 名字库加载
GlyphMgr SHALL 在 `Init()` 阶段通过 `NameLibrary.Load(path)` 加载 `NameLibrary.json`，获取姓氏库、男名库、女名库、道号前缀库、道号后缀库。加载失败时 SHALL 使用空库作为 fallback 并输出警告日志。

#### Scenario: 正常加载名字库
- **WHEN** `NameLibrary.json` 文件存在且格式正确
- **THEN** NameLibrary 包含非空的 Surnames、MaleFirstNames、FemaleFirstNames、DaoTitlePrefixes、DaoTitleSuffixes 数组

#### Scenario: 名字库文件不存在
- **WHEN** `NameLibrary.json` 文件不存在
- **THEN** 输出警告日志，NameLibrary 使用空数组，后续取名使用 fallback 默认值

### Requirement: 随机姓名生成
GlyphMgr SHALL 提供 `RandomName(NpcTypes.Gender gender)` 方法，返回格式为"道号前缀+道号后缀+姓+名"的完整姓名字符串。随机数 SHALL 使用 SoulData 中的 Rng 以保证可复现性。

#### Scenario: 生成男性姓名
- **WHEN** 调用 `GlyphMgr.Instance.RandomName(NpcTypes.Gender.Male)`
- **THEN** 返回一个非空字符串，包含从名字库随机选取的道号+姓+名组合，名字从 MaleFirstNames 池中选取

#### Scenario: 生成女性姓名
- **WHEN** 调用 `GlyphMgr.Instance.RandomName(NpcTypes.Gender.Female)`
- **THEN** 返回一个非空字符串，名字从 FemaleFirstNames 池中选取

#### Scenario: 名字库为空时使用 fallback
- **WHEN** 名字库某个池为空数组
- **THEN** 对应部分使用默认 fallback 值（姓="佚"，名="名"，道号前缀="无极"，道号后缀="子"）

### Requirement: 随机道号生成
GlyphMgr SHALL 提供 `RandomDaoTitle()` 方法，返回"前缀+后缀"格式的道号字符串。

#### Scenario: 生成道号
- **WHEN** 调用 `GlyphMgr.Instance.RandomDaoTitle()`
- **THEN** 返回一个由道号前缀库和道号后缀库随机组合的非空字符串

### Requirement: NpcBioData 内嵌 NpcNameData
`NpcBioData` SHALL 包含一个 `NpcNameData NameData` 子字段。现有的 `Name` 属性 SHALL 改为代理属性，getter 返回 `NameData.FullName`，setter 设置 `NameData.FullName`，以保持完全向后兼容。

#### Scenario: 通过 Name 属性读写与 NameData 一致
- **WHEN** 设置 `bioData.Name = "青玄子·李大柱"`
- **THEN** `bioData.NameData.FullName` 的值为 `"青玄子·李大柱"`

#### Scenario: 通过 NameData 设置后 Name 属性同步
- **WHEN** 设置 `bioData.NameData.FullName = "太虚道人·王小明"`
- **THEN** `bioData.Name` 的值为 `"太虚道人·王小明"`

### Requirement: NpcSystemName 废弃
`NpcSystemName` SHALL 标记 `[Obsolete("已迁移至 GlyphMgr")]`，内部逻辑清空。`NpcMgr` SHALL 移除对 `NpcSystemName` 的引用和使用。

#### Scenario: NpcMgr 不再持有 NameSystem
- **WHEN** 查看 `NpcMgr` 的公开属性
- **THEN** 不存在 `NameSystem` 属性

#### Scenario: NPC 创建流程不调用 NpcSystemName
- **WHEN** 通过 `NpcMgr.Create()` 创建 NPC
- **THEN** 创建流程中不调用任何 `NpcSystemName` 的方法

### Requirement: NpcGenerator 使用 GlyphMgr
`NpcGenerator` SHALL 使用 `GlyphMgr.Instance.RandomName(gender)` 替代 `NpcMgr.Instance.NameSystem.RandomName(gender)` 来生成 NPC 姓名。

#### Scenario: NpcGenerator 通过 GlyphMgr 取名
- **WHEN** `NpcGenerator.GenerateCultivators()` 生成 NPC
- **THEN** 调用 `GlyphMgr.Instance.RandomName(gender)` 获取姓名

### Requirement: GlyphMgr 日志输出
GlyphMgr 的 `Log()` 方法 SHALL 输出名字库统计信息，包括各池的条目数量。

#### Scenario: 日志输出名字库统计
- **WHEN** 调用 `GlyphMgr.Instance.Log()`
- **THEN** 输出包含姓氏数量、男名数量、女名数量、道号前缀数量、道号后缀数量的统计信息