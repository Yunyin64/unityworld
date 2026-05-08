## 1. 枚举定义

- [x] 1.1 在 `Scripts/Game/Data/Enum/EnumTypes.cs` 中新增 `PracticePath` 枚举（None, Ling, Xian, Dao, Wu, Mai, Huang, Hun, Shen, Yi）
- [x] 1.2 在 `Scripts/Game/Data/Enum/EnumTypes.cs` 中新增 `CultivationPointType` 枚举（Card, BehaviorCard, Modifier, Story）

## 2. RealmDefine 数据层

- [x] 2.1 创建 `Scripts/Game/Data/Defines/Practice/RealmDefine.cs`：继承 DefineBase，含 Type(PracticePath)、Level(int)、ProgressRequired(int)、LifespanBonus(float)、Tags(string[]) 字段
- [x] 2.2 创建 `Scripts/Game/Data/Mgr/Practice/RealmDefineMgr.cs`：实现 IDataMgrBase\<RealmDefine\>，含 Get/GetAll/GetByPath/GetByPathAndLevel 方法，JSON 反序列化支持 PracticePath 枚举
- [x] 2.3 创建 `Data/Practice/RealmDefines.json`：至少覆盖 Ling（练气/筑基/金丹）、Wu（锻体/铜皮/铁骨）、Hun（感知/御物/分魂）三个道途各 3 个境界

## 3. CultivationDefine 数据层

- [x] 3.1 创建 `Scripts/Game/Data/Defines/Practice/CultivationDefine.cs`：继承 DefineBase，含 Desc、PathType(PracticePath)、RealmLevel(int)、MaxPoint(int)、Completeness(float)、Points(CultivationPointDefine[])、CoreEffect(CultivationCoreEffect)、Tags(string[])
- [x] 3.2 创建 `CultivationPointDefine` 类（可在同文件或独立文件）：含 Threshold(int)、Type(CultivationPointType)、RefId(string)
- [x] 3.3 创建 `CultivationCoreEffect` 类（可在同文件或独立文件）：含 EffectId(string)、Desc(string)
- [x] 3.4 创建 `Scripts/Game/Data/Mgr/Practice/CultivationDefineMgr.cs`：实现 IDataMgrBase\<CultivationDefine\>，含 Get/GetAll/GetByPath/GetByPathAndRealm 方法
- [x] 3.5 创建 `Data/Practice/CultivationDefines.json`：至少 Ling/Wu/Hun 各 1 本手配示例功法，每本至少 3 个节点（节点 RefId 可使用占位 ID）

## 4. CultivationMgr 运行时骨架

- [x] 4.1 创建 `NpcCultivationData` 类（在 Practice/ 目录）：含 Path、CurrentRealmLevel、RealmProgress、CoreCultivationId、GongFaDatas(List\<GongFa\>)、ActiveSlotIndex
- [x] 4.2 创建 `GongFa` 类：含 DefineId、CurrentPoint、IsCore、GetUnlockedPoints() 方法
- [x] 4.3 创建 `Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs`：实现 IDomainMgrBase，含单例 Instance、NPC 修炼数据字典、Register/GetNpcData/GetNpcPath/GetNpcRealmLevel/GetNpcCoreSlot 方法
- [x] 4.4 CultivationMgr 的 Init/Begin/Tick/End 等生命周期方法保留占位（Tick 不实现逻辑）

## 5. 系统注册与集成

- [x] 5.1 在 `GameDataMgr` 构造函数中注册 RealmDefineMgr 和 CultivationDefineMgr，加载路径为 Data/Practice/ 下对应 JSON
- [x] 5.2 在 `WorldMgr.Initialize()` 中创建并注册 CultivationMgr 到 _mgrs 列表（放在 NpcMgr 之后、StatMgr 之前）

## 6. 验证

- [x] 6.1 确认项目编译通过，无报错
- [x] 6.2 确认 RealmDefines.json 和 CultivationDefines.json 可正常加载（启动时无报错日志）
- [x] 6.3 确认 CultivationMgr.Instance 在初始化后可访问
