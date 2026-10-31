# 只读属性投影

`StatBlock.GetReadOnly(string statId, float defaultValue = 0f)` 是本次观察器新增的属性读取入口。它返回 `float`；上限投影按现有 NPC getter 的顺序，在读取完成（含定义夹紧）之后使用 `(int)` 截断。UI 只消费提交后的整数快照。

## 读取白名单

| 用途 | 允许调用 | 约束 |
| --- | --- | --- |
| 当前气血上限 | `npc.Stats.GetReadOnly("HpMax", float.NaN)` | 只在采集 Tick 快照时读取，检查结果有限 |
| 当前灵力上限 | `npc.Stats.GetReadOnly("MpMax", float.NaN)` | 同上 |
| 当前空间上限 | `npc.Stats.GetReadOnly("SpMax", float.NaN)` | 同上；不能替换成背包格数 |
| UI 刷新、悬停、动画 | 已提交的值快照 | 不再次调用核心属性接口 |

`NaN` 后备值让采集层区分缺失定义与真正零上限；异常继续交给观察器诊断边界。不要在 UI 兜底成零或初始参考值。已存在定义、没有条目的情况，仍按既有 `Get` 返回定义默认值，不自行推导公式。

普通 `Stats.Get()`、NPC 上限 getter、`CombatNpc.GetStat()` 以及 `Stats.Snapshot().Get()` 不加入观察白名单。原 `Snapshot()` 会重建条目的脏状态，并可能借用共享表达式，不能用作只读实现。

## 保留的核心语义

- 干净条目直接读取现有最终缓存。依赖属性改变但目标条目没有被原核心标脏时，仍使用原缓存；此次不修正历史依赖失效规则。
- 脏条目复制累加值、修正值集合和当前参数字典，使用新建的 NCalc `Expression` 求值。不递归重算脏依赖；公式依赖仍取当前最终缓存，与普通查询一致。
- 普通查询与只读查询共用 `StatEntry.Evaluate`，顺序仍为基础/公式、Flat、Percent、累加值、最后一个 Override、Modifier Clamp；`StatBlock` 最后应用定义上下界。
- 正常 `Recalculate` 继续复用原表达式缓存、清除 dirty 并写入最终缓存。只读路径不执行这两次写入，也不访问 `StatBlock.GetExpression`。
- 表达式参数从核心写入的装箱数值复制；不包含可执行 Hook、实体引用或随机源。新表达式没有注册玩法回调，不调用玩法 API、事件总线或随机函数。
- 实现按当前单线程战斗宿主使用；并行写属性与读取不是此次接口承诺。

## 验证

入口为 `Xianxia.Client.Battle.StatProjectionChecks.Run(string dataRoot)`。`dataRoot` 是包含 `Data` 的核心项目目录。宿主应在独立新进程中调用，然后退出；入口自行初始化和结束 `WorldMgr`，不得与已运行战斗共用世界。

测试通过相同操作序列分别构造观察状态与正常查询状态，不使用 `Snapshot()`。覆盖：

- clean/dirty、无定义、无条目、真实零值和公式缺参错误；
- 三种上限、直接添加修正、通过属性块添加修正、移除修正、依赖变化但旧缓存仍有效；
- Flat/Percent/累加顺序、多次 Override、Modifier 夹紧、定义上下界、小数转整数；
- 两个角色交替使用相同公式，以及 2,048 次重复读取；
- 只读前后的条目和容器身份、累加值、修正、dirty、最终缓存、共享 `Expression.Parameters` 的引用与值；
- 只读后恢复正常 `Get`，结果仍与对照状态一致。

2026-10-31 独立临时宿主验证通过：`STAT_PROJECTION_OK scenarios=26 assertions=11560 repeatedReads=2048 sourceState=unchanged sharedParameters=unchanged`。原状态审计仅使用只读反射，没有反射写入。构建与运行记录保存在 `.local-verification/stat-projection/`。

这组验证证明属性层等价性与隔离；连续战斗、观察开关及多 Tick 对照由整体 V1 验证承担，不能用这些属性单测代替。
