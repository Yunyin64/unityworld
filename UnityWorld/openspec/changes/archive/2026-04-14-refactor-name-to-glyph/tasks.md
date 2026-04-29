## 1. 新建 Glyph 基础设施

- [x] 1.1 创建 `Scripts/Game/Domain/GamePlay/Glyph/NameLibrary.cs`：名字库数据模型（Surnames, MaleFirstNames, FemaleFirstNames, DaoTitlePrefixes, DaoTitleSuffixes）+ 静态 `Load(path)` 方法（含 JSON 反序列化与 fallback 逻辑）
- [x] 1.2 创建 `Scripts/Game/Domain/GamePlay/Glyph/GlyphMgr.cs`：实现 `IGameplayMgrBase + ISoulBase`，持有 `NameLibrary` 实例，提供 `RandomName(Gender)` 和 `RandomDaoTitle()` 方法，`Init()` 中加载名字库，`Log()` 输出统计

## 2. 数据层改造

- [x] 2.1 修改 `Scripts/Game/Domain/Object/Npc/Data/NpcBioData.cs`：添加 `NpcNameData NameData` 子字段，将 `Name` 属性改为代理（get => NameData.FullName, set => NameData.FullName = value）
- [x] 2.2 修改 `Scripts/Game/Domain/Object/Npc/Data/NpcNameData.cs`：添加注释说明当前用途（NpcBioData 的子字段），保留 FullName 字段

## 3. NpcMgr 解耦

- [x] 3.1 修改 `Scripts/Game/Domain/Object/Npc/NpcMgr.cs`：移除 `NpcSystemName NameSystem` 属性及其构造函数中的初始化，删除 `Create()` 方法中的 `NameSystem.Register(npc, name)` 调用
- [x] 3.2 修改 `Scripts/Game/Domain/Object/Npc/NpcGenerator.cs`：将 `NpcMgr.Instance.NameSystem.RandomName(gender)` 改为 `GlyphMgr.Instance.RandomName(gender)`

## 4. 废弃旧系统

- [x] 4.1 修改 `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemName.cs`：标记 `[Obsolete("已迁移至 GlyphMgr")]`，清空内部逻辑（保留文件和类壳）

## 5. 注册与集成

- [x] 5.1 修改 `Scripts/Game/World/WorldMgr.cs`：在 `_gameplays` 列表中注册 `new GlyphMgr(seed)`，确保在 NpcMgr 之前初始化（NpcGenerator 依赖 GlyphMgr）
