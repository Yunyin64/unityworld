## Context

战斗系统的卡牌行为由 `Effects[]` 列表描述，每个 Effect 是一个 TCA（Trigger + Condition + Action[]）结构，完全数据驱动。但有一类卡牌机制——如「初始」「消耗」「武器」——改变的是卡牌在系统中的存在方式、生命周期、或与其他卡牌的引用关系，无法用 TCA 的"在某时机满足某条件执行某动作"来表达。

当前「初始」「消耗」「弹药」已在机制原子清单中定义，但缺乏统一的数据身份。本设计将它们统一为 **Keyword Effect**。

## Goals / Non-Goals

**Goals:**

- 让 Keyword Effect 成为 Effect 列表中的一等成员，与 TCA Effect 共存
- Keyword Effect 共享 Score + Tag 体系，无缝参与卡牌随机生成的分数预算
- 每个 Keyword 由引擎硬编码实现，在结算管线的特定节点介入
- 定义首批 9 个 Keyword 的行为规则
- 在设计文档中明确 Keyword 在结算管线中的介入时机

**Non-Goals:**

- 不设计 Keyword 的代码实现方案（那是程序的事）
- 不设计 Keyword 的 UI 渲染规范（只确认 IsKeyword=true 用关键词格式显示）
- 不设计 Keyword 的数值平衡（各 Keyword 的 Score 值留给数值调试）

## Decisions

### Decision 1：Keyword 是 Effect 的一种模式，不是独立层

**选择**：Keyword Effect 就是 EffectData，标记 IsKeyword=true，住在 CardData.Effects[] 里。

**替代方案**：在 CardData 上增加独立的 `Keywords[]` 字段。

**为什么选这个**：
- 卡牌生成只有一条管线，Effect 池 = TCA Effect ∪ Keyword Effect，分数统一分配
- Tag 体系自然覆盖，不需要额外的匹配逻辑
- 一张卡的"全部描述"都在 Effects[] 里，没有分裂

### Decision 2：Keyword Effect 的数据结构

一个 Effect 在 IsKeyword=true 时，不使用 Trigger/Condition/Actions 字段，改用：

```
EffectData（Keyword 模式）:
  IsKeyword   : true
  KeywordId   : string          ← "Initial" / "Consume" / "Weapon" / ...
  KeywordParams : Dict<string, object>  ← 参数包，按需使用
  Score       : float           ← 分数预算
  Tags        : List<Tag>       ← 参与匹配
```

KeywordParams 示例：
- Initial：无参数（或空 Dict）
- Consume：`{"Uses": 1}`
- Ammo：`{"Uses": 3}`
- Overcharge：`{"ManaPerPoint": 1, "MaxExtra": 2}`

### Decision 3：Keyword 在结算管线中的介入时机

Keyword 不是 TCA 管线的一部分，它在管线的**外围**起作用。根据介入时机分为四类：

```
┌───────────────────────────────────────────────────────────┐
│                    卡牌结算管线                              │
│                                                           │
│  [战斗初始化]                                               │
│      ↓                                                    │
│      ├── ⚡ Initial：设 CD=0                               │
│      ├── ⚡ Sluggish：首次 CD ×2                           │
│      ↓                                                    │
│  [CD 循环]                                                 │
│      ↓                                                    │
│  [CD 到达 → 准备结算]                                       │
│      ↓                                                    │
│      ├── ⚡ Weapon：查找武器卡，补全 Context 空位            │
│      ├── ⚡ Overcharge：检查多余灵元，提升数值               │
│      ↓                                                    │
│  [判定：攻防卡 or 效果卡？]                                   │
│      ↓                                                    │
│      ├── 攻防卡 → ⚡ Rush：跳过待发槽，直接直击              │
│      │           → 正常入槽                                │
│      │               ├── 挤出时 → ⚡ Fortify：阻止被挤出    │
│      │               └── 对拼...                           │
│      ├── 效果卡 → 直接结算                                  │
│      ↓                                                    │
│  [结算完成 → 后处理]                                        │
│      ↓                                                    │
│      ├── ⚡ Consume：计数-1，归零则移除卡牌释放 Size         │
│      ├── ⚡ Ammo：计数-1，归零则卡牌休眠停止 CD              │
│      ↓                                                    │
│  [被位移效果选中时]                                         │
│      ↓                                                    │
│      ├── ⚡ Anchored：拦截，位移无效                        │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

四类介入时机：

| 类别 | 时机 | Keyword |
|------|------|---------|
| 初始化修饰 | 战斗开始装载卡组时 | Initial, Sluggish |
| 预处理修饰 | CD到达、TCA结算之前 | Weapon, Overcharge |
| 流程改写 | 改变攻防卡的槽位行为 | Rush, Fortify |
| 后处理修饰 | 使用完成后 | Consume, Ammo |
| 被动拦截 | 被其他效果选中时 | Anchored |

### Decision 4：`<武器>` 的引用规则

武器 Keyword 的行为：
1. 结算前，在卡组中**向上搜索**，找到第一张 CardType=法宝 且带有特定武器 Tag 的卡
2. 从该武器卡的属性中提取：元素类型、物理类型（如果当前 Action 的对应字段为空）
3. 将提取的值填入 Action Context 的空位
4. 如果找不到武器卡，空位保持为 None/无属性

**武器卡本身**不通过 Keyword 标记。武器卡就是一张普通的法宝卡，带有武器相关的 Tag。`<武器>` Keyword 标记在**使用武器的招式卡**上，表示"我需要引用一把武器"。

### Decision 5：首批 Keyword 清单

| KeywordId | 中文名 | 参数 | 介入时机 | 行为描述 |
|-----------|--------|------|----------|----------|
| Initial | 初始 | 无 | 初始化 | 战斗开始时 CD 设为 0，立刻进入"CD已满"状态 |
| Consume | 消耗 | Uses:int | 后处理 | 每次使用后计数-1，归零时从卡组移除，释放其 Size |
| Ammo | 弹药 | Uses:int | 后处理 | 每次使用后计数-1，归零时卡牌休眠，不再参与 CD。可被「装填」类 Action 恢复 |
| Weapon | 武器 | 无 | 预处理 | 结算前从卡组上方搜索武器卡，用其属性补全 Action Context 中的空位字段 |
| Anchored | 锁位 | 无 | 被动拦截 | 被位移 Action 选中时，位移无效 |
| Rush | 速攻 | 无 | 流程改写 | CD 到达后不进待发槽，直接作为直击结算 |
| Fortify | 坚守 | 无 | 流程改写 | 在待发槽中时，不会被后续卡挤出 |
| Sluggish | 迟缓 | 无 | 初始化 | 战斗开始时，首次 CD 翻倍 |
| Overcharge | 超载 | ManaPerPoint:int, MaxExtra:int | 预处理 | CD 到达时检查灵元池，每消耗 ManaPerPoint 个同元素灵元，拼点数值 +1，最多额外 +MaxExtra |

## Risks / Trade-offs

- **[Keyword 数量膨胀]** → 每个 Keyword 都需要硬编码，不像 TCA 可以数据驱动扩展。缓解：严格把关，只有真正无法用 TCA 表达的机制才列为 Keyword。如果一个效果可以用 Trigger+Condition+Action 组合出来，就不应该是 Keyword。
- **[武器引用的空间依赖]** → 武器 Keyword 依赖卡组位置（"上方第一张武器卡"），位移效果会改变引用关系。这是特性不是 bug——位移可以作为反武器流的策略。但需要在体验上确保玩家能理解这个关系。
- **[Fortify + Rush 的边界情况]** → 如果一张卡同时有 Fortify 和 Rush，Rush 优先（不进待发槽，Fortify 无意义）。需要定义 Keyword 间的优先级/冲突规则，首批 Keyword 数量少，逐个处理即可。