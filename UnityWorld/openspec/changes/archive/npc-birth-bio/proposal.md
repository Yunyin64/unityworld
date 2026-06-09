## Why

NPC 战斗模拟需要完整的 NPC 实例。当前 `NpcMgr.Birth()` 只是占位，返回 null，无法创建可用的 NPC。需要实现 Birth 流程的第一步——Bio 层（基础生物信息），让 NPC 出生后拥有姓名、性别、生存状态等基本属性。

## What Changes

- `BirthContext` 新增 `MainNpc` 固定字段，替代 kv 中的 "Self"
- `GlyphMgr.GeneratorNpc()` 改用 `ctx.MainNpc` 访问 NPC 引用
- `NpcSystemBio.OnEntityBorn()` 实现完整的 BioData 初始化（从 ctx kv 取姓名/性别，填充 NpcBioData 并 Register）
- `NpcMgr.Birth()` 补全流程：造壳 → GlyphMgr 铭刻 → 各系统 OnEntityBorn → Add 到 _allEntities
- 其他系统的 OnEntityBorn 暂只做 new Data + Register 占位

## Capabilities

### New Capabilities
- `npc-birth-bio`: NPC 出生流程的 Bio 层实现——创建 NPC 实例、生成姓名性别、初始化 NpcBioData

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/Object/Npc/BirthContext.cs`
- `Scripts/Game/Domain/Object/Npc/NpcMgr.cs`
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBio.cs`
- `Scripts/Game/Domain/GamePlay/Glyph/GlyphMgr.cs`
- 其他 NpcSystem*.cs（占位 OnEntityBorn）