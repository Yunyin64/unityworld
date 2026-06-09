## ADDED Requirements

### Requirement: CardType枚举
CardDefine 和 CardData SHALL 包含 CardType 字段，表示卡牌的风味分类。CardType 决定 Mana 消耗模式等规则差异。

#### Scenario: CardType字段存在
- **WHEN** 读取 CardDefine 或 CardData
- **THEN** 可获取 CardType 值（ZhaoShi/FaShu/FaBao/DanYao/ZhenFa/ShenTong）

### Requirement: ManaCost字段
CardDefine 和 CardData SHALL 包含 ManaCost 字段，描述该卡需要消耗的灵元类型和数量。

#### Scenario: 无Mana需求
- **WHEN** ManaCost 为空列表
- **THEN** 该卡战斗开始后直接进入CD计时

#### Scenario: 有Mana需求
- **WHEN** ManaCost 包含元素需求（如 [火×1]）
- **THEN** 该卡需等到灵元池中有对应灵元后，消耗灵元才开始CD

### Requirement: ActionDefine战斗数值扩展
ActionDefine SHALL 新增可选的战斗数值字段：AtkValue、ShieldValue、DefendValue、Element、PhysicalType。

#### Scenario: 攻击型Action
- **WHEN** ActionDefine 设置了 AtkValue > 0
- **THEN** 包含此Action的Effect被视为攻防Effect，所属卡进入待发槽参与对拼

#### Scenario: 盾型Action
- **WHEN** ActionDefine 设置了 ShieldValue > 0
- **THEN** 包含此Action的Effect被视为攻防Effect，对拼赢时溢出加血

#### Scenario: 防型Action
- **WHEN** ActionDefine 设置了 DefendValue > 0
- **THEN** 包含此Action的Effect被视为攻防Effect，对拼赢时溢出消失

#### Scenario: 纯效果Action
- **WHEN** ActionDefine 未设置 AtkValue/ShieldValue/DefendValue
- **THEN** 该Action不参与拼点，所属卡CD到后直接结算

### Requirement: 攻击属性标记
攻击型Action SHALL 包含五行属性（Element）和物理类型（PhysicalType）。

#### Scenario: 五行属性
- **WHEN** 攻击型Action被使用
- **THEN** 携带 Element 标记（金/木/水/火/土/无）

#### Scenario: 物理类型
- **WHEN** 攻击型Action被使用
- **THEN** 携带 PhysicalType 标记（斩/刺/打/射击）

### Requirement: CombatCardState运行时状态
战斗中每张卡SHALL有独立的运行时状态追踪（CombatCardState），不修改原始CardData。

#### Scenario: CD计时器追踪
- **WHEN** 战斗进行中
- **THEN** CombatCardState 记录当前CD进度（currentCdTick）

#### Scenario: Mana满足状态
- **WHEN** 有Mana需求的卡
- **THEN** CombatCardState 记录Mana是否已消耗（isManaFulfilled）

#### Scenario: 拼点数值提取
- **WHEN** 需要计算拼点数值
- **THEN** 从CardData的Effects中提取最大的AtkValue/ShieldValue/DefendValue作为该卡的拼点数值