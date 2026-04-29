## MODIFIED Requirements

### Requirement: PlaneGenerator 支持完整5步世界生成
原 `PlaneGenerator.Fill(plane, auraDaoMgr)` 方法仅做坐标填充。现修改为：`PlaneGenerator` 新增静态方法 `Generate(plane, rng, regionMgr, landMarkMgr, auraDaoMgr)`，内部顺序执行5步生成流程。原 `Fill()` 方法保留（作为 Step1 被内部调用），`PlaneMgr.CreatePlane` 改为调用 `Generate()`。

**新签名：**
```
PlaneGenerator.Generate(
    Plane plane,
    Rng rng,
    RegionMgr? regionMgr = null,
    LandMarkMgr? landMarkMgr = null,
    AuraDaoMgr? auraDaoMgr = null
)
```

#### Scenario: CreatePlane 调用 Generate 而非 Fill
- **WHEN** 调用 `PlaneMgr.CreatePlane(config)`
- **THEN** 内部调用 `PlaneGenerator.Generate()`，完成5步生成后返回位面实例

#### Scenario: 无 RegionMgr 时退化为仅填充+地形
- **WHEN** 调用 `PlaneGenerator.Generate(plane, rng)` 时 regionMgr 和 landMarkMgr 均为 null
- **THEN** 仅执行 Step1（Fill）和 Step2（TerrainGen），跳过 Step3~5，原初快照也退化为仅地形快照
