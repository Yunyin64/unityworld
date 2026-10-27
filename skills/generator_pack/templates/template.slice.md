# <Slice 标题>

> summary: <≤150字简介>
> svn_rev: <SVN修订版本号>

## 命名提示
- 主 Slice 用 `<name>.<slice_name>.slice`
- 同一主题下的子 Slice 用 `<name>.<subname>.<slice_name>.slice`
- 持久化 Slice 用 `<name>.persistence.slice` — 描述存储位置、Pdef/结构定义、脏标记、持久化时机、缓存机制（详见 SPEC-write.md §10.8）

## 状态字段拆解

### 状态字段1：<field_name>
- 定义/声明点：`<relative/path/to/file>` + `<ClassName>` + `<member_name>`
- 写入点1：`<relative/path/to/file>` + `<FunctionName>`
- 写入点2：`<relative/path/to/file>` + `<FunctionName>` + <简要写入逻辑（复杂时填写）>
- 读取点1：`<relative/path/to/file>` + `<FunctionName>`
- 读取点2：`<relative/path/to/file>` + `<FunctionName>` + <简要读取逻辑（复杂时填写）>

### 状态字段2：<field_name>
- 定义/声明点：`<relative/path/to/file>` + `<ClassName>` + `<member_name>`
- 写入点1：`<relative/path/to/file>` + `<FunctionName>`
- 读取点1：`<relative/path/to/file>` + `<FunctionName>`

### 状态字段3：<field_name>
- 定义/声明点：`<relative/path/to/file>` + `<ClassName>` + `<member_name>`
- 写入点1：`<relative/path/to/file>` + `<FunctionName>`
- 读取点1：`<relative/path/to/file>` + `<FunctionName>`

## 不变量
- <不变量1>
- <不变量2>
