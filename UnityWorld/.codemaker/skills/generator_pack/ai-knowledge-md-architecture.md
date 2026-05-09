# AI Knowledge MD 架构规范（统一版）

> 本文档是知识库生成规范的唯一权威来源（Single Source of Truth）。
> 生成 agent 与读取/查询 agent 必须共享并遵循同一份规范。

## 1. 目标

在保持五层架构不变的前提下，建立可持续、可协作、可增量构建的项目 AI 知识库：

1. 分类稳定：`Flow / Slice / Module / Concept / Experience`
2. 构建可控：支持“小范围先跑、后续补全”
3. 结构清晰：支持主模块/子模块拆分
4. 路径可还原：统一“项目根目录占位 + 相对路径”
5. 治理可执行：状态、置信度、复审周期、DoD 可检查

## 2. 五层定义

1. `Flow`：运行时动态行为链路（时序）
2. `Slice`：数据结构与字段变化（状态）
3. `Module`：能力边界、对外接口、对外配置、内部组织（实现容器）
4. `Concept`：跨模块上位概念、分层认知与查询导航（认知框架）
5. `Experience`：跨模块组合、选型与落地经验（决策实践）

一句话：Flow 管过程，Slice 管状态，Module 管边界，Concept 管分类，Experience 管组合决策。

## 3. 内容优先级原则（强制）

**知识文档的核心目标有三：让 agent/开发者知道"怎么用、怎么配、去哪改"；能根据异常信号快速定位问题；理解系统运作原理以便做设计决策。**

优先级从高到低：

1. **使用与配置指引**（最高优先级）：怎么调用、怎么配置、配置文件在哪、新增功能时该改哪里。必须给出可直接操作的样例和文件路径。
2. **对外接口与对外配置**：Module 必须详细列出可调用的函数/类/命令和可修改的配置项，每个都附带最小使用样例和配置文件路径。
3. **异常定位与排障指引**：运行时链路（Flow）和状态字段（Slice）的首要价值是支撑 bug 定位——从错误日志/异常信号出发，沿链路逐节点排查，或在状态字段的写入/读取点间追溯数据流。必须确保每步都有代码定位锚点。
4. **架构与设计认知**：One-screen 架构、分层模型、概念定义，帮助开发者理解系统全貌、做设计决策时选对模块。

**生成 agent 检查清单**（每篇文档生成后自检）：

使用与配置维度：
- [ ] 一个开发者读完本文档后，能否知道怎么调用/配置这个模块？
- [ ] 配置文件的路径是否明确列出？
- [ ] 新增功能时需要修改哪些文件是否已指出？
- [ ] 是否提供了可直接复制使用的调用样例？
- [ ] 是否指出了常见配置/使用误区？

排障定位维度：
- [ ] 遇到报错时，能否从文档中的异常信号/诊断映射出发，定位到具体的链路节点或状态字段？
- [ ] Flow 的每个步骤是否都有代码定位锚点，能直接跳转到源码？
- [ ] Slice 的写入点和读取点是否完整，能追溯"谁写了这个值、谁读了它"？
- [ ] 是否列出了本模块相关的关键日志关键字/错误模式？

数据结构拆分维度：
- [ ] Module 正文中是否有数据结构描述超过 3 行或需要表格列举字段/枚举？若有，是否已拆分为独立 Slice？
- [ ] 是否存在位掩码/枚举体系（常量 ≥ 4 个）未独立文档化？
- [ ] 是否存在多层级联结构（链表/树/嵌套）或磁盘→内存多步变换未文档化？

引用完整性维度（每次创建/修改/删除文档后必检）：
- [ ] 本文档 depends_on / affects 中引用的所有 ID 是否仍然存在？
- [ ] 若新建文档：知识库中是否已有文档应引用本新文档但未引用？搜索相关关键字补齐
- [ ] 若修改文档 ID：是否全库搜索旧 ID 并更新？
- [ ] 若删除文档：是否全库搜索该文档 ID 的引用并清理？
- [ ] 最终验证：`grep -r "旧ID" .context/code/` 结果为零残留

若上述任何一项回答为否，文档尚未达标。

## 4. 构建方式（强制）

采用“人定边界，机做填充”：

1. 人工给出模块命题与边界（先主模块）
2. AI 产出 `Module` 文档（含代码定位与关系字段）
3. 人工确认后，AI 批量补子模块
4. AI 按优先级补 P0 Flow/Slice，再补 P1/P2 Experience

禁止“一次性全自动全量生成”。

## 4. 主模块/子模块规则

当模块体量大、职责复杂时，必须拆分子模块。

### 4.1 主模块职责

- 给出整体边界与能力地图
- 在 front matter 维护 `submodules`

### 4.2 子模块职责

- 聚焦单一职责域
- 在 front matter 维护 `parent_module`

### 4.3 命名规则

- 知识文档文件名统一使用：`<name>.<category>.md`
- 若是子文档，使用：`<name>.<subname>.<category>.md`
- front matter 里的 `id` 必须与文件名去掉 `.md` 后完全一致
- `name` / `subname` 必须使用语义名称，不绑定源码目录
- 不要把 `mmo`、`engine`、`server_common`、`game` 这类源码路径词写进名称
- 源码归属放在 front matter 或正文里表达，例如：`layer`、`domain`、`module_root`、`owners`
- `<category>` 取值：`module`、`flow`、`slice`；`concept` 和 `experience` 保持 `concept.<name>` / `experience.<name>` 前缀格式

### 4.4 乐观模式（强制）

- module/flow/slice 正文只描述当前文档内容，不维护跨文档关系清单。
- 关系信息统一写在 front matter（`submodules`、`parent_module`、`depends_on`、`affects`）。
- flow/slice 补充按引用缺口创建。
- 创建后不要求回改其他文档正文；关系以 front matter 为准。

### 4.5 模块分层分类标准（强制）

知识文档按**与游戏业务的关联度**分为四个层级，每层一个目录。新建模块时必须按以下标准判定归属。

#### 判定标准

| 层级 | 目录 | 判定标准 | 典型模块 |
|------|------|----------|----------|
| 基础组件 | `infrastructure/` | 与游戏业务无关；金融、互联网等其他行业也有同类组件 | 网络管线、RPC框架、数据库代理、缓存、序列化、脚本运行时 |
| 业务组件 | `game_foundation/` | 与MMO游戏相关，但玩家玩游戏不需要了解；是玩法的底层支撑 | AOI、格子管理、场景管理、运动、玩家同步、登录会话/传送、活动框架 |
| 具体业务 | `business/` | 玩家直接感知的玩法功能；有明确的UI入口和业务闭环 | 中秋活动、IP联动、充值、钓鱼、论武PVP |
| 工具 | `tools/` | 辅助开发的离线工具 | 生成器工具包、索引脚本 |

#### 判定决策流程

判断一个模块属于哪层时，依次回答以下问题：

1. **这个组件是否只存在于游戏行业？** 如果金融/互联网/物联网等行业也有类似组件（如网络通信、数据库访问、缓存），→ `infrastructure/`
2. **玩家是否能直接感知并主动使用这个功能？** 如果玩家有明确的UI入口、可以主动操作、有独立业务闭环，→ `business/`
3. **这个组件是否支撑了上层玩法但玩家不需要了解？** 如果是游戏特有的底层支撑（如AOI、场景管理），玩家无感知但上层玩法依赖它，→ `game_foundation/`
4. **是否是开发辅助工具而非运行时组件？** → `tools/`

#### 边界案例处理

- **框架 vs 具体实现**：活动框架（`activity_framework`）归 `game_foundation/`，具体活动（中秋、IP联动）归 `business/`。框架提供基础设施，具体活动是玩家可感知的玩法。
- **引擎能力**：如果引擎能力同时被游戏逻辑和非游戏逻辑使用（如RPC、序列化），归 `infrastructure/`；如果仅服务于游戏场景（如AOI），归 `game_foundation/`。
- **跨层依赖**：`business/` 可依赖 `game_foundation/`，`game_foundation/` 可依赖 `infrastructure/`，禁止反向依赖。`concept/` 和 `experience/` 可跨层引用。

#### front matter 映射

层级与 front matter `layer` 字段的对应关系：

| 目录 | 典型 `layer` 值 |
|------|-----------------|
| `infrastructure/` | `infra`、`platform` |
| `game_foundation/` | `platform`、`domain` |
| `business/` | `domain`、`scenario` |

`layer` 描述技术分层，目录归属描述业务关联度，两者不要求一一对应。

## 5. 路径与代码定位规则

### 5.1 路径规则

- 知识文档中的代码路径必须是相对路径。
- 知识文档禁止本机绝对路径（如 `S:/...`、`C:/...`）。
- 知识文档禁止 `source_root` 字段。
- 完整路径通过入口规则还原：`<project_root>/<relative_path>`。
- `project.source_root` 仅允许存在于本地生成器配置，不进入共享知识文档。

### 5.2 代码定位规则

- 禁止记录行号。
- 必须记录“文件 + 文件内搜索关键字/函数名/结构名”。
- 对 `Flow` 文档：触发点必须包含可定位锚点（相对路径 + 文件内搜索关键字，函数名或事件名均可）。
- 对 `Flow` 文档：主链路中的每一个节点必须内联“文件 + 搜索关键字”，禁止将“主链路步骤”和“关键定位”拆成两个独立区块。

## 6. Front Matter 规范

### 6.1 最小字段（强制）

```yaml
id: <global-unique-id>
type: flow|slice|module|concept|experience
layer: infra|platform|domain|scenario|crosscut
domain: <domain-name>
status: pending|partial|complete|draft|reviewed|approved|deprecated
confidence: 0.0-1.0
keywords: []
depends_on: []
affects: []
owners: []
review_cycle_days: 30
last_verified: YYYY-MM-DD
```

### 6.2 补充字段

- 模块拆分：`parent_module` / `submodules`
- 路径边界：`module_root`
- 经验编排：`uses_modules` / `uses_flows` / `checks_slices`
- 概念映射：`categorized_as` / `implemented_by` / `uses_concepts`
- 概念形态：`concept_shape`（仅 concept 类型使用，取值 `layered|aggregation|bridge|taxonomy`）
- 决策追溯：`decided_by`
- 增量补全：`missing_info`

### 6.3 关系字段放置规则（强制）

- `submodules` / `parent_module` / `depends_on` / `affects` 必须写在 front matter。
- 正文禁止重复维护“关联 Flow / 关联 Slice”清单。
- 正文可保留简短加载提示，引导 agent 优先读取 front matter。

## 7. 引用关系规范

- `depends_on`：运行依赖
- `affects`：状态影响
- `categorized_as`：Module -> Concept
- `implemented_by`：Concept -> Module
- `uses_concepts`：Flow/Module/Experience -> Concept
- `uses_modules`：Experience -> Module
- `uses_flows`：Experience -> Flow
- `checks_slices`：Experience -> Slice
- `submodules`：主模块 -> 子模块
- `parent_module`：子模块 -> 主模块

## 8. 状态模型与治理

### 8.1 生成态

- `pending`：占位文档
- `partial`：部分完成，含 `missing_info`
- `complete`：当前范围闭环可用

### 8.2 治理态

- `draft`：初稿
- `reviewed`：已审阅
- `approved`：高置信依赖级别
- `deprecated`：废弃，仅保留历史

### 8.3 治理规则

1. 超过 `review_cycle_days` 未复核，降级为 `reviewed`
2. 关键路径（账号/支付/核心玩法）必须保持 `approved`
3. 文档与代码冲突时，以代码事实为准并立即补文档

## 9. 质量门槛（DoD）

1. 每个 `Flow` 至少关联 1 个 `Slice`，并给出终态可校验断言
2. 每个 `Flow` 的主链路每个步骤都必须包含代码定位锚点（相对路径 + 文件内搜索关键字）
3. 每个 `Flow` 的触发点必须包含代码定位锚点（相对路径 + 文件内搜索关键字，函数名或事件名均可）
4. 每个 `Slice` 至少列出 1 个写入方 + 1 个读取方
5. 每个 `Slice` 必须按”状态字段”展开，且每个字段至少包含：
   - 定义/声明点：文件 + 类名（如有）+ 成员名
   - 写入点：文件 + 函数名（复杂逻辑可附简述）
   - 读取点：文件 + 函数名（复杂逻辑可附简述）
6. 每个 `Module` 至少关联 1 个 `Flow` 或 `Slice`，并给出对外能力面
7. 每个 `Module` 必须明确写出”对外接口”和”对外配置”两个维度：
   - 对外接口：函数、类方法、命令、RPC、回调、消息入口等调用面
   - 对外配置：协议表、注册表、模板表、配置文件、枚举映射、约定数据表等配置面
   - 若某一维度不存在，必须显式写”无显式对外接口”或”无显式对外配置”
   - 若模块通过配置向外暴露能力，配置视为 Public Surface，优先级等同于接口
8. 每个 `Module` 的对外接口和对外配置都必须提供可直接使用的描述，至少包含：
   - 作用
   - 使用前置/配置前置
   - 输入输出或关键字段/取值
   - 最小使用样例或典型调用/加载链
   - **配置定义路径**（对外配置必须明确指出配置文件的相对路径，格式如 `program/game/xxx/xxx.lua`）
9. 每个 `Module` 必须给出”配置路径汇总”表格（即使只有一行），列出本模块所有相关配置的定义文件路径和用途，让开发者一眼知道”去哪改”
10. 每个 `Module` 必须给出”总体架构（One-screen）”，让使用方一眼看懂核心运作原理
10. 若 `Module` 无法给出简明总体架构，必须触发拆分：多个主模块或主模块 + 子模块
11. 每个 `Concept` 至少关联 2 个 `Module`
12. 每个 `Concept` 必须明确写出：
   - 概念定义
   - 为什么重要
   - 核心组织结构（由 `concept_shape` 决定子模板：`layered` → 分层与数据流 + 层间契约；`aggregation` → 实现对比与选型；`bridge` → 对接面 + 一致性约束；`taxonomy` → 分类树 + 分类约束）
   - 模块映射
   - 典型查询路径（决策表形式）
   - 诊断信号映射（日志关键字/错误模式 → 归属层/实现 → 下钻目标）
   - 常见误区
   - In / Out 边界
13. `Concept` 不承载具体 Public API 明细，不替代 `Module / Flow / Slice`
14. 当文档主要回答”这套东西该怎么理解、怎么分层、先看谁后看谁”时，应建 `Concept`，不应伪装成 `Module`
15. 每个 `Experience` 至少关联 2 个 `Module` + 1 个 `Flow` + 1 个 `Slice`
16. 主模块声明 `submodules` 时，子模块必须回填 `parent_module`
17. **复杂数据结构必须拆分 Slice**（强制）：当 Module 文档中涉及以下任一情况时，必须为该数据结构创建独立的 Slice 文档，不得仅在 Module 正文中简单描述：
    - C++ 结构体/类有 5 个以上字段（如 CoreGrid_t、CoreObject_t 等引擎核心结构）
    - 包含位掩码/枚举体系（如 Flag 常量、Barrier 类型分级）且常量数量 ≥ 4 个
    - 包含多层级联结构（如 Z 轴链表、树形嵌套）需要图示说明
    - 数据从磁盘到内存经过多步变换（如压缩→解压→反序列化→填充），需文档化加载链路
    - 字段含义不直观（如位域打包、高低位复用），不文档化会导致误用

    判定时机：在编写 Module 文档时，若正文中对某个数据结构的描述超过 3 行或需要表格列举字段/枚举，即触发 Slice 拆分。Slice 拆分后 Module 正文仅保留简要概述和指向 Slice 的引用。

18. **文档创建/修改后必须审计引用完整性**（强制）：每次创建或修改任何知识文档后，必须执行以下审计步骤：
    - **正向引用审计**：检查本文档 `depends_on` / `affects` 中引用的所有文档 ID 是否仍然存在（目标文档未被删除/重命名）
    - **反向引用审计**：在知识库中搜索所有引用了旧 ID（如被重命名的 module、被删除的 submodule）的文档，逐一更新为新 ID
    - **新增文档的引用补全**：新建文档后，搜索知识库中所有 `depends_on`/`affects` 可能需要引用本新文档的已有文档，补齐引用
    - **删除文档的引用清理**：删除文档后，搜索所有引用了该文档 ID 的其他文档，更新或移除引用
    - 审计方法：`grep -r “旧ID” .context/code/` 全库搜索，确保零残留

## 10. 入口与索引文档（必须）

### 10.1 `ENTRY.md`

必须包含：

1. 项目名
2. 项目根目录占位（`<project_root>`）
3. `<project_root>/<relative_path>` 路径拼接规则
4. 当前扫描范围
5. 状态说明（pending/partial/complete + draft/reviewed/approved）

### 10.2 目录结构

知识文档按业务关联度分层组织：

- `infrastructure/`：基础组件（与游戏业务无关，金融/互联网行业也有）
- `game_foundation/`：业务组件（MMO游戏相关，但玩家不直接感知）
- `business/`：具体业务（玩家直接感知的玩法功能）
- `concept/`：跨模块通用概念
- `experience/`：跨模块经验教训
- `generator_pack/`：知识库自身的生成规范与模板

每个模块一个文件夹，内含该模块的 module/flow/slice 文档。

### 10.3 `INDEX.md`

`INDEX.md` 必须说明索引后端与查询方式，不再强制承载全量静态映射大表。

必须包含：

- 索引后端位置（纯文本文件 `knowledge_index.txt`）
- 查询脚本与写入脚本入口
- 输出格式约定（如 `rel_path<TAB>keyword1 keyword2 ...`）
- 全量重建策略（`--rebuild`）

## 11. AI 按需加载策略

1. 选型问题：`Experience -> Concept -> Module(主) -> Module(子) -> Flow -> Slice`
2. 排障问题：`Flow -> Slice -> Module(子) -> Module(主) -> Concept -> Experience`
3. 新功能设计：`Module(主) -> Module(子) -> Flow -> Slice -> Experience -> Concept`

加载预算规则：

- 常规：6~10 篇
- 复杂：10~15 篇
- 超预算：先收敛到 1 个主模块 + 1~2 个子模块

## 12. 命名规则

统一规则：

- 主文档：`<name>.<category>.md`
- 子文档：`<name>.<subname>.<category>.md`
- front matter `id` = 文件名去掉 `.md`

适用类别示例：

- `<name>.module` / `<name>.<subname>.module`
- `<name>.<flow_name>.flow` / `<name>.<subname>.<flow_name>.flow`
- `<name>.<slice_name>.slice` / `<name>.<subname>.<slice_name>.slice`
- `concept.<name>`
- `experience.<name>` / `experience.<name>.<subname>`
- `adr.<name>` / `adr.<name>.<subname>`

补充约束：

- 名称必须是语义命名，不是源码目录命名
- `engine`、`server_common`、`game`、`mmo` 等目录词不应进入知识名，除非它本身就是稳定业务概念
- 代码归属与源码边界通过 `layer`、`domain`、`module_root`、`owners`、正文锚点来表达

## 13. Docs-as-Code 同步要求

1. 关键逻辑变更必须同步更新对应知识 MD
2. 允许先提交 `draft/partial`，后续迭代升到 `reviewed/approved`
3. 未同步知识文档视为不完整交付

## 14. 落地顺序

1. 主模块优先（人工定边界）
2. 子模块拆分（AI 填充）
3. P0 Flow/Slice 闭环
4. Experience + ADR 沉淀

## 15. 生成模板包（必须）

生成 agent 在创建 /更新`flow/slice/module/concept` 文档前，必须先读取：

1. `.context/code/generator_pack/README.md`
2. `.context/code/generator_pack/templates/template.flow.md`
3. `.context/code/generator_pack/templates/template.slice.md`
4. `.context/code/generator_pack/templates/template.module.md`
5. `.context/code/generator_pack/templates/template.concept.md`
6. `.context/code/generator_pack/demos/` 对应类型 demo

规则：

1. 不允许跳过模板直接生成。
2. 生成结果必须保持模板 front matter 字段完整。
3. demo 仅用于风格与结构参考，不得复制 demo 的 ID。

## 16. 关键词索引后端（纯文本 + PowerShell）

为避免索引依赖外部组件，关键词检索使用纯文本文件 + PowerShell 脚本：

1. 索引文件：`.context/code/index/knowledge_index.txt`（每行 `<路径>\t<关键词1>,<关键词2>,...`，bigram 如 `氛围 npc` 含空格前缀整体作为关键词）
2. 写入脚本（生成 agent）：`.context/code/generator_pack/kb_index_write.py`（Python，仅构建时需要）
3. 查询脚本（读取 agent）：`.context/code/generator_pack/kb_index_query.ps1`（PowerShell，零依赖）

约束：

1. 读取 agent 先分词后用 ps1 脚本查文本索引，脚本自动返回匹配目录的 entry.md 完整内容。
2. 生成 agent 文档变更后执行 `--rebuild` 重建索引（全量重建约 1-2 秒）。
3. 查询无需 Python，仅需 PowerShell（Windows 自带）。

关键词预算算法（v5）：每目录关键词数 = `max(min_keywords, dir_weighted_tokens × effective_ratio)`。
当 `token_ratio × 全库token总量` 超过上限时，自动反算 `effective_ratio`。
默认值：`min_keywords=30`, `max_total_keywords=200000`, `token_ratio=0.005`。
