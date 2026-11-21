# 现有 TCA

工作区版本 `retain-pending-tca-designs` · 2026-11-21 核对

[玩法与接口索引](玩法与接口索引.md) · [发布状态、验证与反馈](变更与反馈.md)

已接入 **11 个 T、4 个 C、30 个 A 模板**。另有 **9 个 Pending 条目**，保留原 ID、参数及评分供后续设计。写当前可执行效果时使用 Ready 条目。卡牌和词条 Lua 通过 `[APIFunc]` 调用 C# 结算。

## T · 触发与生命周期

[触发配置](../../../../Data/TriggerDefines.json) · [卡牌 Use、Apply、Tick 和事件接收](../../../../Scripts/Game/Domain/Combat/CombatCard/CombatCard.cs) · [待发、受击和对拼](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs) · [补灵和 MP](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) · [战斗事件队列](../../../../Scripts/Game/Domain/Combat/CombatSceneEvents.cs)

事件接收方通过 `ctx.Caster` 与 `ctx.SourceCard` 判断是不是己方或本卡。卡牌发动过程中产生的事件先入队，发动作用域结束后派发。发动计数、效果执行和事件接收是不同阶段。

| 事件 | 派发时机 | 上下文 |
| --- | --- | --- |
| OnUse | 成功提交灵元和行额度后。拼点卡先入槽，法宝先激活或续期 | Caster、SourceCard |
| OnApply | 卡自身 Apply 与词条回调完成后 | Caster、SourceCard |
| OnAttack | 非平局对拼的攻击侧，含败方。攻击直击也派发 | Caster、SourceCard |
| OnDefend | 非平局对拼的防御侧，含败方 | Caster、SourceCard |
| OnStraight | 轮末待发向有效对手走直击流程，含防御待发 | Caster、SourceCard |
| OnContestOverflow | 非平局产生胜负后、两卡 Apply 前 | Winner、Loser、Overflow、WinnerType、WinnerTypeName、LoserType、WinnerCard、LoserCard；Caster、SourceCard 为胜方 |
| OnBaseManaDraw | 开场和本方轮末，在基础放灵元之前 | Caster 为补灵方 |
| OnManaDraw | 基础补灵或主动 Draw 实际放入灵元并扣 MP 后 | Caster 为补灵方 |
| OnMPChanged | MP 实际变化后 | Caster 为 MP 所属角色 |
| OnDamage | 护盾吸收后实际扣 HP 时，在 HP 清零处理前 | Caster 为受伤者，SourceCard 为伤害来源（可空） |
| OnHeal | 实际正向回复 HP 后 | Caster 为受治疗者 |

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
| OnContest | 全场对拼时机。当前胜负结算用 OnContestOverflow，卡自身构造用 Contest |
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

SelectCards 返回独立的卡牌列表。无合法目标时返回空列表。Ready 选择器调用 SelectCards。旧 AllCard、RandomCardInCD、AdjacentCards 按历史签名保留。IsFabao、IsFaShu、IsGongFa、IsItem、IsEquip、IsZhaoShi 仍接收 CombatCard 并返回布尔值。

| Pending ID | 后续需要对齐的规则 |
| --- | --- |
| cond_all_card | 旧模板传字符串，Scope 入口需角色实例 |
| cond_random_card_in_cd | 确定背包扫描下的候选状态及参数 |
| cond_adjacent_cards | 确定参考卡及各方向的选择范围 |

## A · 动作模板及入口

名称以模板 ID 和函数名双索引。参数是模板给出的选择，运行语义以实现列为准。实现列按文件名和方法定位，源码类型为 `CombatBaseFunc`。调用包装分为 [角色动作](../../../../Data/LuaScripts/CombatNpc_Action.lua)、[卡动作](../../../../Data/LuaScripts/CombatCard_Action.lua) 和 [拼点](../../../../Data/LuaScripts/CombatNpc_Contest.lua)，均调用 `API:Execute`。同类规格、最小用法见 [接口索引](玩法与接口索引.md)。

| 模板 ID / 函数 | 玩家语义 | 参数 | 设计、模板与实卡 | 主线实现 |
| --- | --- | --- | --- | --- |
| `action_atk` / `Attack` | 生成待发攻击拼点，伤害待对拼或轮末处理 | 五行 Element、Zhan/Ci/Da/SheJi、AttackValue | [拼点模板](../../../../Data/Action/ActionTemplate_Contest.json)；[剑刺](../../../../Data/Card/Lua/card_form_jian_ci.lua) Contest，读取装备元素和攻击底值 | [CombatBaseContest.Attack](../../../../Scripts/Game/Domain/!Global/API/Combat/Contest/CombatBaseContest.cs) → `TryPushToPendingSlot` |
| `action_defend` / `Defend` | 生成待发防御拼点 | Shield/Block/Dodge、DefendValue | 同上；[回归卡 bp_guard](../../../../Data/Card/Lua/bp_guard.lua) Contest 走 Block。Shield 拼点与持久盾分别记账 | 同文件 `Defend` → `TryPushToPendingSlot` |
| `action_heal` / `Heal` | 治疗至多 HealValue 点，受最大 HP 限制 | Domain、HealValue | [效果模板](../../../../Data/Action/ActionTemplate_Effect.json)；[再生](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Buff_Mu.lua) 用于基础补灵 | [CombatBaseAction.Heal](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs) → `ApplyHeal`，实际回复后派发 OnHeal |
| `action_self_damage` / `SelfDamage` | 给选中的角色排入伤害，经过受伤管线 | Domain、DamageValue | 同上；[出血](../../../../Data/Modifier/CombatModifierDefines/Lua/Element_Debuff_Jin.lua) 基础补灵自伤 | `CombatBaseAction.SelfDamage` → `AddDamage`；默认斩击、无来源卡 |
| `action_armor_break` / `ArmorBreak` | 只扣持久盾，扣量至多当前盾 | Domain、BreakValue | 同上；[破甲](../../../../Data/Card/Lua/card_jin_armor_break.lua) Apply 令 Target 破盾 5，定义标 Unmigrated | `CombatBaseAction.ArmorBreak` → `ChangeShield` |
| `action_add_npc_buff` / `AddNpcBuff` | 按定义附加或叠加战斗 Modifier | Domain、BuffId、Stacks、Duration | [Buff 模板](../../../../Data/Action/ActionTemplate_Buff.json)；[狼牙](../../../../Data/Card/Lua/card_monster_langya.lua) Apply 使用 Ci_Improved | `CombatBaseAction.AddNpcBuff` → `CombatNpc.AddModifier` |
| `action_add_stat_buff` / `AddStatBuff` | 加持久属性修正 | Domain、StatId、Value、Flat/Percent；Lua 可传 SourceId | 同上；[兽肉](../../../../Data/Card/Lua/card_monster_meat.lua) Apply 带 SourceId，定义标 Unmigrated | `CombatBaseAction.AddStatBuff` → `CombatNpc.AddStatBuff`，旧 Min/Max/ManaAdj 系列会拒绝 |
| `action_add_card_stat_buff` / `AddCardStatBuff` | 给选卡加卡牌属性修正 | 卡牌 Domain、StatId、Value | 同上；金针 Lua 传卡对象并尝试 ManaAdj_Jin，卡定义标 Deprecated，禁止当作可用样例 | [CombatCDAction.AddCardStatBuff](../../../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs) → `CardModifier.StatBuff`，工厂设 Never。效果测试通过废弃属性拒绝断言 |
| `Charge` / `Charge` | 增加整包基准扫描进度 | Self/Target、ReduceTick | [时序模板](../../../../Data/Action/ActionTemplate_CardCD.json)；[爆燃](../../../../Data/Card/Lua/card_huo_burst.lua) Apply 调用，定义标 Migrated | `CombatCDAction.Charge` → `Backpack.Charge`；时序、效果子项 |
| `action_freeze` / `Freeze` | 目标下次完整扫描跳过发动并解冻 | 卡牌 Domain、FreezeTime（兼容占位） | 同上；[水系冻结](../../../../Data/Card/Lua/card_shui_freeze.lua) Apply 随机冻敌卡 | `CombatCDAction.Freeze` → `CombatCard.Freeze`；效果测试通过从未冻结候选取样 |
| `action_slow` / `Slow` | 整包半速，同类延时 | Self/Target、DurationTicks | 同上；[土系减速](../../../../Data/Card/Lua/card_tu_slow.lua) Apply | `CombatCDAction.Slow` → `Backpack.AddSlow`；时序、效果子项 |
| `action_haste` / `Haste` | 整包双速，同类延时 | Self/Target、DurationTicks | 同上；[木系加速](../../../../Data/Card/Lua/card_mu_haste.lua) Apply | `CombatCDAction.Haste` → `Backpack.AddHaste`；时序、效果子项 |
| `action_remove_wound` / `RemoveRandomWound` | 随机选一张场上伤口排队移除 | Domain、Size、Exact（true 等于，false 至多） | [卡组模板](../../../../Data/Action/ActionTemplate_Deck.json)；Exact=true 选等体量，false 选至多该体量。未查到实卡调用 | `CombatBaseAction.RemoveRandomWound` → `RemoveCombatCard` → `DealFieldChange` |
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

位移、候补部署和召回保留为 Pending。现有兼容入口会对执行请求报告待实现。

| Pending ID | 原设计 |
| --- | --- |
| action_displace | 位移到指定位置，原目标范围、位置选项及评分见[卡组配置](../../../../Data/Action/ActionTemplate_Deck.json) |
| action_deploy | 将指定卡从候补池部署到运转池，见[候补配置](../../../../Data/Action/ActionTemplate_Reserve.json) |
| action_recall | 将指定卡从运转池召回候补池，见[候补配置](../../../../Data/Action/ActionTemplate_Reserve.json) |

### 已有函数和内联效果

| 检索名 | 接入与边界 |
| --- | --- |
| 持久加盾 | [测试卡 bp_focus](../../../../Data/Card/Lua/bp_focus.lua) 的 Apply 内联 `self.m_Owner:ChangeShield(2)`。方法 [CombatNpc.ChangeShield](../../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs)，没有独立加盾 A 模板 |
| 长剑下次招式对拼 +1 | [长剑脚本](../../../../Data/Card/Lua/card_fabao_jian.lua) 的 `OnContestOverflow`、`OnBeforePairedContest`、`OnPassiveDeactivated` 串联的卡专用实现。用实际配对消费和失活清理，历史旧长剑事件发动用例已失败 |
| 灵元改色／换色、指定格换位 | [纳音设计](../../../../Data/Card/NayinDesign.md) 有设计来源。改色可组合 C# 两个操作，指定格换位缺核心操作。详见 [背包格能力](玩法与接口索引.md#背包格能力) |
