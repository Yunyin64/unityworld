## 1. Data 层 - StatDefine 体系

- [x] 1.1 创建 `Scripts/Game/Data/Defines/StatDefine.cs`：继承 DefineBase，添加 Type/DefaultValue/MinValue/MaxValue/DisplayFormat/Formula/Category 字段
- [x] 1.2 创建 `Scripts/Game/Data/Mgr/StatDefineMgr.cs`：实现 IDataMgrBase<StatDefine>，添加 GetByType(string type) 方法
- [x] 1.3 创建 `Data/stat_defines.json`：定义初始 Stat（reputation/wealth/charm/luck/resistance 等，Type="Npc"）
- [x] 1.4 在 `GameDataMgr.cs` 构造函数中注册 StatDefineMgr

## 2. Domain 层 - StatMgr 管理

- [x] 2.1 创建 `Scripts/Game/Domain/!Global/Stat/StatMgr.cs`：实现 IDomainMgrBase，建立单例
- [x] 2.2 在 StatMgr 中添加 `_npcBlocks`、`_tileBlocks`、`_planeBlocks` 三个 Dict
- [x] 2.3 实现 `CreateBlock(int id, string objectType)` 方法：查询 Define → 创建空 StatBlock → 存入 Dict → 返回引用
- [x] 2.4 实现 `GetNpcBlock(int id)` / `GetTileBlock(TileId id)` / `GetPlaneBlock(int id)` 查询方法
- [x] 2.5 实现 `RemoveBlock(int id, string objectType)` 移除方法
- [x] 2.6 预留 `OnStatChanged` 事件广播接口（空实现或 TODO 注释）
- [x] 2.7 在 `WorldMgr.Initialize()` 中注册 StatMgr

## 3. Domain 层 - StatEntry 重构

- [x] 3.1 删除 `StatEntry._baseValue` 字段和 `BaseValue` 属性
- [x] 3.2 添加 `StatEntry._statId` 字段，修改构造函数接收 statId 参数
- [x] 3.3 添加 `StatEntry._addValue` 字段（默认 0）
- [x] 3.4 添加 `Add(float amount)` 和 `SetAdd(float value)` 方法
- [x] 3.5 重构 `Recalculate()` 方法：从 StatDefineMgr 读取 DefaultValue → 三层计算 → 两层夹紧
- [x] 3.6 确保 Override 在 AddValue 之后应用

## 4. Domain 层 - StatBlock 重构

- [x] 4.1 删除 `StatBlock.SetBase()` 方法
- [x] 4.2 重构 `Get(statId, defaultValue)`：惰性创建逻辑 → 无 Entry 时返回 Define.DefaultValue
- [x] 4.3 添加 `ApplyDefineClamp(statId, value)` 私有方法：应用 Define 的 Min/Max 夹紧
- [x] 4.4 重构 `AddModifier()`：创建 Entry 时传入 statId

## 5. 调用方迁移 - 删除 StatId 常量

- [ ] 5.1 删除 `Scripts/Game/Domain/Global/Stat/StatId.cs` 文件
- [ ] 5.2 批量替换 `StatId.AgeAccumulated` 等常量为字符串字面量（全局搜索替换）

## 6. 调用方迁移 - Npc 系统

- [ ] 6.1 修改 `NpcMgr.Create()`：调用 `StatMgr.CreateBlock(npc.Id, "Npc")`
- [ ] 6.2 删除 NpcMgr.Create 中的 `npc.Stats.SetBase(...)` 调用（年龄、修为等属于底层机制属性，不再是 Stat）
- [ ] 6.3 修改 `Npc.Stats` 属性：改为查询 `StatMgr.Instance?.GetNpcBlock(Id)` 的快捷方式
- [ ] 6.4 检查并修复 `NpcSystemBio` 中的 Stat 引用（年龄相关逻辑需评估是否仍使用 Stat）

## 7. 调用方迁移 - Trait 系统

- [ ] 7.1 检查 `TraitMgr.ApplyModifiers()`：确保 AddModifier 使用字符串 statId（无需修改，当前已是 string）
- [ ] 7.2 检查 `TraitDefine.BuildModifiers()`：确认返回的 statId 是字符串（无需修改）

## 8. 调用方迁移 - Combat 系统

- [ ] 8.1 修改 `CombatScene.cs`：替换 `StatId.CultivationLevel` 为字符串 `"cultivation_level"`
- [ ] 8.2 检查 CombatScene 中的 HP 读取逻辑，确认是否需要调整

## 9. 调用方迁移 - WebAdapter

- [ ] 9.1 修改 `WorldSnapshotService.cs`：替换 `StatId.AgeAccumulated` 等为字符串

## 10. 验证与测试

- [ ] 10.1 编译通过，无错误
- [ ] 10.2 运行游戏，NPC 创建流程正常
- [ ] 10.3 运行游戏，Trait 的 Modifier 正确应用
- [ ] 10.4 验证惰性创建：新 NPC 的 StatBlock 内部 `_stats` 为空
- [ ] 10.5 验证 Define 热重载：修改 JSON 后 Get 返回新值