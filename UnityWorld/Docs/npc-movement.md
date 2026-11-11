# NPC 周行动与移动

## 职责与入口

规则：World管理Plane，Plane使用自己的Data/System管理地块和NPC。NPC实体由NpcMgr创建／销毁，真实坐标与归属统一通过NpcSystemPosition更新，主行为交给NpcSystemBehavior，移动进度在MoveBehavior中推进。没有额外世界参与名单，也没有独立移动管理层。

| 内容 | 保存与执行位置 |
| --- | --- |
| 位面地块唯一集合 | Plane.TileData / Plane.TileSystem |
| 位面NPC唯一集合、下一个出生序号 | Plane.NpcData / Plane.NpcSystem |
| 已落地、位面、格子、出生序号 | NpcPositionData / NpcSystemPosition |
| 主行为与随机决策 | NpcSystemBehavior |
| 目标和周移动进度 | MoveBehavior |
| 全世界过一周 | WorldMgr.AdvanceWeek |

数字例子：一个Plane有12人，另一个有8人，世界一次过周只加1周，各位面处理自己的成员。临时战斗NPC没有加入Plane，自然不在这20人里。

## 出生、放置与离开

规则：`plane.NpcSystem.Spawn(new BirthContext())` 先在该位面找合法步行陆地，再调用NpcMgr创建，最后请求位置系统落地。无落点或创建失败明确报错，不留下空壳，不消耗成功出生序号。普通Birth和模板装配不自动落地。允许多人同格。

```csharp
var plane = PlaneMgr.Instance.MainPlane;
var npc = plane.NpcSystem.Spawn(new BirthContext());
var position = NpcMgr.Instance.PositionSystem.GetPosition(npc.Id);
```

已有 NPC 通过 `PositionSystem.TryPlace(id, planeId, tile)` 放置，位置与成员一起更新。启动周行动后，角色锁定所在格，直到行动结束。`PlaneMgr.MoveNpc` 同样委托位置系统。`Plane.RemoveNpc` 或销毁位面会解除落地并释放周行动，保留 NPC 实体以便重新放置。`NpcMgr.Remove` 同时清理实体与持卡。世界结束先解除位面成员，再销毁系统。

数字例子：默认坐标为(0,0)但未落地时GetPosition返回null；成功放进Plane后才参与该位面结算。中途创建失败后，下次成功出生仍得到原来的下一个序号。

## 一周行动

每次 `WorldMgr.AdvanceWeek()` 推进整个世界一周。已有周主行为直接续办，已有秒制主行为返回 `busy`，主槽空闲才选新的周行动。当周到达、受阻或原地行动结束，都等下周再选。

| 周主行为 | 启动与结算 |
| --- | --- |
| Move | 指定一个合法非自身邻格，锁定后每周推进路程，到达时换格。 |
| Explore | 留在启动时的地块，花一周，从本格地形的 `EncounterRefs` 中至多抽一个遭遇，再按该遭遇的概率判断是否命中。命中可获得一张行为卡，使用卡要另外发起。 |
| Work | 留在原格一周，记录已工作。本次工作的本地事件、发卡数和收益均为零。 |

调用方可以显式使用 `TryStartMove(id, target)`、`TryStartExplore(id)`、`TryStartWork(id)`。返回成功时主槽已占用，日期和坐标仍是启动前的值。随后调用一次世界过周，才结算这个动作。手动指定和自动选出的行为共用启动校验。

规则：相邻距离统一100，不乘地形消耗。行为每周读取最终速度，推进自己的进度；到达由NpcSystemPosition写回目标格。未到达真实位置留在原格，状态move；到达后状态idle。零速度保留进度；高速只完成一格，多余距离不结转。

数字例子：默认50，第一周50/100、第二周到达。速度50→25→0→25，进度50→75→75→100。速度150一周完成一格，多出的50不用。

## 策略预留

三个函数的实现留给人格 AI。`GetPreferredDestinations` 提供多个想去的点，`SelectWeeklyBehavior` 选择本周行为，`SelectMoveTargetByStrategy` 只在决定开始 Move 时选本步的合法邻格。合法性校验和随机默认选择都在函数外。不同选择分别使用世界种子、位面、出生序和周序派生的随机数。

当 Move、Explore、Work 都能执行时，每种行为的默认机会都是 1/3。选中 Move 后，若有 6 个合法邻格，每格机会为 1/6；若有 2 个，每格机会为 1/2。移速为零或没有合法邻格时，Explore 和 Work 各占 1/2。位置系统的公开查询仍含本格，供现有调用方查看。

## 本地探索配置

`Data/ExploreEncounters.json` 中每条记录使用 `ID`、取值 0 到 1 的 `Chance` 和可选的 `BehaviorCardId`。地形的 `EncounterRefs` 引用事件 ID，所有引用在世界加载时校验。正式地形当前均为空，一次探索仍会正常完成并记录无发现。

例如，验收格仅引用一个命中率为 1 且会授卡的事件。探索一周后，周结果记录事件和卡 ID，角色持有的指定卡增加 1 张、使用次数为 0。在同一格工作一周，事件判定次数为 0。

## 属性与显示

规则：`Data/Stat/NpcExplorationStat.json`保留`ExploreActionSpeed=50`、`ExploreDisplayRadius=5`，归属Npc，沿用Derived与现有修正计算，下限0。位置系统直接通过StatBlock.GetReadOnly读取，不设属性包装类。必需定义和非有限值明确报错。

规则：`PositionSystem.GetVisibleTiles(id)`读取真实位置和半径，委托`plane.TileSystem.GetTilesInRange(center, radius)`。范围查询筛选实际Tile并返回独立观察副本，不携带活动Modifier引用，不再复制一套NPC版地图摘要。元气、环境和水文副本的修改不会影响真实地块。

数字例子：默认5圈内陆最多91格，加值1后6圈127格；5.9取5，0只自己，超大半径最多为位面实际地块数。途中仍围绕出发格，到达才换中心。画面放大125%不改变查询范围或行动候选；Godot缩放接入仍属后续工作。

## 时间与结果

`WorldTime.AdvanceWeek()` 单独推进日期。`WorldMgr.Tick(dt)` 推进旧行为卡及次要行为的秒数。周主行为只在世界过周时处理，探索授卡在这一轮 NPC 结算中完成。

`BehaviorSystem.GetAction(id)` 读取角色当前状态。`WorldWeekResult` 给出前后日期及每个 NPC 的本周结果。查看 `Kind` 可区分 `moving`、`arrived`、`explored`、`worked`、`busy`、`blocked`、`skipped` 和错误。事件与卡 ID 只在实际命中和发卡时有值。在 `WorldMgr.WeekCompleted` 中可以读取完整结果，原周历通知可能早于 NPC 结算。

数字例子：开局4000年新年无普通月相，过一周为一月新月，再过一周为上弦。直接读取WorldDate.MoonPhase，不能用总周数模4覆盖特殊周。

规则：必需配置错误和周数溢出在提交前失败；单NPC非法属性独立返回，不阻断其他NPC。日期提交后的内部故障返回Complete=false并禁止继续周结算，需结束重建，不能重试同一周。没有通用事务或进程恢复框架。

## 运行

在UnityWorld目录：

```powershell
dotnet build Console/UnityWorld.Console.csproj --no-restore
dotnet run --no-build --project Console/UnityWorld.Console.csproj -- --npc-movement-tests
dotnet run --no-build --project Console/UnityWorld.Console.csproj -- --npc-movement-demo --seed 12345 --npc-count 20 --weeks 8
dotnet run --no-build --project Console/UnityWorld.Console.csproj -- --world-calendar-tests
```

演示默认 20 人、8 周。控制台每周列出每个角色的行为、实际坐标和终态。移动显示目标与路程；探索显示命中事件和所得卡，无发现时显示 `none`。同一种子和输入可以重放。零人数或零周有效，负数、未知或重复参数会被拒绝。

## 早期周移动改动记录

- Plane.cs：唯一集合收进Data，操作委托所属System，保留原查询方法。
- PlaneMgr.cs：兼容放置交给位置系统，销毁位面清理成员与落地。
- NpcSystemPosition.cs / NpcPositionData.cs：统一实际位置、位面同步、出生序号、候选与显示查询，消费现有属性。
- NpcSystemBehavior.cs / MoveBehavior.cs：原行为槽执行周移动，保留注释空方法，隔离秒Tick。
- NpcMgr.cs：去掉World包装，保留统一创建失败和删除清理。
- WorldMgr.cs：直接遍历Plane过周，管理完成通知与保护状态；天道只在Initialize创建，避免独立位面调用触发静态实例替换。
- Tile.cs：复用Tile提供独立观察副本，明确权限缺省与显式禁止的区别。
- 基础DomainMgrBase.Remove：保持虚分派，基类删除同样走NPC清理。
- Program.cs及控制台：独立测试／演示改用真实位面与NPC系统。

移除了上一版Movement目录中的代码和独立周协调器；必要结果类型放回NPC数据及World结果位置，不承担管理职责。
