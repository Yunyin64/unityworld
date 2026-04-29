## 1. 枚举与数据结构基础

- [x] 1.1 在 `EnumTypes.cs` 中添加 `BehaviorStoryTrigger` 枚举
- [x] 1.2 创建 `Domain/GamePlay/Behavior/BehaviorStoryEntry.cs`——Story 触发规则数据结构

## 2. BehaviorBase 基类与子类

- [x] 2.1 创建 `Domain/GamePlay/Behavior/BehaviorBase.cs`——行为抽象基类
- [x] 2.2 创建 `Domain/GamePlay/Behavior/MoveBehavior.cs`——移动行为子类
- [x] 2.3 创建 `Domain/GamePlay/Behavior/PracticeBehavior.cs`——闭关修炼行为子类
- [x] 2.4 创建 `Domain/GamePlay/Behavior/ExploreBehavior.cs`——探索行为子类
- [x] 2.5 创建 `Domain/GamePlay/Behavior/SocialBehavior.cs`——社交行为子类
- [x] 2.6 创建 `Domain/GamePlay/Behavior/ExtraBehavior.cs`——通用拓展行为类
- [x] 2.7 创建 `Domain/GamePlay/Behavior/BehaviorFactory.cs`——工厂方法

## 3. ExtraBehaviorDefine 数据层

- [x] 3.1 创建 `Data/Defines/ExtraBehaviorDefine.cs`——行为拓展定义
- [x] 3.2 创建 `Data/Mgr/ExtraBehaviorDefineMgr.cs`——行为拓展定义加载器
- [x] 3.3 在 `GameDataMgr` 构造函数中注册 ExtraBehaviorDefineMgr
- [x] 3.4 创建空 JSON 数据模板文件 `ExtraBehaviorDefines.json`

## 4. NPC 行为运行时数据与子系统

- [x] 4.1 创建 `Domain/Object/Npc/Data/NpcBehaviorData.cs`——NPC 行为运行时数据
- [x] 4.2 创建 `Domain/Object/Npc/Systems/NpcSystemBehavior.cs`——NPC 行为子系统

## 5. BehaviorCardDefine 重构

- [x] 5.1 修改 `BehaviorCardDefine.cs`——新增 BehaviorId/BehaviorDuration/BehaviorIsPrimary 字段，将 StoryIds/StoryTags 拆分为 OnStart/OnEnd/OnInterrupt/OnTick/OnTimer 各自的 StoryIds 和 StoryTags 字段，添加 StoryTickEntry/StoryTimerEntry 等嵌套数据类
- [x] 5.2 更新现有 BehaviorCard JSON 数据文件——迁移 StoryIds → OnStartStoryIds，StoryTags → OnStartStoryTags（旧 JSON 通过自动迁移 setter 处理）

## 6. BehaviorCardMgr 重构

- [x] 6.1 修改 `BehaviorCardMgr.UseCard`——增加空闲检查（NpcSystemBehavior.IsIdle）、根据 BehaviorId 创建 Behavior 实例、将 Define 的 Story 字段转换为 BehaviorStoryEntry 列表、调用 NpcSystemBehavior.AddPrimary 塞入行为槽、处理 IsConsumable

## 7. NpcMgr 集成

- [x] 7.1 在 `NpcMgr` 中注册 NpcSystemBehavior 子系统实例
- [x] 7.2 在 `NpcMgr.Create()` 中调用 NpcSystemBehavior.Register 注册行为数据
- [x] 7.3 确认 NpcMgr.Tick 中对每个存活 NPC 调用 NpcSystemBehavior.OnTick
