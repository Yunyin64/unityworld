## ADDED Requirements

### Requirement: 删除 ActionData
ActionData.cs SHALL 被删除。所有使用 ActionData 的代码 SHALL 改为通过 Lua 或 CombatBaseFunc 直接调用。ContestData.FromActionData() 工厂方法 SHALL 被删除。

#### Scenario: ActionData 文件删除
- **WHEN** 执行 TCA 废弃清理
- **THEN** Scripts/Game/Domain/Object/Card/Data/ActionData.cs 被删除

#### Scenario: ContestData 不再从 ActionData 构造
- **WHEN** 需要构造 ContestData
- **THEN** 通过 CombatBaseFunc.Attack/Shield/Block 直接构造，不经过 ActionData

### Requirement: 删除 EffectData
EffectData.cs SHALL 被删除。所有使用 EffectData 的代码 SHALL 改为通过 Lua OnXxx 函数实现效果逻辑。CardMgr.BuildEffectFromDefine() SHALL 被删除。

#### Scenario: EffectData 文件删除
- **WHEN** 执行 TCA 废弃清理
- **THEN** Scripts/Game/Domain/Object/Card/Data/EffectData.cs 被删除

#### Scenario: 卡牌效果不再通过 EffectData 执行
- **WHEN** 战斗中需要执行卡牌效果
- **THEN** 通过 CombatCard.OnUse(ctx) 调用 Lua 函数，不遍历 EffectData.Actions

### Requirement: 删除 CardEffectData
CardEffectData.cs SHALL 被删除。Card.EffectData 属性 SHALL 被移除。Card.Effects 便捷访问器 SHALL 被移除。

#### Scenario: CardEffectData 文件删除
- **WHEN** 执行 TCA 废弃清理
- **THEN** Scripts/Game/Domain/Object/Card/Data/CardEffectData.cs 被删除

#### Scenario: Card 不再持有 EffectData
- **WHEN** 访问 Card 实例
- **THEN** 不存在 EffectData 属性和 Effects 便捷访问器

### Requirement: CombatCard 移除 TCA 相关方法
CombatCard.BuildContestData() 和 IsAttackDefenseCard() SHALL 被删除。拼点数据由 Lua OnUse 中调用 CombatBaseFunc.Attack() 等直接构造。

#### Scenario: BuildContestData 删除
- **WHEN** 需要构造拼点数据
- **THEN** 不再调用 card.BuildContestData()，而是由 Lua OnUse 中调 Attack() 等函数

#### Scenario: IsAttackDefenseCard 删除
- **WHEN** 需要判断卡牌类型
- **THEN** 不再调用 card.IsAttackDefenseCard()，改为通过 CardType 或 Lua CardData 判断

### Requirement: CardMgr 移除 TCA 构造逻辑
CardMgr.BuildEffectFromDefine() 及相关 TCA 构造逻辑 SHALL 被删除。CardMgr.InstantiateFromDefine() SHALL 简化为只构造 Card 的基础属性（BaseData），不再构造 EffectData。

#### Scenario: InstantiateFromDefine 简化
- **WHEN** 调用 CardMgr.InstantiateFromDefine(cardId)
- **THEN** 只构造 Card 的 Id/DefineId/DisplayName/BaseData，不构造 EffectData

### Requirement: 战斗流程移除 TCA 路径
CombatCardFlowHandler、CombatScene.Setup、CombatContestHandler 中所有 TCA 效果结算路径 SHALL 被移除，统一改为通过 CombatCard.OnUse() 等 Lua 实例方法执行。

#### Scenario: CombatCardFlowHandler 不再遍历 Effects
- **WHEN** 结算效果卡
- **THEN** 调用 card.OnUse(ctx)，不遍历 card.Effects

#### Scenario: CombatContestHandler 不再遍历 Effects
- **WHEN** 拼点结算后执行效果
- **THEN** 通过 Lua 钩子或 CombatCard 实例方法执行，不遍历 Effects

### Requirement: ContestData 改为直接构造
ContestData SHALL 提供直接构造方法（不再依赖 ActionData），由 CombatBaseFunc.Attack/Shield/Block 调用。

#### Scenario: CombatBaseFunc.Attack 直接构造 ContestData
- **WHEN** 调用 CombatBaseFunc.Attack(ctx, element, contestType, value)
- **THEN** 直接 new ContestData() 并设置字段，放入 caster.PendingSlot
