## ADDED Requirements

### Requirement: TileModifierDefine 静态配置
系统 SHALL 提供 `TileModifierDefine`（继承 `DefineBase`）作为元气修正的静态配置模板，含以下字段：
- `AuraData: TileAura`：五行目标偏移量（非每秒速率，是累加到目标浓度的固定量）
- `Duration: float`：持续时间（-1 = 永久，>0 = 有限秒数）

#### Scenario: 永久修正定义
- **WHEN** 配置一个 `Duration = -1` 的 `TileModifierDefine`
- **THEN** 由此实例化的 `TileModifier.IsExpired` 永远为 false

#### Scenario: 有限时长修正定义
- **WHEN** 配置一个 `Duration = 300` 的 `TileModifierDefine`
- **THEN** 由此实例化的 `TileModifier.RemainingTime` 初始为 300，随 Tick 递减，归零后 `IsExpired = true`

### Requirement: TileModifierDefineMgr 数据加载
系统 SHALL 提供 `TileModifierDefineMgr`（实现 `IDataMgrBase<TileModifierDefine>`），从 JSON 文件加载所有修正定义，并在 `GameDataMgr` 中注册。

#### Scenario: 按 ID 查询修正定义
- **WHEN** 调用 `TileModifierDefineMgr.Instance?.Get("aura_fire_boost")`
- **THEN** 返回对应的 `TileModifierDefine` 实例，不存在时返回 null

### Requirement: 从 Define 实例化 TileModifier
系统 SHALL 提供从 `TileModifierDefine` 实例化 `TileModifier` 的工厂方法，`sourceId` 由调用方（LandMark ID 等）传入。

#### Scenario: 工厂方法创建 Modifier
- **WHEN** 调用 `TileModifierDefine.CreateModifier(sourceId: "landmark_001")`
- **THEN** 返回一个 `TileModifier`，其 `SourceId == "landmark_001"`、`AuraData` 和 `Duration` 与 Define 一致
