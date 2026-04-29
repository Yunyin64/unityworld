## ADDED Requirements

### Requirement: HP清零产生伤势
当NPC的HP被伤害清零时，SHALL根据最后一击的伤害数值产生对应的伤势卡，而非直接判负。

#### Scenario: HP清零触发伤势
- **WHEN** NPC的HP被伤害降至0或以下
- **THEN** 根据清零伤害数值生成伤势卡，伤势卡强制塞入该NPC的卡组

#### Scenario: HP部分恢复
- **WHEN** 伤势卡生成后
- **THEN** NPC的HP恢复为最大HP的50%，继续战斗

### Requirement: 伤势卡占用空间
伤势卡SHALL占用卡组空间（有Cost值），并附带负面效果。

#### Scenario: 伤势卡属性
- **WHEN** 伤势卡被生成
- **THEN** 伤势卡拥有Cost（占空间）、Cooldown、以及负面Effect（如自伤）

#### Scenario: 伤势卡示例
- **WHEN** 产生"出血"伤势
- **THEN** 出血卡：Cost=1, CD=2, 效果=对自己造成1点伤害

### Requirement: 伤势严重度映射
伤势卡的严重程度SHALL与清零伤害的数值相关。伤害越大，伤势卡Cost越高。

#### Scenario: 轻伤
- **WHEN** 清零伤害较小
- **THEN** 产生Cost=1的轻伤势卡

#### Scenario: 重伤
- **WHEN** 清零伤害较大
- **THEN** 产生Cost=2或Cost=3的重伤势卡

### Requirement: 伤势持续到战后
伤势卡SHALL持续存在于NPC卡组中直到被疗伤移除，不随战斗结束自动消失。

#### Scenario: 战斗结束后伤势保留
- **WHEN** 战斗结束，CombatResult回写大世界
- **THEN** 伤势卡列表包含在结算数据中，由调用方写入NPC的持久卡组

#### Scenario: 多场战斗伤势累积
- **WHEN** NPC带着伤势卡进入下一场战斗
- **THEN** 伤势卡占用卡组空间，减少可用战斗卡位