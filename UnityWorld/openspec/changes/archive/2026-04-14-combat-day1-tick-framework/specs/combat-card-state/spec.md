## ADDED Requirements

### Requirement: CombatCardState类
每张卡在战斗中 SHALL 有独立的运行时状态追踪（CombatCardState），不修改原始CardData。

#### Scenario: 持有CardData引用
- **WHEN** 创建 CombatCardState
- **THEN** 持有原始 CardData 的只读引用

### Requirement: CD计时器
CombatCardState SHALL 拥有独立的CD计时器。

#### Scenario: 计时器推进
- **WHEN** 调用 TickCd()
- **THEN** CurrentCdTick += 1

#### Scenario: CD就绪判定
- **WHEN** CurrentCdTick >= CardData.Cooldown
- **THEN** TickCd() 返回 true 表示就绪

#### Scenario: 计时器重置
- **WHEN** 调用 ResetCd()
- **THEN** CurrentCdTick = 0

### Requirement: Mana满足状态追踪
CombatCardState SHALL 追踪 Mana 是否已消耗满足。

#### Scenario: 无Mana需求
- **WHEN** 卡的 ManaCost 为空
- **THEN** IsManaFulfilled 默认为 true，直接进入CD计时

#### Scenario: 有Mana需求占位
- **WHEN** 卡有 ManaCost
- **THEN** ⏳Day1默认 IsManaFulfilled = true（占位），Day2接入Mana消耗逻辑

### Requirement: 拼点数值提取
CombatCardState SHALL 提供方法提取该卡的拼点数值。

#### Scenario: 获取拼点数值
- **WHEN** 调用 GetContestValue()
- **THEN** ⏳Day1占位：返回 CardData.ContestValue 临时字段值
- **NOTE** Day2回填：改为从 EffectData→ActionDefine 汇总最大AtkValue/ShieldValue/DefendValue

### Requirement: 攻防类型判定
CombatCardState SHALL 提供方法判定卡的攻防类型。

#### Scenario: 获取对拼类型
- **WHEN** 调用 GetContestType()
- **THEN** ⏳Day1占位：返回 CardData.ContestType 临时字段值（"Atk"/"Shield"/"Defend"/""）
- **NOTE** Day2回填：改为从 ActionDefine 的 AtkValue/ShieldValue/DefendValue 判断

#### Scenario: 是否攻防卡
- **WHEN** 调用 IsAttackDefenseCard()
- **THEN** GetContestType() != "" 时返回 true

### Requirement: 物理类型和元素提取
CombatCardState SHALL 提供方法提取物理类型和元素类型。

#### Scenario: 获取物理类型
- **WHEN** 调用 GetPhysicalType()
- **THEN** ⏳Day1占位：返回 CardData.PhysicalType 临时字段值
- **NOTE** Day2回填：改为从 ActionDefine.PhysicalType 读取

#### Scenario: 获取元素类型
- **WHEN** 调用 GetElement()
- **THEN** ⏳Day1占位：返回 BaseElementType.None
- **NOTE** Day2回填：改为从 ActionDefine.Element 读取

### Requirement: CardData临时战斗字段
CardData SHALL 在Day1新增临时战斗字段供 CombatCardState 读取。

#### Scenario: 临时字段列表
- **WHEN** Day1开发阶段
- **THEN** CardData 包含：ContestValue(float)、ContestType(string)、PhysicalType(PhysicalType)、CardType(CardType)
- **NOTE** ⏳Day2回填：ContestValue/ContestType/PhysicalType 移除（改从ActionDefine汇总），CardType 保留为正式字段