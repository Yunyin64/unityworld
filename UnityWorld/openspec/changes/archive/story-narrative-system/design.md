## Context

游戏已有 NpcMgr、TileMgr、AuraDaoMgr、TagMgr 等领域管理器，拥有完整的 Tag/TagBag 匹配算法、Trait 系统和五行元气系统。当前缺少一套统一的叙事事件驱动层，NPC 和世界状态的变化无法以"故事"的形式呈现给玩家，也无法驱动 NPC 的自主行为决策。

叙事系统需要在不侵入现有领域层的前提下，作为一个独立的 `Domain/Story` 模块接入，并通过 EventMgr 与其他系统通信。

## Goals / Non-Goals

**Goals:**
- 实现天地人三池（宿命/劫缘/抉择）的调度与触发架构
- 实现 StoryBaseDefine → StoryDefine / OptionDefine 的双向持有数据结构
- 实现 StoryEffectFunc 原子效果集合（简单轨）+ LuaStory（复杂轨）双轨执行
- 实现 BehaviorCard 系统（行为卡定义、实例、管理器）
- 所有 Define 通过 JSON 配置加载，支持热更
- 创建 `Docs/叙事设计.txt` 作为设计文档

**Non-Goals:**
- UI 弹窗渲染（属于 UnityAdapter 层，本次不实现）
- NPC AI 选项决策算法（留空占位，后续专项实现）
- Lua 解释器的引入与集成（本次只定义接口，LuaStory 字段预留）
- 多人联机同步

## Decisions

### 决策1：StoryBaseDefine 作为公共基类

**选择**：`StoryDefine` 和 `OptionDefine` 共同继承 `StoryBaseDefine`，后者继承 `DefineBase`。

**理由**：Option 本质是"被选择时触发的迷你 Story"，两者共享 Tags/Conditions/Effects/LuaScript 结构。继承而非组合，避免重复字段定义，保持系统一致性。

**替代方案**：独立两个类，通过接口约束 → 维护成本高，逻辑重复。

---

### 决策2：OptionDefine 双向持有 StoryDefine

**选择**：`StoryDefine.OptionIds` 正向持有 + `OptionDefine.StoryIds` 反向持有，运行时由 `StoryDefineMgr` 在加载后合并构建完整 Option 列表。

**理由**：正向持有满足"这个事件我要定义哪些选项"；反向持有满足"这个选项我要注入到哪些事件"（例如：灾难类通用选项）。两者互补，配置灵活。

**合并规则**：触发 StoryDefine 时，最终 Options = StoryDefine.OptionIds + 所有声明了该 StoryId 的 OptionDefine。

---

### 决策3：三池用独立数据结构，统一触发接口

**选择**：
- `FatePool`：`SortedDictionary<float, List<string>>`（时间 → StoryId列表）
- `KarmaPool`：`List<KarmaEntry>`（StoryId + Weight + Conditions）
- `WillPool`：个体持有的 `List<BehaviorCard>` 实例（通过 BehaviorCardMgr 管理）

三池均通过 `StoryMgr.TriggerStory(storyId, subject)` 统一触发，屏蔽来源差异。

**理由**：三池数据结构差异较大（时间索引/权重随机/个体持有），但触发后逻辑完全一致，统一入口简化代码。

---

### 决策4：StoryEffectFunc 用静态注册表模式

**选择**：`StoryEffectFunc` 是一个静态类，内部维护 `Dictionary<string, Action<StoryContext>>` 注册表，支持运行时注册新函数。

**理由**：Lua 脚本可通过函数名字符串调用 C# 原子操作，无需反射，性能友好。配置文件中 Effects 字段存函数名字符串 + 参数列表，可读性高。

---

### 决策5：BehaviorCardDefine 双模式指向 Story

**选择**：`BehaviorCardDefine` 同时支持：
- `StoryIds: List<string>`（直接指定，确定性触发）
- `StoryTags: List<string>`（TagBag 匹配，动态涌现）

使用时优先走 StoryIds；若 StoryIds 为空则用 StoryTags 在 StoryPool 中动态匹配。

**理由**：给配置者两种粒度的控制。剧情关键节点用直接指定保证确定性；日常行为卡用 Tag 匹配实现丰富的随机涌现。

## Risks / Trade-offs

- **[风险] 双向持有一致性**：StoryDefine.OptionIds 和 OptionDefine.StoryIds 可能在配置时出现遗漏或冲突 → 缓解：StoryDefineMgr 加载时做校验日志，列出孤立 Option 和孤立 StoryId 引用
- **[风险] Conditions 判断覆盖不足**：当前 StoryCondition 支持的判断类型需要提前枚举完整，后期扩展成本较高 → 缓解：提供 LuaScript 复杂轨作为兜底，C# 简单轨覆盖最高频的 80% 场景
- **[风险] 宿命池时间精度**：游戏使用浮点时间，时间比较可能有精度误差 → 缓解：使用容差比较（epsilon = 0.01f），触发后从池中移除
- **[风险] Lua 未集成**：本次 LuaStory 字段仅预留，实际 Lua 执行能力延后 → 缓解：接口预留，字段存在但运行时遇到非空 LuaStory 时打 Warning 日志，不抛异常

## Open Questions

- StoryCondition 的条件类型枚举需要和 NpcMgr/AuraDaoMgr 共同确认支持哪些字段查询
- BehaviorCard 的"消耗"与"保留"逻辑：是否在 BehaviorCardDefine 上配置 `IsConsumable`？
- 宿命池的"时间"单位：使用游戏内 WorldTime 的哪个粒度（年/月/日/Tick）？
