## ADDED Requirements

### Requirement: HP清零触发伤势
当NPC的HP被伤害降至0或以下时，SHALL 产生伤势卡而非直接判负。

#### Scenario: HP清零检测
- **WHEN** ApplyDamage 后 Hp <= 0
- **THEN** 调用 HandleHpZero 处理伤势流程

#### Scenario: 非直接判负
- **WHEN** HP清零
- **THEN** NPC Status 保持 Active（不设为 Defeated），由伤势+SP溢出机制决定是否判负

### Requirement: 伤势严重度映射
伤害数值 SHALL 映射为不同严重度的伤势。

#### Scenario: 轻伤
- **WHEN** 清零伤害 <= 10
- **THEN** 产生 Cost=1 的轻伤势卡

#### Scenario: 中伤
- **WHEN** 清零伤害 > 10 且 <= 25
- **THEN** 产生 Cost=2 的中伤势卡

#### Scenario: 重伤
- **WHEN** 清零伤害 > 25
- **THEN** 产生 Cost=3 的重伤势卡

#### Scenario: 映射规则占位
- **NOTE** ⏳Day1用简单阈值占位，Day3从伤势卡Define模板查询替换

### Requirement: 伤势卡生成
HandleHpZero SHALL 生成伤势卡并塞入NPC卡组。

#### Scenario: 创建伤势卡
- **WHEN** HP清零触发伤势
- **THEN** 调用 CreateInjuryCard(severity) 生成 CardData
- **NOTE** ⏳Day1硬编码CardData（固定Cost/CD），Day3替换为从伤势卡模板Define查询

#### Scenario: 塞入卡组
- **WHEN** 伤势卡生成后
- **THEN** 伤势卡包装为 CombatCardState 加入 NPC 的 CardStates

### Requirement: HP恢复50%
伤势卡生成后 SHALL 将NPC的HP恢复为 MaxHp 的 50%。

#### Scenario: HP部分恢复
- **WHEN** 伤势卡塞入卡组后
- **THEN** Hp = MaxHp * 0.5f

### Requirement: 伤势后SP检查
伤势卡塞入后 SHALL 立即触发SP溢出检查。

#### Scenario: 伤势导致SP溢出
- **WHEN** 伤势卡塞入导致 GetTotalCost() > Sp
- **THEN** 该NPC立即判负（SP溢出）

### Requirement: DamageInfo伤势标记
伤势产生时 SHALL 在 DamageInfo 中记录。

#### Scenario: 标记HP清零和伤势卡
- **WHEN** HP清零产生伤势
- **THEN** DamageInfo.HpZeroed = true，DamageInfo.InjuryCard = 生成的伤势卡

### Requirement: CombatNpc MaxHp属性
CombatNpc SHALL 拥有 MaxHp 属性，用于伤势后恢复50%。

#### Scenario: MaxHp记录
- **WHEN** PreStart阶段 SnapshotHp
- **THEN** 同时记录 MaxHp = 快照值

### Requirement: CombatNpc ApplyDamage改造
CombatNpc.ApplyDamage SHALL 不再直接设置 Defeated，改为返回 bool 表示是否HP清零。

#### Scenario: HP未清零
- **WHEN** 伤害后 Hp > 0
- **THEN** 返回 false

#### Scenario: HP清零
- **WHEN** 伤害后 Hp <= 0
- **THEN** Hp 设为 0，返回 true（不设Status=Defeated）