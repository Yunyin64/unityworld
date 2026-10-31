# 核心表现观察接口

本轮允许修改共享核心 C# 的观察记录、来源关联与只读投影；不修改 JSON、Lua、扫描/对拼规则、随机调用、原始 Trace 或控制台入口。观察数据只在战斗实例内收集，默认关闭，不进入 EventMgr，不同步调用 UI。

## 接入与所有权

```csharp
scene.EnablePresentationRecording(true); // 在 PreStart / Start 之前
scene.PreStart(); scene.Start();
var initialFacts = scene.DrainPresentationEvents(); // Tick 0，IsInitialization=true
scene.Tick(); // 必须正常返回
var facts = scene.ReadPresentationEvents(); // 副本；重复读取不会消费
// 客户端同时采集快照并成功构成完整批次后才 Drain/Clear。
scene.ClearPresentationEvents();
```

`DrainPresentationEvents()` 返回同样的只读值副本并释放已读缓冲。关闭记录、`Cleanup()` 都清理缓冲；关闭时不积攒历史，序号不会因 Clear/Drain/重新开启而倒退。Scene 不判定客户端批次是否成功；异常 Tick 的未提交事实只能用于诊断，不能包装成成功帧。SessionId/SchemaVersion/完整提交由客户端批次负责。场景 Sequence 和观察 Action/Effect/Contest ID 都使用独立计数，绝不占用玩法 EventId、ChainId、全局实体 ID 或 RNG。

`CombatPresentationEvent.cs` 定义只含字符串、整数、record 与只读复制集合的公共值契约。核心的生产入口总是复制关键词、需求、已分配元素、实际占格与形状；历史值不再持有 CombatCard、CombatNpc、ContestData、DamageInfo 等实体。所有 cell/shape 坐标都是零基，只有 UI 说明文字转为 1 起始。

## 事实与取值点

| Kind / payload | 核心取值点 | 语义 |
| --- | --- | --- |
| ManaAssigned / SourceCell, Element, Before, After | CombatBackpack.TryAssign 成功转移后 | 真实源格、接收实例；失败/Convert/ReduceMana/伤口覆盖均不会冒充扫描吸收 |
| CardUsed / Before, After | TryCommitUse 通过资格且 BeginNextActivation 清空后 | 成功使用的独立 ActionId；同 Tick 满亮再清空的中间状态不会消失 |
| PendingChanged / Entered, Reason, Type, Element, Value | AddContestData、TakePending、败退清槽 | 原待发槽事实；ActionId 与使用一致；只待发不等于 Apply |
| ContestResolved / Left, Right, WinnerNpcId, Difference | ResolveContest 读取双方实际拼点后，平局/后续效果之前 | 左右是结算调用的双方，不代表 UI 固定左右；UI 按 NpcId 映射角色。平局 WinnerNpcId=null，直击没有双卡事件 |
| DamageResolved / Source, Target, HpBefore/After, HpDamage, ShieldAbsorbed | DamageIn、盾吸收及扣血后，OnDamage 与 HandleHpZero 前 | HpDamage 是实际扣减，保留负 HP 的溢出，不用帧末差值；全盾吸收 HpDamage=0；允许来源为空 |
| CardAddedToBackpack / Card, ReplacedMana | DealFieldChange Insert、Field、Owner、PreStart、Start 成功后 | 包含新卡完整表现值、真实占格、被替代灵元元素/位置。请求排队或摆放失败不发布成功入包 |
| CardApplied / Origin, IsPassive | Apply 成功完成；或被动作用域实际提交后成功退出 | 每次独立 EffectId，所属执行卡决定人物；不根据 ctx.SourceCard 猜被动归属，不按对拼赢家过滤 |

ContestData 的观察 Action/Contest ID 与 DamageInfo 的观察来源仅作元数据。延迟伤害/入包队列保留 ActionId、ParentActionId、ContestId、EffectId，不改伤害实际 SourceNpc/SourceCard。玩法事件的观察来源存放在场景旁表，出队或失败清理时同步释放，不修改事件调度顺序。

名称事实在成功执行完成后生成；动画调度可按 EffectId/ContestId 组织因果表现。核心不为动画延迟任何操作。新卡入包前若其 Start 有真实效果，事实保留实际发生顺序；播放器需要用该卡完整值协调入包和提示。

## 生效归属覆盖审计

2026-10-31 对 `Data/Card/*.json` 与对应 `Data/Card/Lua/*.lua`、全部关键词以及 C# 战斗 API 做了只读核查：34 个 `Migrated` 定义；其余 `Unmigrated` 内容由已有背包校验拒绝，没有通过本次观察绕过。下表列出当前可执行动作与边界，而非把每一次 Hook 都标成生效。

| 当前内容 / 路径 | 执行卡来源 | 实际提交位置 | 零效果与去重 |
| --- | --- | --- | --- |
| bp_attack、bp_charge、bp_guard；金斩/重斩/金甲/火射；card_fabao_jian | 受控 TryCommitUse 与真正 Apply 的 card | 入槽、ResolveContest、AddDamage、Apply | 条件检查/请求机会本身不弹名；槽满不产生 Use；双方实际 Apply 各一次 |
| bp_focus 直接 `m_Owner:ChangeShield(2)` | InvokeOwnHook 的执行 card | ChangeShield 实际数值变化 | 同一次 Apply 的多个动作共用 EffectId；0 增量不标记 |
| card_huo_drain / Draw | Apply card | PlaceMana、ChangeMP | 空间/MP 不足导致实际 0 时无被动提交；主动 Apply 仍按原流程生效 |
| card_huo_burst / Charge；card_mu_haste；card_tu_slow；card_shui_freeze | 自身 Hook 或关键词执行 card | Backpack.Charge/AddHaste/AddSlow、CombatCard.Freeze | 非正时长、0 进度、已冻结均不标记 |
| card_monster_langya、card_monster_shoupi；其他已迁移 Passive 功法 | Passive.Start 调用的真实 Apply card | AddModifier / StackModifier / AddStatBuff | 初始 Apply 标记初始化；没有自身 Lua 的功法保留核心原有语义 Apply，不虚构额外数值 |
| bp_main_6x5/8x3、bp_wound_small/wide | 无效果脚本 | 实际新增卡走统一入包记录 | 原有布局不会产生新增入包动画；伤口只占格，不虚构持续自伤 |
| 任意自身 Tick/OnPassiveTick/事件 Hook | InvokeOwnHook 保存执行 card，独立于 ctx 来源 | 下列实体动作的提交标记 | 空条件/纯读不生成 CardApplied；一次 invocation 多动作只一条 |
| 任意关键词 Hook | RunKeywordHooks 保存实际 card | 相同实体动作提交点 | 缺失/空函数不发；Apply 内同一卡关键词并入该次 Apply；其他卡嵌套用栈独立 |
| 卡牌直接 EventMgr 回调 | OnCardBuffTriggerEvent 的 this card | 减层或真正移除 CardModifier | 空列表、不匹配 trigger、无改变不发；同次回调一次 |
| Heal/RecoverHP、ArmorBreak、MP恢复/消耗 | 调用栈顶的实际 card | RecoverHP、ChangeShield、ChangeMP | 无目标、非正恢复、无盾/MP变化不标记 |
| NPC/Card Buff 与属性修正 | 调用栈顶的实际 card | 添加、有效叠层/时长更新、真实移除 | 找不到定义/移除目标、到上限且不刷新不标记；Flat/Percent 0 属性新增不标记 |
| Convert/ReduceMana/Draw | 调用栈顶的实际 card | Backpack.RemoveMana/PlaceMana | 没有实际格实体变化不标记，不产生 ManaAssigned |
| RemoveElementBuff 的直接 ReduceStack | 调用栈顶的实际 card | API 比较真实层数前后 | 无候选/未变化不标记；不修改 Lua |
| 有效伤害或卡牌增删请求 | 调用栈顶的实际 card | AddDamage/AddCombatCard/RemoveCombatCard 请求入队及最终对应事实点 | 正伤害请求保留 EffectId；有效空间检查只供观察，原失败仍在原处理点抛出 |

全量当前卡/关键词 Lua 核查没有发现绕过这些入口直接写 Stats、HP、MP、Backpack 集合的可执行脚本；唯一当前直接实体效果是 bp_focus.ChangeShield，已覆盖。所有状态 Modifier 自身自然 Tick/到期属于既有状态系统，不凭空归给最后攻击卡；卡创建 Modifier 的动作归在该卡本次生效。未来增加直接字段写入或新 API 时，必须同步为其真实提交点添加标记与回归，不能将“支持当前全部路径”解释成反射监视任意未来写入。

## 验证

`dotnet run --project tools/client-observer/ClientObserver.csproj -- --root=<外层仓库> --check-presentation-core`

`PresentationCoreChecks` 使用正式世界管理器、真实 Lua 调用和内存测试案例，不改数据文件：67 个断言覆盖真实源格/同名实例/同 Tick 吸收使用/槽满、平局胜负/直击/同 Tick 补槽、双方生效、减伤/全半护盾/溢出扣血后恢复、通用三格入包/失败/移除/伤口覆盖灵元、空检查/零动作/多动作/嵌套/关键词/真实被动 Tick/卡直接事件回调、延迟伤害及伤口的 EffectId、复制/重复读/drain/禁用/清理。另在 Tick 1 新增真实 `card_monster_langya`，验证 Start 提交 Ci_Improved，真实 CardApplied 在 CardAdded 之前且双方身份与实际占格正确；播放器须先完成入包展示再显示同批该卡的名称。

共享入口验证日志：`.local-verification/enhance-battle-presentation/core-checks-integrated.log`，`PRESENTATION_CORE_OK assertions=67`。最终新进程逐 Tick 旧投影/Trace 对照由总体验收统一运行，不能用本组单元案例代替全战斗与 Godot 连续帧验收。
