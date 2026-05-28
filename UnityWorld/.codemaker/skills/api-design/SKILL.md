---
name: api-design
description: |
  设计或补全战斗系统的 API 函数（Action / Contest / Condition）。当用户想新增一个 APIFunc、补全空实现、或讨论 API 设计时使用此技能。
  适用于用户提到"设计API"、"新增Action"、"补全实现"、"写个新效果"、"加个Condition"、"这个API怎么写"、"实现一下这个空函数"等场景。
  即使用户只是模糊地说"加个新效果"或"这个还没实现"也应触发此技能。
---

# 设计 / 补全战斗 API 函数

本技能引导你为 UnityWorld 战斗系统设计新的或补全已有的 API 函数。

核心原则：**先问清楚，再动手写。永远不要在信息不足时猜测实现。**

> **参考文件索引**（按需读取）：
> - `references/existing-api.md` — 现有 API 完整清单（避免重复设计）
> - `references/code-patterns.md` — 代码模板与命名规范

---

## 工作流程

### Phase 0：API 资格检查（门槛判断）

在开始设计任何 API 之前，先用以下三条标准判断"这个效果值不值得成为一个 API"：

1. **有没有游戏寓意？** — 策划能用一个词概括这个玩法体验吗？（疗伤、充能、冻结、破甲...）如果只是"随机删个卡"这种纯机械操作，没有设计语言，不适合做 API。
2. **构不构成玩法？** — 玩家能理解并围绕它做决策吗？如果玩家无法感知或无法应对，它不构成玩法。
3. **风险可控吗？** — 效果边界明确，不会产生策划无法预期的破坏性后果。

> **核心理念：API 是给策划的设计语言，不是程序员的抽象工具函数。**
> 即使底层实现相似，只要策划概念不同（疗伤 vs 销毁 vs 净化），就应该是独立 API。
> 反之，如果一个操作没有明确的策划寓意（如"随机移除任意卡牌"），它就不该暴露为 API——因为它不代表一种可设计的玩法，只是一个危险的工具。

如果候选效果不满足以上标准，告知用户原因，建议换个思路或收窄语义。

---

### Phase 1：信息收集（必须全部确认才能进入 Phase 2）

对每个要设计/补全的 API，逐一向用户确认以下信息。**使用 ask_user_question 工具逐条询问，不要一次性列出所有问题。**

#### 1.1 基础定义

| 问题 | 说明 |
|------|------|
| **功能描述** | 这个 API 做什么？一句话描述效果 |
| **APIType** | `Action`（纯执行效果）/ `Contest`（拼点类）/ `Condition`（条件/选择器） |
| **FuncName** | 命名风格：动词短语，PascalCase，简洁（如 `StealMana`、`Drain`） |

#### 1.2 目标判定（关键！）

> **规则：如果效果不是"确定只对自己用"，就必须带 Target 参数。**

问用户：
- 这个效果是只作用于施法者自身吗？
- 如果不是，目标类型是什么？（`CombatNpc` / `CombatCard` / `List<CombatCard>`）
- 目标是敌方还是任意？（影响日志和后续 Condition 选择器设计）

#### 1.3 参数设计

| 问题 | 说明 |
|------|------|
| **涉及元素吗？** | 是否需要 `Element:String` 参数 |
| **数值参数** | 有几个数值参数？各自的名字、类型（Int/Float）、合理取值范围 |
| **可选参数** | 有没有可选参数？（以 `?` 前缀标记，如 `?Duration:Float`） |
| **Scope** | `Scope.CombatNpc` / `Scope.CombatCard` / `Scope.Card` / `Scope.Global` |

> **主动建议模式参数：** 当一个 API 的筛选/匹配逻辑存在"精确 vs 模糊"两种合理用法时（如按体量精确匹配 vs ≤N），主动提示用户："是否加一个模式参数（如 Exact:Bool）来控制匹配方式？"这类参数通常设为可选、默认 true（精确）。

> **参数数量警戒线：** 当一个 API 的**必填参数**达到 4 个以上时，主动提示用户："参数有点多了，是否考虑拆分成两个语义更明确的 API？"可选参数（`?` 前缀）不计入。参数过多会影响策划理解和配置体验——API 是设计语言，应该一眼能读懂。

#### 1.4 交互与平衡

| 问题 | 说明 |
|------|------|
| **与已有 API 的关系** | 跟哪些已有 API 有冲突或互动？（参考 `references/existing-api.md`） |
| **Score 估算** | 这个效果在 Effect 的强度预算中值多少分？（参考同类 API 的 Score） |
| **边界情况** | 目标不存在怎么办？数值为 0 怎么办？资源不足怎么办？ |

---

### Phase 2：设计文档输出

信息收集完成后，先输出一份设计文档给用户确认，**不要直接写代码**。

格式：

```
## API 设计：{FuncName}

**描述**：{一句话}
**类型**：{APIType} | **Scope**：{Scope}
**目标**：{Self / Target:CombatNpc / Target:CombatCard / ...}

### 参数签名
| 参数名 | 类型 | 必填 | 说明 | 取值范围 |
|--------|------|------|------|----------|
| ... | ... | ... | ... | ... |

### Attribute 声明
[APIFunc("{FuncName}", APIType.{Type}, "{描述}", Scope.{Scope}, "{Param1:Type}", ...)]

### 与已有 API 关系(没冲突不写)
- 冲突：...


等用户确认 "OK" / 修改意见后，再进入 Phase 3。

---

### Phase 3：代码实现

读取 `references/code-patterns.md` 获取代码模板，然后：

1. **确定写入文件**：
   - 大部分写入 `Scripts/Game/Domain/!Global/API/Combat/Action/` 下对应文件
   - 如果是 Condition，写入 `Scripts/Game/Domain/!Global/API/Combat/Condition/` 下
   - 如果现有文件中已有空实现，直接补全

2. **代码规范**：
   - `public static APIContext {FuncName}(APIContext ctx)` 签名
   - 开头取 Caster/Target，null 检查后 early return
   - 用 `ctx.GetValue<T>("ParamName", defaultValue)` 取参数
   - 用 `ctx.Get<T>("ParamName")` 取对象引用
   - 结尾 `LogMgr.Instance.Dbg(...)` 输出关键信息
   - return ctx

3. **写入后更新** `references/existing-api.md`（保持 API 清单最新）

---

## 补全空实现的特殊流程

如果用户指向一个已有的空实现（如 `return ctx;` 占位）：

1. 读取该方法的 `[APIFunc]` Attribute，提取已声明的参数信息
2. 读取上下文（同文件其他方法、相关类的可用方法）
3. **仍然需要向用户确认**：
   - 边界情况处理方式
   - 是否需要额外参数（Attribute 可能不完整）
   - 具体逻辑细节（不要猜）
4. 输出设计文档 → 确认 → 实现

---

## 禁止事项

- 不要在信息不完整时猜测实现
- 不要跳过设计文档直接写代码
- 不要创建新的 .cs 文件（除非用户明确要求拆分）
- 不要修改已有 API 的签名（除非用户明确要求）
- 不要使用 `System.Random` 或 `UnityEngine.Random`，随机数用 `Soul.Random`
