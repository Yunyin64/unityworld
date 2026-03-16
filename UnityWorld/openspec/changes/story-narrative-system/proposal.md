## Why

游戏当前缺乏叙事驱动层。世界中的 NPC、门派、地区乃至世界本身缺乏一套统一的"事件触发与响应"机制，无法实现"世界在玩家来之前就已经在运转"的核心体验。叙事系统是设计哲学中"叙事证明创造"循环的基础，也是玩家从"使用世界"走向"书写世界"的前提。

## What Changes

- 新增 `StoryBaseDefine`：叙事基础数据结构，包含 Tags、Conditions、Effects、LuaScript
- 新增 `StoryDefine`：继承 StoryBaseDefine，增加 IsHide、Title、Text、OptionIds 等字段
- 新增 `OptionDefine`：继承 StoryBaseDefine，增加 Text、StoryIds（反向持有）字段
- 新增 `StoryMgr`：运行时故事管理器，负责三池（宿命/劫缘/抉择）的调度与触发
- 新增 `ActionCardDefine`：行为卡定义，持有 StoryDefineId 列表或 Tags（动态匹配）
- 新增 `ActionCard` 实例与 `ActionCardMgr`：个体持有的行为卡运行时管理
- 新增 `ActionCardDataMgr`：行为卡静态定义加载器
- 新增 `StoryDefineMgr`：故事定义加载器
- 新增 `OptionDefineMgr`：选项定义加载器
- 新增 `StoryEffectFunc`：封装好的原子效果函数集合（给 Lua 和简单轨共用）
- 新增 `StoryCondition`：条件判断结构（支持属性比较、Tag匹配、五行浓度等）

## Capabilities

### New Capabilities

- `story-define`：StoryBaseDefine / StoryDefine / OptionDefine 的数据结构定义与加载，包含双向持有关系
- `story-pool`：天地人三池（宿命池 FatePool、劫缘池 KarmaPool、抉择池 WillPool）的数据结构与调度逻辑
- `story-effect`：StoryEffectFunc 原子效果集合 + LuaStory 双轨执行架构
- `action-card`：ActionCardDefine / ActionCard 实例 / ActionCardMgr 的完整行为卡系统

### Modified Capabilities

（无，本次为全新系统）

## Impact

- **新增文件**：
  - `Scripts/Game/Data/Defines/StoryBaseDefine.cs`
  - `Scripts/Game/Data/Defines/StoryDefine.cs`
  - `Scripts/Game/Data/Defines/OptionDefine.cs`
  - `Scripts/Game/Data/Defines/ActionCardDefine.cs`
  - `Scripts/Game/Data/Mgr/StoryDefineMgr.cs`
  - `Scripts/Game/Data/Mgr/OptionDefineMgr.cs`
  - `Scripts/Game/Data/Mgr/ActionCardDataMgr.cs`
  - `Scripts/Game/Domain/Story/StoryMgr.cs`
  - `Scripts/Game/Domain/Story/StoryPool.cs`（FatePool / KarmaPool / WillPool）
  - `Scripts/Game/Domain/Story/StoryEffectFunc.cs`
  - `Scripts/Game/Domain/Story/StoryCondition.cs`
  - `Scripts/Game/Domain/ActionCard/ActionCard.cs`
  - `Scripts/Game/Domain/ActionCard/ActionCardMgr.cs`
- **修改文件**：
  - `Scripts/Game/Boot/WorldMgr.cs`：注册 StoryMgr、ActionCardMgr
  - `Scripts/Game/Data/GameDataMgr.cs`：注册 StoryDefineMgr、OptionDefineMgr、ActionCardDataMgr
- **依赖系统**：Tag系统（TagBag匹配）、NpcMgr、AuraDaoMgr、EventMgr、Rng
- **Lua 脚本支持**：需要引入 Lua 解释器（如 MoonSharp）用于 LuaStory 执行
- **新增文档**：`Docs/叙事设计.txt`
