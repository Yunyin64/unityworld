## ADDED Requirements

### Requirement: BirthContext 持有 MainNpc 引用
BirthContext SHALL 声明 `public Npc MainNpc` 字段，供 Birth 流程中所有参与者（GlyphMgr、各 NpcSystem）通过固定字段访问当前出生的 NPC 实例。

#### Scenario: Birth 创建 NPC 并赋值给 ctx.MainNpc
- **WHEN** NpcMgr.Birth(ctx) 被调用
- **THEN** ctx.MainNpc SHALL 被赋值为一个新创建的 Npc 实例，其 Id 由 NpcMgr.Soul.NewId() 生成

### Requirement: Birth 编排流程
NpcMgr.Birth SHALL 按以下顺序执行：
1. 创建 Npc 实例并赋值给 ctx.MainNpc
2. 调用 GlyphMgr.Instance.GeneratorNpc(ctx) 铭刻基础信息到 ctx kv
3. 按 Tick 一致的顺序调用所有子系统的 OnEntityBorn(ctx)
4. 调用 Add(npc.Id, npc) 将 NPC 注册到 _allEntities
5. 返回 ctx.MainNpc

#### Scenario: Birth 返回可用的 NPC
- **WHEN** NpcMgr.Birth(ctx) 执行完成
- **THEN** 返回的 Npc SHALL 不为 null，且已注册到 _allEntities 中可被 GetById 查到

#### Scenario: Birth 后所有系统均有 Data
- **WHEN** NpcMgr.Birth(ctx) 执行完成
- **THEN** 每个子系统的 GetData(npc.Id) SHALL 返回非 null 的 Data 实例

### Requirement: GlyphMgr 使用 ctx.MainNpc
GlyphMgr.GeneratorNpc SHALL 通过 `ctx.MainNpc` 访问 NPC 引用，不再使用 `ctx.Get<Npc>("Self")`。

#### Scenario: GlyphMgr 铭刻姓名性别到 ctx kv
- **WHEN** GlyphMgr.GeneratorNpc(ctx) 被调用且 ctx.MainNpc 已赋值
- **THEN** ctx kv 中 SHALL 包含 "Gender"（NpcTypes.Gender）、"Surname"（string）、"GivenName"（string）、"DaoTitle"（string）

### Requirement: BioSystem.OnEntityBorn 初始化 NpcBioData
BioSystem.OnEntityBorn SHALL 创建 NpcBioData 并从 ctx kv 读取值填充，然后 Register 到系统中。

#### Scenario: BioData 字段正确初始化
- **WHEN** BioSystem.OnEntityBorn(ctx) 执行且 ctx kv 已由 GlyphMgr 填充
- **THEN** NpcBioData SHALL 包含：
  - Gender = ctx kv 中的 "Gender"
  - NpcType = NpcTypes.NpcType.Human
  - IsAlive = true
  - AgeAccumulated = 0f
  - BirthTick = 0
  - BaseMoveSpeed = 3f
  - NameData.Surname = ctx kv 中的 "Surname"
  - NameData.GivenName = ctx kv 中的 "GivenName"
  - NameData.DaoTitle = ctx kv 中的 "DaoTitle"
  - AppearanceData.Height = 由 npc.Soul 随机生成

#### Scenario: BioData 已注册到系统
- **WHEN** BioSystem.OnEntityBorn(ctx) 执行完成
- **THEN** BioSystem.GetData(npc.Id) SHALL 返回刚创建的 NpcBioData

### Requirement: 其他系统 OnEntityBorn 占位
未具体实现的系统（Position、Trait、Card、Cultivation、Personality、Behavior）的 OnEntityBorn SHALL 至少创建对应的空 Data 并 Register，保证 GetData 不会报错。

#### Scenario: 占位系统 Data 可访问
- **WHEN** Birth 完成后访问任意占位系统的 GetData(npc.Id)
- **THEN** SHALL 返回非 null 的 Data 实例（字段为默认值）