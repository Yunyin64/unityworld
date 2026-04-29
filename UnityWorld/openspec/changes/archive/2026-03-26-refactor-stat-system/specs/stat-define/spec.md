## ADDED Requirements

### Requirement: StatDefine 数据结构定义
系统 SHALL 提供 `StatDefine` 类继承 `DefineBase`，包含以下字段：
- `Type: string`：归属 Object 类型（如 "Npc" / "Tile" / "Global"）
- `DefaultValue: float`：默认基础值
- `MinValue: float?`：可选的全局下限（null 表示无限制）
- `MaxValue: float?`：可选的全局上限（null 表示无限制）
- `DisplayFormat: string`：显示格式标识（如 "Integer" / "Float2" / "Percent"）
- `Formula: string`：自定义公式（占位字段，当前不解析）
- `Category: string`：UI 分类（如 "生命" / "社会"）

#### Scenario: 加载有效的 StatDefine JSON
- **WHEN** `stat_defines.json` 包含有效数据
- **THEN** 所有 StatDefine 被正确解析并可通过 `StatDefineMgr.Get(id)` 查询

#### Scenario: 字段缺失时使用默认值
- **WHEN** JSON 中 `MinValue` 或 `MaxValue` 未定义
- **THEN** 对应字段为 `null`，表示无限制

---

### Requirement: StatDefineMgr 按 Type 过滤
`StatDefineMgr` SHALL 提供 `GetByType(string type)` 方法，返回所有 `Type` 字段匹配的 StatDefine 列表。

#### Scenario: 查询特定类型的 StatDefine
- **WHEN** 调用 `StatDefineMgr.Instance.GetByType("Npc")`
- **THEN** 返回所有 `Type == "Npc"` 的 StatDefine

#### Scenario: 类型不存在时返回空列表
- **WHEN** 调用 `GetByType("NonExistentType")`
- **THEN** 返回空列表（非 null）

---

### Requirement: StatDefine 与 Flag 的边界
StatDefine 的 `ID` SHALL 在 `stat_defines.json` 中预定义，与 Flag 系统形成清晰区分：
- **Stat**：预定义、有类型归属、支持 Modifier 修正、数值型
- **Flag**：随意命名、无类型约束、直接赋值、任意类型

#### Scenario: Stat ID 必须预先定义
- **WHEN** 代码引用 `StatBlock.Get("unknown_stat")`
- **THEN** 系统返回 `Define?.DefaultValue ?? 0`，若无 Define 则返回默认值 0

#### Scenario: Flag 可随意命名
- **WHEN** 代码调用 `FlagMgr.SetNpc(id, "任意key", value)`
- **THEN** 无需预先定义，直接存储
