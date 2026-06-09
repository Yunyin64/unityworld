## ADDED Requirements

### Requirement: CardBase.UseFabao 全局方法
FaBao.lua SHALL 在全局 `CardBase` table 上定义 `UseFabao(card, ctx)` 方法。该方法 SHALL 调用 `card:TryPayMana()` 检查并扣除灵元消耗，成功后 SHALL 调用 `card:Apply()` 执行效果。

#### Scenario: 灵元充足触发法宝
- **WHEN** 法宝卡的 Lua hook（如 OnDamage）调用 `CardBase.UseFabao(card, ctx)` 且灵元充足
- **THEN** 灵元 SHALL 被扣除，`card:Apply()` SHALL 被调用

#### Scenario: 灵元不足触发法宝
- **WHEN** 法宝卡的 Lua hook 调用 `CardBase.UseFabao(card, ctx)` 且灵元不足
- **THEN** 灵元 SHALL NOT 被扣除，`card:Apply()` SHALL NOT 被调用

### Requirement: TryPayMana C# 方法
CombatCard SHALL 提供 `TryPayMana(): bool` 公开方法。该方法 SHALL 检查当前灵元消耗（GetCombatManaCost），若无消耗或扣费成功返回 true，否则返回 false。该方法 SHALL NOT 修改 CombatCardPhase。

#### Scenario: 无灵元消耗
- **WHEN** 卡牌 ManaCost 为空
- **THEN** TryPayMana SHALL 返回 true 且不修改 Phase

#### Scenario: 灵元足够
- **WHEN** 卡牌 ManaCost 非空且 Owner.ManaPool 满足消耗
- **THEN** TryPayMana SHALL 扣除对应灵元并返回 true

#### Scenario: 灵元不够
- **WHEN** 卡牌 ManaCost 非空且 Owner.ManaPool 不满足消耗
- **THEN** TryPayMana SHALL 返回 false 且 ManaPool 不变

### Requirement: FaBao keyword 不走 CD 循环
法宝卡 CD SHALL 为 0，CreateFromData 时自动获得 Passive keyword。FaBao keyword 的 Apply hook SHALL 将 Phase 设为 Finished（防止意外进入 CD 流程）。

#### Scenario: 法宝卡战斗初始化
- **WHEN** CD=0 的法宝卡进入 CreateFromData
- **THEN** SHALL 自动获得 Passive keyword，Phase 保持非 InCD 状态
