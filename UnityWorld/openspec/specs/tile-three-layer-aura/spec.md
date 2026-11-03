# tile-three-layer-aura

## Purpose

本规范定义 tile-three-layer-aura 能力对世界与调用方提供的行为、数据一致性和可验证场景。以下要求保留既有地图与元气系统约定，后续行为修改通过对应变更的增量规格进行维护。

## Requirements

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
地块元气更新 SHALL 保留现有逐秒累积语义：每次 Tick 将各修正源的五行每秒变化率乘以传入的 deltaTime 后累加到当前元气，并按现有有效时长规则清理过期修正源。MUST NOT 将修正值改解释为固定目标浓度、生成初值或每 Tick 固定增量；本变更不改变当前时长边界处理与更新调度。

#### Scenario: 有正向修正源时当前层数值上升
- **WHEN** 地块存在火元气每秒增加 5.0 的修正源，经过一次 deltaTime 为 0.5 秒的 Tick
- **THEN** 当前火元气增加 2.5，而非固定增加 5.0 或向某个目标浓度收敛

#### Scenario: 修正源到期后自动移除
- **WHEN** 有限持续时间修正源在当前更新中按既有规则判定到期
- **THEN** 该修正源从地块修正列表移除，后续 Tick 不再累积其效果

#### Scenario: 无修正源时当前层数值不变
- **WHEN** 地块修正列表为空并经过一次 Tick
- **THEN** 当前五行元气均不发生变化

### Requirement: PlaneGenerator 生成后执行原初快照
地图生成 SHALL 在所有自然初始元气写入完成、人工地标实际创建之前，对候选位面当前元气准备一次原初快照，并仅在生成成功时与位面一起发布。生成期间 MUST NOT 推进 Tick、等待修正源稳定或将其速率加作初值。快照 SHALL 以位面与地块坐标定位，独立于当前元气保存；失败候选不得注册快照。

#### Scenario: 快照后 AuraDaoMgr 可查询原初值
- **WHEN** 地图生成成功并发布
- **THEN** 使用位面标识和地块坐标可以查询原初元气，值与自然初始化完成时的当前元气一致
- **AND** 主位面既有仅传地块坐标的查询仍解析到主位面原初值

#### Scenario: 快照不依赖模拟时间
- **WHEN** 候选地图包含永久有效的自然地标速率修正源
- **THEN** 生成直接以自然初始化完成的元气准备快照，不等待该修正源停止变化，也不增加世界时间

#### Scenario: 初始化失败不写入原初值
- **WHEN** 候选地图在初始元气计算或快照准备阶段失败
- **THEN** 不公开该候选位面的任何原初值，已有位面快照保持不变

### Requirement: 地图元气按创世自然与动态阶段处理
系统 SHALL 先确定全部 Tile 类型并复制基础元气，再创建所有自然地标，一次性应用 AuraOffset，然后准备原初快照，最后才创建人工地标及动态速率。地区自带和独立散布的地标 SHALL 遵守同一阶段顺序。地区 AuraProfile 仅保留兼容应用功能，正式内容默认零，不做专门配置检查和数值验收。

#### Scenario: 两阶段实际创建实体
- **WHEN** Tile 顺序交错包含地区自带和独立散布的自然、人工地标计划
- **THEN** 所有自然地标实体先创建，快照完成后才创建任何人工地标实体及修正

#### Scenario: 自然偏移与人工动态分界明确
- **WHEN** 两格基础火浓度均为 1，自然地标偏移 3，人工地标偏移 4
- **THEN** 自然格原初与当前为 4，人工格原初与当前为 1；半秒后人工格为 3，原初仍为 1

#### Scenario: 零偏移不产生额外效果
- **WHEN** 地标仅引用每秒火增加 5 的修正，AuraOffset 未配置
- **THEN** 放置时不额外增火，后续半秒只增加 2.5

#### Scenario: 定义与地块初值互相隔离
- **WHEN** 两格引用同一地块定义，其中一格当前元气改变
- **THEN** 另一格与共享定义不受影响
