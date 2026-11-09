# NPC 按周移动

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

规则：已有NPC的显式放置调用`PositionSystem.TryPlace(id, planeId, tile)`；位置与成员一起更新。此入口不是自动跨位面旅行，移动途中不允许改位置。`PlaneMgr.MoveNpc`兼容入口同样委托位置系统。`Plane.RemoveNpc`会解除落地，销毁非主位面也会结束其中未完成移动并解除成员，NPC实体保留以便重新放置。`NpcMgr.Remove`则清理实体与子系统。世界结束先解除位面成员，再销毁系统。

数字例子：默认坐标为(0,0)但未落地时GetPosition返回null；成功放进Plane后才参与该位面结算。中途创建失败后，下次成功出生仍得到原来的下一个序号。

## 一周行动

规则：`WorldMgr.AdvanceWeek()`遍历各Plane的NPC，调用行为系统处理该周。空闲时从自己和合法六邻格中等概率随机选择一次，选择自己停留一周。主行为非空且不是周移动时返回busy；途中保持原目标，不能普通打断。到达后下一周才重新决策。

数字例子：合法六邻格共7个候选，各1/7；两个邻格阻挡后共5个候选，各1/5。只剩自己时正常停留，不无限重抽。

规则：相邻距离统一100，不乘地形消耗。行为每周读取最终速度，推进自己的进度；到达由NpcSystemPosition写回目标格。未到达真实位置留在原格，状态move；到达后状态idle。零速度保留进度；高速只完成一格，多余距离不结转。

数字例子：默认50，第一周50/100、第二周到达。速度50→25→0→25，进度50→75→75→100。速度150一周完成一格，多出的50不用。

规则：需要显式指定邻格时，可使用`BehaviorSystem.TryStartMove(id, target)`这个真实行为入口；它只启动，不推进日期或进度。当前没有玩家输入界面。

## 策略预留

规则：策略扩展只在NpcSystemBehavior中留`SelectMoveTargetByStrategy`，方法体只有TODO注释，当前不填写target。外层单独执行简单随机选择。没有interface、选择器实现类、注入属性或上下文实体。

```csharp
private void SelectMoveTargetByStrategy(
    Npc npc, IReadOnlyList<TileId> candidates, ref TileId? target)
{
    // TODO: 未来在这里填写策略；当前不设置target，继续使用外层随机选择。
}
```

未来填写策略只选目标，仍由外层校验和行为／位置系统执行，不能在这里直接改坐标或推进时间。

## 属性与显示

规则：`Data/Stat/NpcExplorationStat.json`保留`ExploreActionSpeed=50`、`ExploreDisplayRadius=5`，归属Npc，沿用Derived与现有修正计算，下限0。位置系统直接通过StatBlock.GetReadOnly读取，不设属性包装类。必需定义和非有限值明确报错。

规则：`PositionSystem.GetVisibleTiles(id)`读取真实位置和半径，委托`plane.TileSystem.GetTilesInRange(center, radius)`。范围查询筛选实际Tile并返回独立观察副本，不携带活动Modifier引用，不再复制一套NPC版地图摘要。元气、环境和水文副本的修改不会影响真实地块。

数字例子：默认5圈内陆最多91格，加值1后6圈127格；5.9取5，0只自己，超大半径最多为位面实际地块数。途中仍围绕出发格，到达才换中心。画面放大125%不改变查询范围或行动候选；Godot缩放接入仍属后续工作。

## 时间与结果

规则：纯时间入口WorldTime.AdvanceWeek / WorldCalendar.AdvanceWeek继续只改日期；旧WorldMgr.Tick继续按秒更新且不推进周移动。世界过周也不调用秒Tick、战斗、故事、修炼或地块元气。

规则：`BehaviorSystem.GetAction(id)`提供必要的不可变行为观察；`WorldWeekResult`包含Before／After日期、完成标记及逐NPC结果，结果区分waited、moving、arrived、busy、blocked、skipped和错误。世界完成后观察`WorldMgr.WeekCompleted`，原周历通知只承诺日期已更新，NPC可能尚未结算。回调只读，周重入和本功能的可变命令被拒绝，观察者异常不把成功周变成待重试。

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

演示默认20人8周，仅为可调参数。零人数／周数合法，负数、未知或重复参数拒绝。控制台输出真实出生位置、逐周日期／月相、目标、状态和进度。当前范围仅后端与控制台，Godot预览不标作真实模拟。

## 原文件改动说明

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
