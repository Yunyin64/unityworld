## ADDED Requirements

### Requirement: StoryBaseDefine 基础数据结构
系统 SHALL 提供 `StoryBaseDefine` 作为所有叙事定义的公共基类，继承 `DefineBase`，包含 Tags、Conditions、Effects、LuaScript 字段。

#### Scenario: 加载基础叙事定义
- **WHEN** 从 JSON 加载一条叙事定义数据
- **THEN** 系统 SHALL 正确解析 Tags、Conditions、Effects、LuaScript 字段，并通过 ID 可查询

### Requirement: StoryDefine 事件定义
系统 SHALL 提供 `StoryDefine`，继承 `StoryBaseDefine`，额外包含 `IsHide`（隐形/显示开关）、`Title`、`Text`、`OptionIds` 字段。

#### Scenario: 隐形 Story 不含展示字段
- **WHEN** StoryDefine.IsHide 为 true
- **THEN** Title/Text/OptionIds 可为空，系统不展示任何 UI，直接执行 Conditions + Effects

#### Scenario: 显示 Story 含完整展示字段
- **WHEN** StoryDefine.IsHide 为 false
- **THEN** Title 和 Text 必须不为空，系统 SHALL 通过事件通知 UI 层展示弹窗

### Requirement: OptionDefine 选项定义
系统 SHALL 提供 `OptionDefine`，继承 `StoryBaseDefine`，额外包含 `Text`（选项文本）和 `StoryIds`（反向注入到哪些 Story）字段。

#### Scenario: Option 反向注入 Story
- **WHEN** OptionDefine.StoryIds 包含某个 StoryDefine 的 ID
- **THEN** 该 StoryDefine 触发时，最终选项列表 SHALL 包含此 OptionDefine，即使 StoryDefine.OptionIds 未显式引用

### Requirement: 双向持有合并规则
系统 SHALL 在 StoryDefineMgr 加载完成后，对每个 StoryDefine 构建合并后的完整 OptionList = OptionIds（正向）+ 所有反向注入的 OptionDefine。

#### Scenario: 正向与反向合并去重
- **WHEN** 同一 OptionDefine 同时出现在 StoryDefine.OptionIds 和 OptionDefine.StoryIds 中
- **THEN** 最终列表中该 Option SHALL 只出现一次

#### Scenario: 加载时孤立引用警告
- **WHEN** StoryDefine.OptionIds 中包含不存在的 OptionId
- **THEN** 系统 SHALL 打印 Warning 日志并跳过该 Id，不抛出异常

### Requirement: StoryDefineMgr 与 OptionDefineMgr 加载器
系统 SHALL 提供 `StoryDefineMgr` 和 `OptionDefineMgr`，实现 `IDataMgrBase<T>`，从 JSON 文件加载对应数据，并在 `GameDataMgr` 中注册。

#### Scenario: 通过 ID 查询 StoryDefine
- **WHEN** 调用 StoryDefineMgr.Instance?.Get(id)
- **THEN** 系统 SHALL 返回对应的 StoryDefine，不存在时返回 null
