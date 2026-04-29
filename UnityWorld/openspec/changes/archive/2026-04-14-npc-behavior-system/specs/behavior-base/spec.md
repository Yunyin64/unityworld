## ADDED Requirements

### Requirement: BehaviorStoryTrigger 枚举
系统 SHALL 在 EnumTypes.cs 中提供 `BehaviorStoryTrigger` 枚举，定义行为生命周期中的五种 Story 触发时机：OnStart、OnEnd、OnInterrupt、OnTick、OnTimer。

#### Scenario: 枚举值完整性
- **WHEN** 系统加载 BehaviorStoryTrigger 枚举
- **THEN** 枚举 SHALL 包含 OnStart、OnEnd、OnInterrupt、OnTick、OnTimer 五个值

### Requirement: BehaviorStoryEntry 数据结构
系统 SHALL 提供 `BehaviorStoryEntry` 类，作为 Behavior 内部 Story 触发规则的运行时数据，包含 StoryId（string）、StoryTags（List\<string\>，用于 TagBag 匹配）、Trigger（BehaviorStoryTrigger）、Chance（float，OnTick 用每 Tick 概率）、Delay（float，OnTimer 用延迟时间）、HasTriggered（bool，OnTimer 用一次性标记）。

#### Scenario: OnStart 类型的 Entry
- **WHEN** BehaviorStoryEntry.Trigger == OnStart
- **THEN** 该 Entry SHALL 在行为 OnStart 时被结算，Chance/Delay/HasTriggered 字段不使用

#### Scenario: OnTick 类型的 Entry 概率触发
- **WHEN** BehaviorStoryEntry.Trigger == OnTick 且 Chance 为 0.1
- **THEN** 每次 Tick 结算时，系统 SHALL 以 10% 概率触发该 Story

#### Scenario: OnTimer 类型的 Entry 定时触发
- **WHEN** BehaviorStoryEntry.Trigger == OnTimer 且 Delay 为 100
- **THEN** 当行为 ElapsedTime >= 100 且 HasTriggered == false 时，系统 SHALL 触发该 Story 并将 HasTriggered 设为 true

#### Scenario: StoryTags 匹配触发
- **WHEN** BehaviorStoryEntry.StoryId 为空且 StoryTags 不为空
- **THEN** 系统 SHALL 使用 StoryTags 通过 StoryMgr 进行 TagBag 匹配触发，而非直接指定 StoryId

### Requirement: BehaviorBase 抽象基类
系统 SHALL 提供 `BehaviorBase` 抽象类，位于 `Domain/GamePlay/Behavior/`，包含 BehaviorId（string）、IsPrimary（bool）、CanMove（bool）、Duration（float，外部传入）、ElapsedTime（float，Tick 累加）、IsFinished（计算属性：ElapsedTime >= Duration）、StoryEntries（List\<BehaviorStoryEntry\>）。

#### Scenario: 行为时间推进
- **WHEN** OnTick(dt) 被调用
- **THEN** ElapsedTime SHALL 增加 dt，并遍历 StoryEntries 结算 OnTick 和 OnTimer 类型的 Entry

#### Scenario: 行为自然结束
- **WHEN** IsFinished 为 true（ElapsedTime >= Duration）
- **THEN** 管理系统 SHALL 调用 OnEnd()，结算所有 Trigger==OnEnd 的 StoryEntry，然后从行为列表移除

#### Scenario: 行为被打断
- **WHEN** 外部调用 OnInterrupt()
- **THEN** 系统 SHALL 结算所有 Trigger==OnInterrupt 的 StoryEntry，并通过 EventMgr 广播 "BehaviorInterrupted" 事件

#### Scenario: 行为开始
- **WHEN** 行为被添加到 NPC 行为槽，OnStart() 被调用
- **THEN** 系统 SHALL 结算所有 Trigger==OnStart 的 StoryEntry

### Requirement: Story 结算引擎
BehaviorBase SHALL 在基类中实现统一的 Story 结算逻辑，遍历 StoryEntries，根据 Trigger 类型调用 StoryMgr.TriggerStory（StoryId 非空时）或 StoryMgr.TriggerStoryByTags（StoryTags 非空时）。

#### Scenario: 按 StoryId 直接触发
- **WHEN** 结算某 BehaviorStoryEntry，StoryId 不为空
- **THEN** 系统 SHALL 调用 StoryMgr.TriggerStory(storyId, subjectint)

#### Scenario: 按 StoryTags 匹配触发
- **WHEN** 结算某 BehaviorStoryEntry，StoryId 为空且 StoryTags 不为空
- **THEN** 系统 SHALL 调用 StoryMgr.TriggerStoryByTags(storyTags, subjectint)

### Requirement: 官方行为子类——MoveBehavior
系统 SHALL 提供 `MoveBehavior`，继承 BehaviorBase，BehaviorId 为常量 "Move"，IsPrimary=true，CanMove=false。V1 不包含额外的 OnTick 逻辑。

#### Scenario: 创建 MoveBehavior
- **WHEN** 使用 BehaviorId=="Move" 的 BehaviorCard
- **THEN** 系统 SHALL 创建 MoveBehavior 实例，BehaviorId=="Move"，IsPrimary==true，CanMove==false

### Requirement: 官方行为子类——PracticeBehavior
系统 SHALL 提供 `PracticeBehavior`，继承 BehaviorBase，BehaviorId 为常量 "Practice"，IsPrimary=true，CanMove=false。V1 不包含额外的 OnTick 逻辑。

#### Scenario: 创建 PracticeBehavior
- **WHEN** 使用 BehaviorId=="Practice" 的 BehaviorCard
- **THEN** 系统 SHALL 创建 PracticeBehavior 实例，BehaviorId=="Practice"，IsPrimary==true，CanMove==false

### Requirement: 官方行为子类——ExploreBehavior
系统 SHALL 提供 `ExploreBehavior`，继承 BehaviorBase，BehaviorId 为常量 "Explore"，IsPrimary=true，CanMove=false。V1 不包含额外的 OnTick 逻辑。

#### Scenario: 创建 ExploreBehavior
- **WHEN** 使用 BehaviorId=="Explore" 的 BehaviorCard
- **THEN** 系统 SHALL 创建 ExploreBehavior 实例，BehaviorId=="Explore"，IsPrimary==true，CanMove==false

### Requirement: 官方行为子类——SocialBehavior
系统 SHALL 提供 `SocialBehavior`，继承 BehaviorBase，BehaviorId 为常量 "Social"，IsPrimary=true，CanMove=false。V1 不包含额外的 OnTick 逻辑。

#### Scenario: 创建 SocialBehavior
- **WHEN** 使用 BehaviorId=="Social" 的 BehaviorCard
- **THEN** 系统 SHALL 创建 SocialBehavior 实例，BehaviorId=="Social"，IsPrimary==true，CanMove==false

### Requirement: ExtraBehavior 通用拓展行为
系统 SHALL 提供 `ExtraBehavior`，继承 BehaviorBase，BehaviorId 由构造参数传入（对应 ExtraBehaviorDefine.ID），用于数据驱动创建任意行为变体。

#### Scenario: 创建 ExtraBehavior
- **WHEN** BehaviorId 不匹配任何官方行为常量
- **THEN** 系统 SHALL 创建 ExtraBehavior 实例，BehaviorId 设为传入的 ID 值

### Requirement: Behavior 工厂方法
系统 SHALL 提供根据 BehaviorId 字符串创建对应 BehaviorBase 子类实例的工厂方法。官方 ID（"Move"/"Practice"/"Explore"/"Social"）创建对应官方子类，其他 ID 创建 ExtraBehavior。

#### Scenario: 官方 BehaviorId 创建官方子类
- **WHEN** 工厂方法收到 BehaviorId=="Move"
- **THEN** SHALL 返回 MoveBehavior 实例

#### Scenario: 非官方 BehaviorId 创建 ExtraBehavior
- **WHEN** 工厂方法收到 BehaviorId=="fire_meditation"
- **THEN** SHALL 返回 ExtraBehavior 实例，BehaviorId=="fire_meditation"