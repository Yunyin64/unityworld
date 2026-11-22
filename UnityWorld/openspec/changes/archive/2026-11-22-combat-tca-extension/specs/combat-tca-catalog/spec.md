## ADDED Requirements

### Requirement: 平局和法宝生命周期为公开事件

对拼 SHALL 在所有拼点修正后按最终值判平，平局在两张卡 Apply 前广播一次，携带双方角色、卡、类型和最终值。法宝 SHALL 在战斗中按未激活到激活、成功维持激活、激活到未激活分别广播。法宝失活包含未能续期与场上离开。

#### Scenario: 最终平局和法宝续期

- **WHEN** 修正使两个待发拼点都为 4，且一件已激活的法宝在自己的下一次末格检查成功发动
- **THEN** 对拼只广播一次平局，法宝只广播续期。新激活或真实失活分别广播各自事件

### Requirement: 进阶目录为手工验收提供可用模板

本次进阶触发、动作与条件 SHALL 以可用条目公开，权重为 0，说明包含「进阶内容，有调整可能」。

#### Scenario: 配置进阶解冻

- **WHEN** 检查解冻进阶模板并手工应用到已冻结的有效卡
- **THEN** 模板可以执行，解冻成功，抽样池不会选到该进阶模板

## MODIFIED Requirements

### Requirement: 触发目录匹配战斗广播

触发目录 SHALL 对每个公开战斗广播以实际事件名登记一次，并描述真实发生时机。局部 Hook SHALL 在接口说明中记录各自的调用阶段。

#### Scenario: 查询公开广播

- **WHEN** 查询现行战斗触发配置
- **THEN** 收录下表中的广播。由生命周期直接调用的 `Contest`、`Tick`、`OnPassiveActivated`、`OnPassiveDeactivated`、`OnBeforePairedContest` 在接口说明中查阅

| 广播 | 发生时机 |
| --- | --- |
| OnUse | 满足末格资格且提交资源和行额度后 |
| OnApply | 卡牌的结算及对应词条调用后 |
| OnStraight | 轮末待发进入直击流程 |
| OnAttack | 非平局对拼的攻击侧及攻击直击 |
| OnDefend | 非平局对拼的防御侧 |
| OnContest | 双方待发正式配对后、读取最终比较数值前 |
| OnContestOverflow | 非平局对拼产生胜负和差值后 |
| OnRoundStart | 参战者本轮开始，供能和基础补灵前 |
| OnRoundEnd | 参战者本轮扫描、拼点和伤害结算结束后 |
| OnBaseManaDraw | 开场和本方轮末，基础放灵元前 |
| OnManaDraw | 成功放置至少一个灵元并扣 MP 后 |
| OnMPChanged | MP 实际发生变化后 |
| OnDamage | 护盾处理后实际扣 HP 时 |
| OnHeal | 实际增加 HP 后 |
| OnExhausted | 消耗次数从正数扣至零后 |
| OnCardLeave | 卡从场地或候补离开后 |
| OnCardDisplaced | 四向位移实际提交后，事件主语为被移动牌的主人 |
| OnDisplaceOtherCard | 四向位移实际提交且移的是其他角色的牌后，事件主语为位移发起者 |
| OnContestDraw | 最终配对结果为平局，两张卡 Apply 前 |
| OnArtifactActivated | 法宝开启被动时 |
| OnArtifactRenewed | 已激活法宝成功续期时 |
| OnArtifactDeactivated | 战斗中法宝停止被动时 |
| OnManaCharged | 成功提交每枚蓄灵时 |
| OnWoundGained | 新伤势实际落格后 |
| OnWoundHealed | 伤势因治疗实际离场后 |
| OnManaUsed | 一次发动的实付灵元提交后 |
| OnElementStackChanged | 指定的五行状态实际增减层后 |
| OnShieldExhausted | 额外灵盾由正数降至零后 |
| OnShieldChargeReady | 本方扫描轮的实际灵盾吸收达到蓄力阈值时 |

#### Scenario: 需要判断胜负的监听

- **WHEN** 两张攻击卡以 4 和 5 点对拼，或攻击直接结算直击
- **THEN** 配对时先收到一次 `OnContest`。对拼的两个攻击侧和直击的攻击侧分别产生 `OnAttack`，仅非平局对拼的 `OnContestOverflow` 提供 Winner、Loser、WinnerType、LoserType、Overflow、WinnerCard、LoserCard

#### Scenario: 卡牌在同一事件链中遇到重复内容

- **WHEN** 一条事件链重复产生事件名、主语、来源和参数均相同的事件
- **THEN** 每张卡的 Lua 回调最多执行一次。同一 Tick 内独立事件链各自结算，参数不同的事件也分别执行。
