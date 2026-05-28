## Context

当前 25 个 `IDataMgrBase<TDefine>` 实现分为两种硬编码模式：单文件加载 vs 文件夹遍历。每个类 50-80 行，其中 80%+ 是相同逻辑。新增 Define 时需要完整复制一个 Mgr 文件再改类名和泛型参数。

## Goals / Non-Goals

**Goals:**
- 消除 25 个 Mgr 中的重复样板代码
- 统一路径判断逻辑：传入路径自动识别单文件 / 文件夹
- 保持外部 API 签名不变（Get/GetAll/Contains/Query），不破坏调用方
- 子类可自定义 `JsonSerializerOptions`（EnumConverter 等）
- 子类可保留额外方法（GetByType、GetRandom 等）

**Non-Goals:**
- 不改动 `GameDataMgr` 的注册方式和构造函数调用
- 不引入递归目录扫描（保持 TopDirectoryOnly）
- 不自动化 `static Instance` 赋值（保持手动）
- 不改动任何 JSON 数据文件格式

## Decisions

### 1. 用抽象泛型基类而非 helper 方法

**选择**：`abstract class DefineMgrBase<TDefine> : IDataMgrBase<TDefine>`

**备选**：
- 静态 helper 类 `DefineLoader.LoadFile<T>()` / `DefineLoader.LoadFolder<T>()` → 仍需每个 Mgr 写字典 + 查询方法
- Source Generator → 过重，调试困难

**理由**：基类继承最直接地消除重复，且 C# 泛型类可被子类自然扩展。

### 2. 路径判断策略：先 File.Exists 再 Directory.Exists

```csharp
private void LoadPath(string path)
{
    if (File.Exists(path))       → LoadSingleFile(path)
    else if (Directory.Exists(path)) → LoadFolder(path)
    else → LogMgr.Instance.Warn(...)
}
```

**理由**：用户明确要求双检查，且 `.json` 后缀判断不够可靠（未来可能有非 .json 后缀数据文件）。

### 3. JsonOptions 通过 virtual 方法提供

```csharp
protected virtual JsonSerializerOptions CreateJsonOptions() => DefaultJsonOptions;
```

基类提供默认 options（CaseInsensitive + SkipComment）。6 个需要 EnumConverter 的子类 override。

**备选**：构造函数参数传入 → 增加所有子类构造函数复杂度。virtual 更干净。

### 4. 日志前缀用 `virtual string MgrName`

默认 `GetType().Name`。如需定制可 override，但 99% 情况默认就够。

### 5. 文件位置：`Scripts/Core/Base/DefineMgrBase.cs`

与 `DefineBase.cs` 同级，同属 Core 层。namespace `UnityWorld.Core`（或无 namespace，视现有文件风格）。

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| 继承链增加一层，调试 stack trace 多一帧 | 基类逻辑简单透明，不影响可读性 |
| 子类忘记写 `Instance = this` | 不用基类自动化，靠 add-define skill 模板保证 |
| `File.Exists` + `Directory.Exists` 双 IO 调用 | 仅在 Load 时执行一次，性能无影响 |
| 改 25 个文件存在合并冲突风险 | 每个文件改动模式一致，冲突易解 |
