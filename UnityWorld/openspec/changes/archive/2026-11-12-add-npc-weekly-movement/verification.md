# 重做验证记录

日期：2026-11-08。本记录对应用户要求的Plane Data/System重做，替代上一版独立Movement层的完成记录。

## 架构核对

- Plane的地块和NPC唯一集合保存于PlaneTileData、PlaneNpcData；PlaneSystemTile、PlaneSystemNpc是其所属系统，成员不再登记到第二个World名单。
- 随机出生由位面系统请求NpcMgr创建，再通过NpcSystemPosition落地；放置、候选、显示范围、离开与实际到达均使用位置系统。
- NpcSystemBehavior处理主行为和当周随机决策，MoveBehavior.AdvanceWeek推进自身进度；策略方法体只有TODO注释，没有interface、实现类或注入入口。
- WorldMgr直接遍历各Plane；必要的只读结果放在NPC数据和World结果位置，不承载管理职责。
- GamePlay/Movement目录和原四个代码文件、独立周协调器均已删除。源码搜索确认没有原包装类型、移动计划、策略interface或重复地图摘要的引用。
- 属性JSON继续复用现有StatBlock；范围查询复用Plane地块系统和独立Tile观察副本。

## 实际运行结果

| 检查 | 结果 |
| --- | --- |
| Console项目构建（--no-restore） | 0错误；已有依赖审计／编译警告仍保留 |
| --npc-movement-tests | NPC_MOVEMENT_OK assertions=182 |
| --world-calendar-tests | WORLD_CALENDAR_OK assertions=144 |
| --map-runtime-tests | MAP_RUNTIME_OK assertions=11247 |
| --template-tests | TEMPLATE_BACKPACK_TESTS_OK assertions=92 |
| --npc-movement-demo --seed 12345 --npc-count 20 --weeks 8 | NPC_MOVEMENT_DEMO_OK population=20 elapsed=8 |
| 零人1周演示 | population=0 elapsed=1，退出码0 |
| 负人数／负周数／重复参数／未知参数／测试与演示混用 | 各自退出码1 |
| git diff --check | 通过，只有已有CRLF/LF提示 |
| OpenSpec strict校验 | 通过 |

命令在UnityWorld目录运行，测试／演示使用dotnet run --no-build --project Console/UnityWorld.Console.csproj -- 后接对应参数。

## 重点覆盖

默认速度50、相邻距离100、两周到达、原地一周、途中锁定、50→25→0→25、高速不连走、忙碌／死亡／受阻、多人跨两位面仅推进一周、纯历法和秒Tick隔离均通过。

位置和位面成员一致性包含：无落点失败、创建中途失败、同格、跨位面显式放置、基类删除、Plane移出NPC、销毁位面后解除落地／未完成移动但保留NPC实体、世界结束清理。

局部范围验证91／127格、半径取整与零值、边缘、超大半径，以及途中仍在原中心；Tile观察副本的元气／环境／水文修改不影响真实数据。

同进程重建、临时模板创建、实际NPC ID及容器插入顺序变化后，主大陆8名NPC的出生和8周轨迹一致。

周重入、回调修改、观察者异常、错误配置／溢出预检、提交后的内部故障及重建恢复均覆盖。测试故意破坏旧公开容器来验证故障边界，不引入专用业务注入框架。

## 回归修复

地图回归曾发现独立位面的天道实例被替换：位面System首次调用WorldMgr保护方法触发其旧静态AuraDaoMgr构造。改为只在WorldMgr.Initialize创建天道后，原有快照／收支断言恢复通过；没有改动地图原测试预期或数据规则。

原有重复触发器、缺少ExtraElementDefines／Item的数据警告，以及NCalcSync依赖审计警告仍在，本次不处理这些独立问题。Godot、Lua、其他玩法的周迁移和其他OpenSpec变更均未修改。

逐个原文件改动和运行入口见Docs/npc-movement.md。
