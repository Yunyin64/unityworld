## Why

25 个 DefineMgr 子类中存在大量重复代码（字典声明、JsonOptions、Load 逻辑、Get/GetAll/Contains/Query 实现）。每新增一个 Define 都要复制 50-80 行样板。同时，部分 Mgr 硬编码为"读文件夹"、另一部分硬编码为"读单文件"，不够灵活——同一个 Mgr 无法同时兼容两种输入格式。

## What Changes

- 新增泛型抽象基类 `DefineMgrBase<TDefine>`，收拢所有公共逻辑（路径判断、加载、字典管理、查询）
- 路径自动判断：`File.Exists` → 单文件模式；`Directory.Exists` → 文件夹模式（TopDirectoryOnly）
- 子类只需声明 `Instance`、调用 `base(path)`，有需要时 override `CreateJsonOptions()`
- 调整 `IDataMgrBase` 接口签名：`Load(string filePath)` → `Load(string path)`，注释说明支持文件或文件夹
- 现有 25 个 Mgr 文件瘦身为极简形式（保留各自额外方法如 `GetByType`、`GetRandom`）

## Capabilities

### New Capabilities
- `define-mgr-base-class`: 提供 `DefineMgrBase<TDefine>` 泛型基类，统一 JSON 数据管理器的加载与查询逻辑

### Modified Capabilities
（无 spec 级行为变更，仅内部实现重构）

## Impact

- **新增文件**：`Scripts/Core/Base/DefineMgrBase.cs`
- **修改文件**：`Scripts/Core/Base/Interface/IDataMgrBase.cs`（参数名 + 注释）
- **瘦身文件**：`Scripts/Game/Data/Mgr/` 下全部 25 个 Mgr（删除重复逻辑，改为继承基类）
- **不影响**：`GameDataMgr.cs` 构造调用方式不变，外部 API（Get/GetAll/Contains/Query）签名不变
