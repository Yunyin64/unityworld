---
name: add-domain-data
description: |
  为 UnityWorld 项目添加一个新的运行时领域数据类（继承 IDomainDataBase）。当用户需要为游戏实体（Npc/Tile/Trait 等）或已有 Data 添加一个新的运行时数据容器时使用此技能——包括创建 Data 类、Log() 占位、以及在同文件中生成 partial class 便捷访问器。
  适用于用户提到"新建 Data"、"添加运行时数据"、"创建 XxxData"、"给 Npc 加个新数据块"、"新增 DomainData"、"加个 Data 类"等场景，即使用户只是说"加一个新的数据容器"也应触发此技能。注意：如果用户要添加的是静态配置数据（Define），应使用 add-define 技能而非此技能。
---

# 添加新 DomainData（运行时领域数据）

本技能指导你为 UnityWorld 项目创建一个继承 `IDomainDataBase` 的运行时数据类。每个 Data 类是一个**可变的运行时数据容器**，挂载在游戏实体（如 `Npc`、`Tile`、`Trait`）或另一个 Data 类上，用于存储特定领域的游戏状态。

与静态配置的 `*Define` 不同，`*Data` 类是运行时可变的，不从 JSON 加载，而是在游戏运行过程中由各子系统创建和修改。

> **参考文件索引**（按需读取，不要一次全读）：
> - `references/template-on-entity.md` — 场景A模板：Data 直接挂在实体上
> - `references/template-nested.md` — 场景B模板：Data 嵌套在另一个 Data 中
> - `references/coding-rules.md` — 编写规范（命名、注释、默认值、using、访问器）
> - `references/examples.md` — 3 个完整示例（NpcBioData / NpcAppearanceData / NpcCultivationData）

## 前置信息收集

### 必须信息

1. **宿主实体/Data 类型**：挂在哪里？
   - **挂在实体上**（如 `Npc`、`Tile`、`Trait`）→ 用场景 A 模板
   - **挂在另一个 Data 上** → 用场景 B 模板
2. **名称**：`{宿主前缀}{领域名}Data`（如 `NpcBioData`、`TileAuraData`）
3. **业务描述**：一句话 `<summary>` 注释
4. **字段列表**：属性名 + 类型 + 默认值 + 说明

### 可选信息

5. **辅助结构体**：是否需要定义辅助 struct
6. **便捷访问器**：用户未指定则根据字段自动推断
7. **文件存放位置**：默认 `Scripts/Game/Domain/Object/{Entity}/Data/`

## 产出清单

只产出 **1 个文件**（不需要注册步骤）：

| # | 操作 | 文件路径 |
|---|---|---|
| 1 | 新建 | `Scripts/Game/Domain/Object/{Entity}/Data/{Name}Data.cs` |

文件内包含：Data 类本体 + 宿主的 partial class 扩展

## 工作步骤

1. 根据宿主类型读取对应模板 → `references/template-on-entity.md` 或 `references/template-nested.md`
2. 编写时遵循规范 → `references/coding-rules.md`
3. 不确定时参照示例 → `references/examples.md`

## 完成检查

- [ ] 文件名与主 Data 类名完全一致
- [ ] namespace 为 `UnityWorld.Game.Domain`
- [ ] Data 类继承了 `IDomainDataBase`
- [ ] 所有 `public` 属性都有 `<summary>` 注释
- [ ] 字段按业务分组，使用 `// ──` 分隔线
- [ ] 所有属性都有合理的默认值
- [ ] `Log()` 方法存在但体内只有 TODO 注释
- [ ] 同文件底部有宿主的 `partial class` 扩展
- [ ] using 语句完整且排序正确
- [ ] 如果存在辅助 struct，已定义 `Zero` 静态工厂属性
