## Why

NPC 的名字生成逻辑目前耦合在 `NpcSystemName`（Npc 子系统）中，同时承担了"名字库加载 + 随机生成"和"NPC 名字数据存储"两个职责。名字数据还在 `NpcNameData` 和 `NpcBioData.Name` 中存了两份冗余。更关键的是，"取名"并非 NPC 独有的能力——未来 Tile、Region、Sect 等实体也需要名字/称号生成服务。需要将名字生成提升为一个跨实体的 Gameplay 级服务（GlyphMgr），遵循 CultivationMgr 的设计模式："不存数据，只做创建"。

## What Changes

- **新建 `GlyphMgr`**：`IGameplayMgrBase + ISoulBase`，位于 `Scripts/Game/Domain/GamePlay/Glyph/`，管理天下所有实体的 Name 生成（未来扩展外表、称号、道号演变等）
- **新建 `NameLibrary.cs`**：名字库数据模型 + JSON 加载逻辑，从 `NpcSystemName.cs` 中搬出，放到 Glyph 目录
- **`NpcBioData` 内嵌 `NpcNameData`**：将 `NpcNameData` 作为 `NpcBioData` 的子字段，`Name` 属性改为代理访问 `NameData.FullName`，消除冗余
- **废弃 `NpcSystemName`**：标记 `[Obsolete]`，清空逻辑（不删除文件）
- **`NpcMgr` 移除 `NameSystem` 引用**：Create 方法中去掉 `NameSystem.Register` 调用
- **`NpcGenerator` 改用 `GlyphMgr`**：`NpcMgr.Instance.NameSystem.RandomName` → `GlyphMgr.Instance.RandomName`
- **`WorldMgr` 注册 `GlyphMgr`**：加入 `_gameplays` 列表

## Capabilities

### New Capabilities
- `glyph-mgr`: 天道铭刻系统——跨实体的名字生成服务（GlyphMgr），从名字库随机生成姓名、道号，未来扩展称号/绰号/外貌生成

### Modified Capabilities
<!-- 无现有 spec 需要修改 -->

## Impact

- **Domain 层**：`NpcMgr`、`NpcGenerator`、`NpcBioData`、`NpcNameData`、`NpcSystemName` 均需修改
- **World 层**：`WorldMgr.cs` 需注册新的 GlyphMgr
- **Web 层**：`WorldSnapshotService.cs` 走 `npc.GetName()` → `BioData.Name`，代理属性兼容，**不受影响**
- **数据文件**：`NameLibrary.json` 位置不变，只是加载方从 NpcSystemName 变为 GlyphMgr