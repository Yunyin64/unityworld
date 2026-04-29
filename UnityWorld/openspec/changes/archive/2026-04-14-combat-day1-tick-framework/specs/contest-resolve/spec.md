## ADDED Requirements

### Requirement: 对拼数值比较
对拼时 SHALL 比较双方卡的拼点数值（GetContestValue），数值高者为赢方。

#### Scenario: 赢方确定
- **WHEN** 双方卡进入对拼
- **THEN** GetContestValue 较高者为赢方，差值 = 赢方值 - 输方值

#### Scenario: 数值相同平局
- **WHEN** 双方 GetContestValue 相同
- **THEN** 两卡均消耗，无伤害产生

### Requirement: 赢方攻击卡效果
攻击卡赢得对拼时 SHALL 将差值作为伤害施加到输方NPC。

#### Scenario: 攻击卡赢
- **WHEN** 赢方卡 GetContestType() == "Atk"
- **THEN** 差值作为伤害打到输方NPC（调用 ApplyDamage）

### Requirement: 赢方盾卡效果
盾卡赢得对拼时 SHALL 将差值加入己方NPC的HP。

#### Scenario: 盾卡赢
- **WHEN** 赢方卡 GetContestType() == "Shield"
- **THEN** 差值作为治疗加到赢方NPC（调用 ApplyHeal）

### Requirement: 赢方防卡效果
防卡赢得对拼时 SHALL 差值消失，无额外效果。

#### Scenario: 防卡赢
- **WHEN** 赢方卡 GetContestType() == "Defend"
- **THEN** 差值不产生任何效果（纯减伤）

### Requirement: 赢家通吃规则
同物理类型攻击对打时（射击除外），SHALL 触发赢家通吃——赢方造成全额数值伤害而非差值。

#### Scenario: 赢家通吃触发条件
- **WHEN** 双方均为攻击卡（ContestType == "Atk"）
- **AND** 双方 PhysicalType 相同
- **AND** PhysicalType != SheJi（射击）
- **THEN** 触发赢家通吃

#### Scenario: 赢家通吃伤害
- **WHEN** 赢家通吃触发
- **THEN** 赢方造成全额 GetContestValue 伤害（非差值），输方承受全额伤害

#### Scenario: 射击类型不触发
- **WHEN** 任一方 PhysicalType == SheJi
- **THEN** 按普通差值规则结算

### Requirement: 对拼产生DamageInfo
每次对拼 SHALL 构建一个 DamageInfo 记录完整因果。

#### Scenario: DamageInfo填充
- **WHEN** 对拼结算执行
- **THEN** DamageInfo 包含：Source/Target NPC、SourceCard/TargetCard、SourceValue/TargetValue/DeltaValue、赢方类型、是否赢家通吃、最终伤害/治疗值