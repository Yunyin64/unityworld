## 1. 接口与基类

- [x] 1.1 修改 `IDataMgrBase.cs`：将 `Load(string filePath)` 参数名改为 `path`，更新 XML 注释
- [x] 1.2 新建 `Scripts/Core/Base/DefineMgrBase.cs`，实现泛型基类全部逻辑（路径判断、单文件加载、文件夹加载、字典管理、查询方法、virtual CreateJsonOptions、virtual MgrName）

## 2. 子类迁移 — 纯简型（无自定义 Options、无额外方法）

- [x] 2.1 迁移 `TraitDefineMgr`
- [x] 2.2 迁移 `SocialRoleMgr`
- [x] 2.3 迁移 `CardDefineMgr`
- [x] 2.4 迁移 `ConditionDefineMgr`
- [x] 2.5 迁移 `ActionDefineMgr`
- [x] 2.6 迁移 `EffectDefineMgr`
- [x] 2.7 迁移 `EquipDefineMgr`
- [x] 2.8 迁移 `ExtraElementMgr`
- [x] 2.9 迁移 `TagDefineMgr`
- [x] 2.10 迁移 `TriggerDefineMgr`
- [x] 2.11 迁移 `OptionDefineMgr`
- [x] 2.12 迁移 `BehaviorCardDataMgr`
- [x] 2.13 迁移 `NpcModifierDefineMgr`
- [x] 2.14 迁移 `TileModifierDefineMgr`
- [x] 2.15 迁移 `LandMarkDefineMgr`
- [x] 2.16 迁移 `RegionDefineMgr`
- [x] 2.17 迁移 `ExtraBehaviorDefineMgr`
- [x] 2.18 迁移 `CombatModifierDefineMgr`

## 3. 子类迁移 — 需自定义 JsonOptions

- [x] 3.1 迁移 `NpcDefineMgr`（EnumConverter + GetRandom 额外方法）
- [x] 3.2 迁移 `PlaneDefineMgr`（EnumConverter + GetByKind 额外方法）
- [x] 3.3 迁移 `StatDefineMgr`（EnumConverter + GetByType 额外方法）
- [x] 3.4 迁移 `EventDefineMgr`（EnumConverter）
- [x] 3.5 迁移 `CultivationDefineMgr`（EnumConverter + GetByPath/GetByPathAndRealm）
- [x] 3.6 迁移 `RealmDefineMgr`（EnumConverter + GetByPath/GetByPathAndLevel）

## 4. 迁移 — 特殊情况

- [x] 4.1 迁移 `StoryDefineMgr`（保留 BuildMergedOptions 额外方法）

## 5. 验证

- [x] 5.1 全量编译通过
- [x] 5.2 运行 CombatTestRunner 确认数据加载正常
