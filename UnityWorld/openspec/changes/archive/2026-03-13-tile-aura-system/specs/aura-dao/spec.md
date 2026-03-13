## ADDED Requirements

### Requirement: AuraDaoMgr 持有全局原初元气快照
`AuraDaoMgr` SHALL 持有一个 `Dictionary<TileId, TileAura>` 作为原初快照（OriginSnapshot），在世界生成完成后由 PlaneGenerator 一次性写入，之后只读。任何代码 MUST NOT 在运行时修改此快照。

#### Scenario: 快照写入后可按 TileId 查询原初值
- **WHEN** AuraDaoMgr.TakeSnapshot(plane) 被调用
- **THEN** 可通过 AuraDaoMgr.GetOrigin(tileId) 获取该 Tile 快照时刻的五行元气值

#### Scenario: 快照不随 CurrentAura 变化而改变
- **WHEN** Tile.CurrentAura 因 TileModifier 累积而发生变化
- **THEN** AuraDaoMgr.GetOrigin(tileId) 返回值保持不变

### Requirement: AuraDaoMgr 计算全局五行收支偏差
`AuraDaoMgr` SHALL 提供 `CalculateBalance()` 方法，遍历所有已注册 Plane 的全部 Tile，对每个 Tile 计算 `CurrentAura - OriginAura`，汇总得到五行各自的全局净偏差（正值表示过剩，负值表示亏空）。

#### Scenario: 无任何修正时全局收支为零
- **WHEN** 所有 Tile 的 CurrentAura 与 OriginSnapshot 完全一致
- **THEN** CalculateBalance() 返回的五行偏差全部为 0

#### Scenario: 火元气被大量抽取后偏差为负
- **WHEN** 多个 Tile 的 CurrentAura.Huo 低于 OriginAura.Huo
- **THEN** CalculateBalance() 返回的火元偏差为负值，绝对值等于所有 Tile 亏空之和

### Requirement: AuraDaoMgr 对外暴露五行失衡权重
`AuraDaoMgr` SHALL 根据 `CalculateBalance()` 的结果，对外暴露 `GetImbalanceWeight(BaseElementType)` 方法，返回该元素当前的失衡程度（0.0 = 完全平衡，正值 = 过剩压力，负值 = 亏空压力）。世界事件系统 MUST 通过此接口读取失衡权重来调整事件概率，AuraDaoMgr 本身 MUST NOT 直接触发任何事件或修改任何 Tile 数值。

#### Scenario: 平衡时失衡权重为零
- **WHEN** 某元素全局偏差为 0
- **THEN** GetImbalanceWeight 返回 0.0

#### Scenario: 亏空时失衡权重为负
- **WHEN** 火元素全局偏差为 -300（大量亏空）
- **THEN** GetImbalanceWeight(Huo) 返回负值，绝对值随亏空量增大而增大

#### Scenario: 过剩时失衡权重为正
- **WHEN** 水元素全局偏差为 +200（过剩）
- **THEN** GetImbalanceWeight(Shui) 返回正值

### Requirement: AuraDaoMgr 每 Tick 更新账本
`AuraDaoMgr` SHALL 在 WorldMgr 的每次 Tick 中被驱动执行 `OnTick(deltaTime)`，重新计算全局收支并更新失衡权重缓存，供本 Tick 内其他系统查询使用。

#### Scenario: Tick 后失衡权重反映最新状态
- **WHEN** 若干 Tile 的 CurrentAura 在本 Tick 内发生变化后，AuraDaoMgr.OnTick 被调用
- **THEN** GetImbalanceWeight 返回值反映本 Tick 后的最新偏差
