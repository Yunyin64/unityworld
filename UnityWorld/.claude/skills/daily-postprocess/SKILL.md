---
name: daily-postprocess
description: |
  每日后处理：把新改动快速同步进工程。依次执行 JSON 字段修复、卡牌总览生成、文档结构检查、Skill 影响面检查四个步骤，每步自动执行并汇报结果。
  当用户说"跑后处理"、"同步一下"、"日常检查"、"postprocess"、"收尾"、"每日任务"时使用此技能。
  即使用户只是在一轮大改动结束后说"整理一下"，也应建议使用此技能。
---

# 每日后处理

四步串行执行，每步跑完直接汇报结果，全部跑完给一个总结。

---

## Step 1：JSON 字段修复

运行脚本，自动修复 Define 的 JsonPropertyName 和 JSON 文件的 key 名不一致问题。

```bash
cd <项目根目录>
python tools/fix_json_property_names.py --apply
```

汇报：修复了多少个文件、多少处，还是全部通过。

---

## Step 2：卡牌总览生成

运行脚本，从 `Data/Card/*.json` 重新生成 `Docs/战斗/CardOverview.md`。

```bash
python tools/gen_card_overview.py
```

汇报：生成了多少张卡的总览。

---

## Step 3：文档结构检查

自动扫描 `Docs/` 目录的实际文件列表，与 `Docs/File.md` 中记录的结构对比，找出：

- **新增未登记**：磁盘上存在但 File.md 中没记录的文件
- **已登记但缺失**：File.md 中记录了但磁盘上找不到的文件

扫描范围：`Docs/` 下所有 `.txt` 和 `.md` 文件（排除 `.codemaker/`、`openspec/`、`思考(AI不用读取，人类用)/` 目录）。

如果有差异，列出清单并询问用户是否要更新 File.md。如果没有差异，汇报"文档结构同步，无需更新"。

---

---

## Step 4：Skill 影响面检查

检查今天的代码/数据改动是否可能导致 `.codemaker/skills/` 下的 Skill 参考文件过期。

### 4.1 获取今日改动文件列表

运行 git 命令获取今天（或最近一次提交以来）改动的文件列表：

```bash
git diff --name-only HEAD~1
```

如果没有 git 历史，则用用户本次会话中实际修改过的文件列表作为替代。

将改动文件列表记为 `changedFiles`。

### 4.2 Skill 关注范围映射表

每个 Skill 关注特定的文件和目录。以下是映射表（新增 Skill 时需要同步维护）：

| Skill 名称 | 关注范围（文件或目录前缀） |
|---|---|
| `Card-Try-Creation` | `Data/Card/`, `Data/Effect/`, `Data/ActionDefines.json`, `Data/TriggerDefines.json`, `Data/ConditionDefines.json`, `Scripts/Game/Domain/Combat/`, `Scripts/Game/Domain/Object/Card/`, `Scripts/Game/Domain/!Global/API/`, `Docs/战斗` |
| `add-define` | `Scripts/Game/Data/Defines/`, `Scripts/Game/Data/Mgr/`, `Scripts/Game/Data/GameDataMgr.cs`, `Scripts/Core/Base/` |
| `add-domain-data` | `Scripts/Game/Domain/Object/`, `Scripts/Core/Base/IDomainDataBase.cs` |

### 4.3 交叉比对逻辑

对每个 Skill，检查 `changedFiles` 中是否有文件的路径前缀匹配该 Skill 的关注范围。

```
对于每个 Skill:
    matchedFiles = changedFiles 中路径前缀命中该 Skill 关注范围的文件
    如果 matchedFiles 不为空:
        标记该 Skill 为"可能受影响"
```

### 4.4 输出结果

**如果没有任何 Skill 受影响**：
汇报 "Skill 同步：✅ 今日改动未涉及任何 Skill 的关注范围"

**如果有 Skill 受影响**，按以下格式列出：

```
⚠️ 以下 Skill 可能需要更新：

1. Card-Try-Creation
   触发原因：改动了 Data/Effect/Effect_Keyword.json, Scripts/Game/Domain/Combat/CombatScene.cs
   建议检查：references/json-format.md, references/combat-rules.md
   
2. add-define
   触发原因：改动了 Scripts/Game/Data/Mgr/EffectDefineMgr.cs
   建议检查：references/definemgr-template.md
```

然后用 **ask_user_question** 询问用户："以上 Skill 可能需要更新，是否要逐个检查并更新？"

### 4.5 建议检查的推断规则

根据改动文件与 Skill references 的对应关系，推断建议检查哪些 reference 文件：

| 改动涉及的领域 | 建议检查的 reference |
|---|---|
| `Data/Card/`, `Data/Effect/`, Action/Trigger/Condition JSON | `Card-Try-Creation/references/json-format.md` |
| `Scripts/Game/Domain/Combat/` | `Card-Try-Creation/references/combat-rules.md` |
| `Scripts/Game/Domain/!Global/API/` | `Card-Try-Creation/references/api-reference.md` |
| `Scripts/Game/Data/Defines/` | `add-define/references/define-template.md` |
| `Scripts/Game/Data/Mgr/` | `add-define/references/definemgr-template.md` |
| `Scripts/Game/Data/GameDataMgr.cs` | `add-define/references/register-and-json.md` |
| `Scripts/Game/Domain/Object/*/Data/` | `add-domain-data/references/examples.md` |
| `Scripts/Core/Base/` | `add-define/references/define-template.md`, `add-domain-data/references/coding-rules.md` |

---

## 总结格式

四步全部跑完后，输出一个简洁的总结：

```
── 后处理完成 ──
1. JSON 字段：✅ 全部通过 / ⚠️ 修复了 X 个文件 Y 处
2. 卡牌总览：✅ 已生成（N 张卡）
3. 文档结构：✅ 同步 / ⚠️ 发现 N 个差异（已列出）
4. Skill 同步：✅ 无影响 / ⚠️ N 个 Skill 可能需要更新（已列出）
```
