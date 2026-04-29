## ADDED Requirements

### Requirement: 5步世界生成流程
`PlaneGenerator` SHALL 将世界生成扩展为顺序执行的5个步骤，统一由 `PlaneGenerator.Generate(plane, config)` 入口调用，原有 `Fill()` 方法保留为 Step1 内部实现。

生成流程：
1. **Step1 Fill**：按 Width×Height 填充空白 Tile（TerrainType=Plain，Aura 全 1.0）
2. **Step2 TerrainGen**：用简单分区/随机规则分配地形类型，并根据 `TerrainAuraProfile` 写入基础 Aura
3. **Step3 RegionPlace**：先放置 IsGuaranteed 区域，再按权重随机放置其余区域；区域内覆盖地形、标记 RegionId、生成配置地标
4. **Step4 LandMarkPlace**：在无 Region 归属的 Tile 上按条件+权重散布独立地标
5. **Step5 Snapshot**：调用 `AuraDaoMgr.TakeNaturalSnapshot(plane, landMarkMgr)`，仅记录 IsNatural=true 的地标修正后的自然原初态

#### Scenario: 生成完成后每个 Tile 有地形类型
- **WHEN** `PlaneGenerator.Generate()` 完成
- **THEN** 位面内所有 Tile 的 `Terrain` 不为默认 Plain（至少部分 Tile 因地形生成而不同）

#### Scenario: 生成完成后原初快照记录自然态
- **WHEN** `PlaneGenerator.Generate()` 完成
- **THEN** `AuraDaoMgr.GetOrigin(tileId)` 对所有 Tile 有效，且其值包含自然奇观的 Modifier 影响，但不包含 IsNatural=false 的建筑 Modifier

#### Scenario: 必然区域必然出现
- **WHEN** `RegionDefineMgr` 中存在 `IsGuaranteed = true` 的定义
- **THEN** 生成完成后，`RegionMgr.GetByPlane(planeId)` 中必然包含该区域的实例

### Requirement: AuraDaoMgr 支持自然快照
`AuraDaoMgr` SHALL 新增 `TakeNaturalSnapshot(plane, landMarkMgr)` 方法，在拍摄快照时，计算 Tile 的元气时临时排除 `IsNatural=false` 的地标所挂载的 Modifier，得到纯自然态浓度后记录快照。

#### Scenario: 自然快照忽略后天建筑
- **WHEN** 某地块上有一个 `IsNatural=false` 的建筑 Modifier（火+100）和一个自然地标 Modifier（火+20）
- **THEN** `TakeNaturalSnapshot` 后，该地块的原初快照火属性值 = 基础地形火 + 20（不含建筑修正）

#### Scenario: 自然快照包含自然奇观
- **WHEN** 某地块上有一个 `IsNatural=true` 的自然奇观 Modifier（水+50）
- **THEN** 原初快照中该地块的水属性值包含 +50

### Requirement: Region 重叠时优先级规则
世界生成时若两个区域的长方形占位重叠，SHALL 按以下规则处理：
- IsGuaranteed 区域优先，不被后续区域覆盖
- 非 Guaranteed 区域之间，后放置者跳过已被占用的格子（不覆盖），只占用未被占用的格子
- 若某区域配置的 LandMarkLayout 落点已被占用（已有其他 LandMark），则跳过该地标生成，记录 warning

#### Scenario: Guaranteed 区域不被覆盖
- **WHEN** 先放置一个 Guaranteed 区域，后续随机区域与其重叠
- **THEN** 重叠的 Tile 保留 Guaranteed 区域的 RegionId，不被后续区域覆盖
