## Context

NPC 当前只有瞬时的 BehaviorCard → Story 触发链路，缺少持续性行为状态。游戏世界需要知道 NPC "正在做什么"——闭关、移动、探索等——才能驱动 AI 决策、UI 展示和其他系统的交互。

现有结构：
- `BehaviorCardDefine`：静态定义，持有 Tags/StoryIds/StoryTags/IsConsumable
- `BehaviorCard`：运行时实例，DefineId + OwnerId + UsageCount
- `BehaviorCardMgr`：管理器，GiveCard/RemoveCard/GetCards/UseCard（直接触发 Story）
- `NpcSystemBase<T>`：NPC 子系统泛型基类，提供 _dataTable + Register + OnTick 模式
- `StoryMgr`：统一 Story 触发入口

## Goals / Non-Goals

**Goals:**
- NPC 拥有"主行为槽 + 次要行为列表"的运行时状态模型
- 主行为：唯一、CanMove=false、有持续时间、可被打断
- 次要行为：可多个、CanMove=true、预留结构，V1 不做具体逻辑
- BehaviorCard 使用后创建 Behavior 实例并塞入行为槽，Story 由 Behavior 生命周期各时机结算
- 提供官方行为子类（Move/Practice/Explore/Social）和便捷 API（IsMoving/IsPracticing 等）
- 提供 ExtraBehavior + ExtraBehaviorDefine 数据驱动的行为拓展机制
- AI 选卡逻辑预留 TODO，V1 使用随机选择

**Non-Goals:**
- 不实现 AI 决策模型（未来单独做）
- 不实现次要行为的具体逻辑（预留结构）
- 不实现寻路系统（MoveBehavior 只记录"我在移动"，不决定去哪）
- 不实现行为的前置条件判断（条件在 BehaviorCard/Story 侧处理）

## Decisions

### D1: 行为槽模型——主行为 + 次要行为列表

**选择**: NpcBehaviorData 持有 `PrimaryBehavior?`（nullable，null=空闲）+ `List<BehaviorBase> SecondaryBehaviors`。

**理由**: 主行为 null 表示空闲，语义直接，不需要"空闲行为实例"占位。次要行为是列表，未来可扩展。

**备选方案**: 始终用一个 NoneBehavior 占位主行为——被否决，因为设计上"空闲不是行为"。

### D2: CanMove 规则——主行为永远 false

**选择**: 所有主行为 CanMove = false。CanMove = true 专属次要行为。

**理由**: 主行为代表 NPC 的核心活动状态，做主行为时不应该移动。移动本身是一种主行为（MoveBehavior），不是"边做边走"。

### D3: Behavior 不持有 OwnerId

**选择**: BehaviorBase 不存储持有者 ID。

**理由**: 不存在"从行为反推 NPC"的需求。NpcSystemBehavior 的 _dataTable 已维护 npcId → NpcBehaviorData 的映射。

### D4: Behavior 不持有条件/不自定义持续时间

**选择**: Duration 必须外部传入（BehaviorCardDefine.BehaviorDuration），Behavior 自身不定义时长。条件判断由 BehaviorCard 和 Story 侧负责。

**理由**: Behavior 是"我在干什么"的纯状态容器，不是决策器。条件和时长都是"我要干什么"（BehaviorCard）的范畴。

### D5: Story 触发时机按字段分拆（方案 A）

**选择**: BehaviorCardDefine 用分字段方式描述各时机的 Story，如 `OnStartStoryIds`、`OnStartStoryTags`、`OnEndStoryIds`、`OnEndStoryTags`... 共 10 个字段（5 个时机 × Ids/Tags）。

**理由**: 策划配置最直观，JSON 一眼可读。字段多不是问题，Define 无所谓大小。

**备选方案**: 用 List<BehaviorStoryEntry> 统一结构——JSON 配置需要嵌套对象写 Trigger 枚举，不够直观。

**内部转换**: 构造 Behavior 实例时，将 BehaviorCardDefine 的各字段统一转换为内部的 `List<BehaviorStoryEntry>`，BehaviorBase 的结算引擎只遍历一个列表。

### D6: 官方行为子类 vs ExtraBehavior 的定位

**选择**: Move/Practice/Explore/Social 是代码定义的官方子类，ExtraBehavior 是数据驱动的通用拓展。

**理由**:
- 官方子类提供类型安全的 `is` 判断（`primary is MoveBehavior`）和便捷 API
- 官方子类是游戏核心骨架，游戏跑起来必须有的
- ExtraBehavior 是可选的拓展，不需要写代码就能通过 JSON 创建新行为类型
- V1 阶段官方子类 OnTick 逻辑可能为空，但未来可在子类中添加特有逻辑

### D7: 行为被打断时广播事件

**选择**: OnInterrupt() 结算 OnInterrupt Story 后，通过 EventMgr 广播一个 "BehaviorInterrupted" 事件。

**理由**: 其他系统（如 UI、叙事）可能需要感知行为被打断。通过事件解耦，不需要硬编码依赖。

### D8: Behavior 创建工厂——根据 BehaviorId 匹配

**选择**: BehaviorCardMgr 或 NpcSystemBehavior 内部根据 BehaviorId 字符串匹配创建对应子类实例。官方 ID（"Move"/"Practice"/"Explore"/"Social"）创建对应子类，其他 ID 创建 ExtraBehavior。

**理由**: 简单直接。未来如果行为类型增多，可以改用注册表模式。

## Risks / Trade-offs

- **[BehaviorCardDefine 字段膨胀]** → 10 个 Story 字段可能看起来多，但每个都是可选的（默认空列表），JSON 只写需要的字段。序列化框架会自动忽略空值。
- **[Story 结算依赖 StoryMgr 已实现]** → BehaviorBase 的 Story 结算需要调用 StoryMgr.TriggerStory / TriggerStoryByTags。如果这些方法签名未来变化，BehaviorBase 需要同步更新。→ 通过接口隔离降低风险。
- **[次要行为 V1 预留但不实现]** → 结构存在但逻辑为空，可能造成"代码没用到"的观感。→ 这是有意的，次要行为的具体逻辑等 ExtraBehaviorDefine 数据就位后再实现。