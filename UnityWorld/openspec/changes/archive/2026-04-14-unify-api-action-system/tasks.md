## 1. 基础设施：Attribute + ActionContext

- [x] 1.1 修正 `APIFuncAttribute.cs`：改为编译期兼容的设计，构造参数 `(string funcName, string desc = "")`，添加 namespace 和 XML 注释
- [x] 1.2 新建 `ActionContext.cs`（放在 `Scripts/Game/Domain/!Global/API/`）：持有 `ActionData Action` + `ContextBase Env`，提供 `Get<T>(key)` 快捷方法和 `Rng?` 属性

## 2. APIMgr 升级：执行能力

- [x] 2.1 APIMgr 新增 `_handlers` 字典 `Dictionary<string, Action<ActionContext>>`
- [x] 2.2 APIMgr 新增 `ScanHandlers()` 方法：反射扫描当前 Assembly 中所有带 `[APIFunc]` 的静态方法，校验签名为 `static void Xxx(ActionContext ctx)`，注册到 `_handlers`，含重复/签名不匹配的 Warning 日志
- [x] 2.3 APIMgr 新增 `Execute(string funcName, ActionContext ctx)` 方法：查 `_handlers` 调用委托，未注册时 Warning 跳过，异常时 catch + Warning
- [x] 2.4 APIMgr.Init() 中在 `RegisterBuiltinAPIs()` 后调用 `ScanHandlers()`
- [x] 2.5 APIMgr.End() 中清理 `_handlers`
- [x] 2.6 APIMgr.Log() 中补充输出已注册 Handler 数量

## 3. Story 函数签名注册

- [x] 3.1 在 APIMgr.RegisterBuiltinAPIs() 中为 10 个 Story 函数补充 API 签名定义：GiveTrait(int:String, TraitId:String)、RemoveTrait、GiveBehaviorCard、ModifyAura、ModifyStat、TriggerStory、TriggerStoryByTag、AddToFatePool、AddToKarmaPool、TriggerEvent

## 4. StoryBaseFunc：大世界 Handler 实现

- [x] 4.1 重写 `StoryBaseFunc.cs`：将 StoryEffectFunc 中的 10 个 ExecXxx 方法迁移过来，每个方法以 `[APIFunc("xxx")]` 标记，方法签名为 `static void Xxx(ActionContext ctx)`，内部从 ctx.Action / ctx.Env 取参数

## 5. CombatBaseFunc：战斗域 Handler 实现

- [x] 5.1 重写 `CombatBaseFunc.cs`：实现 `[APIFunc("Heal")]` 和 `[APIFunc("SelfDamage")]` 两个战斗 Handler，从 ctx.Get\<CombatNpc\>("Caster") 取主体执行操作

## 6. StoryEffectFunc 改为转发层

- [x] 6.1 重写 StoryEffectFunc.Execute()：将 StoryEffectEntry 的 {funcName, args} 通过 APIMgr.ParseToContext 解析，构造 ActionContext 附加 StoryContext 环境信息，转发给 APIMgr.Execute
- [x] 6.2 删除 StoryEffectFunc 中的全部 ExecXxx 私有方法和 _registry 字典及静态构造函数中的 Register 调用
- [x] 6.3 保留 ExecuteAll 方法（内部循环调用新 Execute）

## 7. 战斗侧接入

- [x] 7.1 修改 CombatCardFlowHandler.ResolveEffectCard：遍历效果卡的 EffectData（筛选 TriggerId 为 OnUse 或空的），对每个非拼点 Action 构造 ActionContext（Caster + Target + Scene），调用 APIMgr.Execute

## 8. 验证与清理

- [x] 8.1 确认编译通过，无 namespace 遗漏
- [x] 8.2 确认 APIMgr.Log() 输出能显示全部已注册 API 签名 + Handler 数量
