## Why

当前 Stat 系统存在硬编码局限性（StatId 常量需改代码扩展）、属性分散管理问题，以及缺乏对 Define 热重载的支持。需要建立一个数据驱动、支持多实体类型、可热重载的属性系统，与 Flag 系统形成清晰分层。

## What Changes

### StatDefine 体系（新增）
- 新增 `StatDefine : DefineBase`，包含 Type、DefaultValue、MinValue、MaxValue、DisplayFormat、Formula、Category 等字段
- 新增 `StatDefineMgr : IDataMgrBase<StatDefine>`，支持 `GetByType(string type)` 按 Object 类型过滤
- 新增 `stat_defines.json` 数据文件

### StatMgr（新增）
- 新增 `StatMgr : IDomainMgrBase`，集中管理所有实体的 StatBlock
- 按实体类型分 Dict 存储：`_npcBlocks`、`_tileBlocks`、`_planeBlocks` 等
- 提供 `CreateBlock(id, type)`、`GetBlock(id)`、`RemoveBlock(id)` 接口
- **BREAKING**: StatBlock 所有权从实体转移到 StatMgr 集中管理
- 预留 `OnStatChanged` 事件广播接口（空实现）

### StatEntry 计算模型（重构）
- **BREAKING**: 删除 `_baseValue` 字段和 `SetBase()` 方法，Base 值改为从 Define.DefaultValue 实时读取
- 新增 `_addValue` 字段和 `Add(float)` / `SetAdd(float)` 方法，支持累加型属性（财富、声望等）
- 新增 `_statId` 字段，支持 Recalculate 时查询 Define
- 新计算公式：`Final = ((Define.Default + FlatSum) × (1 + PctSum) + AddValue) → Override → Clamp`
- StatDefine 的 MinValue/MaxValue 作为最终硬夹紧

### StatBlock（重构）
- 实现**惰性创建**模式：无 Entry 时 Get 直接返回 Define.DefaultValue
- Get() 方法增加 Define 的 Min/Max 硬夹紧逻辑
- 支持热重载：Define 变更无需遍历同步，下次 Get 自动生效

### 迁移影响
- **BREAKING**: 删除 `StatId.cs` 常量类，所有引用改为字符串
- NpcMgr.Create 改为调用 `StatMgr.CreateBlock(npc.Id, "Npc")`
- Npc.Stats 改为查询 StatMgr 的快捷方式

## Capabilities

### New Capabilities
- `stat-define`: 静态属性定义系统，支持 JSON 数据驱动、类型分组、热重载
- `stat-mgr`: 运行时属性管理器，集中持有 StatBlock，支持按实体类型创建和查询
- `stat-entry-calc`: 属性计算模型，支持三层值来源（Define.Default + Modifier + AddValue）

### Modified Capabilities
- 无现有 specs 需要修改（Stat 系统目前无规范化 spec）

## Impact

### 核心代码变更
- `Scripts/Game/Data/Defines/StatDefine.cs` - 新增
- `Scripts/Game/Data/Mgr/StatDefineMgr.cs` - 新增
- `Scripts/Game/Domain/Global/Stat/StatMgr.cs` - 新增
- `Scripts/Game/Domain/Global/Stat/StatEntry.cs` - 重构（删除 BaseValue，新增 AddValue）
- `Scripts/Game/Domain/Global/Stat/StatBlock.cs` - 重构（惰性创建，Define 夹紧）
- `Scripts/Game/Domain/Global/Stat/StatId.cs` - **删除**

### 调用方迁移
- `Scripts/Game/Domain/Object/Npc/NpcMgr.cs` - 初始化改用 StatMgr.CreateBlock
- `Scripts/Game/Domain/Object/Npc/Npc.cs` - Stats 属性改为快捷方式
- `Scripts/Game/Domain/Object/Trait/TraitMgr.cs` - AddModifier 逻辑不变，ID 改为字符串
- `Scripts/Game/Domain/Combat/CombatScene.cs` - StatId 常量改为字符串
- 其他引用 `StatId.xxx` 的文件 - 批量替换为字符串

### 数据文件
- `Data/stat_defines.json` - 新增，定义所有 Stat
