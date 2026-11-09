# Proposal

## Why

NPC 的真实移动需要接入既有位面、位置、行为和属性机制。用户已明确纠正上一版架构：Plane 作为复杂对象使用 Data/System 管理自身地块与 NPC，不另设世界参与登记器；策略扩展只留带注释的空方法，不建立 interface 或选择器体系。

## What Changes

- Plane 的地块与 NPC 集合放入其 Data，所属 System 处理通行、范围查询、随机出生和本位面逐周遍历，维持现有 Plane 方法兼容。
- NpcSystemPosition 统一处理落地、位面成员同步、候选格、显示范围和实际到达换格；NPC 是否参加模拟以其实际 Plane 归属为准。
- NpcSystemBehavior 负责每周决定行动和主行为生命周期；MoveBehavior 自己推进移动进度。策略方法只有注释和空方法体，当前独立使用简单随机逻辑。
- WorldMgr 直接统一推进一周并遍历各 Plane，不另建 WorldWeeklySimulation。纯周历和旧秒 Tick 保留独立语义。
- 删除 GamePlay/Movement 全部包装：不保留 NpcWorldPresence、ExplorationStats、NpcMovement、NpcLocalMap、INpcMoveTargetSelector、移动计划及重复地图摘要模型。
- 复用共享属性 JSON 和 StatBlock，只保留必要的既有数据副本与周结果；出生序号是位置数据字段，不是第二份参与名单。

规则：每次一周，自身停留一周，六邻格统一距离100，默认速度50，途中锁定目标且留在原格；显示半径默认5且与缩放独立。

数字例子：第一周50/100、第二周到达；20个位面成员共享一次周推进。临时战斗NPC未加入Plane，不参与。

## Capabilities

### New Capabilities

- `npc-world-presence`：以实际位面归属表示落地、出生和生命周期，不要求同名类。
- `npc-movement`：相邻移动和注释空方法扩展点。
- `world-weekly-simulation`：世界按位面统一结算，不要求独立协调器对象。
- `exploration-stats`：沿用属性定义和只读消费，范围查询归位面系统。

### Modified Capabilities

无主规格语义修改；本次重做调整本变更的内部职责与扩展形式。

## Impact

修改 Plane 及其 Data/System、NpcSystemPosition、NpcSystemBehavior、MoveBehavior、NpcMgr 和 WorldMgr；更新控制台、测试和说明。保留50/5属性文件、地图生成规则、Lua和Godot现有用途，不迁移其他玩法到周单位。
