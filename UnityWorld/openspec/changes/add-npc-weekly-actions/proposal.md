# Proposal

## Why

现在空闲 NPC 直接从自身格和可走邻格里抽一个目标。四周道路越多，NPC 越容易出门。要让同一个角色既有想去的地方、又能选择留下探索或工作，世界每周需要先结算主行为，再由 Move 决定这一步往哪走。

## What Changes

- 复用一个主行为槽和现有世界周入口。途中先续办，空闲才决策。每个 NPC 一周最多执行一个主行为，到达、探索结束或受阻后，都到下周再决定。
- 预留三个有注释的空函数，分别提供多个意向点位、选择当周主行为、选择本步落脚格。人格、路线偏好和评分规则留给后续人格 AI。当前用独立、可复现的默认抽选跑通流程。
- **BREAKING** 自动决策改成先选可执行行为，仅在选中 Move 后抽合法非自身邻格。旧的“抽中本格便返回 `waited`”不再是自动结果。手动指定邻格、途中锁定和移动进度继续沿用。
- Explore 在真实所在格花一周，至多结算一次本格探索事件。结果可为无发现，事件可授予一张行为卡。Work 新增为独立主行为，原地用掉一周并记为工作，具体收益待玩法确定。
- 让调用方能从本周结果区分移动、探索和工作，即使三者在周末都可能显示为空闲。既有按秒运行的行为卡和次要行为保留它们的推进方式。

## Capabilities

### New Capabilities

- `npc-weekly-actions` — NPC 周主行为的选择、续办、与已有行为槽协作，以及 Explore、Work 的启动和周结果。

### Modified Capabilities

- `npc-movement` — 调整 Move 与自动停留、默认抽选和观察结果之间的关系。该能力已在 [周移动变更](../add-npc-weekly-movement/specs/npc-movement/spec.md) 实现，尚未归档到主规格。本变更沿用同名能力及其原条目，归档时先同步前置变更，再合入此增量。
- `world-weekly-simulation` — 明确本周选中的 Explore 可以在结算内处理本地事件和授卡，同时隔离旧秒制故事及其他系统。基线也来自[周移动变更](../add-npc-weekly-movement/specs/world-weekly-simulation/spec.md)。

## Impact

主要影响核心的 `NpcSystemBehavior`、`BehaviorBase`、`MoveBehavior`、`ExploreBehavior`、新增的 `WorkBehavior`、行为工厂、NPC 周结果和控制台周移动检查。位置仍由 `NpcSystemPosition` 管理，日期仍由 `WorldMgr.AdvanceWeek()` 推进。地形已有 `EncounterRefs`，持卡已有 `BehaviorCardMgr`，在两者之间接一次本地探索结算即可。

本次交付后端。Godot 探索 UI、人格算法、远距离寻路、移动事件内容、闭关产出，以及 Practice、Social、ExtraBehavior 的完整周玩法各自后续规划。原有移动 UI 可继续调用 `TryStartMove`。
