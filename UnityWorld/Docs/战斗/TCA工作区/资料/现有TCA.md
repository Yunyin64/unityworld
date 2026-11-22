# 现有 TCA

工作区版本 `TCA-E-2026-11-22.4` · 轮次动作目录修订

[玩法与接口索引](玩法与接口索引.md) · [发布状态、验证与反馈](变更与反馈.md) · [1122TCA拓展实现](../../1122TCA拓展实现.md)

目录共 **32 个 Ready T、21 个 Ready C、45 个 Ready A 模板**，T 和 C 的计数含空节点。14 条进阶记录可手动选用，抽样权重为 0。另有 **8 个 Pending 条目**，保留原 ID、参数及评分供后续设计。写当前可执行效果时使用 Ready 条目。卡牌和词条 Lua 通过 `[APIFunc]` 调用 C# 结算。

随机组合另用 `trigger_none` 表示无额外触发，并复用 `cond_none` 表示无条件。两者按 JSON 权重抽取。 [抽样规则与组合问题](../随机组合试验/组合规则与问题.md)

## T · 触发与生命周期

[触发配置](../../../../Data/TriggerDefines.json) · [卡牌 Use、Apply、Tick 和事件接收](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) · [待发、受击和对拼](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) · [补灵和 MP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) · [战斗事件队列](../../../../Scripts/Game/Domain/Combat/CombatScene/CombatSceneEvents.cs)

事件接收方通过 `ctx.Caster` 与 `ctx.SourceCard` 判断是不是己方或本卡。卡牌发动过程中产生的事件先入队，发动作用域结束后派发。发动计数、效果执行和事件接收是不同阶段。

| 事件 | 派发时机 | 上下文 |
| --- | --- | --- |
| OnUse | 成功提交灵元和行额度后。拼点卡先入槽，法宝先激活或续期 | Caster、SourceCard |
| OnApply | 卡自身 Apply 与词条回调完成后 | Caster、SourceCard |
| OnAttack | 非平局对拼的攻击侧，含败方。攻击直击也派发 | Caster、SourceCard |
| OnDefend | 非平局对拼的防御侧，含败方 | Caster、SourceCard |
| OnStraight | 轮末待发向有效对手走直击流程，含防御待发 | Caster、SourceCard |
| OnContest | 双方实际配对，读取最终点数前。平局也经过此阶段 | ContestA、ContestB、ContestCardA、ContestCardB、ContestTypeA、ContestTypeB |
| OnRoundStart | 本方首轮或新一轮开始，在供能和基础补灵之前 | Caster、Round |
| OnRoundEnd | 本方最后一行完成结算，轮状态到期前 | Caster、Round |
| OnCardDisplaced | 四向滑移成功并写入位置后，可用 WasCardDisplaced 限定被移动者 | Caster、SourceCard、MovedCard、Direction、OldX、OldY、NewX、NewY |
| OnAmmoExhausted | 本次实扣使余弹由正数变为零 | Caster、SourceCard、Amount（实际扣弹） |
| OnConsumed | 实际扣除一批物品份数 | Caster、SourceCard、Amount、Stack |
| OnFlexible | 灵活的请求已使卡实际离场，格子与 SP 可用 | Caster、SourceCard、RemovalReason |
| OnExhausted | 本次扣除使消耗由正数变为零 | Caster、SourceCard、Amount、Stack |
| OnCardLeave | 场上或候补实际移除后。被移除卡自身的局部 Hook 为 OnLeave | Caster、SourceCard |
| OnContestOverflow | 确定对拼结果后、两卡 Apply 前 | 双方角色、卡、类型、最终点数、IsDraw、Overflow。有胜负时附带 Winner、Loser 及各自卡、类型、类型名和点数，Caster、SourceCard 为胜方。平局时 Caster、SourceCard 取配对 A 方 |
| OnBaseManaDraw | 开场和本方轮末，在基础放灵元之前 | Caster 为补灵方 |
| OnManaDraw | 基础补灵或主动 Draw 实际放入灵元并扣 MP 后 | Caster 为补灵方 |
| OnMPChanged | MP 实际变化后 | Caster 为 MP 所属角色 |
| OnDamage | 护盾吸收后实际扣 HP 时，在 HP 清零处理前 | Caster 为受伤者，SourceCard 为伤害来源（可空） |
| OnHeal | 实际正向回复 HP 后 | Caster 为受治疗者 |
| OnArtifactActivated / OnArtifactRenewed / OnArtifactDeactivated | 法宝分别激活、成功续期和失活 | Caster、SourceCard |
| OnManaCharged | 一枚灵元实际充入卡 | Caster、SourceCard、ChargedElement、LitCount、IsFullyCharged |
| OnWoundGained / OnWoundHealed | 新伤势实际落格或因治疗释放占格 | Caster、SourceCard、WoundSize |
| OnManaUsed | 进阶。一次发动实际提交灵元支付 | Caster、SourceCard、Total、Kinds、各五行实付 |
| OnElementStackChanged | 进阶。指定五行状态实际变化 | Caster、ChangedBuffId、Before、After、Delta |
| OnShieldExhausted | 进阶。灵盾从正数降到零 | Caster、SourceCard、Before、Reason |
| OnShieldChargeReady | 进阶。灵盾实际吸收累计达标 | Caster、SourceBuff、Round、Absorbed、Threshold、DamageElement, ChargeBuffId |

卡牌局部回调由生命周期直接调用。与全场广播使用同一 Lua 环境。

| 局部回调 | 时机 |
| --- | --- |
| Contest | 法术或招式末格发动时构造待发拼点 |
| Tick、OnTick、OnPassiveTick | 卡自身、词条、已激活被动或 Modifier 的逐 Tick 生命周期 |
| OnPassiveActivated、OnPassiveDeactivated | 被动激活状态改变。连续续期不重复执行激活回调 |
| OnBeforePairedContest | 实际配对比较之前，激活的来源可调整本次招式拼点。长剑在此消费加成 |

待实现触发保留在[触发配置](../../../../Data/TriggerDefines.json)。

| Pending ID | 原设计与当前缺口 |
| --- | --- |
| OnDeath | 敌人死亡时触发。当前已接入的是伤势和败退结算 |
| OnTick | 全场逐 Tick 触发。局部周期回调按各自生命周期调用 |

## C · 判断与选目标

[选择模板配置](../../../../Data/Condition/ConditionTemplate_Selector.json) · [Domain 实现](../../../../Scripts/Game/Domain/!Global/API/APIDomainFunc.cs) · [接口索引](玩法与接口索引.md#目标与坐标)

| 模板 | 选择规则 |
| --- | --- |
| cond_none | 恒真，无 Lua 表达式 |
| cond_all_cards_in_domain | SelectCards 的 All、TargetAll，取己方或当前对手的全体场上卡 |
| cond_random_card | SelectCards 的 Random、TargetRandom，取己方或当前对手随机至多一张 |
| cond_adjacent_in_field_order | SelectCards 的 Adjacent，取场上顺序中来源卡前后各一张 |
| cond_select_directional_cards | SelectDirectionalCards 取实际占格上、下、左、右或四向邻卡，按 Keyword 过滤，结果可作 Selected 动作目标 |
| cond_has_directional_card / cond_count_directional_cards | 同一方位规则，返回是否存在或匹配实例数 |
| cond_was_card_used_on_side | WasCardUsedOnSide 使用成功发动瞬间的占格，以监听卡为参照 |
| cond_was_card_displaced | WasCardDisplaced 判断本次位移的被移动卡 |
| cond_check_contest_outcome | CheckContestOutcome 在对拼结算事件中判断指定参战者 Win、Lose 或 Draw |
| cond_ammo_exhausted / cond_has_consume_stock | 本卡余弹为零 / 本卡至少还有一份消耗库存 |
| cond_self_has_keyword / cond_source_has_keyword | HasKeyword 检查本卡或事件来源卡当前是否具有指定词 |
| cond_check_consume_count | CheckConsumeCount 比较指定消耗卡当前次数，支持 Equal、AtLeast、AtMost |
| cond_check_mana_charged / cond_check_wound_event | 筛选充入的灵元或成功提交的伤势 |
| cond_check_mana_used / cond_check_element_stack_change | 进阶。筛选实付灵元或具体五行状态的增减层 |
| cond_card_moved_into_side / cond_check_shield_exhaustion | 进阶。筛选进入的方位或灵盾耗尽原因 |

SelectCards 返回独立的卡牌列表，并登记同一批结果给后续 `Selected` 动作。无合法目标时返回空列表并清掉旧的选中结果。旧 AllCard、RandomCardInCD、AdjacentCards 按历史签名保留。IsFabao、IsFaShu、IsGongFa、IsItem、IsEquip、IsZhaoShi 仍接收 CombatCard 并返回布尔值。

| Pending ID | 后续需要对齐的规则 |
| --- | --- |
| cond_all_card | 旧模板传字符串，Scope 入口需角色实例 |
| cond_random_card_in_cd | 确定背包扫描下的候选状态及参数 |
| cond_adjacent_cards | 确定参考卡及各方向的选择范围 |

## A · 动作模板及入口

解冻、冻结免疫光环、定身、灵元剥离、五行状态免疫和以盾蓄力的配置及代码入口见 [1122TCA拓展实现](../../1122TCA拓展实现.md#进阶)。

名称以模板 ID 和函数名双索引。参数是模板给出的选择，运行语义以实现列为准。实现列按文件名和方法定位，源码类型为 `CombatBaseFunc`。调用包装分为 [角色动作](../../../../Data/LuaScripts/CombatNpc_Action.lua)、[卡动作](../../../../Data/LuaScripts/CombatCard_Action.lua) 和 [拼点](../../../../Data/LuaScripts/CombatNpc_Contest.lua)，均调用 `API:Execute`。同类规格、最小用法见 [接口索引](玩法与接口索引.md)。

| 模板 ID / 函数 | 玩家语义 | 参数 | 设计、模板与实卡 | 主线实现 |
| --- | --- | --- | --- | --- |
| `action_atk` / `Attack` | 生成待发攻击拼点，伤害待对拼或轮末处理 | 五行 Element、Zhan/Ci/Da/SheJi、AttackValue | [拼点模板](../../../../Data/Action/ActionTemplate_Contest.json)；[剑刺](../../../../Data/Card/Lua/card_form_jian_ci.lua) Contest，读取装备元素和攻击底值 | [CombatBaseContest.Attack](../../../../Scripts/Game/Domain/!Global/API/Combat/Contest/CombatBaseContest.cs) → `TryPushToPendingSlot` |
| `action_defend` / `Defend` | 生成待发防御拼点 | Shield/Block/Dodge、DefendValue | 同上；[回归卡 bp_guard](../../../../Data/Card/Lua/bp_guard.lua) Contest 走 Block。Shield 拼点与持久盾分别记账 | 同文件 `Defend` → `TryPushToPendingSlot` |
| `action_gain_shield` / `GainShield` | 增加现有持久灵盾，减伤后优先消耗 | Domain、Amount | 4 盾受 3 点伤害后余 1 盾，再受 2 点则扣余盾及 1 HP | [防御模板](../../../../Data/Action/ActionTemplate_Defense.json) |
| `action_add_armor` / `AddArmor` | 获得一次性护甲，仅敌方正向伤害消费 | Domain、Amount | 护甲 2 遇 5 点敌方伤害，进入后续结算的伤害剩 3 | 同上 |
| `action_add_fortification` / `AddFortification` | 本方当轮的所有入伤固定减免 | Domain、Amount | 坚甲 1 使本轮两次 5 点伤害各减 1，到本方下轮失效 | 同上 |
| `action_add_deflection` / `AddDeflection` | 获得卸力，下一次与敌方攻击配对时减点并消费 | Domain、Amount | `AddDeflection(ctx, "Self", 3)` 使下次配对的敌方 5 点攻击减至 2 | [防御模板](../../../../Data/Action/ActionTemplate_Defense.json) · [卸力 Lua](../../../../Data/Modifier/CombatModifierDefines/Lua/Deflection.lua) |
| `action_change_equipment_stat` / `ChangeEquipmentStat` | 修改指定装备或招式所属装备的攻防 | Domain、Stat、Delta、Rounds | 长剑攻击 4 加 2 后，新生成剑刺用 6 点 | [装备模板](../../../../Data/Action/ActionTemplate_BattleEquipment.json) |
| `action_change_affinity` / `ChangeAffinity` | 修改目标战斗抽灵权重 | Domain、Element、Delta、Rounds | 当前只有木权重为正时，抽取全部是木灵元 | [亲和与供能模板](../../../../Data/Action/ActionTemplate_BattleAffinity.json) |
| `action_add_artifact_power` / `AddArtifactPower` | 轮首随机给场上法宝补当前需要的灵元 | Domain、Element、Amount、Rounds | 火供能只补火，None 可以补任一匹配五行 | 同上 |
| `action_refill_ammo` / `RefillAmmo` | 补充余弹至有效上限 | Domain、Amount | [弹药模板](../../../../Data/Action/ActionTemplate_Ammo.json) | `CombatBaseFunc.RefillAmmo` → `CombatCard.RefillAmount` |
| `action_slide_card` / `SlideCard` | 沿四向滑到障碍前，灵元可穿越，终点灵元让位 | Domain、Direction | 作用于完整卡形，保持蓄灵与 SP | [位移模板](../../../../Data/Action/ActionTemplate_Spatial.json) |
| `action_heal` / `Heal` | 治疗至多 HealValue 点，受最大 HP 限制 | Domain、HealValue | [效果模板](../../../../Data/Action/ActionTemplate_Effect.json)；[再生](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Mu.lua) 用于基础补灵 | [CombatBaseAction.Heal](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `ApplyHeal`，实际回复后派发 OnHeal |
| `action_self_damage` / `SelfDamage` | 给选中的角色排入伤害，经过受伤管线 | Domain、DamageValue | 同上。 [出血](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Jin.lua) 基础补灵自伤 | `CombatBaseAction.SelfDamage` → `AddDamage`。默认斩击。对敌用施加者作为来源，使护甲参与结算。自伤沿用无来源伤害 |
| `action_armor_break` / `ArmorBreak` | 只扣持久盾，扣量至多当前盾 | Domain、BreakValue | 同上；[破甲](../../../../Data/Card/Lua/card_jin_armor_break.lua) Apply 令 Target 破盾 5，定义标 Unmigrated | `CombatBaseAction.ArmorBreak` → `ChangeShield` |
| `action_add_npc_buff` / `AddNpcBuff` | 按定义附加或叠加战斗 Modifier | Domain、BuffId、Stacks、Duration | 可选 14 种拼点强化的持续版和下次版。[Buff 模板](../../../../Data/Action/ActionTemplate_Buff.json) · [强化定义](../../../../Data/Modifier/CombatModifierDefines/Contest_Improved.json) | `CombatBaseAction.AddNpcBuff` → `CombatNpc.AddModifier` |
| `action_add_stat_buff` / `AddStatBuff` | 加持久属性修正 | Domain、StatId、Value、Flat/Percent；Lua 可传 SourceId | 同上；[兽肉](../../../../Data/Card/Lua/card_monster_meat.lua) Apply 带 SourceId，定义标 Unmigrated | `CombatBaseAction.AddStatBuff` → `CombatNpc.AddStatBuff`，旧 Min/Max/ManaAdj 系列会拒绝 |
| `action_add_card_stat_buff` / `AddCardStatBuff` | 给选卡加卡牌属性修正 | 卡牌 Domain、StatId、Value | 同上；金针 Lua 传卡对象并尝试 ManaAdj_Jin，卡定义标 Deprecated，禁止当作可用样例 | [CombatCDAction.AddCardStatBuff](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CardModifier.StatBuff`，工厂设 Never。效果测试通过废弃属性拒绝断言 |
| `Charge` / `Charge` | 增加整包基准扫描进度 | Self/Target、ReduceTick | [时序模板](../../../../Data/Action/ActionTemplate_CardCD.json)；[爆燃](../../../../Data/Card/Lua/card_huo_burst.lua) Apply 调用，定义标 Migrated | `CombatCDAction.Charge` → `Backpack.Charge`；时序、效果子项 |
| `action_freeze` / `Freeze` | 冻结未冻结且未耗尽的主动卡，目标下次完整扫描跳过发动并解冻 | 卡牌 Domain、FreezeTime（兼容占位） | 同上；[水系冻结](../../../../Data/Card/Lua/card_shui_freeze.lua) Apply 随机冻敌卡 | `CombatCDAction.Freeze` → `CombatCard.Freeze`，随机前筛掉无效目标 |
| `action_slow` / `Slow` | 整包半速，同类延时 | Self/Target、DurationTicks | 同上；[土系减速](../../../../Data/Card/Lua/card_tu_slow.lua) Apply | `CombatCDAction.Slow` → `Backpack.AddSlow`；时序、效果子项 |
| `action_haste` / `Haste` | 整包双速，同类延时 | Self/Target、DurationTicks | 同上；[木系加速](../../../../Data/Card/Lua/card_mu_haste.lua) Apply | `CombatCDAction.Haste` → `Backpack.AddHaste`；时序、效果子项 |
| `action_remove_wound` / `RemoveRandomWound` | 随机选一张场上伤口排队移除 | Domain、Size、Exact（true 等于，false 至多） | [卡组模板](../../../../Data/Action/ActionTemplate_Deck.json)；Exact=true 选等体量，false 选至多该体量。未查到实卡调用 | `CombatBaseAction.RemoveRandomWound` → `HealWound` → `DealFieldChange` |
| `action_convert` / `Convert` | 灵元转化／回收格上灵元为 MP，1 个回 1 点 | Domain、Element（None 任意）、MaxAmount | [灵元模板](../../../../Data/Action/ActionTemplate_Mana.json)；[迁移规格](../../../../openspec/specs/combat-mana-manipulation/spec.md)；本轮通过满 MP、MP 空缺、指定元素、保护已点亮测试 | [CombatManaAction.Convert](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) → `RemoveMana`、`RecoverMP` |
| `action_recover_mp` / `RecoverMP` | 直接回复 MP，受上限限制 | Domain、Amount | [MP 模板](../../../../Data/Action/ActionTemplate_MP.json)；浩瀚在基础补灵前按层回复 | CombatManaAction.RecoverMP → CombatNpc.RecoverMP |
| `action_reduce_mp` / `ReduceMP` | 直接减少 MP，最低降至零 | Domain、Amount | 同上；石化在基础补灵前按层削减 | CombatManaAction.ReduceMP → CombatNpc.ReduceMP |
| `action_draw` / `Draw` | 立刻将 MP 放成格上灵元，实际入格才扣款 | Domain、Amount | 同上；[回灵](../../../../Data/Card/Lua/card_huo_drain.lua) Apply 抽 3；效果测试通过 Draw 链 | `CombatManaAction.Draw` → `CombatNpc.DrawMana` |
| `action_reduce_mana` / `ReduceMana` | 摧毁格上灵元，不足部分成功 | Domain、Element（None 任意）、Amount | 同上；效果测试真实 Lua 减灵及后续 Draw，未在生产卡中定位独立调用 | `CombatManaAction.ReduceMana` → `RemoveMana`；保留已点亮资源，MP 无变化 |
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

旧线性位移、候补部署和召回保留为 Pending。四向滑移由独立节点提供。

| Pending ID | 原设计 |
| --- | --- |
| action_displace | 位移到指定位置，原目标范围、位置选项及评分见[卡组配置](../../../../Data/Action/ActionTemplate_Deck.json) |
| action_deploy | 将指定卡从候补池部署到运转池，见[候补配置](../../../../Data/Action/ActionTemplate_Reserve.json) |
| action_recall | 将指定卡从运转池召回候补池，见[候补配置](../../../../Data/Action/ActionTemplate_Reserve.json) |

### 已有函数和内联效果

| 检索名 | 接入与边界 |
| --- | --- |
| 持久加盾 | [测试卡 bp_focus](../../../../Data/Card/Lua/bp_focus.lua) 的 Apply 内联 `self.m_Owner:ChangeShield(2)`，新效果可使用上表的 GainShield 模板。 |
| 长剑下次招式对拼 +1 | [长剑脚本](../../../../Data/Card/Lua/card_fabao_jian.lua) 的 `OnContestOverflow`、`OnBeforePairedContest`、`OnPassiveDeactivated` 串联卡专用规则。拼点消费与法宝失活清理均有回归 |
| 灵元改色／换色、指定格换位 | [纳音设计](../../../../Data/Card/NayinDesign.md) 有设计来源。改色可组合 C# 两个操作，指定格换位缺核心操作。详见 [背包格能力](玩法与接口索引.md#背包格能力) |
