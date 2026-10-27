## MODIFIED Requirements

### Requirement: CombatCard.Tick() 跳过 Passive 卡的 CD 循环
CombatCard.Tick() SHALL 不通过 Waiting/InCD/Ready 驱动独立 CD。纯被动卡保留 keyword OnTick 与自身 OnPassiveTick；带主动机会的卡也可具有被动效果，其被动效果 SHALL 不被冻结或主动待发状态屏蔽。扫描与条件 Keyword 决定主动机会，Passive 不构成排他卡类型。

#### Scenario: Passive 卡的 Tick
- **WHEN** 一张 Phase == Passive 的卡执行 Tick()
- **THEN** 不调用 CheckMana()、不调用 ResetCD()、不改变 Phase、不增加 CD 计时器，继续调用适用的被动回调

#### Scenario: 主动卡由扫描驱动
- **WHEN** 一张具有扫描机会且 Phase == Waiting 的主动卡执行 Tick()
- **THEN** 不自动执行 CheckMana → InCD → ResetCD → Ready；扫描使用仍要求完整扫描、满灵、未冻结和本行机会，附加法宝 Keyword 可另由条件事件尝试使用

#### Scenario: 混合卡冻结时被动仍运行
- **WHEN** 同一卡同时拥有主动机会和被动效果且被冻结
- **THEN** 被动监听与适用 Tick 回调继续，主动 Use 被阻止，不能借被动回调绕过主动资格检查

### Requirement: Passive.lua 实现
Passive Keyword SHALL 提供被动生命周期支持。纯被动卡可处于 Passive 状态；含扫描或条件主动机会的混合卡 SHALL 保留这些机会，不能因 Passive Keyword 的状态设置永久失去主动能力，也不能因禁用旧 CD hooks 而丢失被动效果。

#### Scenario: Passive keyword 生效
- **WHEN** 一张只有被动能力且 Keywords 包含 Passive 的卡执行 PreStart()
- **THEN** 该卡处于被动模式，后续 Tick 不走 CD 循环并执行适用的被动回调

#### Scenario: Passive 与法术法宝共存
- **WHEN** 一张卡同时有 Passive、法术和法宝 Keyword
- **THEN** 被动效果、扫描机会、条件机会均可参与对应流程，各自仍遵守资源和状态规则，不按 Keyword 列表先后互相覆盖能力
