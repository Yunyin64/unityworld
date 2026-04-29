---
name: add-define
description: |
  为 UnityWorld 项目添加一个新的静态数据定义（Define）。当用户需要新增一种游戏配置数据类型时使用此技能——包括创建继承 DefineBase 的数据类、对应的 DefineMgr 加载器、在 GameDataMgr 中注册、以及生成空的 JSON 数据文件模板。
  适用于用户提到"新建 Define"、"添加数据定义"、"新增配置类型"、"创建 XxxDefine"、"我需要一个新的数据表"等场景，即使用户只是笼统地说"加个新数据类型"也应触发此技能。
---

# 添加新 Define（静态数据定义）

本技能指导你为 UnityWorld 项目添加一个完整的静态数据定义，包含 **3 个代码文件 + 1 个 JSON 文件 + 1 处注册修改**。

> **参考文件索引**（按需读取，不要一次全读）：
> - `references/define-template.md` — Define 类代码模板
> - `references/definemgr-template.md` — DefineMgr 类代码模板
> - `references/register-and-json.md` — GameDataMgr 注册 + JSON 模板 + 类型速查
> - `references/example-landmark.md` — LandMark 完整示例

## 前置信息收集

在开始之前，需要从用户处了解以下信息：

1. **名称**：新 Define 的主名称（如 `Weapon`、`Skill`、`Building`），会自动派生出：
   - 类名：`{Name}Define`
   - 管理器：`{Name}DefineMgr`
   - JSON 文件：`{Name}Defines.json`
2. **业务描述**：这个 Define 代表什么？一句话 summary 注释
3. **字段列表**：除了基类自带的 `ID` 和 `DisplayName` 之外，需要哪些属性？每个属性需要：
   - 属性名（PascalCase）
   - 类型（`string`、`int`、`float`、`bool`、`List<string>`、`List<int>` 等）
   - JSON 字段名（**必须与属性名完全一致，PascalCase**）
   - 默认值
   - 一句话说明
4. **存放子目录**（可选）：是否放在 `Defines/` 下的子目录中？如果不指定，默认直接放在 `Defines/` 根目录

## 产出清单

按以下顺序创建/修改文件：

| # | 操作 | 文件路径 |
|---|---|---|
| 1 | 新建 | `Scripts/Game/Data/Defines/{SubDir?}{Name}Define.cs` |
| 2 | 新建 | `Scripts/Game/Data/Mgr/{SubDir?}{Name}DefineMgr.cs` |
| 3 | 修改 | `Scripts/Game/Data/GameDataMgr.cs`（注册新 Mgr） |
| 4 | 新建 | `Data/{SubDir?}{Name}Defines.json`（空数组 JSON 模板） |

## 工作步骤

1. **创建 `{Name}Define.cs`** → 读 `references/define-template.md`
2. **创建 `{Name}DefineMgr.cs`** → 读 `references/definemgr-template.md`（结构完全固定，只替换名称）
3. **在 `GameDataMgr.cs` 中注册 + 创建 JSON** → 读 `references/register-and-json.md`
4. 如果对产出不确定，可参照 `references/example-landmark.md` 查看完整示例

## ⚠️ JsonPropertyName 一致性规则（强制）

**`[JsonPropertyName("...")]` 的值必须与 C# 属性名完全一致（PascalCase）。**

```csharp
// ✅ 正确：JsonPropertyName 与属性名一致
[JsonPropertyName("TriggerId")]
public string TriggerId { get; set; } = "";

// ❌ 错误：JsonPropertyName 使用了 camelCase
[JsonPropertyName("triggerId")]
public string TriggerId { get; set; } = "";
```

JSON 文件中的 key 也必须使用 PascalCase：
```json
// ✅ 正确
{ "TriggerId": "trigger_on_use" }

// ❌ 错误
{ "triggerId": "trigger_on_use" }
```

## 完成检查

- [ ] `{Name}Define.cs` 继承 `DefineBase`，namespace 为 `UnityWorld.Game.Data`
- [ ] 所有 public 属性都有 `[JsonPropertyName]` 和 `<summary>` 注释
- [ ] **`[JsonPropertyName]` 的值与属性名完全一致（PascalCase），不允许 camelCase**
- [ ] `{Name}DefineMgr.cs` 实现 `IDataMgrBase<{Name}Define>`
- [ ] `GameDataMgr.cs` 构造函数中已添加 `_datamgrs.Add(...)` 注册
- [ ] JSON 文件位置与 `GameDataMgr` 中注册的路径一致
- [ ] JSON 模板中的 key 名与 `[JsonPropertyName("...")]` 完全匹配（PascalCase）
- [ ] 文件名与主类名一致（`{Name}Define.cs`、`{Name}DefineMgr.cs`）
