# 策划配置定位指引（Design Data Location Guide）

> 本文档指导 agent/开发者通过"表名+Sheet名+列名"定位策划配置项，
> 涵盖从 Excel 源表到运行时数据的完整路径，确保不遗漏任何转换层。

## 1. 三层模型总览

策划配置从 Excel 到游戏运行时经过三层处理，每一层都可能改变数据的结构、字段或访问方式：

```
Layer 1: Excel → typeparses 转表 → Lua 全局表（BookName_SheetName）
Layer 2: Common/*Ext.lua 静态扩展 → 派生字段/反向索引/公式展开（*_ext_* 表）
Layer 3: Mgr 启动时动态转换 → 运行时数据结构（Mgr.m_* 成员）
```

定位配置时**必须逐层检查**：代码实际读取的可能是任意一层的产出。

## 2. Layer 1：Excel 转表层

### 2.1 核心映射规则


| 输入                                            | 输出                           | 访问模式                            |
| ----------------------------------------------- | ------------------------------ | ----------------------------------- |
| Excel 文件名（BookName）+ Sheet 名（SheetName） | Lua 全局表`BookName_SheetName` | `BookName_SheetName[id].ColumnName` |

示例：

- Excel `FestivalGame.xlsx` 的 Sheet `Festival` → `FestivalGame_Festival[314].BeginTime`
- Excel `Buff.xlsx` 的 Sheet `Buff` → `Buff_Buff[1001].DynamicValue`

### 2.2 多级索引

若 Sheet 中有 `ID`、`ID2`、`ID3` 列，则生成多级索引：

```lua
BookName_SheetName[primaryId]              -- 只有 ID
BookName_SheetName[primaryId][secondaryId]  -- 有 ID + ID2
```

### 2.3 Sheet 名处理规则


| Sheet 名模式          | 处理                                              | 示例                      |
| --------------------- | ------------------------------------------------- | ------------------------- |
| 普通名称              | 直接用作表名后缀                                  | `Sound` → `Audio_Sound`  |
| `SubPage@MainSheet`   | `@` 后的部分为逻辑 Sheet 名，数据合并到 MainSheet | `Ori@Task` → `Task_Task` |
| `SubPage@MainSheet#2` | `#` 后为页码，多页合并到同一表                    | —                        |
| `~` 开头              | 整个 Sheet 被忽略，不转表                         | `~Debug` → 不输出        |

### 2.4 SubBooks 合并

多个 Excel 文件可合并为同一逻辑表，由 `dev/design/data/Common/SubBooks.lua` 定义：

- 例如 `Item` 合并 8 个子文件（`Item_Ori`、`Item_Life`、`Item_EquipUnit` 等）→ 运行时统一为 `Item_Item`
- 合并规则：按 ID 去重，不允许跨文件 ID 冲突
- ID 分区校验：`_subbooks_name2idparition` 定义每个子文件的 ID 范围

**定位时注意**：如果代码中访问的表名（如 `Task_Task`）在 ddtconfig 中找不到对应 doc.Name，
说明它是 SubBooks 合并产出，需要查 SubBooks.lua 找到实际的子文件（如 `Task_Ori`、`Task_QiYu`）。

### 2.5 ddtconfig.lua 注册表

所有策划表的转表配置在 `dev/design/data/ddtconfig.lua`，每个 doc 的关键字段：


| 字段                 | 含义                                               |
| -------------------- | -------------------------------------------------- |
| `doc.Name`           | Excel 文件名 / 全局表前缀                          |
| `doc.Dir`            | 输出子目录（如`GamePlay`、`Character`、`Task`）    |
| `doc.FormulaParsers` | 公式解析器配置（FastActionParser、自定义 Parser）  |
| `doc.CodeFileName`   | 输出文件名覆盖（若设置，替代 Name 作为输出文件名） |

### 2.6 Excel 表头结构

每个 Sheet 的前 5 行为表头，第 6 行起为数据：


| 行号  | 内容         | 说明                                                                       |
| ----- | ------------ | -------------------------------------------------------------------------- |
| Row 1 | 注释         | 不参与转表                                                                 |
| Row 2 | **列名**     | 成为 Lua 字段键                                                            |
| Row 3 | **类型声明** | 如`数字`、`文字`、`数组`、`KV扩展`（逗号分隔，首段为类型名，后续为修饰符） |
| Row 4 | 校验规则     | 正则或校验表达式                                                           |
| Row 5 | **C/S 标记** | `c`=仅客户端、`s`=仅服务端、`cs`=双端；无标记的列不转表                    |

### 2.7 列类型速查

常用类型解析器（定义于 `dev/tools/designdata_tools/luascripts/TypeParsers/`）：


| 中文类型名    | 输出类别 | 示例输入       | Lua 输出                  |
| ------------- | -------- | -------------- | ------------------------- |
| `数字`        | Num      | `42`           | `42`                      |
| `文字`        | Text     | `hello`        | `[=[hello]=]`             |
| `数组`        | Text     | `1;2;3`        | `{1,2,3}`                 |
| `混合集合`    | Text     | `1,abc`        | `{[1]=true,["abc"]=true}` |
| `KV扩展`      | Text     | `1:10,20|2:30` | `{[1]={10,20},[2]={30}}`  |
| `去括号Lua表` | Text     | `1,2,K=1`      | `{1,2,K=1}`               |

## 3. Layer 2：静态扩展层（dev/design/data/common/*Ext.lua）

### 3.1 扩展产出类型


| 产出类型   | 命名规则                   | 说明                               | 示例                                             |
| ---------- | -------------------------- | ---------------------------------- | ------------------------------------------------ |
| DTE 扩展表 | `<Module>_ext_<SheetName>` | DTE 框架处理后的完整表副本         | `Task_ext_Task`、`Npc_ext_Npc`                   |
| 派生字段   | 原表行上新增字段           | 在已有 row 对象上添加计算属性      | `v.__Module`、`v.ParentCardID`                   |
| 反向索引表 | `<Module>_<DerivedName>`   | 通过`rawset(_G, ...)` 注册的全局表 | `MainBuffId2SubBuffIds`、`Skill_SkillChange_Rev` |

### 3.2 基础表可能被回收

部分基础表在扩展完成后被设为 `nil`，**只有 `_ext_` 版本在运行时存活**：

```lua
-- AfterDesignTablesLoaded.lua 中
Task_Task = nil
Npc_Npc = nil
```

**定位时注意**：如果代码中访问 `Task_ext_Task` 而非 `Task_Task`，说明该表已被扩展替换。
反之如果代码访问的是基础表名，需确认该表未被回收。

### 3.3 DTE 列处理器

DTE 框架（`dev/design/data/Common/CommonDynamicExt.lua`）提供列级处理器：


| 处理器                               | 功能                                      | 适用场景      |
| ------------------------------------ | ----------------------------------------- | ------------- |
| `DTE.CP_LvFormula`                   | 将含`Lv` 占位符的公式字符串逐级展开为数值 | 等级相关属性  |
| `DTE.CP_LvSubstitute`                | 将`Lv` 替换为具体等级数字                 | 等级文本      |
| `DTE.CP_LvSum`                       | 累加前 N 级数值                           | 累计经验/属性 |
| `DTE.CP_FunctionParseOrLvSubstitute` | 先尝试公式解析，失败则等级替换            | 混合公式列    |
| `DTE.CP_Task_ReplaceLink`            | 解析`<link npc=...>` 为富文本链接         | 任务描述      |
| `DTE.CP_Npc_Location`                | 跨表关联 NPC 与地图坐标                   | NPC 定位      |
| `DTE.CP_Npc_TaskList`                | 跨表关联 NPC 与任务列表                   | NPC 任务      |

### 3.4 如何判断表是否有扩展

三个信号：

1. **DesignTableList.lua**：列出了所有 Ext/DynamicExt 文件及其加载目标
2. **命名规则**：运行时存在 `<Module>_ext_<Sheet>` 全局表 或 `<TABLENAME>_DYNAMIC_EXT_LOADED` 标记
3. **require 链**：Ext 文件 `require` 基础表后执行后处理逻辑

## 4. Layer 3：运行时动态转换层

### 4.1 转换模式

Mgr 类在 `StartUp` 时调用 `_Load*` / `Load` 方法，读取策划表并转换为运行时数据结构：


| 转换模式          | 说明                                      | 典型示例                                                                              |
| ----------------- | ----------------------------------------- | ------------------------------------------------------------------------------------- |
| **字符串拆分**    | 分号/逗号分隔字符串 → 数组               | `LiveSkill_Category.ActiveSkill` → `self.m_Type2ActiveAddSkill`                      |
| **代码编译**      | `loadstring()` 编译公式字符串为可执行函数 | `Buff_Buff.DynamicValue` → `self.m_BuffId2Effects`                                   |
| **跨表连接**      | 一个表的数据关联另一个表                  | `Gameplay_Gameplay` + `TeamMatch_Category` → `self.m_GameplayTemplateId2DesignData`  |
| **键翻转/重索引** | 按不同维度重建索引                        | `EquipmentTemplate_Equip` → `self.m_EquipColorGradeTbl[type][subtype][color][grade]` |
| **多轮依赖**      | 前一轮结果作为后一轮输入                  | `m_BuffId2Effects` → 被 `_LoadControlBuff` 和 `_LoadTransformBuffs` 消费             |
| **OO 包装**       | 行数据封装为类实例                        | `CGameplayDesignData:ParseDesignData(row)`                                            |

### 4.2 定位运行时转换逻辑

从 Mgr 类名出发，搜索模式：

```
<MgrClassName>:_Load*      -- 查找所有 _Load 方法
<MgrClassName>:StartUp     -- 查找启动入口，内部调用各 _Load
bddpairs(<TableName>)      -- 查找遍历策划表的代码
```

常见 Mgr 与策划表的关系：


| Mgr 类            | 文件位置                             | 读取的策划表                                                   | 核心转换方法                                        |
| ----------------- | ------------------------------------ | -------------------------------------------------------------- | --------------------------------------------------- |
| `CBuffMgr`        | `gas/lua/fight/BuffMgr.lua`          | `Buff_Buff`, `Buff_Control`, `Effect_Basic`                    | `_LoadEffects`, `_LoadCategory`, `_LoadControlBuff` |
| `CLiveSkillMgr`   | `gas/lua/liveskill/LiveSkillMgr.lua` | `LiveSkill_Category`, `LiveSkill_Level`, `LiveSkill_Gashapon*` | `_LoadDesignData`, `_LoadDesignData_Gashapon`       |
| `CGameplayMgr`    | `gas/lua/gameplay/GameplayMgr.lua`   | `Gameplay_Gameplay`, `TeamMatch_Category`                      | `_LoadDesignData`                                   |
| `CEquipHelperMgr` | `gas/lua/item/EquipHelperMgr.lua`    | `EquipmentTemplate_Equip`, `ItemDrop_PlayerBestDrop`           | `LoadEquipColorTCTbl`, `LoadEquipAction`            |

## 5. 定位操作指南（从表名+Sheet名+列名到具体值）

### 5.1 快速定位流程

```
输入：表名 + Sheet名 + 列名（如 FestivalGame + Festival + BeginTime）

Step 1: 确认 Layer 1 基础表
  → 全局表名 = BookName_SheetName（如 FestivalGame_Festival）
  → 访问模式 = FestivalGame_Festival[id].BeginTime
  → 验证：在代码中搜索 "FestivalGame_Festival" 确认表存在

Step 2: 检查 Layer 2 是否有扩展
  → 搜索 "<Module>Ext.lua" 或 "<Module>DynamicExt.lua"（如 FestivalGameDyncExt.lua）
  → 搜索 "<Module>_ext_<Sheet>" 全局表（如 FestivalGame_ext_Festival）
  → 搜索 "<TABLENAME>_DYNAMIC_EXT_LOADED" 标记
  → 如果存在扩展：
     - 确认目标列是否被 DTE 处理器修改
     - 确认基础表是否被回收（AfterDesignTablesLoaded.lua）

Step 3: 检查 Layer 3 是否有运行时转换
  → 搜索 "BookName_SheetName" 在 gac/lua/ 和 gas/lua/ 中的引用
  → 定位到 Mgr 类的 _Load* 方法
  → 确认目标列是否参与转换、转换后存储在哪个 m_* 成员中
  → 如果代码实际读取的是 m_* 成员而非策划表，则运行时值 ≠ 策划表原值

输出：该配置项在各层的实际访问路径
```

### 5.2 配置引用标准格式

知识库文档中引用策划配置时，使用以下格式：

**简单引用**（未经过 Layer 2/3 转换）：

```
FestivalGame_Festival[314].BeginTime
```

**涉及扩展**（Layer 2 转换后）：

```
Task_ext_Task[taskId].PostTasks（原始列 PostTasks 经 TaskCommonExt 扩展）
```

**涉及运行时转换**（Layer 3 转换后）：

```
Buff_Buff[buffCls].DynamicValue → CBuffMgr._LoadEffects → m_BuffId2Effects[buffId]
（原始公式经 loadstring 编译为可执行函数，按 cls*100+level 重索引）
```

### 5.3 常见陷阱


| 陷阱                            | 说明                                                              | 规避方法                                        |
| ------------------------------- | ----------------------------------------------------------------- | ----------------------------------------------- |
| **访问了已回收的基础表**        | 如`Task_Task` 在运行时为 nil                                      | 确认 AfterDesignTablesLoaded.lua 是否回收了该表 |
| **SubBooks 合并后的表名不匹配** | 代码访问`Task_Task`，但 ddtconfig 中是 `Task_Ori`、`Task_QiYu` 等 | 查 SubBooks.lua 找合并映射                      |
| **Layer 3 重索引后 ID 变了**    | 如`Buff_Buff[buffCls]` 变为 `m_BuffId2Effects[buffCls*100+level]` | 追踪 _Load* 方法的键变换逻辑                    |
| **DTE 展开后列值类型变了**      | 如`LvFormula` 将字符串展开为每级数值数组                          | 检查 DTE 列处理器是否作用于该列                 |
| **运行时值 ≠ 策划表原值**      | Mgr 对原始值做了计算/编译/连接                                    | 确认代码读取的是策划表还是 m_* 成员             |

## 6. 关键文件速查


| 用途              | 路径                                                       |
| ----------------- | ---------------------------------------------------------- |
| 转表注册表        | `dev/design/data/ddtconfig.lua`                            |
| SubBooks 合并定义 | `dev/design/data/Common/SubBooks.lua`                      |
| 扩展文件目录      | `dev/design/data/Common/*Ext.lua`、`*DynamicExt.lua`       |
| DTE 框架          | `dev/design/data/Common/CommonDynamicExt.lua`              |
| 扩展注册表        | `dev/design/data/Common/DesignTableList.lua`               |
| 表回收逻辑        | `dev/design/data/Common/AfterDesignTablesLoaded.lua`       |
| GAC 回收逻辑      | `dev/design/data/Common/AfterGacDesignTablesLoaded.lua`    |
| 类型解析器        | `dev/tools/designdata_tools/luascripts/TypeParsers/`       |
| 转表引擎          | `dev/tools/designdata_tools/luascripts/ExcelText2Lua.lua`  |
| 新版类型系统      | `dev/tools/designdata_tools/luascripts/NewTypeParsers.lua` |
