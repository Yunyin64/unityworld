## Context

当前 NPC 名字系统的实现存在以下问题：

1. **职责耦合**：`NpcSystemName` 同时承担名字库加载、随机生成、NPC 名字数据存储三项职责
2. **数据冗余**：名字同时存储在 `NpcNameData.FullName`（NpcSystemName._dataTable）和 `NpcBioData.Name` 中
3. **实体绑定**：名字生成被锁在 NPC 子系统内，无法被 Tile/Region/Sect 等其他实体复用
4. **查询不一致**：`Npc.GetName()` 走的是 `BioData.Name`，而非 `NpcSystemName.GetName()`

现有架构中，`IGameplayMgrBase` 模式（如 `CultivationMgr`）已提供了"跨实体玩法服务"的范式：不持有数据表，数据归实体所有，Mgr 只做操作/生成。

## Goals / Non-Goals

**Goals:**
- 将名字生成逻辑从 `NpcSystemName` 提升为跨实体的 `GlyphMgr`（IGameplayMgrBase）
- 消除 `NpcNameData` 与 `NpcBioData.Name` 的数据冗余，统一为 `NpcBioData.NameData`
- 遵循 CultivationMgr 模式：GlyphMgr 不存数据，只做创建
- 保持 `Npc.GetName()` 等现有外部 API 完全兼容

**Non-Goals:**
- 本次不实现外貌生成（AppearanceData 随机化）
- 本次不实现 Tile/Region/Sect 等非 NPC 实体的取名
- 本次不实现称号/绰号/道号演变等高级名字功能
- 不删除 `NpcSystemName.cs` 和 `NpcNameData.cs` 文件（标记废弃即可）

## Decisions

### D1: GlyphMgr 采用 IGameplayMgrBase + ISoulBase 模式

**选择**：与 CultivationMgr 保持一致，实现 `IGameplayMgrBase` 和 `ISoulBase`。

**理由**：
- ISoulBase 提供 SoulData 和 Rng，名字生成需要可复现的随机数
- IGameplayMgrBase 提供标准生命周期，在 `WorldMgr._gameplays` 中统一管理
- 与现有 GamePlay 目录下的 Mgr 风格一致

**备选**：单纯的静态工具类 → 放弃，因为需要 Rng 状态和 JSON 加载的实例生命周期

### D2: NameLibrary 独立为单独文件

**选择**：将 `NameLibraryData` 类重命名为 `NameLibrary`，独立到 `Glyph/NameLibrary.cs`，包含数据模型和 JSON 加载的静态方法。

**理由**：
- 名字库是 GlyphMgr 的核心依赖，独立文件便于未来扩展（如增加 Tile 名字库、门派名字库）
- 加载逻辑内聚在数据类中，GlyphMgr 只需调用 `NameLibrary.Load(path)`

### D3: NpcNameData 内嵌为 NpcBioData 子字段

**选择**：在 `NpcBioData` 中添加 `NpcNameData NameData` 属性，`Name` 属性改为代理：
```csharp
public NpcNameData NameData { get; set; } = new();
public string Name { get => NameData.FullName; set => NameData.FullName = value; }
```

**理由**：
- 消除冗余的同时保持 `BioData.Name` 的读写兼容性
- 所有现有的 `BioData.Name = xxx` 和 `npc.GetName()` 无需修改
- NpcNameData 保留扩展性，未来可加 DaoTitle/Nickname/Title 字段

**备选**：全部改成 `NameData.FullName` → 改动面过大，收益不明显

### D4: NpcSystemName 标记 [Obsolete] 而非删除

**选择**：清空 `NpcSystemName` 逻辑体，保留文件，标记 `[Obsolete("已迁移至 GlyphMgr")]`。

**理由**：项目规则不允许删除 .cs 文件

## Risks / Trade-offs

- **[风险] Rng 序列变化** → GlyphMgr 使用独立 seed 构造 SoulData，与原 NpcSystemName 共享 NpcMgr.Soul.rng 不同，可能导致生成的名字序列不同。**缓解**：这是可接受的，因为名字是创建时一次性生成的，不影响存档兼容性
- **[风险] NpcNameData.cs 文件保留但使用方式变化** → 原来由 NpcSystemName 管理的 _dataTable 不再存在，NpcNameData 的唯一用途变为 NpcBioData 的子字段。**缓解**：在文件中添加注释说明当前用途
- **[权衡] GlyphMgr 目前只有 Name 功能** → 看起来"大材小用"，但为未来称号/外貌等扩展预留了位置，符合设计哲学中"天道铭刻"的定位