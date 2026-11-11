# Tasks

## 1. 周主行为与现有行为槽

- [x] 1.1 在行为实例上表达秒制或周制，调整主槽 Tick、当前行动快照和离位清理，保留旧构造与查询入口。用周 Move 和原有“探索四方”卡交错执行秒 Tick 与世界过周，核对各自由正确时钟推进。
- [x] 1.2 整理 `NpcSystemBehavior.OnWeek`，先核对位面和成员，再继续已有周行为或跳过秒制主行为，只有主槽空闲才进入新决策。测试只有次要行为时仍能开始周行动，途中、到达和受阻的当周都不再决策。
- [x] 1.3 扩展 `NpcActionSnapshot` 和 `NpcWeekResult` 的只读表达，保留已有移动字段与失败语义。在结果上验证能区分当前占槽行为、当周完成的行为及可空事件与卡标识，旧移动结果调用方仍能编译。

## 2. 三个决策位置与 Move

- [x] 2.1 在 NPC 行为系统留出目标点喜好、周主行为选择和短期落脚格选择三个空函数。逐个在函数体中注释输入、留空输出与调用时机，核对三处均无评分、权重、路线或随机实现。
- [x] 2.2 在调用处构造实际可执行的种类列表并提供可复现的默认抽选；只有选中 Move 后才处理短期目标。沿用按出生序和周序派生的随机数，区分选行为、选格、探索用途。核对六邻与二邻各自都只贡献一个 Move 名额，同种子重放结果一致。
- [x] 2.3 共用手动与自动 Move 启动校验，保留旧位置查询含自身格，过滤得到合法非自身落脚格。更新只依赖旧 `waited` 的断言，复验指定本格/远格被拒、50/100 跨周锁目标、150 当周到达、途中速度为零保留进度以及目标消失返回受阻。

## 3. 一周的 Explore 和 Work

- [x] 3.1 增加 `TryStartExplore(id)`、`TryStartWork(id)` 及周工作行为，接入行为工厂和主槽，激活两种原地候选。核对启动不改日期、不动坐标，非空主槽和无效角色被拒，零速或没有合法邻格时这两种行动仍然可用。
- [x] 3.2 给本格 `EncounterRefs` 接入最小遭遇定义和启动校验，提供事件 ID、有效命中率、可选行为卡 ID。用受控配置覆盖命中率 0、1、未配置与未知引用，能定位无效 ID 和概率，正式地图空表能正常启动。
- [x] 3.3 让周 Explore 至多判定一个本格事件，复用持卡管理器提交授卡并记录实际结果。核对命中时持卡数只加 1、使用次数为 0；无卡事件只记事件 ID，空表或未命中都能完成，反复查询与完成通知均不重复发卡。
- [x] 3.4 让周 Work 原地结算工作并释放主槽，在同一必定触发探索事件的格子分别完成 Explore 和 Work，核对周末都为空闲，Work 无本地事件、授卡和收益。核对死亡、离位、暂停位面、源格消失时不会串格或补执行。

## 4. 整周联调

- [x] 4.1 在现有控制台演示中展示实际周行动及探索结果，更新现有命令说明和原有移动示例的随机预期。用固定种子、20 人、8 周各运行两次，对照日期、出生序、行为、位置、事件与授卡结果。
- [x] 4.2 从 `UnityWorld/` 运行设计中的控制台构建、`--npc-movement-tests`、`--world-calendar-tests`。核实显式探索获卡、随后移动两周、下周原地工作这一完整序列；再核实纯周历、秒 Tick 和周完成观察者不会替 NPC 重复行动。
- [x] 4.3 在交付说明逐项列出修改的原文件、各自的行为变化和验证结果；核对三处空函数、两个时钟、周内发卡等容易误用的位置已有必要代码注释。

## 后续归档顺序

先将 `add-npc-weekly-movement` 的基线归入主规格，再同步或归档本提案的同名增量。此次开提案即可评审各项规则，归档顺序不影响实现排期。

## 实施交付核对

下表中的路径相对 `UnityWorld/`。新增文件另列，便于对照原文件的改动。

| 改过的原文件 | 行为变化 |
| --- | --- |
| `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBehavior.cs` | 主槽统一识别周实例，隔离秒 Tick，保护已绑定主行为和次要槽。周调度拆到同一类的新文件。 |
| `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemPosition.cs` | 三种周行动都锁定本步位置，离位和注销时释放绑定。 |
| `Scripts/Game/Domain/Object/Npc/Data/NpcWeekResult.cs` | 给每人的周结果补上行动、实际事件和所得卡 ID，保留原构造、解构及移动字段。 |
| `Scripts/Game/Domain/Object/Npc/NpcMgr.cs` | NPC 注销时清除持有的行为卡。 |
| `Scripts/Game/Domain/Object/Plane/System/PlaneSystemNpc.cs` | 分散新增用途的随机种子，使默认行动抽选、移动方向和探索判定能独立重放。 |
| `Scripts/Game/Domain/GamePlay/Behavior/Behaviors/BehaviorBase.cs` | 由实例构造确定时钟，周实例记录起始位面和格子并忽略秒 Tick。 |
| `Scripts/Game/Domain/GamePlay/Behavior/Behaviors/MoveBehavior.cs` | 使用共享的周构造和位置绑定，保留移动进度和到达逻辑。 |
| `Scripts/Game/Domain/GamePlay/Behavior/Behaviors/ExploreBehavior.cs` | 增加周构造和本格结算入口，旧行为卡仍能按秒启动。 |
| `Scripts/Game/Domain/GamePlay/Behavior/BehaviorMgr.cs` | 行为工厂识别 Work，并保留行为卡的秒数和故事参数。 |
| `Scripts/Game/World/WorldMgr.cs` | 在世界启动时注册行为工厂，让真实用卡入口可以创建实例。 |
| `Scripts/Game/Data/GameDataMgr.cs` | 在地形与卡定义加载完后校验遭遇配置，通过才发布。 |
| `Scripts/Game/Data/Defines/TileDefine.cs` | 明确地形遭遇引用在本格探索时使用。 |
| `Scripts/Game/Domain/Object/BehaviorCard/BehaviorCardMgr.cs` | 提供可确认成功的内部授卡与注销清理，保护周结算边界，并向旧秒制卡传递本次用卡随机数。 |
| `Console/NpcMovementConsole.cs` | 演示分别输出移动进度和探索事件、卡 ID，周末空闲时也能看到当周行动。 |
| `Docs/npc-movement.md` | 更新手动入口、默认行动概率、遇事配置、只读结果和重放说明。 |
| `Scripts/Tests/NpcMovementTests.cs` | 现有测试命令接入含真实地形与行为卡配置的测试世界。 |
| `Scripts/Tests/NpcMovementWeekTests.cs` | 更新自动停留的旧预期，验证零速、本格和邻格启动规则。 |
| `Scripts/Tests/NpcMovementLifecycleTests.cs` | 验证死亡、离位、暂停恢复、源格失效和不同实体历史下的重放。 |
| `openspec/changes/add-npc-weekly-actions/tasks.md` | 记录任务完成情况、测试结果及交付清单。 |

新增 `NpcSystemBehavior.Weekly.cs`、`WorkBehavior.cs`、`ExploreEncounterData.cs`、`Data/ExploreEncounters.json`、`NpcWeeklyDecisionTests.cs`、`NpcWeeklyEncounterTests.cs`。正式地形尚未配置遭遇，授卡验收使用测试世界独立加载的数据。

控制台构建 0 错误。周行动套件通过 441 项断言，周历通过 144 项断言，核心启动检查生成 40,000 格并推进 3 次秒 Tick。种子 12345、20 人、8 周的两次演示中，日期、出生和 NPC 行动共 190 行完全一致。160 条周行动包含移动中 40、到达 35、探索 41、工作 44。构建仍报告既有编译警告、NCalcSync 包告警及 NuGet 漏洞源访问告警。
