## ADDED Requirements

### Requirement: 战斗参数属性化原则
战斗系统中所有因 NPC 而异的数值参数 SHALL 定义为 NPC 身上的动态属性，而非系统常量。战斗引擎只读取这些属性，不定义其值。

#### Scenario: 战斗引擎读取 NPC 属性
- **WHEN** CombatScene 初始化某个 NPC 的战斗状态
- **THEN** 灵元转化频率、转化量、元素分配方案等参数 SHALL 从该 NPC 的属性中读取

#### Scenario: 不同 NPC 参数不同
- **WHEN** 两个不同境界的 NPC 同时参与战斗
- **THEN** 他们的灵元转化频率、转化量等参数 SHALL 各自独立，可以不同

### Requirement: 灵元转化频率（ManaConvertInterval）
NPC SHALL 持有 ManaConvertInterval 属性，表示每隔多少 Tick 触发一次灵元转化。该属性为动态值，受道途、功法、境界、天赋等因素影响。

#### Scenario: 按频率转化灵元
- **WHEN** 战斗进行中，经过了 ManaConvertInterval 个 Tick
- **THEN** 系统 SHALL 为该 NPC 触发一次灵元转化

### Requirement: 灵元转化量（ManaConvertAmount）
NPC SHALL 持有 ManaConvertAmount 属性，表示每次灵元转化时产生的灵元数量。

#### Scenario: 转化产出灵元
- **WHEN** NPC 触发灵元转化
- **THEN** 系统 SHALL 从 NPC 的 MP 中扣除对应蓝量，产生 ManaConvertAmount 个灵元

### Requirement: 灵元元素分配方案（ManaElementDistribution）
NPC SHALL 持有 ManaElementDistribution 属性，定义灵元转化时各五行元素的分配比例或规则。

#### Scenario: 按方案分配元素
- **WHEN** NPC 触发灵元转化并产生灵元
- **THEN** 产生的灵元 SHALL 按该 NPC 的 ManaElementDistribution 方案分配五行属性

#### Scenario: 道途影响元素分配
- **WHEN** 一个灵修·火系 NPC 和一个灵修·水系 NPC 各自转化灵元
- **THEN** 火系 NPC 产出的火灵元比例 SHALL 高于水系 NPC，反之亦然

### Requirement: 属性值当前阶段使用默认值
在属性计算公式尚未实现的阶段，所有 NPC 战斗属性 SHALL 使用合理的默认值。默认值足以支撑战斗系统正常运行。

#### Scenario: 默认值可用
- **WHEN** NPC 的战斗属性未被任何系统修改
- **THEN** 该 NPC SHALL 持有可用的默认属性值，战斗可正常进行

### Requirement: 属性清单可扩展
NPC 战斗属性清单 SHALL 支持未来追加新属性，当发现新的"应为 NPC 属性"的战斗参数时可随时加入，无需重构。

#### Scenario: 追加新属性
- **WHEN** 发现战斗系统中某个参数应该因 NPC 而异
- **THEN** 该参数 SHALL 能够被追加到 NPC 战斗属性清单中，现有属性不受影响
