# 策划数据表知识库生成规范 v2（Design Data Spec）

> 替代原三维(howtouse/howtocheck/howtowork)规范，采用三类文档(profile/pipeline/coderef)职责分离设计。

## 1. 定位与目标

为策划配置表生成三类文档，各司其职：

| 文档 | 代号 | 核心问题 | 主要受众 |
|------|------|----------|----------|
| 配置档案 | profile | 这列什么意思？怎么填？填了什么效果？影响谁？ | 策划/QA/程序 |
| 数据管线 | pipeline | Excel 里的值怎么变成运行时的值？ | 程序/QA |
| 代码引用 | coderef | 每一列在代码哪里读？怎么用？ | 程序 |

**适用表**：`dev/design/data/` 下所有在 `ddtconfig.lua` 中注册的策划表。

**不适用的内容**：
- 不替代现有 Module 文档中的"对外配置"章节——Module 说"我用了哪张表"，本规范说"这张表怎么填/转/用"
- 不展开通用流程的服务端链路细节（遵循 SPEC-business-module.md §3 通用 vs 专属分离原则）

**核心原则：代码为准，表头仅参考（强制）**

策划表 .txt 文件表头 Row1（注释行）中的列描述**不可信**——它可能未随代码更新，或描述模糊、不准确、已过时。生成知识库文档时：

1. **列含义以代码逻辑为准**：通过追踪代码消费点，总结出该列在运行时的实际用途，写入文档的"语义"字段
2. **表头仅作辅助参考**：表头描述可作为理解列名的线索，但不得直接复制为最终语义
3. **偏差必须标注**：当代码中的实际用途与表头描述不一致时，在列定义的"语义"字段后追加 `⚠️ 表头描述偏差` 标注，写明表头原文与代码实际行为的差异
4. **缺失/模糊必须标注**：当表头对某列无描述、或描述过于模糊无法理解时，在"语义"字段后追加 `⚠️ 表头描述缺失` 或 `⚠️ 表头描述模糊` 标注

标注格式：
```markdown
| 列名 | 类型 | C/S | 语义 | 约束 | 默认值 | 依赖 |
|------|------|-----|------|------|--------|------|
| XXX  | 数字 | cs  | 实际语义（由代码验证） ⚠️ 表头描述偏差：表头写"XXX用途"但代码实际用于YYY | ... | ... | ... |
| YYY  | 文字 | s   | 实际语义 ⚠️ 表头描述缺失：表头无注释 | ... | ... | ... |
```

## 2. 术语定义

| 术语 | 含义 | 示例 |
|------|------|------|
| **Book** | 策划数据 Excel 文件，即 ddtconfig 中的 doc.Name | `Buff`、`Gameplay`、`Item` |
| **SubBook** | 通过 SubBooks.lua 合并到同一 Book 的分表 Excel 文件 | `Buff_FuBen`、`Buff_ShuZhi`、`Buff_AI` |
| **Sheet** | Excel 中的 Sheet 页（Tab），转表后成为全局表 `BookName_SheetName` | `Buff`、`Appear`、`Control`、`Readme` |
| **SubSheet** | Sheet 内用 `@` 语法定义的子页，数据合并到主 Sheet | `AI@Buff`、`FB@Buff`、`任务@Message`、`庄园@Message` |

**层次关系**：

```
Book (Buff.xlsm)
├── SubBook: Buff_FuBen.xlsm, Buff_ShuZhi.xlsm, ...  (合并到同一 Book)
├── Sheet: Buff        → 全局表 Buff_Buff
│   ├── SubSheet: AI@Buff    (合并到 Buff_Buff)
│   └── SubSheet: FB@Buff    (合并到 Buff_Buff)
├── Sheet: Appear      → 全局表 Buff_Appear
├── Sheet: Control     → 全局表 Buff_Control
Message (Message.xlsm)
└── Sheet: Message      → 全局表 Message_Message
    ├── SubSheet: 任务@Message  (合并到 Message_Message)
    └── SubSheet: 庄园@Message  (合并到 Message_Message)
```

## 3. 文档拆分规则

### 3.1 以 Sheet 为列定义的原子边界

**核心规则：一个 Sheet 及其列定义必须是一个独立块，不同 Sheet 的列不能混合在同一个块中。**

列定义文档的拆分粒度：

| 场景 | 拆法 |
|------|------|
| 一个 Sheet，列数适中且功能统一 | 该 Sheet 整体为一个块 |
| 一个 Sheet，列数多且功能域清晰（如核心标识 vs UI 展示 vs 战斗配置） | 按**功能域**拆分为多个块，但每个块内仍只含该 Sheet 的列 |
| 多个简单小 Sheet（如 OffLineLast、NotRemove 各 2-3 列） | 可合并到一个文档，但**每个 Sheet 仍为独立块**，用二级标题分隔 |
| 复杂大 Sheet | 可在 Sheet 内按功能拆分列来聚合介绍 |

**示例**：
- `Gameplay` Book 的 `Gameplay` Sheet 200+ 列 → 按**功能域**拆：core / entry_check / scene_options / ui_display / combat_buff / statistics_season，每个块内仅含 Gameplay Sheet 的列
- `Buff` Book 的 `Buff` Sheet + `OverlappingBuff` SubSheet → 同属一个逻辑表，可合并在一个块中
- `Buff` Book 的 `Appear` Sheet → 独立一个块
- `Buff` Book 的 OffLineLast / NotRemove / ExclusiveBuffs 等小 Sheet → 合并到一个文档，但各 Sheet 仍为独立块

### 3.2 三类文档的拆分策略

| 文档类型 | 是否拆分 | 理由 |
|---------|---------|------|
| profile | 按 Sheet/功能域拆 | 策划按 Sheet 查表 |
| pipeline | 通常不拆 | 管线是整体的，拆开反而不连贯 |
| coderef | 可按 Sheet 拆（对应 profile） | 与 profile 对齐便于交叉引用 |

### 3.3 文件命名

```
<name>.designdata.md                    # profile 主文档（索引 + 概述）
<name>.<sheet_or_func>.designdata.md    # profile 子文档（按 Sheet 或功能域）
<name>.pipeline.designdata.md           # 数据管线文档
<name>.coderef.designdata.md            # 代码引用文档
<name>.<sheet_or_func>.coderef.designdata.md  # 代码引用子文档
```

- `<name>` = Book 名 snake_case（如 `buff`、`gameplay`、`item`），与子目录名对应
- `<sheet_or_func>` = Sheet 名小写（如 `appear`、`control`）或功能域名（如 `core`、`entry_check`）

### 3.4 SubBooks 合并表

多个 Excel 分表通过 `dev/design/data/Common/SubBooks.lua` 合并为同一逻辑全局表。SubBooks 合并表生成**一份**知识文档集，而非每个分表各一份。

**必须执行的操作**：

1. **搜集所有分表**：读取 `SubBooks.lua` 中 `_subbooks_name2books[BookName]`，获取所有分表名
2. **逐一读取每个分表的 .txt 文件**表头，提取列定义
3. **对比分表差异**：
   - 相同 Sheet 结构的分表：合并描述，标注"所有分表结构相同"
   - 不同 Sheet 结构的分表：按分表分节说明
   - 列定义有差异的：在列定义表中用注释标注差异来源分表
4. **ID 分区说明**：在 profile 主文档"表概述"中列出所有分表及 ID 分区范围（来自 `_subbooks_name2idparition`），在约束列添加跨分表 ID 唯一性约束

## 4. 目录归属

策划数据表文档作为独立的一级目录 `design_data/` 存放，每个 Excel 文档一个子目录，子目录名即 Excel 文件名（去掉 .xlsm 后缀）：

```
.context/code/design_data/
├── Gameplay/
│   ├── gameplay.designdata.md                       # profile 主文档
│   ├── gameplay.core.designdata.md                  # profile 子文档
│   ├── gameplay.entry_check.designdata.md           # profile 子文档
│   ├── gameplay.pipeline.designdata.md              # pipeline
│   └── gameplay.coderef.designdata.md               # coderef
├── Buff/
│   ├── buff.designdata.md                           # profile 主文档
│   ├── buff.appear.designdata.md                    # profile 子文档
│   ├── buff.control.designdata.md                   # profile 子文档
│   ├── buff.pipeline.designdata.md                  # pipeline
│   └── buff.coderef.designdata.md                   # coderef
├── Skill/
│   ├── skill.designdata.md                          # profile 主文档
│   └── skill.pipeline.designdata.md                 # pipeline
├── Item/
│   └── item.designdata.md
└── ...
```

## 5. profile 文档规范

### 5.1 主文档结构

```markdown
# <BookName> 策划数据表

> summary: ≤150字简介，说明该 Book 配置什么

## Book 概述
- **Book 名**：`<BookName>`（Excel 文件 `<BookName>.xlsm`）
- **所属目录**：`<ddtconfig 中的 Dir>`
- **用途**：<一句话说明这个 Book 配置什么>
- **填表时机**：<什么时候需要新增/修改行>
- **C/S 分发**：<仅客户端/仅服务端/双端>
- **ID 规则**：<ID 编码规则，如 BuffId = BuffCls*100+Level；无特殊规则写"自增">

## SubBooks

<若无 SubBooks，写"无 SubBooks，全部数据在主 Book 中">

| SubBook | 说明 | ID 分区 |
|---------|------|---------|
| <SubBook1> | <说明> | <ID范围> |
| <SubBook2> | <说明> | <ID范围> |

> ID 分区来自 SubBooks.lua 中 `_subbooks_name2idparition`。

## Sheet 总览

| 来源 | Sheet / SubSheet | 说明 | 子文档 |
|------|-----------------|------|--------|
| Book | Buff | 主 Buff 定义，核心效果与行为配置 | [buff](<name>.designdata.md) |
| Book | OverlappingBuff@Buff | 可叠加 Buff，数据合并到 Buff Sheet | 同上 |
| SubBook: Buff_AI | AI@Buff | AI 专用 Buff，合并到 Buff Sheet | 同上 |
| Book | Appear | Buff 客户端表现：图标、特效、显示位置、受击特效等 | [appear](<name>.appear.designdata.md) |
| Book | Control | 控制效果公式：眩晕/减速/浮空等的持续时间与命中率公式 | [control](<name>.control.designdata.md) |
| Book | OffLineLast | 下线后持续生效的 Buff 列表 | [misc](<name>.misc.designdata.md) |
| Book | NotRemove | 不可移除 Buff 黑名单 | 同上 |
| Book | ExclusiveBuffs | 互斥 Buff 组（组内 Buff 二选一） | 同上 |
| ... | ... | ... | ... |

> "来源"列标注该 Sheet/SubSheet 属于 Book 还是某个 SubBook。SubSheet 标注 `@` 前的部分。说明列需结合 Sheet 内容给出有意义的描述，不要只重复 Sheet 名。

## 联动与影响

### 被依赖（谁引用了我）

| 引用方 | 引用方列 | 本 Book 列 | 关系 | 说明 |
|--------|---------|-----------|------|------|
| Skill_Skill | BuffId | Buff.ID | 外键 | 技能挂 Buff |
| Gameplay_Gameplay | BuffsRemoveInGameplay | Buff.ID | 引用 | 副本移除 Buff 列表 |

### 我依赖（我引用了谁）

| 本 Book 列 | 被引用方 | 被引用列 | 关系 | 说明 |
|-----------|---------|---------|------|------|
| Buff.NewEffectList | Effect_Basic | ID | 引用 | 效果定义 |
| Control.CDBuff | Buff_Buff | ID | 外键 | 冷却 Buff |

**关系类型**：
- **外键**：本列值必须存在于目标表中
- **引用**：本列值应在目标表中存在，缺失时可能降级
- **反向**：目标表中引用了本表的 ID
- **配对**：本表与目标表必须成对配置

### 影响范围

<一句话总结：修改本表 ID/核心列会影响哪些系统的行为。如"Buff ID 变更影响技能系统、副本系统、装备词缀、称号系统的 Buff 挂载">

## 新增行操作指引

新增一个 <配置类型> 时，需按以下步骤操作：

1. 在本表/分表中新增一行，分配 ID（ID 区间：<XXXX-XXXX>）
2. 在 <关联表1> 中新增对应的 <配置项>（搜索关键字：`<keyword>`）
3. 在 <关联表2> 中注册 <配置项>（搜索关键字：`<keyword>`）
4. 配置 <其他必要步骤>
5. 验证：<游戏内验证步骤>
```

### 5.2 子文档结构

**核心规则：每个 Sheet 及其列定义必须是独立块，不同 Sheet 的列不能混合。**

一个子文档可以包含一个或多个 Sheet，但每个 Sheet 必须用二级标题分隔为独立块：

```markdown
# <BookName> <子文档标题>

> summary: ≤150字简介

## Sheet: <SheetName1>

### 列定义

| 列名 | 类型 | C/S | 语义 | 约束 | 默认值 | 依赖 |
|------|------|-----|------|------|--------|------|
| ID | 数字 | cs | ... | ... | - | - |
| ColA | 文字 | cs | ... | ... | ... | ... |

### 枚举值：<<枚举列名>>

| 值 | 含义 | 对应行为/Handler | 关联子表 |
|----|------|-----------------|---------|
| <值1> | <含义1> | <Handler/行为> | <子表> |

### 高价值行示例

**示例1：<场景描述>**

| 列 | 值 |
|----|-----|
| ID | <值> |
| <核心列1> | <值> |

**示例2：<场景描述>**

| 列 | 值 |
|----|-----|
| ... | ... |

---

## Sheet: <SheetName2>

### 列定义

| 列名 | 类型 | C/S | 语义 | 约束 | 默认值 | 依赖 |
|------|------|-----|------|------|--------|------|
| ... | ... | ... | ... | ... | ... | ... |

### 高价值行示例

...
```

**复杂 Sheet 的功能域拆分**：当一个 Sheet 列数多且功能域清晰时，可在 Sheet 块内部按功能域分组：

```markdown
## Sheet: Gameplay — 功能域拆分

### 核心标识

| 列名 | 类型 | C/S | 语义 | 约束 | 默认值 | 依赖 |
|------|------|-----|------|------|--------|------|
| ID | ... | ... | ... | ... | ... | ... |
| GameplayClass | ... | ... | ... | ... | ... | ... |
| GameplayMode | ... | ... | ... | ... | ... | ... |

### 进入条件

| 列名 | 类型 | C/S | 语义 | 约束 | 默认值 | 依赖 |
|------|------|-----|------|------|--------|------|
| OwnerCheckGrade | ... | ... | ... | ... | ... | ... |
| FollowerCheckGrade | ... | ... | ... | ... | ... | ... |

### 场景与副本选项

| 列名 | ... |
...
```

**字段说明**：
- **类型**：从 .txt 表头 Row3 提取（数字/文字/数组/KV扩展/混合集合/去括号Lua表）
- **C/S**：从 .txt 表头 Row5 提取（c/s/cs）
- **语义**：**以代码逻辑为准**，追踪代码消费点总结运行时实际用途。不直接复制表头 Row1。偏差加 `⚠️ 表头描述偏差：表头写"..."实际用于...`；缺失加 `⚠️ 表头描述缺失`；模糊加 `⚠️ 表头描述模糊`
- **约束**：合并了校验信息——必填/选填、取值范围、格式要求、外键引用。简练标记如 `必填`、`正整数`、`格式: Team;N,M;`、`外键→Scene_Map.ID`、`0/1`、`0=否 1=是`
- **默认值**：空值时运行时行为（如 `空=不生效`、`空=0`、`空=使用通用配置`）
- **依赖**：条件必填/互斥/联动（如 `当 GameplayMode=1 时必填`、`与 XX 列互斥`、`随 XX 列变化`）

> 枚举值表仅为取值为有限枚举的列生成。无枚举约束的列跳过。
>
> 高价值行示例：每个 Sheet 选 2-3 个代表性行，覆盖不同枚举值/配置场景。仅列出关键列，不需列全。这些行可作为填表模板和 QA 检查参照。
```

### 5.3 不拆分的 profile 文档

当表功能统一无需拆分时，主文档直接包含列定义、枚举值、高价值行示例，不再另建子文档。

## 6. pipeline 文档规范

```markdown
# <表名> 数据管线

> summary: ≤150字简介，从 Excel 到运行时的完整转换链路

## 三层管线总览

| 层级 | 产出 | 关键文件 | 说明 |
|------|------|----------|------|
| Layer 1: 转表 | `BookName_SheetName` 全局表 | `ddtconfig.lua` → `<Dir>/<BookName>.txt` | Excel → txt → Lua 全局表 |
| Layer 2: 扩展 | 派生字段/反向索引/_ext_表 | `*Ext.lua` / `*DynamicExt.lua` | 若无扩展写"无" |
| Layer 3: 运行时 | `Mgr.m_*` 成员 | `<Mgr路径>` → `<Load方法>` | Mgr 启动时加载 |

> 基础表是否回收：<是/否>。若回收，注明 `AfterDesignTablesLoaded.lua` 中条目，运行时仅 _ext_ 版本可访问。

## Layer 1: 转表

### ddtconfig 注册

- **doc.Name**：`<BookName>`
- **doc.Dir**：`<Dir>`
- **FormulaParsers**：<有/无，若有列出>
- **SubBooks**：<分表列表及 ID 分区；无则"无">

### 表头结构

- Sheet 名：`<Sheet名>`
- 列数：<N> 列
- C/S 标记分布：<cs 列 N 个, c 列 M 个, s 列 K 个>

### 类型解析要点

<仅列出本表特有的类型解析规则，通用规则引用 GUIDE-design-data-location.md §2.7>

> 若无特殊类型解析，写"无特殊类型解析规则"。

## Layer 2: 静态扩展

### 扩展产出

| 产出类型 | 产出名 | 来源文件 | 说明 |
|---------|--------|---------|------|
| 派生字段 | `v.__XXX` | `*Ext.lua` | 含义 |
| 反向索引 | `TableName` | `*Ext.lua` | `rawset(_G, ...)` 注册，用途 |
| DTE 处理列 | `<列名>` | `*DynamicExt.lua` | 被何种 DTE 处理器处理，处理后值变化 |

> 若无扩展，整节写"本表无 Layer 2 扩展"。

## Layer 3: 运行时转换

### Mgr 加载链

| 方法 | 消费表 | 产出 m_* 成员 | 关键转换逻辑 |
|------|--------|-------------|-------------|
| `_LoadXXX` | BookName_SheetName | `m_XXX` | 转换描述 |
| `_LoadYYY` | BookName_SheetName | `m_YYY` | 转换描述 |

### 值转换速查

| 列名 | Excel 原始值 | 运行时值 | 转换方式 | 转换位置 |
|------|-------------|---------|---------|---------|
| PlayerType | `Team;4,6;` | `{type=eTeam, min=4, max=6}` | DealPlayerType 解析 | `GameplayCommonInc.lua` |
| NewEffectList | `{EffectId=13090043,...}` | 编译后的函数列表 | loadstring 编译 | `BuffMgr._LoadEffects` |
| MonsterGradeType | `Player` | 字符串 `"Player"` | 直传 | `ParseDesignData` |

> 仅列出**运行时值 ≠ Excel 原值**的列。原值直传的列不列。

### 运行时数据流

#### 场景：<场景名称>

```
1. <步骤描述>
   → <端>: `<函数调用>`  [搜索: <关键字>]

2. <步骤描述>
   → <端>: `<函数调用>`  [搜索: <关键字>]

3. <步骤描述>
   → ...
```

> 选择 1-2 个核心场景，描述数据从表到最终消费的完整流转。每步必须有代码定位。
```

**设计要点**：
- 独立文档，不嵌在其他文档中
- **值转换速查**是核心——一目了然哪些列的运行时值和 Excel 填的值不同
- 不重复 GUIDE-design-data-location.md 中的通用机制（转表流程、SubBooks 合并、DTE 处理器、类型解析规则等），用引用代替

## 7. coderef 文档规范

```markdown
# <表名> 代码引用

> summary: ≤150字简介，每列的代码引用点与行为映射

## 代码消费点

### GAS（服务端）

| 文件 | 搜索关键字 | 消费方式 | 消费的列 |
|------|-----------|----------|---------|
| `gas/lua/xxx/XXX.lua` | `BookName_SheetName` | bddpairs 遍历 | 全部 |
| `gas/lua/xxx/YYY.lua` | `GetXXX(id)` | 按 ID 查表 | ID, ColA, ColB |

### GAC（客户端）

| 文件 | 搜索关键字 | 消费方式 | 消费的列 |
|------|-----------|----------|---------|
| ... | ... | ... | ... |

### Common（共享）

| 文件 | 搜索关键字 | 消费方式 | 消费的列 |
|------|-----------|----------|---------|
| ... | ... | ... | ... |

### Master（主控）

| 文件 | 搜索关键字 | 消费方式 | 消费的列 |
|------|-----------|----------|---------|
| ... | ... | ... | ... |

> 某端无消费点则写"无"。

## 列→代码引用点

| 列名 | 引用文件 | 引用方式 | 用途 |
|------|---------|---------|------|
| ID | `XXX.lua` → `funcA` | 按ID查表 | 唯一标识定位 |
| GameplayClass | `GameplayMgr.lua` → `RegisterGameplay` | 值→分发 | 决定走哪个Handler |
| PlayerType | `GameplayCommonInc.lua` → `DealPlayerType` | 字符串解析 | 提取人数限制 |
| MapId | `GameplayCommonInc.lua` → `DealCreateSceneIds` | 拆分→创建场景 | 分号分隔→多场景 |
| ... | ... | ... | ... |

> 每列一行，精简到"哪个文件哪个函数怎么用"。纯展示列（如描述文本、名称）可省略。

## 关键列→行为映射

| 列名 | 值 | 触发的代码行为 | 代码定位 |
|------|----|---------------|---------|
| GameplayClass | `SimplePlay` | 创建 CSimplePlay，走标准副本流程 | `gameplay/SimplePlay/SimplePlayMgr.lua` |
| GameplayClass | `RoguePlay` | 创建 CRoguePlay，走 Roguelike 流程 | `gameplay/RoguePlay/RoguePlayMgr.lua` |
| GoodOrBad | `-1` | 有害：红框+加仇恨+可驱散 | `BuffMgr.lua` → `_SelectBadBuffs` |
| CumulateTime | `-1` | 刷新Buff持续时间 | `BuffMgr.lua` → `_AddBuff` |
| CumulateTime | `0` | 新Buff替换老Buff | 同上 |
| ... | ... | ... | ... |

> 仅列**值影响代码分支**的映射。纯数据透传不列。每个映射必须有代码定位。

## 异常信号

| 日志关键字 | 可能的配置错误 | 归属列 | 排查方向 |
|-----------|---------------|--------|---------|
| `Can't find gameplay handler for class XXX` | GameplayClass 不在代码注册表 | GameplayClass | 对照枚举值表检查 |
| `Map not found: XXXX` | MapId 引用不存在的场景 | MapId | 检查 Scene_Map 表 |

> 从代码校验逻辑、日志关键字、历史 bug 报告中提取。若暂无已知模式，写"暂无已知异常信号"。
```

**设计要点**：
- 独立文档，不嵌在其他文档中
- **"列→代码引用点"表**是核心——每列一行精确定位，v1 缺失的信息
- 消费点表按端分组，每端列出代表性消费点
- 不重复 profile 中已有的枚举值表
- 不重复 pipeline 中已有的值转换细节

## 8. 生成前必读（强制）

### 7.1 规范文档（必须全部加载）

1. `.context/code/generator_pack/SPEC-write.md` — 通用写作规范
2. `.context/code/generator_pack/SPEC-design-data.md` — 本规范（你正在读的）
3. `.context/code/generator_pack/GUIDE-design-data-location.md` — 策划配置定位指引
4. `.context/code/generator_pack/templates/template.designdata.md` — 策划数据表模板

**必须在开始生成前输出本次加载的规范文档清单，确认无遗漏后再继续。**

### 7.2 必须读取的源文件

生成**每张表**的文档前，必须先读取以下文件：

| 序号 | 文件 | 目的 |
|------|------|------|
| 1 | `dev/design/data/ddtconfig.lua` | 确认该表的注册信息（doc.Name、doc.Dir、FormulaParsers） |
| 2 | `dev/design/data/<Dir>/<BookName>.txt` | 读取表头（前5行：列名、类型、校验规则、C/S标记）+ 抽样数据行 |
| 3 | `dev/design/data/Common/SubBooks.lua` | 检查该表是否为 SubBooks 合并表 |
| 4 | `dev/design/data/Common/DesignTableList.lua` | 检查该表是否有 Ext/DynamicExt 扩展 |
| 5 | `dev/design/data/Common/*Ext.lua`（若存在） | 读取扩展逻辑，了解派生字段和反向索引 |
| 6 | `dev/design/data/Common/AfterDesignTablesLoaded.lua` | 检查该表是否在运行时被回收 |

### 7.3 必须执行的代码搜索

生成文档前，**必须**在代码库中搜索该表的消费点：

```bash
# 搜索全局表名在代码中的引用
grep -r "BookName_SheetName" program/game/gac/lua/ program/game/gas/lua/ program/game/common/lua/ program/game/master/lua/ --include="*.lua" -l

# 搜索 bddpairs 遍历
grep -r "bddpairs(BookName_SheetName)" program/game/ --include="*.lua" -l

# 搜索运行时 _Load 方法（定位 Mgr 的数据加载）
grep -r "BookName_SheetName" program/game/gas/lua/ --include="*.lua" -n | grep -i "load"

# 搜索列名引用（逐列，用于 coderef 的"列→代码引用点"）
grep -r "\.ColumnName\b" program/game/ --include="*.lua" -n
```

## 9. 生成前追踪步骤（强制）

生成文档前，**必须**沿以下路径逐层追踪：

### 8.1 Layer 1：Excel 转表层

1. 从 ddtconfig.lua 确认该表的 BookName、Dir、CodeFileName
2. 读取 .txt 文件表头（前5行），提取所有列名、类型、校验规则、C/S标记
3. 确认是否有 SubBooks 合并（SubBooks.lua），若有则列出合并的子文件
4. 确认多级索引结构（ID/ID2/ID3 列）
5. **列语义代码验证（强制）**：对每个列，在代码中搜索该列名的引用，确认其运行时实际用途。将代码验证后的语义与表头 Row1 注释对比，标注偏差/缺失/模糊

### 8.2 Layer 2：静态扩展层

1. 检查 DesignTableList.lua，确认是否有 Ext/DynamicExt
2. 若有 Ext 文件，读取并提取：
   - 新增的派生字段（`v.__XXX` 或 `v.XXX`）
   - 反向索引表（`rawset(_G, ...)` 注册的全局表）
   - DTE 列处理器（CP_LvFormula 等）作用了哪些列
3. 确认基础表是否被回收（AfterDesignTablesLoaded.lua）

### 8.3 Layer 3：运行时转换层

1. 搜索该表名在 GAC/GAS/Master 代码中的引用
2. 定位 Mgr 类的 `_Load*` / `Load` / `StartUp` 方法
3. 提取关键转换逻辑：
   - 字符串拆分（分号/逗号分隔 → 数组）
   - 代码编译（`loadstring()` 编译公式）
   - 跨表连接（哪些表的数据与本表关联）
   - 键翻转/重索引（运行时索引结构与原始表不同）
   - OO 包装（行数据封装为类实例）
4. 记录运行时数据结构（`m_*` 成员名 + 含义）

### 8.4 列引用追踪

对表中每个列，搜索代码中对该列名的引用：
1. 确定哪些文件/函数读取了该列
2. 确定读取方式（按ID查表、bddpairs遍历、公式调用等）
3. 确定该列值是否影响代码分支/行为
4. 填入 coderef 的"列→代码引用点"表

### 8.5 联动追踪

1. 从代码中提取跨表连接点（Mgr._Load* 中的跨表 join）
2. 从 Ext.lua 中提取反向索引（谁引用了本表的 ID）
3. 从 .txt 表头 Row4 校验规则中提取外键引用
4. 填入 profile 的"联动与影响"

### 8.6 示例选取

1. 从 .txt 数据行中选取高价值行（代表性 ID、覆盖不同枚举值/配置场景）
2. 每个功能块选 2-3 个
3. 仅列出关键列的值

### 8.7 关键列→代码入口追踪

若表中某列值决定不同的代码行为（如 GameplayClass 决定走哪个 Handler），需额外追踪：

1. **列→代码映射**：该列的每个值对应的代码入口（Handler 类/Mgr 方法/分支逻辑）
2. **列→子表映射**：该列每个值关联的子配置表

## 10. 生成顺序（强制）

1. **先追踪，后写文档** — 沿 §8.1~8.6 路径完整追踪一遍
2. **先查重，后引用** — 识别通用机制，查知识库是否已有，已有则引用
3. **先 profile，后 pipeline/coderef** — 列语义是管线和代码引用的基础
4. **先核心列，后边缘列** — 影响代码行为的列优先文档化，纯数据列后补
5. **先主文档，后子文档** — 大表先写主文档概述，再拆分子文档
6. **缺失内容追踪** — 引用的通用流程在知识库中不存在时，按 SPEC-write.md §3.1 追加至 `.context/TODO-knowledge-gaps.md`

## 11. 通用 vs 专属分离原则

遵循 SPEC-business-module.md §3 的精神，策划数据表文档也必须区分"通用机制"和"本表专属内容"：

| 类别 | 定义 | 写法 |
|------|------|------|
| 通用机制 | 多张表共用的转表/加载/校验机制 | 不展开细节，用搜索关键字指向已有知识库文档 |
| 本表专属 | 本表特有的列、转换规则、代码映射 | 完整文档化 |

**通用机制引用格式**：

```markdown
- 转表流程：参见 `concept.design_data_pipeline`（搜索关键字 `ddtconfig`）
- SubBooks 合并机制：参见 `GUIDE-design-data-location.md` §2.4
- DTE LvFormula 处理器：参见 `GUIDE-design-data-location.md` §3.3
- 类型解析规则：参见 `GUIDE-design-data-location.md` §2.7
```

**判断标准**：如果一个机制换一张表后仍然成立，那就是通用机制，不应在本表文档中展开。

## 12. 与现有 Module 文档的关系

### 互不替代

- **Module 文档**回答"这个代码模块怎么用/配/改"——从代码视角出发
- **designdata 文档**回答"这张策划表怎么填/转/用"——从数据视角出发

### 交叉引用

- Module 文档的"对外配置"章节引用策划表时，写 `BookName_SheetName`，不加详细列说明
- designdata 文档的 coderef 引用代码模块时，写模块名 + 搜索关键字，不展开代码架构
- 两类文档通过 BookName_SheetName 和代码文件路径自然关联

### 避免重复

- 策划表的列定义和约束**只写在 profile 文档**中，Module 文档不再重复
- 代码的消费方式和数据流**只写在 coderef 和 pipeline 文档**中，Module 文档的"对外配置"仅列出表名

## 13. 质量门槛 DoD

### profile 文档

- [ ] 列语义来自代码验证，非表头复制
- [ ] 表头偏差/缺失/模糊已标注 ⚠️
- [ ] 枚举列有完整枚举值表
- [ ] 每个功能块有 2-3 个高价值行示例
- [ ] 联动与影响覆盖双向（被依赖 + 我依赖）
- [ ] 影响范围有一句话总览
- [ ] 新增行指引列出了所有需同步修改的关联表
- [ ] 列定义表的约束列包含了校验信息，无需单独的校验规则表

### pipeline 文档

- [ ] 三层管线总览完整
- [ ] 值转换速查覆盖了所有"运行时值 ≠ Excel原值"的列
- [ ] 运行时数据流覆盖至少 1 个核心场景
- [ ] 基础表回收状态已注明
- [ ] Mgr 加载链列出了关键转换方法及产出

### coderef 文档

- [ ] 代码消费点覆盖 GAC/GAS/Common/Master 四端
- [ ] 每列有代码引用点（纯展示列可省略）
- [ ] 关键列→行为映射仅列影响代码分支的列，每个映射有代码定位
- [ ] 异常信号表列出了关键日志关键字

### 通用

- [ ] 通用机制用引用而非展开
- [ ] 三类文档间无重复内容（profile 含约束校验，pipeline 不重复，coderef 不重复枚举）
- [ ] 引用的知识库文档确实存在，不存在的已追加至 TODO-knowledge-gaps.md
- [ ] summary ≤150 字且为完整句子

## 14. 生成后自检清单

### 配置档案维度

- [ ] 一个策划人员读完 profile 后，能否知道每列填什么、怎么填、填错了会怎样？
- [ ] 列语义是否来自代码验证，而非直接复制表头注释？
- [ ] 表头描述偏差是否已明确标注？
- [ ] 新增一行时，需要同步修改哪些表是否已指出？
- [ ] 是否提供了可直接参考的高价值行示例？
- [ ] 联动与影响是否覆盖了双向关系？

### 数据管线维度

- [ ] 数据从 Excel 到运行时的完整路径是否可追溯？
- [ ] 是否有 Layer 2/3 的转换会导致"策划表填的值 ≠ 运行时实际值"？是否在值转换速查中标出？
- [ ] 基础表是否被回收？回收后运行时访问的是哪个版本？

### 代码引用维度

- [ ] 一个开发人员读完 coderef 后，能否知道这张表每列的数据在代码里怎么被使用？
- [ ] 关键列填不同的值，代码行为有什么不同是否已说明？
- [ ] 遇到运行时报错时，能否从异常信号表定位到归属列？

### 完整性维度

- [ ] 三类文档（profile/pipeline/coderef）是否都已覆盖？
- [ ] 列定义表是否包含了表头中的所有列？
- [ ] 代码消费点是否搜索了 GAC/GAS/Common/Master 四端？
- [ ] 功能块拆分是否按业务功能而非列数？
- [ ] 是否对每个新增/更新文档执行了 `--upsert`？（entry.md 由脚本自动刷新，无需 agent 手动生成或验证）

若上述任何一项回答为否，文档尚未达标。
