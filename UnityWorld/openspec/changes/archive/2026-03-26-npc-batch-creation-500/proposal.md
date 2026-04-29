## Why

世界初始化时需要一次性生成 500 个各具特色的修士 NPC，让世界在游戏开始时就充满生命力。当前 `NpcMgr.Create()` 依赖 `NpcDefine` 模板，仅有 5 个模板（其中 3 个是凡人），无法产生丰富多样性。需要一套绕过模板、全随机组合的批量创建流程，配合扩展后的 Trait 池（8 → 55），让每个修士在道途、境界、功法、性格、天赋等维度上各不相同。

## What Changes

- 新增 `NpcMgr.RandomCreate()` 方法：绕过 `NpcDefine` 模板，直接接受道途/境界/年龄/寿元/Trait 等参数创建修士 NPC
- 新增 `NpcGenerator` 工具类：封装 500 修士的批量生成算法（全随机道途、境界、功法、Trait、位置）
- 扩展 `Traits.json`：从 8 个扩展到 55 个（性格 20 + 天赋 20 + 社交 10 + 特殊 5）
- 新增 `NpcMgr.PrintFullInfo(Npc)` 方法：打印修士完整信息（含修炼道途、境界、功法进度、Trait 列表等）
- 改造 `WorldMgr.Initialize()` 和 `WebHost.RunAsync()`：在初始化末尾调用 `NpcGenerator` 生成 500 修士，并打印玩家角色信息
- 位置散布策略为纯随机（0~199, 0~199），留 TODO 坑位给未来叙事系统/势力系统重分布

## Capabilities

### New Capabilities
- `npc-random-create`: NPC 全随机创建方法，绕过 Define 模板，直接接受参数创建修士
- `npc-batch-generator`: 批量修士生成器，封装道途/境界/功法/Trait/位置的随机组合算法
- `npc-full-info-print`: NPC 完整信息打印，展示修士的全部维度数据

### Modified Capabilities
（无现有 spec 需要修改）

## Impact

- **代码修改**：`NpcMgr.cs`（新增 RandomCreate + PrintFullInfo）、`WorldMgr.cs`（接入 Generator）、`WebHost.cs`（接入 Generator）
- **新增文件**：`NpcGenerator.cs`（Domain/Object/Npc/ 下）
- **数据扩展**：`Data/Traits.json`（8 → 55 条）
- **依赖**：依赖已实现的 `CultivationMgr`、`RealmDefineMgr`、`CultivationDefineMgr`、`TraitMgr`
- **运行时影响**：初始化时间增加（创建 500 个 NPC + 注册修炼数据），内存增加约 500 个 NPC 实体