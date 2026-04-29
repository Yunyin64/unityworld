## ADDED Requirements

### Requirement: StatMgr 单例与管理
`StatMgr` SHALL 实现 `IDomainMgrBase` 接口，作为运行时属性管理器的单例入口。

#### Scenario: 单例访问
- **WHEN** 代码访问 `StatMgr.Instance`
- **THEN** 返回当前有效的 StatMgr 实例（可为 null，使用时需空值检查）

---

### Requirement: 按 Object 类型分 Dict 存储
`StatMgr` SHALL 按实体类型分 Dictionary 存储 StatBlock：
- `_npcBlocks: Dictionary<int, StatBlock>`
- `_tileBlocks: Dictionary<TileId, StatBlock>`
- `_planeBlocks: Dictionary<int, StatBlock>`
- 后续按需扩展

#### Scenario: 独立查询各类型
- **WHEN** 调用 `StatMgr.GetNpcBlock(npcId)`
- **THEN** 仅从 `_npcBlocks` 查询，返回 `StatBlock?`

---

### Requirement: 创建 StatBlock 并初始化
`StatMgr` SHALL 提供 `CreateBlock(int id, string objectType)` 方法：
1. 查询 `StatDefineMgr.GetByType(objectType)` 获取该类型的所有 Define
2. 创建新的 StatBlock
3. 遍历 Define 列表，对每个 Stat **不预填充 Entry**（惰性创建）
4. 存入对应类型的 Dict
5. 返回 StatBlock 引用

#### Scenario: 创建 NPC 的 StatBlock
- **WHEN** 调用 `StatMgr.CreateBlock(npcId, "Npc")`
- **THEN** 返回初始化的 StatBlock，已存入 `_npcBlocks`
- **AND** 该 StatBlock 内部 `_stats` 为空（惰性创建）

#### Scenario: 重复创建相同 ID
- **WHEN** 对相同 `id` 和 `type` 调用两次 `CreateBlock`
- **THEN** 第二次覆盖第一次（或抛出异常，由实现决定）

---

### Requirement: 移除 StatBlock
`StatMgr` SHALL 提供 `RemoveBlock(int id, string objectType)` 方法，从对应 Dict 中移除指定 ID 的 StatBlock。

#### Scenario: 移除存在的 StatBlock
- **WHEN** 调用 `StatMgr.RemoveBlock(npcId, "Npc")` 且该 NPC 存在
- **THEN** 从 `_npcBlocks` 中移除，后续 `GetNpcBlock(npcId)` 返回 null

---

### Requirement: 实体持有引用（StatPtr 模式）
为保持 `npc.Stats` 的便捷访问，实体类型（如 Npc）SHALL 持有 StatBlock 的引用，该引用指向 StatMgr 管理的同一对象。

#### Scenario: Npc.Stats 与 StatMgr 指向同一对象
- **WHEN** `npc.Stats` 被访问
- **THEN** 返回 `StatMgr.Instance?.GetNpcBlock(npc.Id)` 的结果
- **AND** 修改 `npc.Stats` 等同于修改 StatMgr 中的对象

---

### Requirement: Stat 变化事件广播接口预留
`StatMgr` SHALL 预留 `OnStatChanged` 事件广播机制，当前为空实现或占位接口。

#### Scenario: 事件广播未实现
- **WHEN** StatBlock 内值发生变更
- **THEN** 当前不触发任何事件（后续扩展点）
