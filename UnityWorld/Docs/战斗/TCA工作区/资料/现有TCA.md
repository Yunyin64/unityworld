# 现有 TCA

资料版本 `TCA-E-2026-11-23.1` · 已发布 · 2026-11-23（北京时间）

项目基线 `master` / `85cd9577290b8f59c8b57907ce2f758a87ab2c91`

[玩法与接口索引](玩法与接口索引.md) · [发布状态、验证与反馈](变更与反馈.md) · [现行目录规格](../../../../openspec/specs/combat-tca-catalog/spec.md)

目录共 **32 个 Ready T、21 个 Ready C、45 个 Ready A 模板**，T 和 C 的计数含空节点。14 条进阶记录可手动选用，抽样权重为 0。另有 **8 个 Pending 条目**，保留原 ID、参数及评分供后续设计。各表均以页首提交为准。Ready 表示正式目录收录可执行入口，Pending 表示正式目录保留历史或待接入设计。配置 34 行 T 对应 34 个唯一 ID，当前无重复。所有 Ready 模板均有目录及 Lua 签名回归，行为测试和实卡采用以条目证据为准。[目录校验](../../../../Scripts/Tests/TcaAlignmentTests.Catalog.cs)卡牌和词条 Lua 通过 `[APIFunc]` 调用 C# 结算。

随机组合另用 `trigger_none` 表示无额外触发，并复用 `cond_none` 表示无条件。两者按 JSON 权重抽取。 [抽样规则与组合问题](../随机组合试验/组合规则与问题.md)

## T · 触发与生命周期

[触发配置](../../../../Data/TriggerDefines.json) · [卡牌 Use、Apply、Tick 和事件接收](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) · [待发、受击和对拼](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) · [补灵和 MP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) · [战斗事件队列](../../../../Scripts/Game/Domain/Combat/CombatScene/CombatSceneEvents.cs)

事件接收方通过 `ctx.Caster` 与 `ctx.SourceCard` 判断是不是己方或本卡。卡牌发动过程中产生的事件先入队，发动作用域结束后派发。发动计数、效果执行和事件接收是不同阶段。

| Ready 事件 | 派发时机 | 上下文 | 主线派发位置 |
| --- | --- | --- | --- |
| OnUse | 成功提交灵元和行额度后。拼点卡先入槽，法宝先激活或续期。事件携带发动时的占格快照 | Caster、SourceCard | [CombatCard.Use、Apply](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) |
| OnApply | 卡自身 Apply 与词条回调完成后 | Caster、SourceCard | [CombatCard.Use、Apply](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) |
| OnAttack | 非平局对拼的攻击侧，含败方。攻击直击也派发 | Caster、SourceCard | [ResolveContest、Straight](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnDefend | 非平局对拼的防御侧，含败方 | Caster、SourceCard | [ResolveContest、Straight](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnStraight | 轮末待发向有效对手走直击流程，含防御待发 | Caster、SourceCard | [ResolveContest、Straight](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnContest | 双方实际配对，读取最终点数前。平局也经过此阶段 | ContestA、ContestB、ContestCardA、ContestCardB、ContestTypeA、ContestTypeB | [ResolveContest、Straight](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnRoundStart | 本方首轮或新一轮开始，在供能和基础补灵之前 | Caster、Round | [BeginBackpackRound](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcRoundFunc.cs) |
| OnRoundEnd | 仍在战斗的角色完成本方最后一行的拼点、直击与伤害后，轮状态到期前 | Caster、Round | [FinishScanRows](../../../../Scripts/Game/Domain/Combat/CombatScene/CombatSceneFunc.cs) |
| OnCardDisplaced | 四向滑移成功提交后，可用 WasCardDisplaced 限定被移动者 | Caster 为被移动卡的主人，SourceCard 与 MovedCard 均为被移动卡。Direction、OldX、OldY、NewX、NewY | [TrySlide](../../../../Scripts/Game/Domain/Combat/CombatBackpack/CombatBackpackFunc.cs) |
| OnDisplaceOtherCard | 实际移动了其他角色的卡时，从发起方再广播一次 | Caster 为发起者，SourceCard 为发起卡，MovedCard、实际方向和前后锚点坐标沿用移动结果 | [TrySlide](../../../../Scripts/Game/Domain/Combat/CombatBackpack/CombatBackpackFunc.cs) |
| OnAmmoExhausted | 本次实扣使余弹由正数变为零 | Caster、SourceCard、Amount（实际扣弹） | [CurrentAmount、TryConsume](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardDataAmount.cs) |
| OnConsumed | 实际扣除一批物品份数 | Caster、SourceCard、Amount、Stack | [CurrentAmount、TryConsume](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardDataAmount.cs) |
| OnFlexible | 灵活的请求已使卡实际离场，格子与 SP 可用 | Caster、SourceCard、RemovalReason | [Flexible.OnLeave](../../../../Data/LuaScripts/Keywords/Flexible.lua) |
| OnExhausted | 本次扣除使消耗由正数变为零 | Caster、SourceCard、Amount、Stack | [CurrentAmount、TryConsume](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardDataAmount.cs) |
| OnCardLeave | 场上或候补实际移除后。被移除卡先执行局部 Hook OnLeave | Caster、SourceCard、RemovalReason | [DealFieldChange](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcCardFunc.cs) |
| OnContestOverflow | 确定对拼结果后、两卡 Apply 前 | 双方角色、卡、类型、最终点数、IsDraw、Overflow。有胜负时附带 Winner、Loser 及各自卡、类型、类型名和点数，Caster、SourceCard 为胜方。平局时 Caster、SourceCard 取配对 A 方 | [ResolveContest、Straight](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnBaseManaDraw | 开场和本方轮末，在基础放灵元之前 | Caster 为补灵方 | [DoManaDraw、DrawMana、ChangeMP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) |
| OnManaDraw | 基础补灵或主动 Draw 实际放入灵元并扣 MP 后 | Caster 为补灵方 | [DoManaDraw、DrawMana、ChangeMP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) |
| OnMPChanged | MP 实际变化后 | Caster 为 MP 所属角色 | [DoManaDraw、DrawMana、ChangeMP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) |
| OnDamage | 护盾吸收后实际扣 HP 时，在 HP 清零处理前 | Caster 为受伤者，SourceCard 为伤害来源（可空） | [CombatNpc.ApplyDamage](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs) |
| OnHeal | 实际正向回复 HP 后 | Caster 为受治疗者 | [ApplyHeal](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) |
| OnArtifactActivated / OnArtifactRenewed / OnArtifactDeactivated | 法宝分别激活、成功续期和失活 | Caster、SourceCard | [FaBao.OnScanEnd、OnPhaseChanged](../../../../Data/LuaScripts/Keywords/FaBao.lua) |
| OnManaCharged | 一枚灵元实际充入卡 | Caster、SourceCard、ChargedElement、LitCount、IsFullyCharged | [DispatchManaCharged、DispatchManaUsed](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardManaFunc.cs) |
| OnWoundGained / OnWoundHealed | 新伤势实际落格或因治疗释放占格 | Caster、SourceCard、WoundSize。治疗移除时还有 RemovalReason | [DealFieldChange](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcCardFunc.cs) |
| OnManaUsed | 进阶。一次发动实际提交灵元支付 | Caster、SourceCard、Total、Kinds、各出现元素的 PaidJin / PaidMu / PaidShui / PaidHuo / PaidTu | [DispatchManaCharged、DispatchManaUsed](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardManaFunc.cs) |
| OnElementStackChanged | 进阶。指定五行状态实际变化 | Caster、ChangedBuffId、Before、After、Delta | [五行状态层数变更](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs) |
| OnShieldExhausted | 进阶。灵盾从正数降到零 | Caster、SourceCard、SourceNpc、Before、Reason（Damage/Effect） | [CombatNpc.DispatchShieldExhausted](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs) |
| OnShieldChargeReady | 进阶。灵盾实际吸收累计达标 | Caster、SourceBuff、Round、Absorbed、Threshold、DamageElement、ChargeBuffId，SourceCard 可为空 | [CombatNpc.ApplyDamage](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs) |

卡牌局部回调由生命周期直接调用。与全场广播使用同一 Lua 环境。

| 局部回调 | 时机 |
| --- | --- |
| Contest | 法术或招式末格发动时构造待发拼点 |
| Tick、OnTick、OnPassiveTick | 卡自身、词条、已激活被动或 Modifier 的逐 Tick 生命周期 |
| OnPassiveActivated、OnPassiveDeactivated | 被动激活状态改变。连续续期不重复执行激活回调 |
| OnPendingCancelled | 已支付的待发因败退或战斗结束被取消。Reason 记录原因，灵活可据此检查余量 |
| OnBeforePairedContest | 实际配对比较之前，激活的来源可调整本次招式拼点。长剑在此消费加成 |

行为样例可查[对拼](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs)、[轮次与装备](../../../../Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs)、[空间](../../../../Scripts/Tests/TcaAlignmentTests.Spatial.cs)、[资源与离场](../../../../Scripts/Tests/TcaAlignmentTests.Consumables.cs)、[扩展](../../../../Scripts/Tests/TcaAlignmentTests.Extension.cs)。剑斩、剑刺生成拼点，长剑监听结算。其他事件在行为夹具中的采用范围以各测试为准。

待实现触发保留在[触发配置](../../../../Data/TriggerDefines.json)。

| Pending ID | 原设计与当前缺口 |
| --- | --- |
| OnDeath | 敌人死亡时触发。当前已接入的是伤势和败退结算 |
| OnTick | 全场逐 Tick 触发。局部周期回调按各自生命周期调用 |

## C · 判断与选目标

[条件 Lua 包装](../../../../Data/LuaScripts/CombatCard_Condition.lua) → [CombatBaseFunc 条件实现](../../../../Scripts/Game/Domain/!Global/API/Combat/Condition/CombatBaseCondition.cs)。下表均为主线定义，Ready 条目的参数列摘录 Lua 签名，期望的 C 类型和参数选项查 ID 链接内的 JSON。

| Ready 模板 ID | 玩家语义 | 调用与参数 | Lua 返回 | 行为回归 |
| --- | --- | --- | --- | --- |
| [`cond_check_consume_count`](../../../../Data/Condition/ConditionTemplate_Consume.json) | 本卡库存满足 Equal、AtLeast 或 AtMost | `CheckConsumeCount(ctx, self.m_Self, "{Comparison}", {Amount})` | 布尔值 | 资源与离场 |
| [`cond_ammo_exhausted`](../../../../Data/Condition/ConditionTemplate_Consume.json) | 本卡有弹药机制且余弹为零 | `CheckAmmoExhausted(ctx, self.m_Self)` | 布尔值 | 资源与离场 |
| [`cond_has_consume_stock`](../../../../Data/Condition/ConditionTemplate_Consume.json) | 本卡至少有一份消耗库存 | `CheckConsumeCount(ctx, self.m_Self, "AtLeast", 1)` | 布尔值 | 资源与离场 |
| [`cond_self_has_keyword`](../../../../Data/Condition/ConditionTemplate_Consume.json) | 监听卡有指定词 | `HasKeyword(ctx, self.m_Self, "{Keyword}")` | 布尔值 | 目录及 资源与离场 |
| [`cond_source_has_keyword`](../../../../Data/Condition/ConditionTemplate_Consume.json) | 事件来源卡有指定词，允许来源为空 | `HasKeyword(ctx, ctx.SourceCard, "{Keyword}")` | 布尔值 | 资源与离场 |
| [`cond_check_contest_outcome`](../../../../Data/Condition/ConditionTemplate_Contest.json) | 指定角色在实际配对中胜、负或平，读 OnContestOverflow | `CheckContestOutcome(ctx, self.m_Owner, "{Outcome}")` | 布尔值 | 对拼 |
| [`cond_none`](../../../../Data/Condition/ConditionTemplate_Selector.json) | 无前置限制 | 无 | 恒真占位，无 Lua 表达式 | 目录 |
| [`cond_all_cards_in_domain`](../../../../Data/Condition/ConditionTemplate_Selector.json) | 选己方或对手全体场上卡 | `SelectCards(ctx, "{Domain}")` | 多目标列表 | 目标 |
| [`cond_random_card`](../../../../Data/Condition/ConditionTemplate_Selector.json) | 随机选己方或对手的一张场上卡 | `SelectCards(ctx, "{Domain}")` | 至多一卡的列表 | 目标 |
| [`cond_adjacent_in_field_order`](../../../../Data/Condition/ConditionTemplate_Selector.json) | 取场序前后各一张 | `SelectCards(ctx, "Adjacent")` | 至多两卡的列表 | 目标 |
| [`cond_select_directional_cards`](../../../../Data/Condition/ConditionTemplate_Spatial.json) | 按占格共边选邻卡 | `SelectDirectionalCards(ctx, "{Direction}", "{Keyword}")` | 多目标列表 | 空间 |
| [`cond_has_directional_card`](../../../../Data/Condition/ConditionTemplate_Spatial.json) | 判断指定方位是否有匹配邻卡 | `HasDirectionalCard(ctx, "{Direction}", "{Keyword}")` | 布尔值 | 空间 |
| [`cond_count_directional_cards`](../../../../Data/Condition/ConditionTemplate_Spatial.json) | 匹配的共边实例各计一次 | `CountDirectionalCards(ctx, "{Direction}", "{Keyword}")` | 整数（Scale） | 空间 |
| [`cond_was_card_used_on_side`](../../../../Data/Condition/ConditionTemplate_Spatial.json) | 指定方位有卡成功发动，读 OnUse 快照 | `WasCardUsedOnSide(ctx, self.m_Self, "{Direction}", "{Keyword}")` | 布尔值 | 空间 |
| [`cond_was_card_displaced`](../../../../Data/Condition/ConditionTemplate_Spatial.json) | 本卡是实际被移动的卡，读 OnCardDisplaced | `WasCardDisplaced(ctx, self.m_Self)` | 布尔值 | 空间 |
| [`cond_check_mana_charged`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 筛选实充的五行、接收卡和充满状态，读 OnManaCharged | `CheckManaCharged(ctx, "{WantedElement}", "{Keyword}", {OnlyFull})` | 布尔值 | 扩展 |
| [`cond_check_wound_event`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 筛选所属阵营和伤势体量，读 OnWoundGained 或 OnWoundHealed | `CheckWoundEvent(ctx, self.m_Owner, "{Side}", {AtLeastSize})` | 布尔值 | 扩展 |
| [`cond_check_mana_used`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 进阶。检查本次实付数、种类数或某五行数量，读 OnManaUsed | `CheckManaUsed(ctx, "{Measure}", {AtLeast})` | 布尔值 | 扩展 |
| [`cond_check_element_stack_change`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 进阶。筛选指定五行状态增减层，读 OnElementStackChanged | `CheckElementStackChange(ctx, "{StatusId}", "{Change}")` | 布尔值 | 扩展 |
| [`cond_card_moved_into_side`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 进阶。匹配从未共边到共边的方位变化，读 OnCardDisplaced | `WasCardMovedIntoSide(ctx, self.m_Self, "{Direction}", "{Keyword}")` | 布尔值 | 扩展 |
| [`cond_check_shield_exhaustion`](../../../../Data/Condition/ConditionTemplate_TcaExtension.json) | 进阶。区分灵盾因伤害或效果降为零，读 OnShieldExhausted | `CheckShieldExhaustion(ctx, "{Cause}")` | 布尔值 | 扩展 |

[目标](../../../../Scripts/Tests/TcaAlignmentTests.Targeting.cs) · [空间](../../../../Scripts/Tests/TcaAlignmentTests.Spatial.cs) · [对拼](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs) · [资源与离场](../../../../Scripts/Tests/TcaAlignmentTests.Consumables.cs) · [扩展](../../../../Scripts/Tests/TcaAlignmentTests.Extension.cs)

选卡返回独立列表，同时把该批结果登记给 `Selected`。无合法目标时清掉旧选中结果。`Adjacent` 按场序取邻卡，几何共边见[目标与坐标](玩法与接口索引.md#目标与坐标)。`IsFabao`、`IsFaShu`、`IsGongFa`、`IsItem`、`IsEquip`、`IsZhaoShi` 保留为接受 CombatCard 并返回布尔值的旧入口。

| Pending ID | 原模板 | 当前缺口 |
| --- | --- | --- |
| [`cond_all_card`](../../../../Data/Condition/ConditionTemplate_Selector.json) | `AllCard(ctx, "{Target}")` | 旧模板传字符串，Scope 入口需角色实例 |
| [`cond_random_card_in_cd`](../../../../Data/Condition/ConditionTemplate_Selector.json) | `RandomCardInCD(ctx, "{Target}")` | 待明确背包扫描下的候选状态及参数 |
| [`cond_adjacent_cards`](../../../../Data/Condition/ConditionTemplate_Selector.json) | `AdjacentCards(ctx, "{Direction}")` | 待明确参考卡与各方向选择范围 |

## A · 动作模板及入口

下表均为主线 Ready。名称以模板 ID 和函数名双索引。效果示例用于说明语义，实卡和行为回归以条目标示为准。参数是模板给出的选择，运行语义以实现列为准。实现列按文件名和方法定位，源码类型为 `CombatBaseFunc`。调用包装分为 [角色动作](../../../../Data/LuaScripts/CombatNpc_Action.lua)、[卡动作](../../../../Data/LuaScripts/CombatCard_Action.lua)、[拼点](../../../../Data/LuaScripts/CombatNpc_Contest.lua) 和 [光环](../../../../Data/LuaScripts/CombatCard_Aura.lua)，均调用 `API:Execute`。同类规格、最小用法见 [接口索引](玩法与接口索引.md)。

| 模板 ID / 函数 | 玩家语义 | 参数 | 设计、模板与实卡 | 主线实现 |
| --- | --- | --- | --- | --- |
| `action_atk` / `Attack` | 生成待发攻击拼点，伤害待对拼或轮末处理 | 五行 Element、Zhan/Ci/Da/SheJi、AttackValue | [拼点模板](../../../../Data/Action/ActionTemplate_Contest.json)；[剑刺](../../../../Data/Card/Lua/card_form_jian_ci.lua) Contest，读取装备元素和攻击底值 | [CombatBaseContest.Attack](../../../../Scripts/Game/Domain/!Global/API/Combat/Contest/CombatBaseContest.cs) → `TryPushToPendingSlot` |
| `action_defend` / `Defend` | 生成待发防御拼点 | Shield/Block/Dodge、DefendValue | 同上；[回归卡 bp_guard](../../../../Data/Card/Lua/bp_guard.lua) Contest 走 Block。Shield 拼点与持久盾分别记账 | 同文件 `Defend` → `TryPushToPendingSlot` |
| `action_gain_shield` / `GainShield` | 增加现有持久灵盾，减伤后优先消耗 | Domain、Amount | 4 盾受 3 点伤害后余 1 盾，再受 2 点则扣余盾及 1 HP。[伤害回归 VerifyDamageDefense](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs) | [防御模板](../../../../Data/Action/ActionTemplate_Defense.json) · [GainShield → ChangeShield](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_add_armor` / `AddArmor` | 获得一次性护甲，仅敌方正向伤害消费 | Domain、Amount | 护甲 2 遇 5 点敌方伤害，进入后续结算的伤害剩 3。[伤害回归 VerifyDamageDefense](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs) | 同上 · [AddArmor → CombatNpc.AddArmor](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_add_fortification` / `AddFortification` | 本方当轮的所有入伤固定减免 | Domain、Amount | 坚甲 1 使本轮两次 5 点伤害各减 1，到本方下轮失效。[伤害回归 VerifyDamageDefense](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs) | 同上 · [AddFortification → CombatNpc.AddFortification](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_add_deflection` / `AddDeflection` | 获得卸力，下一次与敌方攻击配对时减点并消费 | Domain、Amount | `AddDeflection(ctx, "Self", 3)` 使下次配对的敌方 5 点攻击减至 2。[对拼回归 VerifyDeflection](../../../../Scripts/Tests/TcaAlignmentTests.Contest.cs) | [防御模板](../../../../Data/Action/ActionTemplate_Defense.json) · [卸力 Lua](../../../../Data/Modifier/CombatModifierDefines/Lua/Deflection.lua) · [AddDeflection → 卸力 Lua](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_change_equipment_stat` / `ChangeEquipmentStat` | 修改指定装备或招式所属装备的攻防 | Domain、Stat、Delta、Rounds | 长剑攻击 4 加 2 后，新生成剑刺用 6 点。[装备回归 VerifyEquipmentAndAffinity](../../../../Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs) | [装备模板](../../../../Data/Action/ActionTemplate_BattleEquipment.json) · [ChangeEquipmentStat → CombatNpc.ChangeEquipmentStat](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_change_affinity` / `ChangeAffinity` | 修改目标战斗抽灵权重 | Domain、Element、Delta、Rounds | 当前只有木权重为正时，抽取全部是木灵元。[亲和回归 VerifyEquipmentAndAffinity](../../../../Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs) | [亲和与供能模板](../../../../Data/Action/ActionTemplate_BattleAffinity.json) · [ChangeAffinity → ChangeBattleAffinity](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) |
| `action_add_artifact_power` / `AddArtifactPower` | 轮首随机给场上法宝补当前需要的灵元 | Domain、Element、Amount、Rounds | 火供能只补火，None 可以补任一匹配五行。[供能回归 VerifyArtifactPower](../../../../Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs) | 同上 · [AddArtifactPower → PowerArtifacts](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) |
| `action_refill_ammo` / `RefillAmmo` | 补充余弹至有效上限 | Domain、Amount | [弹药模板](../../../../Data/Action/ActionTemplate_Ammo.json) · [弹药 Lua 回归](../../../../Scripts/Tests/TcaAlignmentTests.Consumables.cs) | [CombatBaseFunc.RefillAmmo](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `CombatCard.RefillAmount` |
| `action_slide_card` / `SlideCard` | 沿四向滑到障碍前，灵元可穿越，终点灵元让位 | Domain、Direction | 作用于完整卡形，保持蓄灵与 SP。[空间回归 VerifySpatialEffects、VerifyRandomSlide](../../../../Scripts/Tests/TcaAlignmentTests.Spatial.cs) | [位移模板](../../../../Data/Action/ActionTemplate_Spatial.json) · [SlideCard → TrySlide](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_heal` / `Heal` | 治疗至多 HealValue 点，受最大 HP 限制 | Domain、HealValue | [效果模板](../../../../Data/Action/ActionTemplate_Effect.json)；[再生](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Mu.lua) 用于基础补灵 | [CombatBaseAction.Heal](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `ApplyHeal`，实际回复后派发 OnHeal |
| `action_self_damage` / `SelfDamage` | 给选中的角色排入伤害，经过受伤管线 | Domain、DamageValue | 同上。 [出血](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Jin.lua) 基础补灵自伤 | `CombatBaseAction.SelfDamage` → `AddDamage`。默认斩击。对敌用施加者作为来源，使护甲参与结算。自伤沿用无来源伤害 |
| `action_armor_break` / `ArmorBreak` | 只扣持久盾，扣量至多当前盾 | Domain、BreakValue | 同上；[破甲](../../../../Data/Card/Lua/card_jin_armor_break.lua) Apply 令 Target 破盾 5，定义标 Unmigrated | `CombatBaseAction.ArmorBreak` → `ChangeShield` |
| `action_add_npc_buff` / `AddNpcBuff` | 按定义附加或叠加战斗 Modifier | Domain、BuffId、Stacks、Duration | 可选 14 种拼点强化的持续版和下次版。[Buff 模板](../../../../Data/Action/ActionTemplate_Buff.json) · [强化定义](../../../../Data/Modifier/CombatModifierDefines/Contest_Improved.json) | `CombatBaseAction.AddNpcBuff` → `CombatNpc.AddModifier` |
| `action_add_stat_buff` / `AddStatBuff` | 加持久属性修正 | Domain、StatId（HpMax）、Value、ModifierType（Flat）；Lua 可传 SourceId | 同上；[兽肉](../../../../Data/Card/Lua/card_monster_meat.lua) Apply 带 SourceId，定义标 Unmigrated | `CombatBaseAction.AddStatBuff` → `CombatNpc.AddStatBuff`，旧 Min/Max/ManaAdj 系列会拒绝 |
| `action_add_card_stat_buff` / `AddCardStatBuff` | 给选卡加卡牌属性修正 | 卡牌 Domain、StatId（AmountMaxAdd）、Value | 同上；金针 Lua 传卡对象并尝试 ManaAdj_Jin，卡定义标 Deprecated，禁止当作可用样例 | [CombatCDAction.AddCardStatBuff](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CardModifier.StatBuff`，工厂设 Never。效果测试通过废弃属性拒绝断言 |
| `Charge` / `Charge` | 增加整包基准扫描进度 | Self/Target、ReduceTick | [时序模板](../../../../Data/Action/ActionTemplate_CardCD.json)；[爆燃](../../../../Data/Card/Lua/card_huo_burst.lua) Apply 调用，定义标 Migrated | `CombatCDAction.Charge` → `Backpack.Charge`；时序、效果子项 |
| `action_freeze` / `Freeze` | 冻结有扫描资格且未冻结、未受免疫保护的主动卡，消耗卡还须有库存，目标下次完整扫描跳过发动并解冻 | 卡牌 Domain、FreezeTime（兼容占位） | 同上；[水系冻结](../../../../Data/Card/Lua/card_shui_freeze.lua) Apply 随机冻敌卡 | `CombatCDAction.Freeze` → `CombatCard.Freeze`，随机前筛掉无效目标 |
| `action_slow` / `Slow` | 整包半速，同类延时 | Self/Target、DurationTicks | 同上；[土系减速](../../../../Data/Card/Lua/card_tu_slow.lua) Apply | `CombatCDAction.Slow` → `Backpack.AddSlow`；时序、效果子项 |
| `action_haste` / `Haste` | 整包双速，同类延时 | Self/Target、DurationTicks | 同上；[木系加速](../../../../Data/Card/Lua/card_mu_haste.lua) Apply | `CombatCDAction.Haste` → `Backpack.AddHaste`；时序、效果子项 |
| `action_remove_wound` / `RemoveRandomWound` | 随机选一张场上伤口排队移除 | Domain、Size、Exact（true 等于，false 至多） | [卡组模板](../../../../Data/Action/ActionTemplate_Deck.json)；Exact=true 选等体量，false 选至多该体量。[伤势选择回归](../../../../Scripts/Tests/TcaAlignmentTests.Targeting.cs)，实卡调用待证 | `CombatBaseAction.RemoveRandomWound` → `HealWound` → `DealFieldChange` |
| `action_convert` / `Convert` | 灵元转化／回收格上灵元为 MP，1 个回 1 点 | Domain、Element（None 任意）、MaxAmount | [灵元模板](../../../../Data/Action/ActionTemplate_Mana.json)；[迁移规格](../../../../openspec/specs/combat-mana-manipulation/spec.md)；本版通过满 MP、MP 空缺、指定元素、保护已点亮测试 | [CombatManaAction.Convert](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) → `RemoveMana`、`RecoverMP` |
| `action_recover_mp` / `RecoverMP` | 直接回复 MP，受上限限制 | Domain、Amount | [MP 模板](../../../../Data/Action/ActionTemplate_MP.json)；浩瀚在基础补灵前按层回复 | CombatManaAction.RecoverMP → CombatNpc.RecoverMP |
| `action_reduce_mp` / `ReduceMP` | 直接减少 MP，最低降至零 | Domain、Amount | 同上；石化在基础补灵前按层削减 | CombatManaAction.ReduceMP → CombatNpc.ReduceMP |
| `action_draw` / `Draw` | 立刻将 MP 放成格上灵元，实际入格才扣款 | Domain、Amount | [灵元模板](../../../../Data/Action/ActionTemplate_Mana.json)；[回灵](../../../../Data/Card/Lua/card_huo_drain.lua) Apply 抽 3；效果测试通过 Draw 链 | `CombatManaAction.Draw` → `CombatNpc.DrawMana` |
| `action_reduce_mana` / `ReduceMana` | 摧毁格上灵元，不足部分成功 | Domain、Element（None 任意）、Amount | [灵元模板](../../../../Data/Action/ActionTemplate_Mana.json)；效果测试真实 Lua 减灵及后续 Draw，未在生产卡中定位独立调用 | `CombatManaAction.ReduceMana` → `RemoveMana`；保留已点亮资源，MP 无变化 |
| `action_add_element_buff` / `AddElementBuff` | 循环施加元素状态，每次加一层 | Domain、Element、IsDebuff、Count | [五行状态模板](../../../../Data/Action/ActionTemplate_ElementBuff.json)；[锐意](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Jin.lua) 与 [中毒](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Mu.lua) 真实 Lua。具名同族下列 | `CombatManaAction.AddElementBuff` → `AddModifier` 人物叠层；None 每次随机 |
| `action_remove_element_buff` / `RemoveElementBuff` | 循环选匹配状态，每次减一层 | Domain、Element、IsDebuff、Count | 同上；[载德](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Tu.lua) 清减益、[灼烧](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Huo.lua) 清增益 | `CombatManaAction.RemoveElementBuff` → `ReduceStack(1)`；None 匹配任一对应正负的五行状态 |

`AddElementBuff` 的十个固定元素/正负变体共用同一个函数和堆叠规则，模板仍分别检索。

| 元素 | 增益模板 | 减益模板 | 参数 |
| --- | --- | --- | --- |
| 金 | `action_add_jin_buff` 锐意 | `action_add_jin_debuff` 出血 | Domain、Count |
| 木 | `action_add_mu_buff` 再生 | `action_add_mu_debuff` 中毒 | 同上 |
| 水 | `action_add_shui_buff` 浩瀚 | `action_add_shui_debuff` 寒意 | 同上 |
| 火 | `action_add_huo_buff` 心火 | `action_add_huo_debuff` 灼烧 | 同上 |
| 土 | `action_add_tu_buff` 载德 | `action_add_tu_debuff` 石化 | 同上 |

### 进阶动作与光环

[扩展动作模板](../../../../Data/Action/ActionTemplate_TcaExtension.json) · [扩展实战夹具](../../../../Scripts/Tests/TcaAlignmentTests.Extension.cs) · [扩展归档](../../../../openspec/changes/archive/2026-11-22-combat-tca-extension/proposal.md)

六项均有模板、Lua 包装、C# 落点和当前控制台回归，正式生产卡采用待证。配置权重为 0，手工选用须按进阶机制评审。

| 模板 ID / 函数 | 玩家语义 | 参数 | 主线实现 |
| --- | --- | --- | --- |
| `action_strip_card_mana` / `StripCardMana` | 按充入先后剥离每张目标卡已点亮的匹配灵元，重算蓄灵阶段 | 卡牌 Domain、Element（None 任意）、Amount | [CombatManaAction.StripCardMana](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) → `StripAssignedMana`，ctx 记 RemovedCount |
| `action_add_element_immunity` / `AddElementImmunity` | 登记指定五行状态的免疫次数，抵掉后续匹配的施加 | 角色 Domain、BuffId、Amount | [CombatManaAction.AddElementImmunity](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) → `AddModifier` |
| `action_thaw_card` / `ThawCard` | 立刻解除已有冻结，随后按原末格时机发动 | 卡牌 Domain | [CombatCDAction.ThawCard](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CombatCard.Thaw` |
| `action_freeze_immunity_aura` / `GrantFreezeImmunityAura` | 来源激活时按当前位置和词过滤己方卡，给覆盖目标免疫新冻结 | Direction=All/四向/Neighbor、Keyword | [CombatBaseAura.GrantFreezeImmunityAura](../../../../Scripts/Game/Domain/!Global/API/Combat/Aura/CombatBaseAura.cs) → `CombatCard.IsFreezeImmune`。类型 Aura |
| `action_root_card` / `RootCard` | 目标在若干所属扫描轮内无法滑移，含本轮 | 卡牌 Domain、Rounds | [CombatCDAction.RootCard](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CombatCard.Root`，由 `TrySlide` 检查 |
| `action_add_shield_charge` / `AddShieldCharge` | 持久灵盾的实际吸收达标，同元素档每轮触发一次 | 角色 Domain、Element（None 任意）、Amount（阈值） | [CombatBaseAction.AddShieldCharge](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `CombatNpc.AddShieldCharge`、`AccumulateShieldCharge` |

### 保留的待接入动作

旧线性位移、候补部署和召回保留为 Pending。四向滑移由独立节点提供。

| Pending ID / 函数 | 原设计与参数 | 主线接口状态 |
| --- | --- | --- |
| `action_displace` / `Displace` | 旧线性位移，DoMain、Position（First/Last/Random）。 [模板](../../../../Data/Action/ActionTemplate_Deck.json) | 兼容入口拒绝执行。 [CombatBaseFunc.Displace](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) |
| `action_deploy` / `Deploy` | 从候补池部署指定卡，Domain、CardId。 [模板](../../../../Data/Action/ActionTemplate_Reserve.json) | 兼容入口拒绝执行。 [CombatBaseFunc.Deploy](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatReserveAction.cs) |
| `action_recall` / `Recall` | 从运转池召回指定卡，Domain、CardId。 [模板](../../../../Data/Action/ActionTemplate_Reserve.json) | 兼容入口拒绝执行。 [CombatBaseFunc.Recall](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatReserveAction.cs) |

### 已有函数和内联效果

| 检索名 | 接入与边界 |
| --- | --- |
| 持久加盾 | [测试卡 bp_focus](../../../../Data/Card/Lua/bp_focus.lua) 的 Apply 内联 `self.m_Owner:ChangeShield(2)`，新效果可使用上表的 GainShield 模板。 |
| 长剑下次招式对拼 +1 | [长剑脚本](../../../../Data/Card/Lua/card_fabao_jian.lua) 的 `OnContestOverflow`、`OnBeforePairedContest`、`OnPassiveDeactivated` 串联卡专用规则。拼点消费与法宝失活清理均有回归 |
| 灵元改色／换色、指定格换位 | [纳音设计](../../../../Data/Card/NayinDesign.md) 有设计来源。改色可组合 C# 两个操作，指定格换位缺核心操作。详见 [背包格能力](玩法与接口索引.md#背包格能力) |
