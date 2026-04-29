## ADDED Requirements

### Requirement: PracticePath 枚举定义
系统 SHALL 在 `EnumTypes.cs` 中定义 `PracticePath` 枚举，包含 10 个值：None（凡人）、Ling（灵修）、Xian（仙修）、Dao（道修）、Wu（武修）、Mai（脉修）、Huang（荒修）、Hun（魂修）、Shen（神修）、Yi（异修）。

#### Scenario: 枚举值完整性
- **WHEN** 代码引用 `PracticePath` 枚举
- **THEN** 枚举 SHALL 包含 None=0, Ling, Xian, Dao, Wu, Mai, Huang, Hun, Shen, Yi 共 10 个值

### Requirement: CultivationPointType 枚举定义
系统 SHALL 在 `EnumTypes.cs` 中定义 `CultivationPointType` 枚举，表示功法节点的奖励类型。

#### Scenario: 节点类型枚举
- **WHEN** 代码引用 `CultivationPointType` 枚举
- **THEN** 枚举 SHALL 包含 Card、BehaviorCard、Modifier、Story 四个值