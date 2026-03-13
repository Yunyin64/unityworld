## Context

当前 `Tile` 只持有一个静态的 `BaseAura`（五行各 1.0），`PlaneGenerator` 生成的地图是完全均质的死水，没有地形起伏、没有元气差异、没有历史痕迹。

本次变更引入三层元气模型（当前层 / 未来层 / 过去层快照）、通用修正源体系（ModifierBase → TileModifier）、以及元气天道领域（AuraDaoMgr），使世界的五行元气场成为动态、可演化、有历史记忆的活系统。

现有代码约束：
- `TileAura.cs` 已有完整五行数值容器，可复用
- `Plane` 持有 `Dictionary<TileId, Tile>`，遍历全位面 Tile 有成熟路径
- `NpcSystemBase` 已示范了"System 层持有业务逻辑"的模式，TileSystemAura 应沿用
- `WorldMgr` 是 Tick 驱动入口，AuraDaoMgr 的 OnTick 应在此注册

---

## Goals / Non-Goals

**Goals:**
- Tile 持有动态的 CurrentAura（实时值）和 TileModifier 列表（未来层趋势）
- ModifierBase 体系建立，TileModifier 携带 AuraData，NpcModifier/CardModifier 空占位
- AuraDaoMgr 持有原初快照、计算全局五行收支偏差、暴露失衡权重接口
- TileSystemAura 每 Tick 将修正源累积到 CurrentAura 并清理过期修正
- PlaneGenerator 生成完毕后触发 AuraDaoMgr 快照

**Non-Goals:**
- 本次不实现事件系统读取失衡权重后触发具体事件（AuraDaoMgr 只暴露权重接口）
- 本次不实现 NPC 死亡/修炼向 Tile 写入 TileModifier 的逻辑（NPC 个体层后续迭代）
- 本次不实现元气扩散（相邻 Tile 之间的元气流动），保持每个 Tile 独立计算
- 本次不实现存档序列化扩展

---

## Decisions

### D1：过去层（原初锚点）存在 AuraDaoMgr，不存在 Tile 上

**决策**：Tile 不持有 OriginAura，AuraDaoMgr 用 `Dictionary<TileId, TileAura>` 统一存储全局快照。

**理由**：
- Tile 对象数量级为 `Width × Height`（主世界 200×200 = 40,000 个），每个 Tile 多存一份 TileAura 增加内存压力
- 原初锚点在语义上是"天道的记忆"，不属于 Tile 自身的运行时状态
- AuraDaoMgr 计算收支时需要遍历所有 Tile，快照集中在 Mgr 里遍历更高效

**备选**：在 Tile 上增加 `OriginAura` 字段 → 被否决（内存冗余、职责模糊）

---

### D2：TileModifier 列表直接挂在 Tile 上，不通过独立的 ModifierMgr 中转

**决策**：`Tile.Modifiers: List<TileModifier>` 直接持有，TileSystemAura 遍历 Plane 时直接读取每个 Tile 的列表。

**理由**：
- 修正源的生效范围是单个 Tile，数据归属清晰
- 避免引入额外的全局 ModifierMgr，减少间接层级
- TileSystemAura 已经在遍历所有 Tile，顺手处理 Modifier 是自然的

**备选**：全局 ModifierMgr 统一管理所有 Tile 的修正源 → 被否决（过度中心化，查询路径变长）

---

### D3：ModifierBase 使用抽象类而非接口

**决策**：`ModifierBase` 为 `abstract class`，不是 `interface`。

**理由**：
- Duration / RemainingTime / IsExpired 的倒计时逻辑在所有子类中完全一致，放在基类避免重复实现
- 未来子类（NpcModifier、CardModifier）可能共享更多状态字段，抽象类扩展性更好
- 项目整体风格（NpcSystemBase 等）沿用抽象类模式

---

### D4：AuraDaoMgr 不主动触发事件，只暴露失衡权重

**决策**：AuraDaoMgr 只计算并缓存 `ImbalanceWeight[5]`，不持有事件系统引用，不主动调用任何事件。

**理由**：
- 天道的设计哲学是"看不见的手"——它感知世界，但通过概率场间接影响，不是强制执行者
- 事件系统尚未建立，避免单向耦合导致死锁
- 失衡权重作为纯数据接口，任何系统都可查询，保持松耦合

---

### D5：AuraDaoMgr 放在 Domain/AuraDao/ 而非 World/

**决策**：新建 `Scripts/Game/Domain/AuraDao/` 目录，AuraDaoMgr 放在此处，与 Tile/Npc/Plane 等领域平级。

**理由**：
- AuraDaoMgr 是一个玩法领域，它有自己的数据（快照、账本）和业务规则（收支计算、失衡判定）
- `World/` 目录目前是 WorldMgr + WorldTime，是驱动层而非领域层
- 放在 Domain 层符合项目已有的"领域驱动"分层惯例

---

### D6：TileAura 类保留并复用，不新建 AuraData 类

**决策**：TileModifier 的 AuraData 字段类型直接复用现有 `TileAura` 类，语义上理解为"每 Tick 变化量"。

**理由**：
- TileAura 已有完整的 Get/Set/AddFrom 接口，复用减少重复代码
- "每 Tick 变化量"与"当前元气值"的数据结构完全相同，只是语义不同，无需新类
- 命名上通过字段名 `AuraData` 加注释区分语义

---

## Risks / Trade-offs

**[风险1] TileSystemAura 遍历全位面性能压力**
主世界 200×200 = 40,000 个 Tile，每 Tick 全量遍历修正源。
→ 缓解：大多数 Tile 的 Modifier 列表为空，可以只维护"有活跃修正的 Tile 集合"做脏标记优化（本期不实现，作为后续优化项记录）

**[风险2] AuraDaoMgr 的快照时机耦合 PlaneGenerator**
快照必须在自然地标生成稳定后、任何后天修正加入前执行，时机敏感。
→ 缓解：PlaneGenerator.Fill() 末尾显式调用 AuraDaoMgr.TakeSnapshot(plane)，不依赖事件，调用顺序在代码中明确

**[风险3] BaseAura 字段的 BREAKING 变更影响现有代码**
Tile.BaseAura 被 Tile.CurrentAura 替换，所有读取 BaseAura 的调用点需要同步修改（Plane.GetEffectiveAura 等）。
→ 缓解：全局 grep BaseAura，列入 tasks，逐一替换

---

## Migration Plan

1. 新增 `ModifierBase.cs`、`TileModifier.cs`、`NpcModifier.cs`（空）、`CardModifier.cs`（空）至 `Domain/Modifier/`
2. 修改 `TileAura.cs`：保持结构不变，仅作容器复用
3. 修改 `Tile.cs`：去掉 `BaseAura`，改为 `CurrentAura` + `List<TileModifier> Modifiers`
4. 修改 `Plane.cs`：`GetEffectiveAura` 改为直接返回 `tile.CurrentAura`（PlaneAuraBonus 逻辑后续评估是否保留）
5. 新增 `TileSystemAura.cs` 至 `Domain/Tile/System/`
6. 新建 `Domain/AuraDao/AuraDaoMgr.cs`
7. 修改 `PlaneGenerator.cs`：Fill() 末尾调用 AuraDaoMgr.TakeSnapshot
8. 修改 `WorldMgr.cs`：注册 AuraDaoMgr，驱动 OnTick

**回滚**：所有修改均在独立文件或明确标注的字段上，git revert 可完整回滚。

---

## Open Questions

- `Plane.PlaneAuraBonus` 是否保留？当前它对所有地块施加一个位面级加成，引入 TileModifier 后可以用一批永久 TileModifier 替代，但改造成本不在本期范围内，暂保留，待后续统一重构
- TileSystemAura 的 Tick 驱动方式：是挂在 WorldMgr 的全局 Tick，还是每个 Plane 独立持有一个 TileSystemAura 实例？建议每个 Plane 持有，隔离性更好，但需要 PlaneMgr 配合，本期先实现 WorldMgr 统一驱动的简单版本
