## 1. BirthContext 改造

- [x] 1.1 在 `BirthContext.cs` 中添加 `public Npc MainNpc` 字段

## 2. GlyphMgr 适配

- [x] 2.1 `GlyphMgr.GeneratorNpc()` 中将 `ctx.Get<Npc>("Self")` 改为 `ctx.MainNpc`

## 3. BioSystem.OnEntityBorn 实现

- [x] 3.1 在 `NpcSystemBio.OnEntityBorn(ctx)` 中创建 NpcBioData，从 ctx kv 读取 Gender/Surname/GivenName/DaoTitle 填充字段
- [x] 3.2 设置 NpcType=Human、IsAlive=true、AgeAccumulated=0f、BirthTick=0、BaseMoveSpeed=3f
- [x] 3.3 使用 npc.Soul 随机生成 AppearanceData.Height
- [x] 3.4 调用 Register(npc, data) 注册到系统

## 4. 其他系统 OnEntityBorn 占位

- [x] 4.1 `NpcSystemPosition.OnEntityBorn` — new NpcPositionData + Register
- [x] 4.2 `NpcSystemTrait.OnEntityBorn` — new NpcTraitData + Register
- [x] 4.3 `NpcSystemCard.OnEntityBorn` — new NpcCardData + Register
- [x] 4.4 `NpcSystemCultivation.OnEntityBorn` — 改用 ctx.MainNpc
- [x] 4.5 `NpcSystemPersonality.OnEntityBorn` — 改用 ctx.MainNpc
- [x] 4.6 `NpcSystemBehavior.OnEntityBorn` — 改用 ctx.MainNpc + 补 Register

## 5. NpcMgr.Birth 编排

- [x] 5.1 补全 Birth 方法：造壳 → GlyphMgr 铭刻 → 按 Tick 顺序调用所有系统 OnEntityBorn → Add → return
