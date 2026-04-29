## Why

NPC 修炼体系是游戏的核心玩法系统。当前 `CultivationLevel` 只是 `NpcBioData` 上的一个 int 字段，无法支撑道途选择、境界独立进度、功法修炼节点解锁等丰富的修炼体验。需要建立完整的数据驱动修炼基础体系，让 500 个修士 NPC 各自拥有道途、境界、功法，形成有层次的修炼生命周期。

## What Changes

- 新增 `PracticePath` 枚举（9 个核心道途：None/Ling/Xian/Dao/Wu/Mai/Huang/Hun/Shen/Yi）
- 新增 `RealmDefine`：境界定义，每个道途拥有独立境界序列（如灵修：练气→筑基→金丹→…），数据驱动加载
- 新增 `CultivationDefine`：功法定义，包含道途类型、境界等级、修炼点数上限、完整度、节点序列（Card/BehaviorCard/Modifier/Story 四种类型）、核心效果
- 新增 `RealmDefineMgr` / `CultivationDefineMgr`：数据管理器，加载 JSON 配置
- 新增 `Data/Practice/` 目录：存放 `RealmDefines.json` 和 `CultivationDefines.json`
- 新增 `CultivationMgr`：功法运行时管理器（本次仅搭建骨架，不实现生成逻辑）
- **BREAKING**：`NpcBioData.CultivationLevel` 将被新的境界系统替代，现有引用需迁移

## Capabilities

### New Capabilities
- `practice-path-enum`：道途枚举定义（PracticePath），9 个核心道途类型
- `realm-define`：境界定义与加载系统（RealmDefine + RealmDefineMgr），每道途独立境界序列
- `cultivation-define`：功法定义与加载系统（CultivationDefine + CultivationDefineMgr），含修炼节点序列与核心效果
- `cultivation-mgr`：功法运行时管理器骨架（CultivationMgr），NPC 功法持有与进度追踪

### Modified Capabilities
<!-- 无需修改现有 spec 级别的行为要求 -->

## Impact

- **Data 层**：新增 `Data/Defines/Practice/` 下 RealmDefine.cs、CultivationDefine.cs；新增 `Data/Mgr/Practice/` 下对应 DefineMgr
- **Data 文件**：新增 `Data/Practice/RealmDefines.json`、`Data/Practice/CultivationDefines.json`
- **Domain 层**：新增 `Domain/GamePlay/Practice/CultivationMgr.cs`
- **枚举**：`EnumTypes.cs` 新增 PracticePath 枚举和 CultivationPointType 枚举
- **现有代码**：`NpcBioData.CultivationLevel`、`NpcMgr.Create()` 中修为相关逻辑、`CombatScene` 中 CultivationLevel 引用均需迁移适配
- **GameDataMgr**：注册新的 RealmDefineMgr 和 CultivationDefineMgr
- **WorldMgr**：注册 CultivationMgr