## ADDED Requirements

### Requirement: Tile 持有当前层元气
Tile 实体 SHALL 持有一个 `CurrentAura`（`TileAura` 实例），表示该地块当前的五行元气实时数值。`CurrentAura` 是唯一在 Tile 上存储的元气数据，不存在"原初锚点"等历史数据。

#### Scenario: 创建 Tile 时 CurrentAura 初始化
- **WHEN** 通过构造函数创建一个 Tile
- **THEN** Tile.CurrentAura 不为 null，五行各值默认为 1.0

#### Scenario: 可以读取 Tile 当前任意五行浓度
- **WHEN** 调用 Tile.CurrentAura.Get(BaseElementType.Huo)
- **THEN** 返回该地块当前的火元气浓度数值

### Requirement: Tile 持有未来层修正列表
Tile 实体 SHALL 持有一个 `List<TileModifier>`，表示当前施加在该地块上的所有修正源（自然地标、建筑、NPC 事件等）。修正列表决定每 Tick 当前层如何变化。

#### Scenario: 新建 Tile 时修正列表为空
- **WHEN** 创建一个新 Tile
- **THEN** Tile.Modifiers 为空列表，不为 null

#### Scenario: 添加修正源后列表可查询
- **WHEN** 调用 Tile.AddModifier(modifier)
- **THEN** Tile.Modifiers 包含该修正源

#### Scenario: 移除修正源后列表更新
- **WHEN** 调用 Tile.RemoveModifier(modifier)
- **THEN** Tile.Modifiers 不再包含该修正源

### Requirement: TileSystemAura 每 Tick 累积修正到当前层
`TileSystemAura` SHALL 在每次 Tick 时遍历 Plane 内所有 Tile，将各 Tile 的有效修正源产生的 AuraDelta 累积到 CurrentAura，并清理已过期的修正源。

#### Scenario: 有正向修正源时当前层数值上升
- **WHEN** Tile 上存在一个火元气 +5.0/tick 的 TileModifier，经过一次 Tick
- **THEN** Tile.CurrentAura.Huo 增加 5.0

#### Scenario: 修正源到期后自动移除
- **WHEN** 一个有限持续时间的 TileModifier 剩余时间归零
- **THEN** 该 modifier 从 Tile.Modifiers 中移除，后续 Tick 不再累积其效果

#### Scenario: 无修正源时当前层数值不变
- **WHEN** Tile.Modifiers 为空，经过一次 Tick
- **THEN** Tile.CurrentAura 各值不发生变化

### Requirement: PlaneGenerator 生成后执行原初快照
`PlaneGenerator` SHALL 在所有自然地标放置完毕、初始 TileModifier 稳定后，通知 `AuraDaoMgr` 对当前位面所有 Tile 的 CurrentAura 执行一次原初快照，作为过去层锚点存入 AuraDaoMgr。

#### Scenario: 快照后 AuraDaoMgr 可查询原初值
- **WHEN** PlaneGenerator.Fill() 完成并触发快照
- **THEN** AuraDaoMgr 可通过 TileId 查询到该 Tile 的原初元气快照，且与快照时刻的 CurrentAura 一致
