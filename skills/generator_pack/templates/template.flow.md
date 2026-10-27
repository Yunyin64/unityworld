# <Flow 标题>

> summary: <≤150字简介>
> svn_rev: <SVN修订版本号>

## 命名提示
- 主 Flow 用 `<name>.<flow_name>.flow`
- 同一主题下的子 Flow 用 `<name>.<subname>.<flow_name>.flow`

## 触发点
- 触发点1：<描述触发行为/入口事件>
  - 代码定位：`<relative/path/to/file>`
  - 搜索关键字：`<function-or-event-keyword>`
- 触发点2（可选）：<描述触发行为/入口事件>
  - 代码定位：`<relative/path/to/file>`
  - 搜索关键字：`<function-or-event-keyword>`

## 主链路
1. <步骤1描述>
   - 代码定位：`<relative/path/to/file1>`
   - 搜索关键字：`<keyword1>`、`<keyword2>`
   - 日志关键字：`<log_keyword>`（可选，该步骤产生的关键日志；无则省略）
2. <步骤2描述>
   - 代码定位：`<relative/path/to/file2>`
   - 搜索关键字：`<keyword3>`
3. <步骤3描述>
   - 代码定位：`<relative/path/to/file3>`
   - 搜索关键字：`<keyword4>`
   - 日志关键字：`<log_keyword>`

## 终态校验
- <不变量/可验证结果1>
- <不变量/可验证结果2>
