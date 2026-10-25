## MODIFIED Requirements

### Requirement: CombatCard.Tick() 跳过 Passive 卡的 CD 循环
CombatCard.Tick() SHALL 在 Phase == Passive 时，仅调用 keyword hooks 的 OnTick 和卡自身 Lua env 的 `OnPassiveTick`（如果定义了），然后 return，不执行 Waiting/InCD/Ready 的状态转换逻辑。主动卡 SHALL 改由背包扫描决定使用机会，Tick 不再驱动独立 CD 达成 Ready。主功法卡固定占格、副功法按普通卡处理；特殊被动效果如何接入由后续卡牌设计调整，不以复刻全部旧卡行为为本变更前置条件。

#### Scenario: Passive 卡的 Tick
- **WHEN** 一张 Phase == Passive 的卡执行 Tick()
- **THEN** 不调用 CheckMana()、不调用 ResetCD()、不改变 Phase、不增加 CD 计时器

#### Scenario: 主动卡由扫描驱动
- **WHEN** 一张 Phase == Waiting 的主动卡执行 Tick()
- **THEN** 不再自动执行 CheckMana → InCD → ResetCD → Ready 流程；仅在背包扫描调度确认完整扫描、资源满足和本行发动机会后使用
