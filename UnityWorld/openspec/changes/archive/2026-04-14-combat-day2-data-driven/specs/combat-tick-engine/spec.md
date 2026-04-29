## MODIFIED Requirements

### Requirement: CombatCardState 拼点方法数据驱动
CombatCardState SHALL 移除对 CardData 临时字段的直接读取，改为通过 `BuildContestData()` 从 ActionData 实例中提取拼点信息。原有的 `GetContestValue()`、`GetContestType()`、`GetPhysicalType()`、`GetElement()` 方法 SHALL 被移除或重构为从 ContestData 获取。

#### Scenario: 攻防判定改用 ActionData
- **WHEN** 调用 CombatCardState.IsAttackDefenseCard()
- **THEN** 通过遍历关联 CardData 的 EffectData.Actions 检查是否存在 FuncName 为 Attack/Shield/Block 的 ActionData，而非读取 CardData.ContestType

#### Scenario: BuildContestData 构造拼点快照
- **WHEN** 一张卡 CD 满需要进入待发槽
- **THEN** 调用 BuildContestData() 从 ActionData 中提取数值、类型、元素，生成 ContestData

### Requirement: 待发槽类型变更
CombatNpc.PendingSlot SHALL 从 `CombatCardState?` 变更为 `ContestData?`。所有对 PendingSlot 的读写操作 SHALL 使用 ContestData 类型。

#### Scenario: PendingSlot 存放 ContestData
- **WHEN** 一张攻防卡 CD 满进入待发槽
- **THEN** PendingSlot 存放的是 ContestData 实例，包含来源卡引用

### Requirement: 对拼结算使用 ContestData
CombatContestHandler SHALL 从两个 ContestData 中读取 ContestType、ContestValue、Element、PhysicalType 进行对拼结算，而非从 CombatCardState 读取。

#### Scenario: 攻击 vs 攻击对拼
- **WHEN** 两个 ContestData 都是 ContestType="Attack"
- **THEN** 比较 ContestValue 大小，赢方对输方造成差值伤害；同物理类型触发赢家通吃

#### Scenario: 攻击 vs 盾对拼
- **WHEN** 一个 ContestType="Attack"，另一个 ContestType="Shield"
- **THEN** 盾方赢时差值加入己方血条

### Requirement: 效果卡直接结算占位
CombatCardFlowHandler SHALL 在效果卡（非攻防卡）CD 满时直接标记为"已使用"并重置 CD。实际 Effect 执行逻辑留占位（TODO Day3-4），不阻塞流程。

#### Scenario: 效果卡 CD 满直接重置
- **WHEN** 效果卡（IsAttackDefenseCard() 返回 false）的 CD 满
- **THEN** 重置 CD，记录日志，不进入待发槽