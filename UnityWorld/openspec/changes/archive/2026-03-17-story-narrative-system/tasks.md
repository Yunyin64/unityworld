## 1. 静态数据层（Define + DefineMgr）

- [x] 1.1 创建 `Scripts/Game/Data/Defines/StoryBaseDefine.cs`：继承 DefineBase，含 Tags、Conditions(List\<StoryCondition\>)、Effects(List\<StoryEffectEntry\>)、LuaScript 字段
- [x] 1.2 创建 `Scripts/Game/Data/Defines/StoryDefine.cs`：继承 StoryBaseDefine，含 IsHide、Title、Text、OptionIds 字段
- [x] 1.3 创建 `Scripts/Game/Data/Defines/OptionDefine.cs`：继承 StoryBaseDefine，含 Text、StoryIds 字段
- [x] 1.4 创建 `Scripts/Game/Data/Defines/BehaviorCardDefine.cs`：继承 DefineBase，含 Tags、StoryIds、StoryTags、IsConsumable 字段
- [x] 1.5 创建 `Scripts/Game/Data/Mgr/StoryDefineMgr.cs`：实现 IDataMgrBase\<StoryDefine\>，加载 JSON，Begin() 中构建双向 Option 合并表，孤立引用打 Warning
- [x] 1.6 创建 `Scripts/Game/Data/Mgr/OptionDefineMgr.cs`：实现 IDataMgrBase\<OptionDefine\>，加载 JSON
- [x] 1.7 创建 `Scripts/Game/Data/Mgr/BehaviorCardDataMgr.cs`：实现 IDataMgrBase\<BehaviorCardDefine\>，加载 JSON
- [x] 1.8 在 `GameDataMgr` 构造函数中注册 StoryDefineMgr、OptionDefineMgr、BehaviorCardDataMgr

## 2. 条件与效果执行层

- [x] 2.1 创建 `Scripts/Game/Domain/Story/StoryCondition.cs`：含 TargetType(枚举)、FieldName、Operator(枚举)、Value 字段，提供 `bool Evaluate(StoryContext ctx)` 方法
- [x] 2.2 在 `Scripts/Game/Domain/Enum/EnumTypes.cs` 中添加 `StoryConditionTargetType` 枚举（NpcStat、NpcTag、AuraElement、WorldTime、Relation 等）和 `StoryConditionOperator` 枚举（GreaterThan、LessThan、Equal、Contains 等）
- [x] 2.3 创建 `Scripts/Game/Domain/Story/StoryContext.cs`：含 Subject(object)、SourcePool(枚举)、CurrentTime(float)、Rng 实例
- [x] 2.4 创建 `Scripts/Game/Domain/Story/StoryEffectEntry.cs`：含 FuncName(string)、Args(List\<string\>) 字段，对应配置中的 Effects 条目
- [x] 2.5 创建 `Scripts/Game/Domain/Story/StoryEffectFunc.cs`：静态类，维护 `Dictionary<string, Action<StoryContext, List<string>>>` 注册表，内置所有原子效果函数（GiveTrait/RemoveTrait/GiveBehaviorCard/ModifyAura/TriggerStory/TriggerStoryByTag/AddToFatePool/AddToKarmaPool/ModifyStat/EmitEvent），提供 `Register` 和 `Execute` 方法

## 3. 三池数据结构

- [x] 3.1 创建 `Scripts/Game/Domain/Story/FatePool.cs`：内部为 `SortedDictionary<float, List<string>>`，提供 Add、Tick(currentTime) 方法，到时触发后移除条目
- [x] 3.2 创建 `Scripts/Game/Domain/Story/KarmaEntry.cs`：含 StoryId、Weight、Conditions 字段
- [x] 3.3 创建 `Scripts/Game/Domain/Story/KarmaPool.cs`：内部为 `List<KarmaEntry>`，提供 Add、TryTrigger(rng, ctx) 方法，按 Weight 加权随机，无满足条件时静默跳过
- [x] 3.4 在 `Scripts/Game/Domain/Enum/EnumTypes.cs` 中添加 `StoryPoolSource` 枚举（Fate、Karma、Will）

## 4. StoryMgr 运行时管理器

- [x] 4.1 创建 `Scripts/Game/Domain/Story/StoryMgr.cs`：实现 IDomainMgrBase，单例，维护所有主体的 FatePool 和 KarmaPool 字典，提供 `TriggerStory(storyId, subject)` 统一入口
- [x] 4.2 在 StoryMgr.Tick(dt) 中推进所有主体的 FatePool 时间检查
- [x] 4.3 在 StoryMgr.Tick(dt) 中驱动所有主体的 KarmaPool 周期触发（周期可配置）
- [x] 4.4 在 TriggerStory 中：检查 Conditions → 执行 Effects → 若 IsHide=false 则通过 EventMgr 广播显示事件（UI层监听）
- [x] 4.5 在 `WorldMgr.Initialize()` 中注册 StoryMgr

## 5. BehaviorCard 系统

- [x] 5.1 创建 `Scripts/Game/Domain/BehaviorCard/BehaviorCard.cs`：运行时实例类，含 DefineId、OwnerId(int)、UsageCount 字段
- [x] 5.2 创建 `Scripts/Game/Domain/BehaviorCard/BehaviorCardMgr.cs`：实现 IDomainMgrBase，单例，维护 `Dictionary<int, List<BehaviorCard>>`，提供 GiveCard、RemoveCard、GetCards、UseCard 方法
- [x] 5.3 UseCard 方法中：解析 BehaviorCardDefine → 优先走 StoryIds / 否则 TagBag 匹配 → 调用 StoryMgr.TriggerStory → 若 IsConsumable 则移除实例
- [x] 5.4 在 `WorldMgr.Initialize()` 中注册 BehaviorCardMgr

## 6. 文档

- [x] 6.1 创建 `Docs/叙事设计.txt`：记录天地人三池设计、StoryBaseDefine 继承体系、双向持有规则、双轨执行架构、BehaviorCard 系统的完整设计思路
