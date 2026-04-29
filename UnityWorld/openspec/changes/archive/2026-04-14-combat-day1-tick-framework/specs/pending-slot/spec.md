## ADDED Requirements

### Requirement: 待发槽容量
每个CombatNpc SHALL 拥有一个待发槽（PendingSlot），默认上限1张卡。

#### Scenario: 待发槽初始为空
- **WHEN** 战斗开始
- **THEN** 每个NPC的PendingSlot为null

### Requirement: 攻防卡入槽
攻防卡CD就绪后 SHALL 尝试推入所属NPC的待发槽。

#### Scenario: 待发槽为空时入槽
- **WHEN** 攻防卡就绪且PendingSlot == null
- **THEN** 卡进入待发槽（PendingSlot = 该CombatCardState）

#### Scenario: 入槽后检查双方
- **WHEN** 卡成功入槽
- **THEN** 立刻检查自己和Target的待发槽是否都有卡

### Requirement: 待发槽溢出直击
当待发槽已满时新卡就绪，SHALL 挤出旧卡执行直击。

#### Scenario: 溢出挤出
- **WHEN** 攻防卡就绪但PendingSlot != null
- **THEN** 旧卡被挤出，对Target执行直击（全额攻击数值伤害）

#### Scenario: 挤出后新卡入槽
- **WHEN** 旧卡被挤出后
- **THEN** 新卡进入待发槽

#### Scenario: 新卡入槽后再次检查双方
- **WHEN** 新卡入槽
- **THEN** 再次检查自己和Target的待发槽是否都有卡

### Requirement: 双方待发槽触发对拼
当自己和Target的待发槽都有卡时 SHALL 立刻触发对拼。

#### Scenario: 触发对拼
- **WHEN** 自己PendingSlot != null 且 Target.PendingSlot != null
- **THEN** 调用对拼结算（ResolveContest），消耗双方PendingSlot的卡

#### Scenario: 对拼后待发槽清空
- **WHEN** 对拼结算完成
- **THEN** 双方PendingSlot设为null，被消耗卡的CD重置