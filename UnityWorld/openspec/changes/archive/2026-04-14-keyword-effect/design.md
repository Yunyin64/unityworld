## Context

当前卡牌的 EffectData / EffectDefine 只支持 TCA（Trigger+Condition+Action）模式。
文档《战斗_机制原子清单》定义了第二种 Effect 模式：**Keyword Effect**——
用 `KeywordId + KeywordParams` 标记，由引擎硬编码执行，修饰卡牌在系统中的存在方式、生命周期或引用关系。

Keyword 与 TCA Effect 共存于同一个 `Effects[]` 列表中，共享 Score/Tags 字段。
两者通过 `IsKeyword` 布尔值区分。

本次以 **Initial**（初始：战斗开始时 CD 设为满值）作为第一个 Keyword 端到端实现。

## Goals / Non-Goals

**Goals:**
- 在 EffectDefine / EffectData 中增加 IsKeyword 和 KeywordParams 字段，与 TCA 字段互斥共存
- CardMgr.BuildEffectFromDefine 支持 Keyword 分支构建
- 战斗初始化阶段（CombatScene.Start 之前）扫描并执行 Keyword
- Initial 的完整实现：CD 设为满值，第一个 Tick 即可触发
- 提供一条 Initial 的 EffectDefine JSON 数据验证管线

**Non-Goals:**
- 不实现 Initial 以外的其他 Keyword（Sluggish/Weapon/Rush/Consume/Ammo/Fortify/Overcharge/Anchored）
- 不实现 Keyword 处理器的复杂注册/插件机制——先用 switch-case 分发，后续 Keyword 多了再重构
- 不修改卡牌随机生成系统（CardSystemGenerate）

## Decisions

### D1：Keyword 字段放在 EffectDefine/EffectData 中，而非 CardDefine/CardData

**选择**：Keyword 是 Effect 级别，不是 Card 级别。

**理由**：
- 文档明确说"Keyword Effect 与 TCA Effect 共存于 Effects[] 列表中"
- 一张卡可以同时有 TCA Effect 和多个 Keyword Effect（例如：Initial + Consume）
- 放在 Effect 层保持了 Card = List<Effect> 的统一模型

### D2：用 EffectDefine.ID 本身做 Keyword 分发，不引入 KeywordId 或 KeywordType

**选择**：不新增 KeywordId 字段和 KeywordType 枚举，直接用 EffectDefine 的 ID（如 `"kw_initial"`）做 switch 分发。

**理由**：
- 每个 Keyword 本身就是一条独立的 EffectDefine，已有唯一 ID
- 再加 KeywordId 是同一概念的重复描述（ID、KeywordId、枚举三个名字）
- 代码中 `switch(effect.DefineId)` 即可分发，简洁无废话
- 减少一个枚举和一个字段的维护成本

### D3：KeywordParams 使用 Dictionary<string, string>

**选择**：`KeywordParams` 类型为 `Dictionary<string, string>`，空字典表示无参数。

**理由**：
- 不同 Keyword 参数结构差异大（Initial 无参数、Ammo 有 Uses:int、Overcharge 有 ManaPerPoint+MaxExtra）
- Dictionary 是最灵活的 JSON 兼容类型
- 具体 Keyword 处理器内部自行解析参数并做类型转换

### D4：Keyword 执行时机——在 CombatScene.Start() 中统一扫描

**选择**：在 `CombatScene.Start()` 阶段（CardStates 已初始化、战斗正式开始前），遍历所有 CombatNpc 的 CardStates，扫描 Keyword Effect 并按类别分发。

**理由**：
- Initial 和 Sluggish 都是"初始化修饰"类，只需在战斗开始时执行一次
- 后续 Keyword（预处理/后处理/流程改写）在不同时机执行，但底层扫描逻辑可复用
- 在 Start() 而非 PreStart() 执行，是因为 PreStart 负责基础数据准备，Start 负责战斗规则初始化

### D5：Initial 的实现——直接设置 CombatCardState.CurrentCdTick

**选择**：将 `CurrentCdTick` 设为 `Card.Cooldown` 的值（即 CD 满值）。

**理由**：
- CombatCardState.TickCd() 判断 `CurrentCdTick >= Card.Cooldown` 为就绪
- 直接设满即可在第一个 Tick 被 CollectReadyCards 收集
- 需要为 CombatCardState 新增一个 `SetCdFull()` 公开方法

## Risks / Trade-offs

- **[风险] switch-case 分发不够扩展** → 首次只有 Initial，switch 足够。当 Keyword 超过 5 个时考虑重构为注册式处理器模式。
- **[风险] KeywordParams 的类型安全** → string Dictionary 无编译期保证。每个 Keyword 处理逻辑内部需做参数校验和明确报错。
- **[权衡] Initial 卡在第一个 Tick 同时就绪可能打破节奏** → 这是设计意图（"第一个 Tick 即可触发"），策划通过 Size/ManaCost 控制代价。