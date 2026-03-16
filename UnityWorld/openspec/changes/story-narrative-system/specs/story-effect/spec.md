## ADDED Requirements

### Requirement: StoryCondition 条件判断结构
系统 SHALL 提供 `StoryCondition` 结构，支持对主体属性（Stat）、Tag拥有情况、五行浓度（Aura）、时间、关系值等进行条件判断，每个 Condition 包含 TargetType、FieldName、Operator、Value 字段。

#### Scenario: 属性值条件满足
- **WHEN** StoryCondition.TargetType 为 NpcStat，Operator 为 GreaterThan，Value 为 60
- **THEN** 系统 SHALL 查询主体对应 Stat 值，大于 60 时返回 true

#### Scenario: 多条件全部满足才触发
- **WHEN** StoryBaseDefine.Conditions 包含多个 StoryCondition
- **THEN** 系统 SHALL 要求全部条件同时满足（AND 逻辑）才允许触发

### Requirement: StoryEffectFunc 原子效果注册表（简单轨）
系统 SHALL 提供 `StoryEffectFunc` 静态类，内置以下原子效果并通过字符串 Key 注册，配置文件中 Effects 字段存 `{FuncName, Args}` 结构：

- `GiveTrait(npcId, traitId)`：给 NPC 添加 Trait
- `RemoveTrait(npcId, traitId)`：移除 NPC 的 Trait
- `GiveActionCard(npcId, cardDefineId)`：给 NPC 添加行为卡
- `ModifyAura(planeId, element, delta)`：修改地块五行浓度
- `TriggerStory(storyId, subjectId)`：链式触发另一个 Story
- `TriggerStoryByTag(tags, subjectId)`：按 Tag 匹配触发 Story
- `AddToFatePool(subjectId, time, storyId)`：向宿命池写入条目
- `AddToKarmaPool(subjectId, storyId, weight)`：向劫缘池写入条目
- `ModifyStat(npcId, statId, delta)`：修改 NPC 属性值
- `EmitEvent(eventName, args)`：通过 EventMgr 广播事件

#### Scenario: 链式触发 Story
- **WHEN** 某 StoryEffectFunc 调用 TriggerStory(storyId, subjectId)
- **THEN** 系统 SHALL 通过 StoryMgr.TriggerStory 触发目标 Story，形成嵌套链式触发

#### Scenario: 未知 FuncName 时警告不崩溃
- **WHEN** Effects 中包含注册表中不存在的 FuncName
- **THEN** 系统 SHALL 打印 Warning 日志并跳过，不抛出异常

### Requirement: LuaStory 复杂轨接口预留
系统 SHALL 在 StoryBaseDefine 中预留 `LuaScript` 字段（string，可为 null）。运行时若该字段非空，系统 SHALL 打印 Warning 日志（"LuaStory not yet integrated"）并回退到简单轨执行，不抛出异常。

#### Scenario: LuaScript 非空时降级处理
- **WHEN** StoryBaseDefine.LuaScript 不为 null
- **THEN** 系统 SHALL 打印 Warning，执行简单轨的 Effects，不中断游戏流程

### Requirement: StoryContext 执行上下文
系统 SHALL 提供 `StoryContext` 类，作为 Effect 执行时的上下文容器，包含触发主体（Subject）、触发来源（SourcePool）、当前时间、Rng 实例等信息，传入所有 StoryEffectFunc 调用。

#### Scenario: Effect 访问触发主体
- **WHEN** StoryEffectFunc 执行时
- **THEN** 可通过 StoryContext.Subject 访问触发该 Story 的主体对象
