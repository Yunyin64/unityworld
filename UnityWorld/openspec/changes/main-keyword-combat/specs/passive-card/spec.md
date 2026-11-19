## MODIFIED Requirements

### Requirement: CardDefine Keywords 字段
`CardDefine` SHALL 通过 `Keywords` 列表配置主、普通 Keyword，保留现有 JSON 列表格式。卡牌定义在用于创建 Card 之前 SHALL 满足 `keyword-system` 的唯一 Main 校验。

#### Scenario: JSON 中配置 Keywords
- **WHEN** CardDefine JSON 的 `Keywords` 包含 `Passive` 与 `Item`
- **THEN** 判定 `Passive` 为 Main，并保留 `Item` 为普通 Keyword

#### Scenario: JSON 中未配置 Keywords
- **WHEN** CardDefine JSON 缺少 `Keywords` 或其列表为空
- **THEN** 在使用该定义创建 Card 前报告缺少 Main

## ADDED Requirements

### Requirement: 常驻被动执行在场回调
以 `Passive` 为 Main 的卡 SHALL 在场期间继续执行适用的周期和事件回调。被动生命周期 SHALL 独立于扫描进度、满灵、主动待发及冻结，卡牌 SHALL 在离场时结束由在场状态提供的常驻被动。

#### Scenario: Passive 卡的 Tick
- **WHEN** 一张常驻被动卡在场并配置有逐 Tick 效果
- **THEN** 正常执行其被动回调，不进入独立冷却或扫描发动流程

#### Scenario: 被动与扫描额度无关
- **WHEN** 本行的唯一额度已由法术使用，随后符合常驻被动的触发条件
- **THEN** 被动照常执行，本行额度保持已用

#### Scenario: 被动卡被冻结
- **WHEN** 在场的常驻被动卡被冻结，随后收到符合条件的监听事件
- **THEN** 被动效果继续执行，冻结照既有规则等待下一次完整扫描处理

### Requirement: 被动 Main 入场激活且可为空
`Passive` SHALL 提供入场即激活的常驻被动生命周期。被动效果本身为可选，空被动 SHALL 是合法的卡牌主机制。

#### Scenario: Passive keyword 生效
- **WHEN** 以 `Passive` 为 Main 的卡进入战斗背包
- **THEN** 在场时立即视为已激活，适用的被动效果可执行，不支付蓄灵或行额度

#### Scenario: 伤口使用空被动
- **WHEN** 只用于占格的伤口卡带 `Passive` 和 `Wound`，且没有被动效果脚本
- **THEN** 该卡满足唯一 Main 的要求，并按 `Wound` 执行伤口占格规则

## REMOVED Requirements

### Requirement: CombatCard.Tick() 跳过 Passive 卡的 CD 循环
**Reason**: 原规则容许 Passive 与扫描、条件发动能力并列作为卡的核心规则。现在主机制由唯一 Main 决定。

**Migration**: 常驻被动走「常驻被动执行在场回调」，法宝被动走自身激活生命周期。

### Requirement: Passive.lua 实现
**Reason**: `Passive` 改为独占 Main，原有同卡多主机制场景失效。

**Migration**: 使用「被动 Main 入场激活且可为空」，附加效果仍可由普通 Keyword 或卡自身定义提供。
