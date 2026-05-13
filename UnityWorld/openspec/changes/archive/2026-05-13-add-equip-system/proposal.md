## Why

当前游戏中 NPC 的能力主要由 Trait（特质）和 Card（卡牌）系统驱动，缺少「装备」这一经典 RPG 维度。装备体系能为角色成长、物品经济和战斗策略提供新的深度层次——NPC 可以穿戴武器、护甲、饰品等来获得属性加成、附带技能卡或触发特殊效果。作为第一步，本次变更仅建立装备的**静态数据定义层**（EquipDefine + EquipDefineMgr），为后续的运行时穿戴、战斗集成、掉落生成等功能打下基础。

## What Changes

- 新增 `EquipDefine` 类（继承 `DefineBase`），定义装备的静态配置数据结构
- 新增 `EquipDefineMgr` 类（实现 `IDataMgrBase<EquipDefine>`），负责从 `Data/Equip/` 文件夹加载所有 JSON 文件
- 在 `GameDataMgr` 构造函数中注册 `EquipDefineMgr`
- 新增 `Data/Equip/` 文件夹，放置装备定义 JSON 文件（文件夹模式，与 Card、Stat 一致）

## Capabilities

### New Capabilities
- `equip-define`: 装备静态数据定义——EquipDefine 数据结构、EquipDefineMgr 加载器、JSON 数据文件夹规范

### Modified Capabilities
<!-- 本次不修改现有 capability 的需求层行为 -->

## Impact

- **新增文件**：`Scripts/Game/Data/Defines/EquipDefine.cs`、`Scripts/Game/Data/Mgr/EquipDefineMgr.cs`
- **修改文件**：`Scripts/Game/Data/GameDataMgr.cs`（构造函数中注册新 Mgr）
- **新增数据目录**：`Data/Equip/`（含初始空模板 JSON）
- **无破坏性变更**：不影响现有 Define / Mgr / 运行时逻辑
