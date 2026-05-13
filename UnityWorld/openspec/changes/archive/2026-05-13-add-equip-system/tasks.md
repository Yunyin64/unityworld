## 1. EquipDefine 数据结构

- [x] 1.1 创建 `Scripts/Game/Data/Defines/EquipDefine.cs`，继承 `DefineBase`，包含 Size(int=1)、AttackBase(int=0)、DefendBase(int=0)、SpeedBase(float=0)、AmountBase(int=1)、FormListBase(List\<string\>=[]) 六个字段，属性名 PascalCase，带 `<summary>` 注释

## 2. EquipDefineMgr 加载器

- [x] 2.1 创建 `Scripts/Game/Data/Mgr/EquipDefineMgr.cs`，实现 `IDataMgrBase<EquipDefine>`，文件夹遍历模式（参照 CardDefineMgr），含 Instance 单例、Get/GetAll/Contains/Query 方法
- [x] 2.2 在 `Scripts/Game/Data/GameDataMgr.cs` 构造函数中注册 `new EquipDefineMgr(Path.Combine(dataDir, "Equip"))`

## 3. Equip 运行时实例

- [x] 3.1 创建 `Scripts/Game/Domain/Object/Equip/Equip.cs`，实现 `IFormDefine<EquipDefine>`，包含 DefineId、DisplayName、Size(int)、Attack(int)、Defend(int)、Speed(float)、Amount(int)、FormList(List\<string\>) 字段，带 `<summary>` 注释
- [x] 3.2 提供静态工厂方法 `Equip.FromDefine(EquipDefine define)` 从 Define 复制 Base 值创建实例

## 4. 数据文件

- [x] 4.1 创建 `Data/Equip/` 目录
- [x] 4.2 创建 `Data/Equip/Equip_Template.json`，包含至少一个示例 EquipDefine 条目（如 short_sword），验证 Mgr 可正常加载
