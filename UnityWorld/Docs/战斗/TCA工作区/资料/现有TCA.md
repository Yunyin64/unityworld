# 现有 TCA

资料版本 `TCA-E-2026-11-20.1` · 基线 `master` / `82e0563f4f27db8975cb5af2956c3759d8135c92` · 2026-11-20 核对

[玩法与接口索引](玩法与接口索引.md) · [发布状态、验证与反馈](变更与反馈.md)

配置收录 **9 个 T ID（20 行）**、**4 个 C 模板**、**31 个 A 模板（21 个函数）**。各表「主线」指文件已进入上述提交。实卡的脚本调用、可装配状态及本轮测试分别记录。配置中的候选值供生成与估分，当前执行走卡牌和词条 Lua、`[APIFunc]` 与 C# 方法体。`GameDataMgr` 加载配置，未查到战斗运行时直接展开这些 `LuaTemplate` 的路径。 [装配校验 NpcSystemCard](../../../../Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCard.cs)

## T · 触发与生命周期

[触发配置](../../../../Data/TriggerDefines.json) · [卡牌 Use、Apply、Tick 和事件接收](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) · [待发、受击和对拼](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) · [补灵和 MP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) · [战斗事件队列](../../../../Scripts/Game/Domain/Combat/CombatSceneEvents.cs)

事件接收方通过 `ctx.Caster` 与 `ctx.SourceCard` 判断是不是己方或本卡。卡牌发动过程中产生的事件先入队，发动作用域结束后派发。发动计数、效果执行和事件接收是不同阶段。

| ID | 玩家语义与实际时机 | 参数或上下文 | 设计和接入 | 主线、实卡或测试证据 |
| --- | --- | --- | --- | --- |
| `OnUse` | 成功消耗蓄灵、提交行额度后。普通拼点卡先尝试入槽，法宝先激活或续期 | Caster、SourceCard | 目录有两行，现行扫描规格有发动规则 | 配置和派发在主线。见 `CombatCard.Use`。旧 `CardModifier.CDSpeed` 将它设为移除触发 |
| `OnApply` | 卡牌自己的 `Apply` 及词条执行完后入队。待发卡结算后才进入 Apply，有显式 Apply 的常驻被动可在入场产生此事件 | Caster、SourceCard | 配置和派发 | 主线 `CombatCard.Apply`。实卡 Apply 样例见动作表 |
| `OnAttack` | 非平局对拼中两边的攻击类型各派一次，输掉也会派发。攻击直击也派发，在伤害入队之前 | Caster、SourceCard，此事件无胜负字段 | 配置写「己方攻击拼点赢时」，与派发不符 | 主线 `ResolveContest`、`Straight`。[回旋斩](../../../../Data/Card/Lua/card_jin_whirlwind.lua) 的 `OnAttack` 仅过滤己方，该卡标 Unmigrated |
| `OnStraight` | 轮末待发向有效对手直击。防御待发也经过此事件，基础规则不给伤害 | Caster、SourceCard | 配置和对拼排程 | 主线 `Straight`；背包与时序测试有直击路径 |
| `OnContest` | 模板描述为对拼时。卡脚本有建槽阶段 `Contest`，实际非平局结果见 `OnContestOverflow` | 模板无参数 | 配置在主线，未找到同名派发 | `CombatCard.Contest`、`ResolveContest` |
| `OnBaseManaDraw` | 开场、本方轮末的基础补灵入口，先请求此事件再调用 `DrawMana`，即使 MP 或空格为零也请求 | Caster 为补灵方，无抽取数量 | 配置、[五行状态规格](../../../../openspec/specs/element-buff-defines/spec.md) 和派发在主线 | `DoManaDraw`；五行 Modifier 按主人过滤。效果测试实际验证寒意、心火时序 |
| `OnManaDraw` | 成功放入至少一个灵元并扣 MP 后；基础补灵和主动 `Draw` 都可产生 | Caster；无数量或格坐标字段 | 配置和派发 | 主线 `DrawMana`；效果测试运行了 Draw 链，未单独核验监听回调 |
| `OnTick` | 此 ID 有配置。实际 Tick 生命周期包括卡自己的 `Tick`、词条 `OnTick`/旧名 `Tick`、激活被动 `OnPassiveTick`、Modifier 自己的 `OnTick` | 各自生命周期的 ctx | 生命周期 Hook 在主线；未找到同名全场派发 | `CombatCard.Tick`、`CombatNpcModifier.Tick` |
| `OnDeath` | 配置说敌人死亡。当前伤害可产生伤口，SP 超上限走败退 | 模板无参数 | 配置在主线；未找到同名战斗派发或实卡监听 | `HandleHpZero`、`CheckDefeated` |

重复 ID 按 [DefineMgrBase.AddDefine](../../../../Scripts/Core/Base/DefineMgrBase.cs) 的先登记保留规则核对。两条 `OnUse` 名称、权重相同，首条 score 0、有「主动/使用」标签，次条 score -4、空标签并强调无论是否生效。十一条 `OnDeath` 字段一致。目录需要 C 单独处理重复行与旧描述。

### 可直接查证的事件和局部 Hook

以下在主线源码有入口，未列入本版 T 配置。

| 名称 | 时机与参数 | 设计／使用与证据 |
| --- | --- | --- |
| `OnContestOverflow` | 非平局比较完成，双方 attack/defend 事件请求后、卡 Apply 前。含 Winner、Loser、Overflow、WinnerType、WinnerTypeName、LoserType、WinnerCard、LoserCard | `ResolveContest`；[长剑](../../../../Data/Card/Lua/card_fabao_jian.lua) 用己方斩击胜出记录一份后续加成 |
| `OnBeforePairedContest` | 双方真实对拼比较前，仅对招式，调用该方激活且声明此 Hook 的卡，`ctx.ContestData` 可改本次点数 | `ApplyPairedFormBonuses` → `ModifyPairedContest`；长剑内联消费一份 +1，轮末直击不消费，失活清除 |
| `OnPassiveActivated` / `OnPassiveDeactivated` | `SetPassiveActive` 在状态实际变化时调用，法宝续期已激活时不会再调用 Activated | [Main Keyword 的被动规格](../../../../openspec/changes/main-keyword-combat/specs/passive-card/spec.md)；长剑在 Deactivated 清下次加成 |
| `OnDefend` | 非平局对拼中防御类型一侧，含 Caster、SourceCard，无胜负参数 | `ResolveContest`；未查到当前实卡消费 |
| `OnDamage` | 持久盾吸收后实际扣 HP 才产生，`Caster` 是受伤者，`SourceCard` 为原来伤害的来源，HP 清零流程随后处理 | [CombatNpc.ApplyDamage](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs)；旧测试注入事件，最新复跑未到该项 |
| `OnHeal` | `ApplyHeal` 先请求事件，再调用 `RecoverHP`。可在实际没有正向回血前请求 | `CombatNpc.ApplyHeal`；再生调用 Heal，未查到独立实卡监听 |
| `OnMPChanged` | `ChangeMP` 改完 MP 后，Caster 为 MP 所属者，无差值参数 | `CombatNpc.ChangeMP`；效果测试验证回收只发一次、满 MP 不发回收事件 |

## C · 判断与选目标

[四个选择模板](../../../../Data/Condition/ConditionTemplate_Selector.json) · [选择函数](../../../../Scripts/Game/Domain/!Global/API/Combat/Scope/CombatBaseScope.cs) · [目标域函数](../../../../Scripts/Game/Domain/!Global/API/APIDomainFunc.cs) · [调用签名与例子](玩法与接口索引.md#目标与坐标)

| 模板 ID／函数 | 玩家用语 | 输入、结果类型 | 设计、模板与接入 | 主线证据 |
| --- | --- | --- | --- | --- |
| `cond_none` | 无条件 | 无，逻辑恒真 | 空 LuaTemplate，无独立函数 | 配置 |
| `cond_all_card` / `AllCard` | 某方场上所有卡 | CombatNpc 实例，返回多目标 `List<CombatCard>` 写入 ctx.Result，Ret=true（空列表也为真） | 模板把 Target 写成字符串。包装器需对象且没有 Lua return。未查到实卡用此包装器 | 配置、包装、C#。只读使用结果，当前集合来自 GetField |
| `cond_random_card_in_cd` / `RandomCardInCD` | 在 CD 中随机一张 | CombatNpc 实例，单目标写 Result，无候选 Ret=false | 模板使用字符串，包装器需对象。现行扫描不设置旧 InCD，不能用于找等待发动的卡 | 配置、包装、C#，未查到现行实卡使用 |
| `cond_adjacent_cards` / `AdjacentCards` | 参考卡前方或后方全部牌 | CombatCard 实例，Above/Below 返回多目标和 Ret；All 未实现 | 模板的两个实参不含 Target，方向会占 Target 参数。旧列表语义，几何定义见索引 | 配置、包装、C#，未查到实卡使用 |
| 无模板 / `IsFabao`、`IsFaShu`、`IsGongFa`、`IsItem`、`IsEquip`、`IsZhaoShi` | 卡是否带指定关键词 | CombatCard 实例，返回布尔，并写 Ret/Result | 依次匹配 FaBao、FaShu、GongFa、Item、Equip、ZhaoShi。IsEquip 只测字面 Equip，装备来源需另查 | [C# 实现](../../../../Scripts/Game/Domain/!Global/API/Combat/Condition/CombatBaseCondition.cs) 和 [Lua 返回](../../../../Data/LuaScripts/CombatCard_Condition.lua)，未查到实卡使用这些包装器 |
| 无上述模板 / `GetTargetCard`、`GetTargetNpc` | 给动作指定对象 | Domain 字符串和 ctx，返回多目标列表（部分域最多一项） | `Adjacent` 为场上列表前后一张，`AboveAll` 为整张卡的所有格严格在源卡顶行以上。几何共边见 [索引](玩法与接口索引.md#目标与坐标) | 主线动作真实复用，`EffectMigrationTests.SpatialAndControl` 通过 AboveAll 的异列和乱序用例 |

## A · 动作模板及入口

名称以模板 ID 和函数名双索引。参数是模板给出的选择，运行语义以实现列为准。实现列按文件名和方法定位，源码类型为 `CombatBaseFunc`。调用包装分为 [角色动作](../../../../Data/LuaScripts/CombatNpc_Action.lua)、[卡动作](../../../../Data/LuaScripts/CombatCard_Action.lua) 和 [拼点](../../../../Data/LuaScripts/CombatNpc_Contest.lua)，均调用 `API:Execute`。同类规格、最小用法见 [接口索引](玩法与接口索引.md)。

| 模板 ID / 函数 | 玩家语义 | 参数 | 设计、模板与实卡 | 主线实现 |
| --- | --- | --- | --- | --- |
| `action_atk` / `Attack` | 生成待发攻击拼点，伤害待对拼或轮末处理 | 五行 Element、Zhan/Ci/Da/SheJi、AttackValue | [拼点模板](../../../../Data/Action/ActionTemplate_Contest.json)；[剑刺](../../../../Data/Card/Lua/card_form_jian_ci.lua) Contest，读取装备元素和攻击底值 | [CombatBaseContest.Attack](../../../../Scripts/Game/Domain/!Global/API/Combat/Contest/CombatBaseContest.cs) → `TryPushToPendingSlot` |
| `action_defend` / `Defend` | 生成待发防御拼点 | Shield/Block/Dodge、DefendValue | 同上；[回归卡 bp_guard](../../../../Data/Card/Lua/bp_guard.lua) Contest 走 Block。Shield 拼点与持久盾分别记账 | 同文件 `Defend` → `TryPushToPendingSlot` |
| `action_heal` / `Heal` | 治疗。当前实现遗漏上限截断 | Domain、HealValue | [效果模板](../../../../Data/Action/ActionTemplate_Effect.json)；[再生](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Mu.lua) 用于基础补灵 | [CombatBaseAction.Heal](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `ApplyHeal` 计算 val 却把 amount 给 `RecoverHP` |
| `action_self_damage` / `SelfDamage` | 给选中的角色排入伤害，经过受伤管线 | Domain、DamageValue | 同上；[出血](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Jin.lua) 基础补灵自伤 | `CombatBaseAction.SelfDamage` → `AddDamage`；默认斩击、无来源卡 |
| `action_armor_break` / `ArmorBreak` | 只扣持久盾，扣量至多当前盾 | Domain、BreakValue | 同上；[破甲](../../../../Data/Card/Lua/card_jin_armor_break.lua) Apply 令 Target 破盾 5，定义标 Unmigrated | `CombatBaseAction.ArmorBreak` → `ChangeShield` |
| `action_add_npc_buff` / `AddNpcBuff` | 按定义附加或叠加战斗 Modifier | Domain、BuffId、Stacks、Duration | [Buff 模板](../../../../Data/Action/ActionTemplate_Buff.json)；[狼牙](../../../../Data/Card/Lua/card_monster_langya.lua) Apply 使用 Ci_Improved | `CombatBaseAction.AddNpcBuff` → `CombatNpc.AddModifier` |
| `action_add_stat_buff` / `AddStatBuff` | 加持久属性修正 | Domain、StatId、Value、Flat/Percent；Lua 可传 SourceId | 同上；[兽肉](../../../../Data/Card/Lua/card_monster_meat.lua) Apply 带 SourceId，定义标 Unmigrated | `CombatBaseAction.AddStatBuff` → `CombatNpc.AddStatBuff`，旧 Min/Max/ManaAdj 系列会拒绝 |
| `action_add_card_stat_buff` / `AddCardStatBuff` | 给选卡加卡牌属性修正 | 卡牌 Domain、StatId、Value | 同上；金针 Lua 传卡对象并尝试 ManaAdj_Jin，卡定义标 Deprecated，禁止当作可用样例 | [CombatCDAction.AddCardStatBuff](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CardModifier.StatBuff`，工厂设 Never。效果测试通过废弃属性拒绝断言 |
| `Charge` / `Charge` | 增加整包基准扫描进度 | Self/Target、ReduceTick | [时序模板](../../../../Data/Action/ActionTemplate_CardCD.json)；[爆燃](../../../../Data/Card/Lua/card_huo_burst.lua) Apply 调用，定义标 Migrated | `CombatCDAction.Charge` → `Backpack.Charge`；时序、效果子项 |
| `action_freeze` / `Freeze` | 目标下次完整扫描跳过发动并解冻 | 卡牌 Domain、FreezeTime（兼容占位） | 同上；[水系冻结](../../../../Data/Card/Lua/card_shui_freeze.lua) Apply 随机冻敌卡 | `CombatCDAction.Freeze` → `CombatCard.Freeze`；效果测试通过从未冻结候选取样 |
| `action_slow` / `Slow` | 整包半速，同类延时 | Self/Target、DurationTicks | 同上；[土系减速](../../../../Data/Card/Lua/card_tu_slow.lua) Apply | `CombatCDAction.Slow` → `Backpack.AddSlow`；时序、效果子项 |
| `action_haste` / `Haste` | 整包双速，同类延时 | Self/Target、DurationTicks | 同上；[木系加速](../../../../Data/Card/Lua/card_mu_haste.lua) Apply | `CombatCDAction.Haste` → `Backpack.AddHaste`；时序、效果子项 |
| `action_remove_wound` / `RemoveRandomWound` | 随机选一张场上伤口排队移除 | Domain、Size、Exact（true 等于，false 至多） | [卡组模板](../../../../Data/Action/ActionTemplate_Deck.json)；模板写 ≤，默认 Exact=true。未查到实卡调用 | `CombatBaseAction.RemoveRandomWound` → `RemoveCombatCard` → `DealFieldChange` |
| `action_displace` / `Displace` | 旧线性位移 | 卡牌 Domain、First/Last/Random | 同上，旧卡保留 Pending | `CombatBaseAction.Displace` 立即拒绝；效果测试已验证 |
| `action_deploy` / `Deploy`、`action_recall` / `Recall` | 旧候补部署、召回 | Domain、CardId | [候补模板](../../../../Data/Action/ActionTemplate_Reserve.json)，保留待迁移 | [CombatReserveAction](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatReserveAction.cs) 两方法立即拒绝；效果测试已验证 |
| `action_convert` / `Convert` | 灵元转化／回收格上灵元为 MP，1 个回 1 点 | Domain、Element（None 任意）、MaxAmount | [灵元模板](../../../../Data/Action/ActionTemplate_Mana.json)；[迁移规格](../../../../openspec/specs/combat-mana-manipulation/spec.md)；本轮通过满 MP、MP 空缺、指定元素、保护已点亮测试 | [CombatManaAction.Convert](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs) → `RemoveMana`、`RecoverMP` |
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

### 已有函数和内联效果

| 检索名 | 接入与边界 |
| --- | --- |
| `RecoverMP`、`ReduceMP` | 无对应 A 模板。角色动作包装和 `CombatManaAction` 在主线，传 Domain、Amount。C# NPC 方法返回实际变化量；Lua 包装无返回。石化实调 ReduceMP。浩瀚实卡脚本给 RecoverMP 多传 `"None"`，Amount 读成第三个参数，回复回落为 0，待 C 复核 |
| 持久加盾 | [测试卡 bp_focus](../../../../Data/Card/Lua/bp_focus.lua) 的 Apply 内联 `self.m_Owner:ChangeShield(2)`。方法 [CombatNpc.ChangeShield](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs)，没有独立加盾 A 模板 |
| 长剑下次招式对拼 +1 | [长剑脚本](../../../../Data/Card/Lua/card_fabao_jian.lua) 的 `OnContestOverflow`、`OnBeforePairedContest`、`OnPassiveDeactivated` 串联的卡专用实现。用实际配对消费和失活清理，历史旧长剑事件发动用例已失败 |
| 灵元改色／换色、指定格换位 | [纳音设计](../../../../Data/Card/NayinDesign.md) 有设计来源。改色可组合 C# 两个操作，指定格换位缺核心操作。详见 [背包格能力](玩法与接口索引.md#背包格能力) |
