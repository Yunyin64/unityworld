---
name: Card-Try-Creation
description: |
  辅助为 UnityWorld 战斗系统设计和配置卡牌。当用户想要设计一张新卡牌、配置战斗卡数据、
  给 Card/XXX.json 添加卡牌、或者讨论卡牌设计时触发此技能。
  适用于用户提到"设计一张卡"、"配一张新卡"、"加个攻击卡"、"写个法术卡"、"伤势卡怎么配"、
  "我想要一张XXX的卡"等场景。即使用户只是给了一个模糊的卡牌想法，也应使用此技能引导具体化。
---

# Card-Try-Creation

辅助用户为 UnityWorld 战斗系统设计和实现卡牌数据。

> **参考文件索引**（按需读取，不要一次全读）：
> - `references/api-reference.md` — 已注册 API 速查表 + 参数说明 + 新增 API 流程
> - `references/trigger-condition.md` — Trigger/Condition 表 + 选择器模式说明
> - `references/json-format.md` — JSON 格式参考 + ID 命名规范 + 文件路径
> - `references/combat-rules.md` — 战斗机制速查 + 数值参考范围

## 启动时必读文件

激活此 Skill 后，**必须按以下顺序读取文件**以建立上下文：

1. `Docs/战斗设计.txt` — 战斗系统完整规则
2. `Data/Action/` — 已有 Action 列表
3. `Data/TriggerDefines.json` — 已有 Trigger 列表
4. `Data/ConditionDefines.json` — 已有 Condition 列表
5. `Data/Effect/` — 已有 Effect 组合
6. `Data/Card/XXX.json` — 已有 Card 定义
7. `Scripts/Game/Domain/!Global/API/APIMgr.cs` — 查看已注册 API 签名

## 核心理念

通过对话把用户的卡牌想法翻译成正确的 JSON 数据。如果发现现有框架无法表达用户的设计意图，**不要硬凑**——标记问题，和用户讨论扩展方案。

## 工作流程

### 第一步：理解卡牌意图

从用户描述中提取：卡名/ID、CardType、Cost、Cooldown、ManaCost、行为描述。信息不全就追问。

### 第二步：拆解为 TCA / Keyword 结构

卡牌行为 = Effect 列表，每个 Effect 是 TCA 模式（Trigger + Condition + Action 列表）或 Keyword 模式。
用简明表格展示拆解结果，**确认后再写 JSON**：
```
「快斩」Cost=1, CD=2, 招式, 无Mana
└─ Effect: trigger_on_use + 无条件
   └─ Action: Attack("None", "Zhan", 2)

「速攻剑」Cost=1, CD=3, 招式, 无Mana
├─ Keyword: kw_initial（战斗开始 CD 就满）
└─ Effect: trigger_on_use + 无条件
   └─ Action: Attack("Jin", "Zhan", 2)
```

### 第三步：检查 API 可用性

将 Action 所需的 funcName 与已注册 API 对照 → 读 `references/api-reference.md`

### 第四步：检查 Trigger / Condition 可用性

核对现有 Trigger/Condition 是否满足需求 → 读 `references/trigger-condition.md`

### 第五步：生成 JSON 数据

按先底层后上层的顺序写入 → 读 `references/json-format.md` 获取格式和命名规范：
1. `Data/Action/Action_Attack.json`（攻击类）/ `Action_Defense.json`（防御类）/ `Action_Support.json`（辅助类）— 新增 Action
2. `Data/Effect/Effect_Element.json` — 新增五行 TCA Effect
   `Data/Effect/Effect_Keyword.json` — 新增 Keyword Effect
3. `Data/Card/XXX.json` — 新增 Card

数值不确定时可参照 `references/combat-rules.md` 的数值参考范围。

## 注意事项

- **多问用户**：这是辅助设计工具，每一步确认后再动手
- **一次一张**为主：配完一张再开始下一张，用户要求批量时也可以
- **展示拆解**：写 JSON 前先用简明格式展示 TCA 拆解让用户确认
- **框架问题**：缺 API / 缺 Trigger / 数据结构不够用时，和用户讨论扩展方案
- **Keyword 优先检查**：如果卡牌需要"开局即可用"、"用完消失"、"弹药限次"等行为，优先查 `references/json-format.md` 中的 Keyword 列表，而非试图用 TCA 硬凑
