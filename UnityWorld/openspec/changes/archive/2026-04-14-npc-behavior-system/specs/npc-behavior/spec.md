## ADDED Requirements

### Requirement: NpcBehaviorData 运行时数据
系统 SHALL 提供 `NpcBehaviorData`，实现 `IDomainDataBase`，位于 `Domain/Object/Npc/Data/`，包含 PrimaryBehavior（BehaviorBase?，null=空闲）和 SecondaryBehaviors（List\<BehaviorBase\>，V1 预留结构）。

#### Scenario: 空闲状态
- **WHEN** NpcBehaviorData.PrimaryBehavior 为 null
- **THEN** 该 NPC SHALL 被视为空闲状态，可以使用 BehaviorCard

#### Scenario: 主行为占用
- **WHEN** NpcBehaviorData.PrimaryBehavior 不为 null
- **THEN** 该 NPC SHALL 被视为忙碌状态，不可使用 BehaviorCard

#### Scenario: Log 输出
- **WHEN** NpcBehaviorData.Log() 被调用
- **THEN** SHALL 输出当前主行为 BehaviorId（或"空闲"）、ElapsedTime/Duration、次要行为数量

### Requirement: NpcSystemBehavior 子系统
系统 SHALL 提供 `NpcSystemBehavior`，继承 `NpcSystemBase<NpcBehaviorData>`，位于 `Domain/Object/Npc/Systems/`，负责 NPC 行为的注册、添加、打断、Tick 推进和查询。

#### Scenario: 注册时无默认行为
- **WHEN** NpcSystemBehavior.Register(npc, data) 被调用
- **THEN** NpcBehaviorData.PrimaryBehavior SHALL 为 null（空闲状态）

#### Scenario: OnTick 推进行为时间
- **WHEN** NpcSystemBehavior.OnTick(npc, dt) 被调用，且 NPC 有主行为
- **THEN** 系统 SHALL 调用主行为的 OnTick(dt)，推进 ElapsedTime，并结算 Story

#### Scenario: OnTick 主行为自然结束
- **WHEN** 主行为 IsFinished 为 true
- **THEN** 系统 SHALL 调用主行为的 OnEnd()，然后将 PrimaryBehavior 设为 null

#### Scenario: OnTick 次要行为推进
- **WHEN** NPC 有次要行为
- **THEN** 系统 SHALL 遍历次要行为列表，逐个调用 OnTick(dt)，移除 IsFinished 的（调用 OnEnd）

### Requirement: NpcSystemBehavior 主行为管理 API
系统 SHALL 提供以下主行为管理方法：

- `AddPrimary(npcId, BehaviorBase)` — 设置主行为（仅当当前为空闲时）
- `InterruptPrimary(npcId)` — 打断当前主行为（调用 OnInterrupt 后置 null）
- `GetPrimary(npcId)` — 获取当前主行为（null=空闲）
- `IsIdle(npcId)` — 主行为是否为 null

#### Scenario: 空闲时添加主行为
- **WHEN** 调用 AddPrimary(npcId, behavior)，且当前 PrimaryBehavior 为 null
- **THEN** 系统 SHALL 设置 PrimaryBehavior 为传入的 behavior，并调用 behavior.OnStart()

#### Scenario: 忙碌时添加主行为失败
- **WHEN** 调用 AddPrimary(npcId, behavior)，且当前 PrimaryBehavior 不为 null
- **THEN** 系统 SHALL 返回 false，不替换当前行为，打印警告日志

#### Scenario: 打断主行为
- **WHEN** 调用 InterruptPrimary(npcId)，且当前有主行为
- **THEN** 系统 SHALL 调用当前主行为的 OnInterrupt()，然后将 PrimaryBehavior 设为 null

### Requirement: NpcSystemBehavior 官方便捷查询 API
系统 SHALL 提供以下便捷查询方法，基于主行为的类型判断：

- `IsMoving(npcId)` — 主行为是否为 MoveBehavior
- `IsPracticing(npcId)` — 主行为是否为 PracticeBehavior
- `IsExploring(npcId)` — 主行为是否为 ExploreBehavior
- `IsSocializing(npcId)` — 主行为是否为 SocialBehavior
- `IsInBehavior(npcId, behaviorId)` — 主行为的 BehaviorId 是否匹配

#### Scenario: IsMoving 判断
- **WHEN** 调用 IsMoving(npcId)，且主行为是 MoveBehavior 实例
- **THEN** SHALL 返回 true

#### Scenario: IsInBehavior 通用判断
- **WHEN** 调用 IsInBehavior(npcId, "fire_meditation")，且主行为 BehaviorId=="fire_meditation"
- **THEN** SHALL 返回 true

#### Scenario: 空闲时所有查询返回 false
- **WHEN** NPC 空闲（PrimaryBehavior==null）
- **THEN** IsMoving/IsPracticing/IsExploring/IsSocializing/IsInBehavior SHALL 全部返回 false

### Requirement: NpcSystemBehavior 次要行为管理 API（V1 预留）
系统 SHALL 提供以下次要行为管理方法（V1 结构预留，具体逻辑 TODO）：

- `AddSecondary(npcId, BehaviorBase)` — 添加次要行为
- `RemoveSecondary(npcId, behaviorId)` — 移除次要行为
- `GetSecondaries(npcId)` — 获取次要行为列表

#### Scenario: 添加次要行为
- **WHEN** 调用 AddSecondary(npcId, behavior)
- **THEN** 系统 SHALL 将 behavior 加入 SecondaryBehaviors 列表，并调用 behavior.OnStart()

### Requirement: Npc partial class 便捷访问器
系统 SHALL 在 NpcBehaviorData.cs 中提供 `partial class Npc` 的便捷访问器，包括 BehaviorData 属性引用。

#### Scenario: 通过 Npc 访问行为数据
- **WHEN** 访问 npc.BehaviorData
- **THEN** SHALL 返回该 NPC 的 NpcBehaviorData 实例

### Requirement: NpcMgr 注册 NpcSystemBehavior
系统 SHALL 在 NpcMgr 中注册 NpcSystemBehavior 子系统，并在 NPC 创建时调用 Register 注册行为数据，在 Tick 中驱动行为系统。

#### Scenario: NPC 创建时注册行为数据
- **WHEN** NpcMgr.Create() 创建新 NPC
- **THEN** 系统 SHALL 调用 NpcSystemBehavior.Register(npc, new NpcBehaviorData())

#### Scenario: Tick 驱动行为系统
- **WHEN** NpcMgr.Tick(dt) 被调用
- **THEN** 系统 SHALL 对每个存活 NPC 调用 NpcSystemBehavior.OnTick(npc, dt)