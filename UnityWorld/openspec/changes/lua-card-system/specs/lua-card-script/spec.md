## ADDED Requirements

### Requirement: Lua 卡牌文件结构
每张卡牌的 .lua 文件 SHALL 位于 `Data/LuaCards/{cardId}.lua`，文件名与 CardDefine.ID 一致。

#### Scenario: 文件路径匹配
- **WHEN** CardDefine.ID = "card_jin_whirlwind"
- **THEN** 对应脚本路径为 `Data/LuaCards/card_jin_whirlwind.lua`

### Requirement: CardData 覆盖声明
Lua 脚本 MAY 声明 `CombatCard.CardData` table 覆盖 CardDefine 中的战斗属性（Size/Cooldown/CardType/ManaCost）。未声明时 SHALL 使用 CardDefine 中的值。

#### Scenario: Lua 覆盖 Cooldown
- **WHEN** .lua 中声明 `CombatCard.CardData = { Cooldown = 6 }`
- **THEN** 战斗中该卡的 CooldownTicks 使用 6×10=60 而非 CardDefine 中的值

#### Scenario: Lua 不声明 CardData
- **WHEN** .lua 中没有 CombatCard.CardData
- **THEN** 使用 CardDefine.json 中的 Cooldown/Size 等值

### Requirement: Keywords 声明
Lua 脚本 MAY 声明 `CombatCard.Keywords` table 列出该卡的关键字效果（如 Initial）。

#### Scenario: 声明 Initial 关键字
- **WHEN** .lua 中声明 `CombatCard.Keywords = {"Initial"}`
- **THEN** 战斗开始时该卡 CD 设为满值（立即就绪）

### Requirement: OnUse Hook
Lua 脚本 SHALL 定义 `function CombatCard:OnUse(ctx)` 作为卡牌使用时的主逻辑。框架在卡牌 CD 就绪时直接调用此函数。

#### Scenario: OnUse 正常执行
- **WHEN** card_jin_whirlwind 的 CD 就绪
- **THEN** 框架调用其 OnUse(ctx)，ctx 中包含 Caster/Target/SelfCardId

### Requirement: 被动 Hook 函数
Lua 脚本 MAY 定义被动 Hook 函数（OnAttack/OnContestWin/OnContestLose/OnDominate/OnDominated/OnHitBody/OnAfterCardUse），框架自动注册为事件监听。

#### Scenario: OnAttack 被动触发
- **WHEN** 卡牌所属 NPC 发起攻击且 EventMgr 触发 trigger_on_attack
- **THEN** 该卡牌的 OnAttack(ctx) 被调用

#### Scenario: 多个被动 Hook 共存
- **WHEN** .lua 同时定义 OnAttack 和 OnContestWin
- **THEN** 两个 Hook 分别注册到对应事件，互不干扰

### Requirement: self 绑定
在 Hook 函数执行时，`self` SHALL 绑定到当前 CombatCard 实例的 Lua 代理，提供 CardId、LogName、CardBuffs 等访问。

#### Scenario: self.CardId 可用
- **WHEN** OnAttack 函数中访问 `self.CardId`
- **THEN** 返回该 CombatCard 的 DefineId 字符串

### Requirement: ctx 参数内容
Hook 函数的 ctx 参数 SHALL 包含：Caster（CombatNpc）、Target（CombatNpc）、SelfCardId（本卡ID）。事件触发型 Hook 还 SHALL 包含事件特定数据（如 OnAfterCardUse 的 ctx.Card）。

#### Scenario: OnUse 的 ctx
- **WHEN** OnUse 被调用
- **THEN** ctx.Caster 为卡牌持有者，ctx.Target 为默认目标，ctx.SelfCardId 为本卡 ID

#### Scenario: OnAfterCardUse 的 ctx.Card
- **WHEN** OnAfterCardUse 被调用
- **THEN** ctx.Card 为刚使用完的卡牌对象，ctx.Card.IsAttack 可判断是否为攻击卡