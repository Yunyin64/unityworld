
本文件用途：这是 CLI 工具的使用说明文档，供开发者/AI 参考如何通过命令行操作游戏数据。
修改本文件 = 修改文档内容（如增补示例、修正格式），不等于执行实际数据操作。
如需实际创建/修改游戏数据，请直接使用 CLI 命令或对应的 JSON 数据文件。

# DataManager CLI Guide

本工具是游戏数据的统一管理入口。所有游戏配置（卡牌、怪物、物品等）都是工作区内的 JSON 文件，通过 Named Pipe 与 DataManager 交互，本 CLI 直接增删改查。要游戏里查数据、改数据，就用这些命令。

所有数据文件为 JSON 数组，条目以 `ID` 字段为主键。

## 前提条件

- **DataManager GUI 必须已启动**，且已打开一个工作区（文件夹）。CLI 不是独立程序，它连接到正在运行的 DataManager 进程。
- `common-cli.exe` 已加入系统 PATH，可直接调用。
- **无需 cd 到任何目录**：DataManager 启动时会自动将 `common-cli.exe` 所在目录注册到用户 PATH。新开的终端里任意位置直接输入命令即可。

## 调用格式

```
common-cli.exe DataManager auto <command> [args...]
```

- `DataManager` — 工具名（固定）
- `auto` — 自动发现当前活动的工作区。如只有一个工作区则自动连接；多个时会提示选择
- 也可指定工作区 ID：`common-cli.exe DataManager <WorkspaceId> <command> [args...]`

### 示例

```bash
common-cli.exe DataManager auto status
common-cli.exe DataManager auto list-files
common-cli.exe DataManager auto query FaBao --ID XXX
```

### 连接失败排查

| 错误信息 | 原因 |
|----------|------|
| `Failed to connect to tool` | DataManager 未启动或未加载工作区 |
| `'common-cli.exe' is not recognized` | common-cli.exe 未在 PATH 中，重启终端或检查部署 |
| `没有活动的工作区` | DataManager 已启动但未打开任何文件夹 |
| `存在多个工作区` | 多个实例运行中，需指定具体 WorkspaceId 而非 `auto` |

## 命令列表

### status
工作区状态。
```
status
```

### list-files
列出工作区所有 JSON 文件（精简格式）。
```
list-files
```
返回示例：
```json
{"workspace":"F:\\Openclaw\\UnityWorld\\Data\\Card","count":10,"files":["FaBao","FormBase","GongFaBase","HuoCardBase","Item_Monster","JinCardBase","MuCardBase","ShuiCardBase","TuCardBase","Wound"]}
```
有未保存文件时额外返回 `"dirty":["FaBao","FormBase"]`。

### get
获取文件内容。支持用逗号 `,` 分隔一次获取多个文件。
```
get <file>
get <file1>,<file2>,<file3>
```
单文件返回原始 JSON 内容；多文件返回 `{ "文件名": 内容, ... }` 结构。

示例：
```bash
get FaBao
get FaBao,FormBase,GongFaBase
```

### query
按条件查询。
```
query <file> --ID <ID>          # 按 ID 查一条
query <file> --path <jsonpath>  # 按 JSONPath 查
query <file>                    # 返回全部
```

### add
创建新条目（必须提供 ID 和 DisplayName）。
```
add <file> --ID <ID> --DisplayName <显示名>
```
示例：
```
add FaBao --ID XXX --DisplayName "XXX"
```

### update
按 ID 定位条目，修改属性。有两种用法：

**批量设置多个字段（推荐）**：省略 `--path`，`--value` 传 JSON 对象，一次 merge 多个属性：
```
update <file> --ID <ID> --value '{"字段1":"值1","字段2":123,"字段3":["a","b"]}'
```

**设置单个字段**：用 `--path` 指定属性路径（支持点分嵌套如 `stats.atk`）：
```
update <file> --ID <ID> --path <prop> --value <json>
```

示例：
```bash
# 推荐：一次设置多个属性
update FaBao --ID XXX --value '{"Desc":"XXX","Rarity":0,"Size":1,"Cooldown":60,"Keywords":["FaBao"]}'

# 单个属性
update FaBao --ID XXX --path "Cost" --value '3'
```

### delete
按 ID 删除条目。
```
delete <file> --ID <ID>
```

### set
按全局 JSONPath 修改已有节点（不按 ID 定位）。
```
set <file> --path <jsonpath> --value <json>
```

### batch-add
批量追加多条条目（每条必须含 ID 和 DisplayName）。
```
batch-add <file> --value '[{"ID":"card_a","DisplayName":"卡A"},{"ID":"card_b","DisplayName":"卡B"}]'
```

### save
保存修改到磁盘。
```
save <file>   # 保存指定文件
save          # 保存所有已修改文件
```

## 典型工作流

```bash
# 1. 先看工作区有哪些文件
list-files

# 2. 查看目标文件现有数据（了解字段结构）
get FaBao

# 3. 新增条目
add FaBao --ID XXX --DisplayName "XXX"

# 4. 一次性设置所有属性（推荐，比逐条 update 高效）
update FaBao --ID XXX --value '{"Desc":"XXX","Rarity":0,"Size":1,"Cooldown":60,"Keywords":["FaBao"]}'

# 5. 保存到磁盘
save FaBao
```

## 注意事项

- **选项大小写敏感**：用 `--ID` 不是 `--id`，用 `--DisplayName` 不是 `--displayname`
- `add` 必须同时提供 `--ID` 和 `--DisplayName`
- 文件名可省略 `.json` 后缀
- `--value` 中的 JSON 字符串需要引号包裹：`'"文本"'`
- 所有修改在内存中进行，需 `save` 才写入磁盘
- `update --path` 是相对条目的属性路径，不是 JSONPath
- `set --path` 是全局 JSONPath（如 `$[0].name`）
