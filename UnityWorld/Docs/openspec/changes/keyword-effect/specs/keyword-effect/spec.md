## ADDED Requirements

### Requirement: Effect 支持 Keyword 模式

EffectData 必须（SHALL）支持两种模式：TCA 模式（Trigger+Condition+Action[]）和 Keyword 模式（IsKeyword=true）。两种模式共享 Score 和 Tags 字段，共存于 CardData.Effects[] 列表中。

Keyword 模式的 EffectData 必须（SHALL）包含 KeywordId（string）和 KeywordParams（Dict）字段。Keyword 模式下 Trigger、Condition、Actions 字段不使用。

#### Scenario: 一张卡同时拥有 TCA Effect 和 Keyword Effect
- **WHEN** 一张卡的 Effects[] 中包含一个 Keyword Effect（如 Weapon）和一个 TCA Effect（如 OnUse→Attack）
- **THEN** 两者共存于同一列表中，Keyword 在结算管线特定节点介入，TCA 正常走通用管线

#### Scenario: Keyword Effect 参与分数预算
- **WHEN** 卡牌随机生成系统从 Effect 池中选取 Effect
- **THEN** Keyword Effect 与 TCA Effect 在同一个池中，其 Score 值参与总分数预算计算

#### Scenario: Keyword Effect 的 Tag 参与匹配
- **WHEN** 系统对 Effect 进行 Tag 匹配查询（如搜索所有带"消耗"Tag 的 Effect）
- **THEN** Keyword Effect 的 Tags 与 TCA Effect 的 Tags 行为一致，均可被匹配

---

### Requirement: Initial（初始）关键词

标记为 Initial 的 Keyword Effect 必须（SHALL）在战斗初始化装载卡组时，将该卡的 CD 计时器设为已满状态（CD=0），使其在第一个 Tick 即可触发。

#### Scenario: 带初始关键词的卡开战立刻触发
- **WHEN** 战斗开始，一张带有 Initial Keyword 的攻防卡装入卡组
- **THEN** 该卡的 CD 计时器从 0 开始（已满），第一个 Tick 即进入待发槽或直接结算

---

### Requirement: Consume（消耗）关键词

标记为 Consume 的 Keyword Effect 必须（SHALL）在卡牌每次使用完成后，将内部计数减 1。计数归零时，该卡必须（SHALL）从卡组中移除，其占用的 Size 必须（SHALL）释放回 SP 空间。

KeywordParams 必须（SHALL）包含 `Uses`（int），表示总可用次数。

#### Scenario: 消耗卡用完后移除
- **WHEN** 一张 Consume(Uses=1) 的卡使用完成
- **THEN** 计数变为 0，该卡从卡组移除，其 Cost 值从已占用 SP 中释放

#### Scenario: 消耗卡多次使用
- **WHEN** 一张 Consume(Uses=2) 的卡第一次使用完成
- **THEN** 计数变为 1，该卡保留在卡组中，正常进入下一轮 CD

---

### Requirement: Ammo（弹药）关键词

标记为 Ammo 的 Keyword Effect 必须（SHALL）在卡牌每次使用完成后，将内部计数减 1。计数归零时，该卡必须（SHALL）进入休眠状态，不再参与 CD 循环，但保留在卡组中继续占用 Size。

休眠的弹药卡必须（SHALL）可被「装填」类 Action 恢复弹药数，恢复后重新参与 CD 循环。

KeywordParams 必须（SHALL）包含 `Uses`（int），表示总可用次数。

#### Scenario: 弹药用完后休眠
- **WHEN** 一张 Ammo(Uses=3) 的卡第三次使用完成
- **THEN** 计数变为 0，该卡进入休眠状态，不再推进 CD，但仍占用 Size

#### Scenario: 休眠弹药卡被装填恢复
- **WHEN** 另一张卡的 Action 对休眠的弹药卡执行装填操作
- **THEN** 弹药卡恢复指定弹药数，重新参与 CD 循环

---

### Requirement: Weapon（武器）关键词

标记为 Weapon 的 Keyword Effect 必须（SHALL）在 TCA 结算之前介入。系统必须（SHALL）从该卡在卡组中的位置**向上搜索**，找到第一张带有武器 Tag 的法宝卡（CardType=FaBao），提取其属性（元素类型、物理类型），并将提取值填入该卡 TCA Effect 的 Action Context 中对应的空位字段。

如果找不到武器卡，空位必须（SHALL）保持为 None/无属性。

#### Scenario: 武器关键词补全攻击元素
- **WHEN** 一张带有 Weapon Keyword 的卡 CD 到达，其 TCA Effect 为 Attack(None, "Zhan", 3)，卡组上方有一张火属性武器卡
- **THEN** 系统将 Attack 的 Element 空位补全为 "Huo"，最终以 Attack("Huo", "Zhan", 3) 进入结算

#### Scenario: 找不到武器卡
- **WHEN** 一张带有 Weapon Keyword 的卡 CD 到达，但卡组上方没有武器卡
- **THEN** Action Context 空位保持为 None，以 Attack(None, "Zhan", 3) 进入结算

---

### Requirement: Anchored（锁位）关键词

标记为 Anchored 的 Keyword Effect 必须（SHALL）使该卡免疫位移类 Action 的影响。当位移 Action 选中该卡作为目标时，位移必须（SHALL）无效化。

#### Scenario: 位移对锁位卡无效
- **WHEN** 对方的位移 Action 试图将一张带有 Anchored Keyword 的卡移到卡组底部
- **THEN** 位移被拦截，该卡位置不变

---

### Requirement: Rush（速攻）关键词

标记为 Rush 的 Keyword Effect 必须（SHALL）使该攻防卡在 CD 到达后跳过待发槽机制，直接作为直击打向对方本体（全额伤害）。

#### Scenario: 速攻卡直接直击
- **WHEN** 一张带有 Rush Keyword 的攻击卡 CD 到达
- **THEN** 该卡不进入待发槽，直接以全额攻击值对敌方造成直击伤害，然后 CD 重置

---

### Requirement: Fortify（坚守）关键词

标记为 Fortify 的 Keyword Effect 必须（SHALL）使该卡在待发槽中时，不会被后续 CD 到达的卡挤出。

如果待发槽已有 Fortify 卡且新卡也要入槽，新卡必须（SHALL）直接直击对方（等同于被挤出的行为转移到新卡上）。

#### Scenario: 坚守卡不被挤出
- **WHEN** 待发槽中有一张带 Fortify 的卡，另一张攻防卡 CD 到达要入槽
- **THEN** Fortify 卡保持在槽中，新到的卡直接作为直击打向对方

---

### Requirement: Sluggish（迟缓）关键词

标记为 Sluggish 的 Keyword Effect 必须（SHALL）在战斗初始化时，将该卡的首次 CD 翻倍。后续 CD 循环恢复正常值。

#### Scenario: 迟缓卡首次 CD 翻倍
- **WHEN** 战斗开始，一张 CD=4 的卡带有 Sluggish Keyword
- **THEN** 该卡的首次 CD 为 8，第一次使用后恢复正常 CD=4

---

### Requirement: Overcharge（超载）关键词

标记为 Overcharge 的 Keyword Effect 必须（SHALL）在 CD 到达、TCA 结算之前介入。系统必须（SHALL）检查灵元池中与该卡攻击元素相同的剩余灵元，每消耗 KeywordParams.ManaPerPoint 个灵元，将拼点数值 +1，最多额外增加 KeywordParams.MaxExtra 点。

#### Scenario: 超载提升拼点数值
- **WHEN** 一张 Overcharge(ManaPerPoint=1, MaxExtra=2) 的火系攻击卡 CD 到达，灵元池有 3 个火灵元
- **THEN** 消耗 2 个火灵元（受 MaxExtra=2 限制），拼点数值 +2，剩余 1 个火灵元