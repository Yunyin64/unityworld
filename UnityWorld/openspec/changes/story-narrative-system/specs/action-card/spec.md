## ADDED Requirements

### Requirement: ActionCardDefine 行为卡定义
系统 SHALL 提供 `ActionCardDefine`，继承 `DefineBase`，包含 Tags、`StoryIds`（直接指定触发的 StoryId 列表）、`StoryTags`（TagBag 动态匹配用）、`IsConsumable`（使用后是否消耗）字段。

#### Scenario: 直接指定 StoryIds 触发
- **WHEN** ActionCardDefine.StoryIds 不为空
- **THEN** 使用该 ActionCard 时，系统 SHALL 从 StoryIds 列表中随机选一个触发，不走 TagBag 匹配

#### Scenario: StoryIds 为空时走 TagBag 匹配
- **WHEN** ActionCardDefine.StoryIds 为空，StoryTags 不为空
- **THEN** 系统 SHALL 用 StoryTags 在全局 StoryPool 中进行 TagBag 加权匹配，选出权重最高的 Story 触发

### Requirement: ActionCard 运行时实例
系统 SHALL 提供 `ActionCard` 运行时类，包含 DefineId（引用 ActionCardDefine）、OwnerId（持有者 NpcId）、UsageCount 等字段，由 ActionCardMgr 统一管理。

#### Scenario: 消耗型卡使用后移除
- **WHEN** ActionCard 被使用，且 ActionCardDefine.IsConsumable 为 true
- **THEN** ActionCardMgr SHALL 从持有者的卡池中移除该 ActionCard 实例

#### Scenario: 非消耗型卡使用后保留
- **WHEN** ActionCard 被使用，且 ActionCardDefine.IsConsumable 为 false
- **THEN** ActionCard 实例 SHALL 保留在持有者卡池中，可重复使用

### Requirement: ActionCardMgr 运行时管理器
系统 SHALL 提供 `ActionCardMgr`，实现 `IDomainMgrBase`，负责所有 ActionCard 实例的创建、持有、使用和移除，在 WorldMgr.Initialize() 中注册。

#### Scenario: 给 NPC 添加行为卡
- **WHEN** 调用 ActionCardMgr.GiveCard(npcId, cardDefineId)
- **THEN** 系统 SHALL 创建一个新的 ActionCard 实例并加入该 NPC 的持有列表

#### Scenario: 查询 NPC 持有的所有行为卡
- **WHEN** 调用 ActionCardMgr.GetCards(npcId)
- **THEN** 系统 SHALL 返回该 NPC 当前持有的所有 ActionCard 实例列表

### Requirement: ActionCardDataMgr 静态定义加载器
系统 SHALL 提供 `ActionCardDataMgr`，实现 `IDataMgrBase<ActionCardDefine>`，从 JSON 文件加载行为卡定义，在 `GameDataMgr` 中注册。

#### Scenario: 通过 ID 查询 ActionCardDefine
- **WHEN** 调用 ActionCardDataMgr.Instance?.Get(id)
- **THEN** 系统 SHALL 返回对应的 ActionCardDefine，不存在时返回 null

### Requirement: ActionCard 使用触发 Story
系统 SHALL 确保 ActionCard 被使用时，通过 StoryMgr.TriggerStory 统一触发对应 Story，不绕过三池统一入口。

#### Scenario: ActionCard 使用走统一触发入口
- **WHEN** ActionCardMgr.UseCard(cardId, subjectId) 被调用
- **THEN** 系统 SHALL 解析目标 StoryId 后调用 StoryMgr.TriggerStory，与三池其他来源行为一致
