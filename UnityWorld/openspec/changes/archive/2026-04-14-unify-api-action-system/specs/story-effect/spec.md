## MODIFIED Requirements

### Requirement: StoryEffectFunc 原子效果注册表（简单轨）
系统 SHALL 保留 `StoryEffectFunc` 类及其 `Execute/ExecuteAll` 公开方法签名不变，但内部实现 SHALL 改为转发层：
1. 将 StoryEffectEntry 的 `{FuncName, Args}` 通过 `APIMgr.ParseToContext` 解析为命名参数的 ContextBase
2. 构造 ActionContext，附加 StoryContext 中的环境信息（Subject → Env["Subject"]，Rng → Env["Rng"]，CurrentTime → Env["Time"]，StoryId → Env["StoryId"]，SourcePool → Env["SourcePool"]）
3. 调用 `APIMgr.Execute(funcName, actionCtx)` 转发执行

原有的 10 个 `ExecXxx` 私有方法 SHALL 全部迁移到 `StoryBaseFunc` 静态类中，以 `[APIFunc]` Attribute 标记，函数签名改为 `static void Xxx(ActionContext ctx)`。

内置迁移的函数列表（保持功能不变）：
- `GiveTrait(int:Int, TraitId:String)`：给 NPC 添加 Trait
- `RemoveTrait(int:Int, TraitId:String)`：移除 NPC 的 Trait
- `GiveBehaviorCard(int:Int, CardDefineId:String)`：给 NPC 添加行为卡
- `ModifyAura(PlaneId:Int, Element:String, Delta:Float)`：修改地块五行浓度
- `TriggerStory(StoryId:String, SubjectId:Int)`：链式触发另一个 Story
- `TriggerStoryByTag(Tags:String)`：按 Tag 匹配触发 Story（多 Tag 用逗号分隔）
- `AddToFatePool(SubjectId:Int, Time:Float, StoryId:String)`：向宿命池写入条目
- `AddToKarmaPool(SubjectId:Int, StoryId:String, Weight:Float)`：向劫缘池写入条目
- `ModifyStat(int:Int, StatId:String, Delta:Float)`：修改 NPC 属性值
- `TriggerEvent(EventName:String)`：通过 EventMgr 广播事件

#### Scenario: 链式触发 Story
- **WHEN** 某 StoryEffectFunc 调用 TriggerStory(storyId, subjectId)
- **THEN** 系统 SHALL 通过 StoryMgr.TriggerStory 触发目标 Story，形成嵌套链式触发

#### Scenario: 未知 FuncName 时警告不崩溃
- **WHEN** Effects 中包含注册表中不存在的 FuncName
- **THEN** 系统 SHALL 打印 Warning 日志并跳过，不抛出异常（由 APIMgr.Execute 内部处理）

#### Scenario: StoryMgr 调用方无感知
- **WHEN** StoryMgr 调用 StoryEffectFunc.ExecuteAll(effects, storyCtx)
- **THEN** 调用方代码 SHALL 无需任何修改，StoryEffectFunc 内部自动转发到 APIMgr

### Requirement: Story 函数的 API 签名注册
系统 SHALL 在 APIMgr.RegisterBuiltinAPIs() 中为全部 10 个 Story 函数补充 API 签名定义（参数名+类型），使其参数可通过 `APIMgr.ParseToContext` 从 `List<string>` 解析为命名参数的 ContextBase。

#### Scenario: GiveTrait 参数解析
- **WHEN** StoryEffectEntry 为 `{funcName: "GiveTrait", args: ["42", "brave"]}`
- **THEN** APIMgr.ParseToContext("GiveTrait", args) SHALL 返回 ContextBase 包含 {int="42", TraitId="brave"}

#### Scenario: 未注册签名时降级处理
- **WHEN** StoryEffectEntry 的 funcName 未在 APIMgr 中注册签名
- **THEN** StoryEffectFunc SHALL 构造一个仅含环境信息的 ActionContext（Action 参数为空），并尝试调用 APIMgr.Execute，由 Handler 自行处理
