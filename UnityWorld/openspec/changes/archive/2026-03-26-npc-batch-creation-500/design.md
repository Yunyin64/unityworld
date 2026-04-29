## Context

当前 `NpcMgr.Create(NpcDefine, x, y)` 依赖预定义的 `NpcDefine` 模板来创建 NPC。模板库仅有 5 个条目（含 3 个凡人），多样性极低。修炼基础体系（`CultivationMgr`、`RealmDefineMgr`、`CultivationDefineMgr`）已经实现但尚未接入 NPC 创建流程。Trait 池仅有 8 个条目，无法支撑 500 个 NPC 的个性化。

世界主平面大小为 200×200（40,000 Tile），足以容纳 500 个 NPC 散布。

## Goals / Non-Goals

**Goals:**
- 世界初始化时生成 500 个修士 NPC，每个修士有独立的道途、境界、功法、Trait、位置
- 所有随机维度等概率分布（无权重），多样性从组合中涌现
- 提供完整信息打印函数，用于调试和展示玩家角色
- Trait 池扩展到 55 个（性格 20 + 天赋 20 + 社交 10 + 特殊 5）

**Non-Goals:**
- 不做权重分布（道途/境界/Trait 全等概率）
- 不做 Trait 互斥机制（未来 Trait 会非常多，互斥不现实）
- 不做位置的智能分布（纯随机，留坑给叙事系统/势力系统）
- 不改造现有 `Create(NpcDefine)` 方法（保持向后兼容）
- 不处理凡人 NPC（500 个全是修士）

## Decisions

### D1: 绕过 NpcDefine 模板，新增 RandomCreate 方法

**选择**: 在 `NpcMgr` 中新增 `RandomCreate(...)` 方法，直接接受所有参数。

**理由**: NpcDefine 是"策划配模板→批量创建同类 NPC"的模式，而 500 个修士需要"算法组合→每个都不同"的模式。新增方法比改造 NpcDefine 更简洁、职责更清晰。

**替代方案**: 运行时动态构造 NpcDefine 对象传给 Create()——但这是在滥用模板概念，增加无意义的中间层。

### D2: NpcGenerator 作为独立工具类

**选择**: `NpcGenerator` 放在 `Scripts/Game/Domain/Object/Npc/NpcGenerator.cs`，是一个无状态工具类，接受 `Rng` 和 `count` 参数。

**理由**: 生成逻辑与 NpcMgr 的日常管理职责正交，独立出来便于测试和复用。放在 Npc 目录下因为它只操作 NPC 相关 API。

### D3: 年龄/寿元按境界等级查表

**选择**: 用一个简单的内部数组映射境界等级到年龄范围和寿元：

| 境界 Level | 年龄范围 | 基础寿元 | 移动速度 |
|-----------|---------|---------|---------|
| 1 | 16~80 | 150 | 4.0 |
| 2 | 40~200 | 300 | 5.0 |
| 3 | 100~500 | 800 | 6.5 |

**理由**: RealmDefine 中有 `lifespanBonus`，但它是比例值。需要一个基础寿元来乘。查表最简单直接，未来可迁移到数据驱动。

### D4: RandomCreate 内部复用现有子系统注册

**选择**: `RandomCreate()` 内部调用 `BioSystem.Register()`、`NameSystem.Register()`、`RoleSystem.Register()` 等，与 `Create()` 走相同的子系统注册路径，额外增加 `CultivationMgr.Register()` + `CultivationMgr.AddCultivation()`。

**理由**: 保证所有 NPC 无论通过哪种方式创建，在子系统中的数据一致性。

### D5: Trait 全随机无互斥

**选择**: 从全部 55 个 Trait 池中等概率随机抽 2~4 个，不做互斥检查。

**理由**: 用户明确要求不做互斥——未来 Trait 会非常多，互斥维护成本过高。"勇敢+胆小"同时出现可以理解为"表面勇敢内心胆小"的戏剧张力。

### D6: 位置纯随机 + TODO 坑位

**选择**: 在 200×200 主世界范围内纯随机分布坐标，代码中留 `// TODO: 后续由叙事系统/势力系统重分布` 注释。

**理由**: 当前没有势力/门派/区域归属系统，强行做聚集分布没有意义。先随机占位，未来有语义时再重分布。

### D7: 玩家角色 = 第一个生成的 NPC

**选择**: `NpcGenerator.GenerateCultivators()` 返回的列表中第一个 NPC 作为玩家角色，生成后立即调用 `PrintFullInfo()` 展示。

**理由**: 玩家角色也是完全随机的，与其他修士没有特殊待遇。第一个即可。

## Risks / Trade-offs

- **[风险] 功法匹配可能失败**: 某些 path+level 组合在 `CultivationDefines.json` 中可能无对应功法 → **缓解**: Generator 中做 fallback，无匹配功法时跳过修炼注册并打印警告日志
- **[风险] 500 NPC 初始化性能**: 创建 500 个 NPC 需要注册大量子系统数据 → **缓解**: 当前规模下可接受（毫秒级），后续如需优化可批量注册
- **[权衡] Trait 不互斥可能产生逻辑矛盾**: "体魄强健"+"体弱多病"同时出现 → **接受**: 用户明确选择此方案，未来 Trait 效果系统可处理冲突
- **[权衡] 全等概率导致分布均匀**: Level 3 修士和 Level 1 一样多，不像现实的金字塔 → **接受**: 用户明确要求无权重，游戏初期平坦分布便于测试各境界