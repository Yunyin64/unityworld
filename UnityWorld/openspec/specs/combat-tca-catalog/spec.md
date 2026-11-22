# combat-tca-catalog Specification

## Purpose

规定策划可复用的战斗触发、选卡与动作模板，确保它们能经由发布的 Lua 接口按已记录的规则结算，并说明战斗广播与卡牌局部生命周期各自的用途。

## Requirements

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
| OnContestOverflow | 每次实际对拼确定最终结果后、两张卡 Apply 前 |
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
- **THEN** 配对时先收到一次 `OnContest`。对拼的两个攻击侧和直击的攻击侧分别产生 `OnAttack`。对拼的 `OnContestOverflow` 带双方最终点数、`IsDraw = false`、`Overflow = 1` 和胜负方信息。直击只走直击流程

#### Scenario: 卡牌在同一事件链中遇到重复内容

- **WHEN** 一条事件链重复产生事件名、主语、来源和参数均相同的事件
- **THEN** 每张卡的 Lua 回调最多执行一次。同一 Tick 内独立事件链各自结算，参数不同的事件也分别执行。

### Requirement: 可选模板能调用现行 Lua 接口

状态为 `Ready` 的 TCA 选择和动作模板 SHALL 使用已发布 Lua 入口的参数顺序和类型，描述执行阶段、目标和数量规则。已接入的选择模板 SHALL 使用现行 Domain 或已发布的二维选择。独立 CD、旧线性位移和候补部署及召回保留为 `Pending`。旧 Lua 兼容接口遵循既有行为。

#### Scenario: 直接增减 MP 和格上操作

- **WHEN** 展开直接增减 MP、抽取或回收灵元模板
- **THEN** 直接增减 MP 使用 `RecoverMP(ctx, Domain, Amount)` 与 `ReduceMP(ctx, Domain, Amount)`，并受 MP 上下限约束。抽取只对成功放置的灵元扣 MP，回收按 MP 空缺与未分配灵元计算

#### Scenario: 结算量和模板描述一致

- **WHEN** 展开攻击拼点、冻结或移除伤势模板
- **THEN** 攻击参数生成拼点而非固定伤害，冻结生效到下次完整扫描，移除伤势按 `Exact=true` 等于体量或 `Exact=false` 至多该体量选择

### Requirement: 治疗按实际回复量结算

Heal SHALL 按角色的战斗 HP 上限结算。只有实际恢复正数 HP 时，系统 SHALL 派发 `OnHeal`，`ctx.Caster` 表示接受治疗的角色。

#### Scenario: 治疗超过缺少的 HP

- **WHEN** 角色当前 18 HP、上限 20 HP，收到 5 点治疗
- **THEN** 角色恢复 2 点至 20 HP，监听在事件中可读到 20 HP

#### Scenario: 无正向回复

- **WHEN** 对满 HP 角色治疗，或治疗量小于等于零
- **THEN** HP 保持当前值且没有 `OnHeal`

### Requirement: 目录保留待实现的设计

TCA 配置 SHALL 保留待实现条目的 ID、参数、原始设计说明及评分数据，并在 JSON 中标记 `Pending` 和说明当前缺口。策划查阅时将 `Ready` 或省略状态的条目视为已接入。状态供目录查阅，战斗事件由实际派发点决定，Lua 接口由实际实现决定。

#### Scenario: 查阅待实现设计

- **WHEN** 按原 ID 查阅候补部署、召回、位移、旧对象或 CD 选择器，或尚未派发的触发
- **THEN** JSON 保留 `Pending` 条目及原设计资料。仅改变目录标注时，战斗派发和 Lua 调用结果保持原有行为

#### Scenario: 旧数据兼容

- **WHEN** 加载已接入但配置未声明状态的模板
- **THEN** 目录按已接入理解该条目，Lua 按实际接口签名执行

### Requirement: 新基础机制提供可复用的 TCA 节点

配置和接口索引 SHALL 提供施加拼点强化 Buff、装备属性、亲和、延迟收益、卸力、护甲、坚甲、灵盾、法宝供能、四向滑移、位置存在与计数、消耗比较的可用节点。胜负、邻卡使用、被位移和全场位移 SHALL 能通过公开事件及对应条件表达。

#### Scenario: 通过通用施加 Buff 选用拼点强化

- **WHEN** 策划选择攻击类型、防御类型、五行攻击、全攻击或全防御强化的持续版或下次版
- **THEN** 使用同一个 `AddNpcBuff` 动作和对应 Buff ID。对应 Lua 在匹配拼点生成时生效

#### Scenario: 通过防御动作施加卸力

- **WHEN** 策划为角色施加卸力 3
- **THEN** 选择防御动作 `AddDeflection` 并配置 3 点，该动作施加 3 层 `Deflection` Buff。对应 Lua 在角色下一次与敌方攻击配对时生效

#### Scenario: 组合邻卡使用与延迟加盾

- **WHEN** 策划配置右侧火法成功使用时，为本方在下一轮开始获得 2 灵盾
- **THEN** 可组合使用发布的触发、位置条件和延迟效果来执行，无需重写扫描、轮时钟或护盾扣减

### Requirement: 对拼结果和法宝生命周期为公开事件

对拼 SHALL 在所有拼点修正后比较最终值，并在两张卡 Apply 前广播一次 `OnContestOverflow`，携带双方角色、卡、类型、最终值、`IsDraw` 和 `Overflow`。有胜负时附带胜负方信息。法宝 SHALL 在战斗中按未激活到激活、成功维持激活、激活到未激活分别广播。法宝失活包含未能续期与场上离开。

#### Scenario: 最终平局和法宝续期

- **WHEN** 修正使两个待发拼点都为 4，且一件已激活的法宝在自己的下一次末格检查成功发动
- **THEN** 对拼广播一次 `OnContestOverflow`，其中 `IsDraw = true`、`Overflow = 0`，双方的 `CheckContestOutcome` 均匹配 `Draw`。法宝只广播续期。新激活或真实失活分别广播各自事件

### Requirement: 进阶目录为手工验收提供可用模板

本次进阶触发、动作与条件 SHALL 以可用条目公开，权重为 0，说明包含「进阶内容，有调整可能」。

#### Scenario: 配置进阶解冻

- **WHEN** 检查解冻进阶模板并手工应用到已冻结的有效卡
- **THEN** 模板可以执行，解冻成功，抽样池不会选到该进阶模板
