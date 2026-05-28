## ADDED Requirements

### Requirement: Scope 数据结构
系统 SHALL 提供 `Scope` 类，包含三个字段：`Owner`（ScopeOwner 枚举）、`Filters`（List\<string\> 条件列表）、`Selector`（ScopeSelector 对象）。所有字段支持 JSON 反序列化。

#### Scenario: 最简 Scope（仅 Owner）
- **WHEN** JSON 为 `{"owner": "Self"}`
- **THEN** 反序列化后 Scope.Owner == ScopeOwner.Self，Filters 为空列表，Selector 为默认 All

#### Scenario: 完整 Scope
- **WHEN** JSON 为 `{"owner": "Enemy", "filters": ["HasCD", "IsSpell"], "selector": {"type": "Random", "count": 1}}`
- **THEN** 反序列化后 Owner=Enemy，Filters 含 2 项，Selector.Type="Random"，Selector.Count=1

#### Scenario: 全局 Scope（无目标）
- **WHEN** JSON 为 `{"owner": "None"}`
- **THEN** Scope.Owner == ScopeOwner.None，表示全局效果无具体目标

### Requirement: ScopeOwner 枚举
系统 SHALL 提供 `ScopeOwner` 枚举，包含值：Self、Enemy、Any、None。

#### Scenario: 枚举值完整
- **WHEN** 代码引用 ScopeOwner.Self / ScopeOwner.Enemy / ScopeOwner.Any / ScopeOwner.None
- **THEN** 编译通过

### Requirement: ScopeSelector 数据结构
系统 SHALL 提供 `ScopeSelector` 类，包含 `Type`（string: "All"/"Random"/"Adjacent"/"Top"）和 `Count`（int，默认 0 表示不限）。

#### Scenario: Random 选择器
- **WHEN** ScopeSelector.Type == "Random" 且 Count == 2
- **THEN** 运行时从 Filter 结果中随机选取 2 个目标

#### Scenario: All 选择器（默认）
- **WHEN** ScopeSelector 为 null 或 Type == "All"
- **THEN** 运行时选取 Filter 结果中的所有目标

### Requirement: Scope 求值
系统 SHALL 提供 Scope 求值逻辑：先按 Owner 确定归属方，再按 Filters 逐条过滤，最后按 Selector 选取最终目标列表。

#### Scenario: Self + IsSpell + Random(1) 求值
- **WHEN** Scope(Self, ["IsSpell"], Random(1)) 在战斗中求值
- **THEN** 返回自己手中随机一张法术卡

#### Scenario: Filter 结果为空
- **WHEN** Scope 求值后 Filter 阶段无符合条件目标
- **THEN** 返回空列表，后续 Func 不执行（静默跳过）
