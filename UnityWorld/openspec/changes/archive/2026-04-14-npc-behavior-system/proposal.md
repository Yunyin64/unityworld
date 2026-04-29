## Why

NPC 当前缺少"正在做什么"的运行时状态模型。BehaviorCard 只能触发瞬时的 Story，无法表达"闭关修炼 300 tick"、"正在移动中"等持续行为。游戏要跑起来，NPC 需要一个行为槽模型：主行为（唯一，不可移动）+ 次要行为列表（可多个，可移动），由 BehaviorCard 驱动塞入行为，Tick 推进时间并结算各时机的 Story。

## What Changes

- 新增 **BehaviorBase** 抽象基类及 Story 结算引擎，支持 OnStart/OnEnd/OnInterrupt/OnTick/OnTimer 五种 Story 触发时机
- 新增 **官方行为子类**：MoveBehavior、PracticeBehavior、ExploreBehavior、SocialBehavior（游戏核心骨架，提供便捷 API 如 `IsMoving`、`IsPracticing`）
- 新增 **ExtraBehavior** 通用拓展行为类，由 ExtraBehaviorDefine（JSON 数据驱动）创建任意行为变体
- 新增 **NpcBehaviorData** 运行时数据（PrimaryBehavior + SecondaryBehaviors 列表）
- 新增 **NpcSystemBehavior** 子系统，负责行为的添加/打断/Tick 推进/自然结束/便捷查询
- **重构 BehaviorCardDefine**：新增 BehaviorId、BehaviorDuration、BehaviorIsPrimary 字段；将 StoryIds/StoryTags 按时机拆分为 OnStart/OnEnd/OnInterrupt/OnTick/OnTimer 各自的 Ids 和 Tags 字段 **BREAKING**
- **重构 BehaviorCardMgr.UseCard**：从"直接触发 Story"改为"创建 Behavior 实例塞入行为槽，Story 由 Behavior 生命周期结算" **BREAKING**

## Capabilities

### New Capabilities
- `behavior-base`: 行为系统基础设施——BehaviorBase 基类、BehaviorStoryEntry 数据结构、BehaviorStoryTrigger 枚举、Story 结算引擎
- `npc-behavior`: NPC 行为子系统——NpcBehaviorData 运行时数据、NpcSystemBehavior 子系统（行为管理 + 便捷 API）
- `extra-behavior-define`: 行为拓展定义——ExtraBehaviorDefine + ExtraBehaviorDefineMgr，数据驱动的行为变体

### Modified Capabilities
- `action-card`: BehaviorCardDefine 新增 Behavior 关联字段 + Story 按时机分拆；BehaviorCardMgr.UseCard 改为创建 Behavior 并塞入行为槽

## Impact

- **Data/Defines/BehaviorCardDefine.cs**：字段结构变化（StoryIds/StoryTags 拆分为按时机的字段），现有 JSON 数据需同步更新
- **Domain/Object/BehaviorCard/BehaviorCardMgr.cs**：UseCard 流程重构，依赖 NpcSystemBehavior
- **Domain/Object/Npc/NpcMgr.cs**：注册 NpcSystemBehavior 子系统
- **Data/Mgr/GameDataMgr.cs**：注册 ExtraBehaviorDefineMgr
- **Data/Enum/EnumTypes.cs**：新增 BehaviorStoryTrigger 枚举
- **现有 BehaviorCard JSON 数据文件**：需迁移字段格式