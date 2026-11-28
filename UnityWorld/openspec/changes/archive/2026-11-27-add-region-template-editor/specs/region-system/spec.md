## ADDED Requirements

### Requirement: 区域描述

版本 2 的区域定义 SHALL 支持可选的文本描述。旧定义省略描述时 SHALL 能直接加载。

#### Scenario: 读取作者填写的描述
- **WHEN** 地图目录读取带有 `Description` 的区域定义
- **THEN** 读取结果保留这段原文，区域编辑器可以预览及更新该内容。
