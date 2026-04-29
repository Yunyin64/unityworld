## ADDED Requirements

### Requirement: 天池——宿命池（FatePool）
系统 SHALL 为每个主体（Subject：NPC/门派/世界等）维护一个宿命池，结构为时间→StoryId列表的有序映射，时间到达时检查 Conditions，满足则触发，不满足则跳过（不重新入池）。

#### Scenario: 宿命到时触发
- **WHEN** 当前 WorldTime >= FatePool 中某条目的时间
- **THEN** 系统 SHALL 检查对应 StoryDefine.Conditions，满足时触发该 Story，并从池中移除该条目

#### Scenario: 宿命条件不满足时跳过
- **WHEN** 宿命条目时间到达，但 StoryDefine.Conditions 不满足
- **THEN** 系统 SHALL 跳过触发并从池中移除该条目，打印 Dbg 日志，不重新入池

### Requirement: 地池——劫缘池（KarmaPool）
系统 SHALL 为每个主体维护一个劫缘池，每个条目包含 StoryId、Weight、Conditions。每隔固定周期（可配置），系统 SHALL 筛选满足 Conditions 的条目，按 Weight 加权随机触发其中一个。

#### Scenario: 周期性筛选与触发
- **WHEN** 劫缘池触发周期到达
- **THEN** 系统 SHALL 过滤出所有 Conditions 满足的条目，按 Weight 加权随机选一个触发

#### Scenario: 无满足条件条目时静默跳过
- **WHEN** 劫缘池触发周期到达，但无任何条目满足 Conditions
- **THEN** 系统 SHALL 静默跳过本次触发，不产生任何 Story 事件

### Requirement: 人池——抉择池（WillPool）通过 BehaviorCard 驱动
系统 SHALL 通过 BehaviorCard 实例表示个体的抉择池。个体在空闲状态时，由 AI（或玩家输入）从持有的 BehaviorCard 中选择一张使用，从而触发对应的 Story。

#### Scenario: NPC 空闲时自动选卡
- **WHEN** NPC 处于空闲状态且持有至少一张 BehaviorCard
- **THEN** 系统 SHALL 由 AI 决策模块（暂留空）选择一张 BehaviorCard 并触发对应 Story

#### Scenario: 玩家手动选卡
- **WHEN** 玩家在 UI 中选择一张 BehaviorCard
- **THEN** 系统 SHALL 触发对应 Story，并通过 StoryMgr.TriggerStory 统一处理

### Requirement: 统一触发入口 StoryMgr.TriggerStory
系统 SHALL 提供统一的 `StoryMgr.TriggerStory(storyId, subject)` 接口，屏蔽三池来源差异，所有触发均走此入口。

#### Scenario: 三池统一走触发入口
- **WHEN** 宿命池/劫缘池/抉择池任意一个触发 Story
- **THEN** 均调用 StoryMgr.TriggerStory，后续逻辑完全一致，与来源无关

### Requirement: StoryMgr 在 WorldMgr 中注册并每帧 Tick
系统 SHALL 在 WorldMgr.Initialize() 中注册 StoryMgr，并在 Tick 中驱动宿命池时间检查和劫缘池周期检查。

#### Scenario: Tick 驱动时间检查
- **WHEN** WorldMgr.Tick(deltaTime) 被调用
- **THEN** StoryMgr.Tick(deltaTime) SHALL 被调用，推进宿命池时间并检查劫缘池触发周期
