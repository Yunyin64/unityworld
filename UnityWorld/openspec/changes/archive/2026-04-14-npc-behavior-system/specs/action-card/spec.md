## MODIFIED Requirements

### Requirement: BehaviorCardDefine 行为卡定义
系统 SHALL 提供 `BehaviorCardDefine`，继承 `DefineBase`，包含以下字段：

**保留字段：**
- Tags（List\<string\>，语义标签）
- IsConsumable（bool，使用后是否消耗）

**新增行为关联字段：**
- BehaviorId（string，启动哪种行为："Move"/"Practice"/"Explore"/"Social"/ExtraBehaviorDefine.ID）
- BehaviorDuration（float，行为持续时间，必填，传给 Behavior 构造）
- BehaviorIsPrimary（bool，默认 true，标识主/次行为）

**Story 按时机分拆字段（替代原 StoryIds/StoryTags）：**
- OnStartStoryIds（List\<string\>，行为开始时直接触发的 StoryId 列表）
- OnStartStoryTags（List\<string\>，行为开始时 TagBag 匹配触发）
- OnEndStoryIds（List\<string\>，行为自然结束时触发）
- OnEndStoryTags（List\<string\>，行为自然结束时 TagBag 匹配触发）
- OnInterruptStoryIds（List\<string\>，行为被打断时触发）
- OnInterruptStoryTags（List\<string\>，行为被打断时 TagBag 匹配触发）
- OnTickStoryIds（List\<StoryTickEntry\>，每 Tick 概率触发，含 StoryId + Chance）
- OnTickStoryTags（List\<StoryTickTagEntry\>，每 Tick 概率 TagBag 匹配触发，含 Tags + Chance）
- OnTimerStoryIds（List\<StoryTimerEntry\>，定时触发，含 StoryId + Delay）
- OnTimerStoryTags（List\<StoryTimerTagEntry\>，定时 TagBag 匹配触发，含 Tags + Delay）

#### Scenario: 直接指定 OnStartStoryIds 触发
- **WHEN** BehaviorCardDefine.OnStartStoryIds 不为空
- **THEN** 使用该 BehaviorCard 创建的 Behavior 在 OnStart 时 SHALL 触发这些 StoryId

#### Scenario: OnStartStoryTags 走 TagBag 匹配
- **WHEN** BehaviorCardDefine.OnStartStoryTags 不为空
- **THEN** 使用该 BehaviorCard 创建的 Behavior 在 OnStart 时 SHALL 用这些 Tags 进行 TagBag 匹配触发

#### Scenario: OnTickStoryIds 概率触发
- **WHEN** BehaviorCardDefine.OnTickStoryIds 包含 {StoryId:"random_event", Chance:0.05}
- **THEN** 创建的 Behavior 每 Tick SHALL 以 5% 概率触发 "random_event"

#### Scenario: OnTimerStoryIds 定时触发
- **WHEN** BehaviorCardDefine.OnTimerStoryIds 包含 {StoryId:"insight", Delay:100}
- **THEN** 创建的 Behavior 在 ElapsedTime >= 100 时 SHALL 触发 "insight"（仅一次）

#### Scenario: BehaviorId 关联行为类型
- **WHEN** BehaviorCardDefine.BehaviorId 为 "Practice"
- **THEN** 使用该 BehaviorCard 时 SHALL 创建 PracticeBehavior 实例

#### Scenario: BehaviorDuration 传入行为
- **WHEN** BehaviorCardDefine.BehaviorDuration 为 300
- **THEN** 创建的 Behavior 实例 Duration SHALL 为 300

### Requirement: BehaviorCardMgr 运行时管理器
系统 SHALL 提供 `BehaviorCardMgr`，实现 `IDomainMgrBase`，负责所有 BehaviorCard 实例的创建、持有、使用和移除，在 WorldMgr.Initialize() 中注册。

#### Scenario: UseCard 创建行为并塞入行为槽
- **WHEN** 调用 BehaviorCardMgr.UseCard(npcId, cardDefineId)
- **THEN** 系统 SHALL：
  1. 检查 NPC 是否空闲（NpcSystemBehavior.IsIdle），非空闲时返回失败
  2. 根据 BehaviorCardDefine.BehaviorId 通过工厂方法创建 BehaviorBase 实例
  3. 将 BehaviorCardDefine 的各时机 Story 字段转换为 List\<BehaviorStoryEntry\> 塞入 Behavior
  4. 调用 NpcSystemBehavior.AddPrimary(npcId, behavior) 塞入行为槽
  5. 处理 IsConsumable 逻辑（消耗型卡移除）

#### Scenario: 空闲检查失败
- **WHEN** 调用 UseCard，但 NPC 当前有主行为（非空闲）
- **THEN** 系统 SHALL 返回 false，打印警告日志，不创建行为

#### Scenario: 给 NPC 添加行为卡
- **WHEN** 调用 BehaviorCardMgr.GiveCard(npcId, cardDefineId)
- **THEN** 系统 SHALL 创建一个新的 BehaviorCard 实例并加入该 NPC 的持有列表

#### Scenario: 查询 NPC 持有的所有行为卡
- **WHEN** 调用 BehaviorCardMgr.GetCards(npcId)
- **THEN** 系统 SHALL 返回该 NPC 当前持有的所有 BehaviorCard 实例列表

### Requirement: BehaviorCard 运行时实例
系统 SHALL 提供 `BehaviorCard` 运行时类，包含 DefineId（引用 BehaviorCardDefine）、OwnerId（持有者 int）、UsageCount 等字段，由 BehaviorCardMgr 统一管理。

#### Scenario: 消耗型卡使用后移除
- **WHEN** BehaviorCard 被使用，且 BehaviorCardDefine.IsConsumable 为 true
- **THEN** BehaviorCardMgr SHALL 从持有者的卡池中移除该 BehaviorCard 实例

#### Scenario: 非消耗型卡使用后保留
- **WHEN** BehaviorCard 被使用，且 BehaviorCardDefine.IsConsumable 为 false
- **THEN** BehaviorCard 实例 SHALL 保留在持有者卡池中，可重复使用

### Requirement: BehaviorCardDataMgr 静态定义加载器
系统 SHALL 提供 `BehaviorCardDataMgr`，实现 `IDataMgrBase<BehaviorCardDefine>`，从 JSON 文件加载行为卡定义，在 `GameDataMgr` 中注册。

#### Scenario: 通过 ID 查询 BehaviorCardDefine
- **WHEN** 调用 BehaviorCardDataMgr.Instance?.Get(id)
- **THEN** 系统 SHALL 返回对应的 BehaviorCardDefine，不存在时返回 null

### Requirement: BehaviorCard 使用触发 Story
系统 SHALL 确保 BehaviorCard 被使用时，Story 的触发由创建的 Behavior 生命周期各时机结算，通过 StoryMgr.TriggerStory 统一触发入口。

#### Scenario: BehaviorCard 使用走 Behavior 生命周期
- **WHEN** BehaviorCardMgr.UseCard(npcId, cardDefineId) 被调用
- **THEN** 系统 SHALL 通过 Behavior 的 OnStart/OnTick/OnTimer/OnEnd/OnInterrupt 结算 Story，不再在 UseCard 中直接触发

## REMOVED Requirements

### Requirement: BehaviorCardDefine 旧 StoryIds/StoryTags 字段
**Reason**: StoryIds 和 StoryTags 已按触发时机拆分为 OnStartStoryIds/OnStartStoryTags/OnEndStoryIds/OnEndStoryTags/OnInterruptStoryIds/OnInterruptStoryTags/OnTickStoryIds/OnTickStoryTags/OnTimerStoryIds/OnTimerStoryTags。
**Migration**: 将原 StoryIds 值迁移到 OnStartStoryIds，原 StoryTags 值迁移到 OnStartStoryTags（语义等价：原来的"使用时触发"等同于"行为开始时触发"）。