## Why

Tag 系统是游戏世界语义描述的核心基础设施。当前 TagDefine 结构过于简单（仅 ID/DisplayName/Desc），无法表达 Tag 之间的层级关系和冲突关系。

同时，随着未来 Tag 数量的大量增长（AI 生成），单文件存储（TagDefines.json）将难以维护。

本变更实现 Tag 的三层分类体系，并为未来的 AI 辅助生成范式奠定基础。

## What Changes

**TagDefine 扩展**
- 新增 `Parent` 字段：支持树状层级结构
- 新增 `ConflictTags` 字段：显式声明冲突关系（不继承）

**数据存储重构**
- **BREAKING**：从单文件 `Data/TagDefines.json` 改为目录扫描 `Data/Tag/**/*.json`
- 目录命名统一：`Object` → `Entity`，`xuni` → `Virtual`
- 按一级元分类分目录存储

**TagDefineMgr 能力扩展**
- 新增层级查询：GetDepth、GetParent、GetChildren、GetAncestors、GetDescendants、GetByDepth
- 新增冲突查询：IsConflict
- 支持多文件加载和合并

**TagMgr 初始化**
- 实现 `Init()` 方法，完成空生命周期实现

## Capabilities

### New Capabilities

- `tag-hierarchy`：Tag 层级系统，支持 Parent 链条表达的树状结构，提供层级查询能力
- `tag-multi-file-loader`：Tag 多文件加载器，扫描目录下所有 JSON 文件并合并加载

### Modified Capabilities

- 无（现有 Tag 系统无规格文档）

## Impact

**数据文件**
- 删除：`Data/TagDefines.json`
- 新增：`Data/Tag/Core/`、`Data/Tag/Entity/`、`Data/Tag/Virtual/` 下的 JSON 文件

**代码文件**
- 修改：`Scripts/Game/Data/Defines/TagDefine.cs` - 扩展字段
- 修改：`Scripts/Game/Data/Mgr/TagDefineMgr.cs` - 多文件加载 + 层级查询
- 修改：`Scripts/Game/Domain/Object/Tag/TagMgr.cs` - 实现 Init()
- 修改：`Scripts/Game/Data/GameDataMgr.cs` - 更新 TagDefineMgr 初始化路径

**设计约束**
- 层级深度隐含生成来源：Depth=1 人类配置、Depth=2 AI+人类审核、Depth≥3 AI+AI自检
- 冲突关系显式声明，不自动继承
- Tag 引用使用简洁 ID，不需要全路径
