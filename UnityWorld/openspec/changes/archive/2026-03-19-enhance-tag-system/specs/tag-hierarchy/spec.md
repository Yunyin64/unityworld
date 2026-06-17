## ADDED Requirements

### Requirement: Tag 层级结构定义
TagDefine SHALL 支持 `Parent` 字段，用于构建树状层级结构。

- `Parent` 为 `string` 类型，`null` 表示根节点
- 每个 Tag 最多有一个父节点
- 通过 Parent 链条可追溯到根节点

#### Scenario: 根节点 Tag
- **WHEN** Tag 的 Parent 为 null
- **THEN** 该 Tag 为根节点，Depth = 1

#### Scenario: 子节点 Tag
- **WHEN** Tag 的 Parent 指向另一个已存在的 Tag
- **THEN** 该 Tag 为子节点，Depth = 父节点 Depth + 1

#### Scenario: 循环引用检测
- **WHEN** Tag 的 Parent 链条形成循环
- **THEN** 系统输出警告日志，且该 Tag 的 Depth 返回 0

### Requirement: 层级深度查询
TagDefineMgr SHALL 提供 `GetDepth(string tagId)` 方法，返回 Tag 距根节点的深度。

- 根节点 Depth = 1
- 每向下一层 Depth + 1
- 不存在的 Tag 返回 0
- 孤儿 Tag（Parent 不存在）Depth = 0，并输出警告

#### Scenario: 查询有效 Tag 的深度
- **WHEN** 调用 `GetDepth("fire")` 且 fire → element → core → null
- **THEN** 返回 3

#### Scenario: 查询不存在的 Tag
- **WHEN** 调用 `GetDepth("nonexistent")`
- **THEN** 返回 0

### Requirement: 父节点查询
TagDefineMgr SHALL 提供 `GetParent(string tagId)` 方法，返回父节点定义。

- 返回 `TagDefine?` 类型
- 根节点返回 null
- 不存在的 Tag 返回 null

#### Scenario: 查询子节点的父节点
- **WHEN** 调用 `GetParent("fire")` 且 fire 的 Parent 为 "element"
- **THEN** 返回 element 的 TagDefine

#### Scenario: 查询根节点的父节点
- **WHEN** 调用 `GetParent("core")` 且 core 的 Parent 为 null
- **THEN** 返回 null

### Requirement: 子节点查询
TagDefineMgr SHALL 提供 `GetChildren(string tagId)` 方法，返回所有直接子节点。

- 返回 `IEnumerable<TagDefine>`
- 无子节点返回空集合
- 不存在的 Tag 返回空集合

#### Scenario: 查询有子节点的 Tag
- **WHEN** 调用 `GetChildren("element")` 且 fire、water、jin 等 Tag 的 Parent 为 "element"
- **THEN** 返回包含 fire、water、jin 等的集合

#### Scenario: 查询无子节点的 Tag
- **WHEN** 调用 `GetChildren("fire")` 且无 Tag 的 Parent 为 "fire"
- **THEN** 返回空集合

### Requirement: 祖先查询
TagDefineMgr SHALL 提供 `GetAncestors(string tagId)` 方法，返回从父节点到根节点的所有祖先。

- 返回 `IEnumerable<TagDefine>`，按从近到远排序（父→祖父→...→根）
- 根节点返回空集合

#### Scenario: 查询 Tag 的祖先链
- **WHEN** 调用 `GetAncestors("fire")` 且 fire → element → core
- **THEN** 返回 [element, core]

### Requirement: 后代查询
TagDefineMgr SHALL 提供 `GetDescendants(string tagId)` 方法，返回所有后代节点（递归）。

- 返回 `IEnumerable<TagDefine>`
- 使用深度优先遍历

#### Scenario: 查询 Tag 的所有后代
- **WHEN** 调用 `GetDescendants("element")` 且 fire → burn、water → freeze
- **THEN** 返回 [fire, burn, water, freeze, ...]

### Requirement: 按深度查询
TagDefineMgr SHALL 提供 `GetByDepth(int depth)` 方法，返回指定层级的所有 Tag。

- 返回 `IEnumerable<TagDefine>`
- depth < 1 返回空集合

#### Scenario: 查询所有根节点
- **WHEN** 调用 `GetByDepth(1)`
- **THEN** 返回所有 Parent 为 null 的 Tag

#### Scenario: 查询所有语义 Tag（第三层）
- **WHEN** 调用 `GetByDepth(3)`
- **THEN** 返回所有 Depth = 3 的 Tag

### Requirement: 冲突关系定义
TagDefine SHALL 支持 `ConflictTags` 字段，显式声明与哪些 Tag 冲突。

- `ConflictTags` 为 `List<string>` 类型
- 默认为空列表
- 冲突关系不继承到子节点

#### Scenario: 配置冲突关系
- **WHEN** Tag "fire" 的 ConflictTags 包含 ["ice", "water"]
- **THEN** "fire" 与 "ice"、"water" 冲突

#### Scenario: 冲突不继承
- **WHEN** "fire" 与 "ice" 冲突，且 "burn" 的 Parent 为 "fire"
- **THEN** "burn" 不自动与 "ice" 冲突

### Requirement: 冲突查询
TagDefineMgr SHALL 提供 `IsConflict(string tagId1, string tagId2)` 方法，检查两 Tag 是否冲突。

- 返回 `bool`
- 冲突关系双向对称
- 不存在的 Tag 视为无冲突

#### Scenario: 查询直接声明的冲突
- **WHEN** 调用 `IsConflict("fire", "ice")` 且 fire.ConflictTags 包含 "ice"
- **THEN** 返回 true

#### Scenario: 查询非冲突 Tag
- **WHEN** 调用 `IsConflict("fire", "mu")` 且 fire.ConflictTags 不包含 "mu"
- **THEN** 返回 false
