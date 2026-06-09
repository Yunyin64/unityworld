## ADDED Requirements

### Requirement: 蓝条转化灵元
NPC SHALL 拥有蓝条(MP)，战斗中定期扣除蓝量转化为带元素属性的灵元(Mana)。

#### Scenario: 开战时首次转化
- **WHEN** 战斗Start阶段
- **THEN** 触发一次蓝条转化，扣除MP产生灵元存入灵元池

#### Scenario: 定期转化
- **WHEN** 每隔固定Tick间隔
- **THEN** 再次触发蓝条转化，扣MP产生灵元

#### Scenario: 蓝条耗尽
- **WHEN** MP不足以执行转化
- **THEN** 不再产生新灵元，依赖已有灵元

### Requirement: 灵元池
每个CombatNpc SHALL 维护一个灵元池，记录各元素类型的灵元数量。

#### Scenario: 灵元入池
- **WHEN** 转化产生灵元
- **THEN** 对应元素的灵元数量增加

#### Scenario: 灵元消耗
- **WHEN** 有Mana需求的卡满足条件后开始CD
- **THEN** 从灵元池扣除对应元素和数量

### Requirement: Mana消耗模式
不同CardType SHALL 有不同的Mana消耗模式。

#### Scenario: 法术每次消耗
- **WHEN** CardType为法术(FaShu)的卡完成一轮CD循环后
- **THEN** 下一轮循环需重新等待Mana

#### Scenario: 法宝激活制
- **WHEN** CardType为法宝(FaBao)的卡首次消耗Mana
- **THEN** 激活后续N次使用免费，次数用完需再次消耗Mana