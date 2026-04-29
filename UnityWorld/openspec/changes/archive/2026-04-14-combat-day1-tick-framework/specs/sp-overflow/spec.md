## ADDED Requirements

### Requirement: SP溢出检查
CombatScene SHALL 在每个Tick结束时检查所有存活NPC的卡组空间占用。

#### Scenario: 正常状态
- **WHEN** NPC的 GetTotalCost() <= Sp
- **THEN** 继续战斗

#### Scenario: 溢出判负
- **WHEN** NPC的 GetTotalCost() > Sp（通常因伤势卡塞入导致）
- **THEN** 该NPC立即判负，Status = Defeated

### Requirement: SP溢出EndReason
SP溢出导致的判负 SHALL 使用 SpaceOverflow 作为 EndReason。

#### Scenario: 记录EndReason
- **WHEN** NPC因SP溢出判负
- **THEN** CombatEndReason = SpaceOverflow

### Requirement: SP溢出后战斗结束检查
SP溢出判负后 SHALL 立即检查是否只剩一方存活。

#### Scenario: 触发战斗结束
- **WHEN** SP溢出导致某方全灭
- **THEN** 战斗结束，另一方胜利

### Requirement: CombatNpc SP属性
CombatNpc SHALL 拥有 Sp 属性表示卡组空间上限。

#### Scenario: SP属性
- **WHEN** 读取 CombatNpc.Sp
- **THEN** 返回该NPC的卡组空间上限值
- **NOTE** ⏳Day1占位硬编码，Day5回填从 Npc.GetSpMax() 读取

### Requirement: CombatNpc GetTotalCost方法
CombatNpc SHALL 提供 GetTotalCost() 方法计算当前卡组Cost总和。

#### Scenario: Cost总和
- **WHEN** 调用 GetTotalCost()
- **THEN** 遍历所有 CardStates，累加每张卡的 CardData.Cost