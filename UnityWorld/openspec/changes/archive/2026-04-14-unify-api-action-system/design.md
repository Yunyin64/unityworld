## Context

当前项目中存在两套孤立的"函数名 → 执行"机制：

1. **APIMgr**（`Scripts/Game/Domain/!Global/API/APIMgr.cs`）：为 Card/Action 系统提供函数签名注册（参数名+类型）、参数解析（`ParseToContext`）和校验（`Validate`）。但只是"签名注册表"，**没有执行能力**。战斗中卡牌的非拼点 Action（Heal、SelfDamage 等）在 `CombatCardFlowHandler.ResolveEffectCard` 里只有占位日志，无法真正生效。

2. **StoryEffectFunc**（`Scripts/Game/Domain/Story/StoryEffectFunc.cs`）：为 Story 系统提供 10 个原子效果函数的注册和执行。采用 `Dictionary<string, Action<StoryContext, List<string>>>` 模式。有执行能力，但**没有参数签名定义**，参数以 `List<string>` 传入由各函数自行解析。

两者本质上做同一件事：注册函数 → 传入参数 → 执行效果。区别仅在于上下文不同（战斗 vs 大世界）和参数格式不同（ContextBase vs List\<string\>）。

## Goals / Non-Goals

**Goals:**
- APIMgr 升级为"签名 + 执行"一体的统一 API 注册表
- 通过 `[APIFunc]` Attribute 标记执行函数，反射扫描自动注册，函数声明即注册
- 提供统一的 `ActionContext`（基于 ContextBase），同时承载 Action 参数和环境信息（主体对象等）
- 战斗侧效果卡（非拼点卡）能通过 APIMgr.Execute 真正执行 Action
- StoryEffectFunc 的全部 10 个函数迁移到统一体系，StoryEffectFunc 变为薄转发层
- 不同域的函数用不同函数名（如战斗的 `Heal` 和大世界的 `HealInjury`），不需要 Domain 枚举区分

**Non-Goals:**
- 不做 Trigger/Condition 执行系统（本次只解决 Action 执行，Trigger/Condition 留后续）
- 不改变拼点类 Action（Attack/Shield/Block）在战斗中的特殊处理路径——它们仍走 ContestData → 待发槽 → 对拼
- 不改变 API 签名的手动注册方式（`RegisterBuiltinAPIs` 保留，Attribute 未来可扩展自动注册签名）
- 不改变 ActionDefine/ActionData/EffectData 等已有数据结构
- 不做 StoryEffectEntry 数据格式的变更（`{funcName, args}` JSON 格式保持不变）

## Decisions

### D1: 不引入 ActionDomain 枚举

**决定**：同一个 FuncName 全局唯一，不需要 `(FuncName, Domain)` 二元组做索引。不同域用不同函数名区分语义。

**理由**：战斗和大世界中"同名"函数的效果天差地别（如 Heal 在战斗里是恢复当前HP，在大世界是治疗伤势），本来就应该是不同的函数名。强行用 Domain 区分反而制造了"假统一"。

**替代方案**：引入 `ActionDomain.Combat/World` 枚举 → 否决，因为函数名天然区分。

### D2: ActionContext 复用 ContextBase

**决定**：新增 `ActionContext` 类，内部持有一个 `ContextBase` 存储环境信息（主体对象、Rng 等），同时持有 `ActionData` 引用提供参数访问。

**结构**：
```
ActionContext
├── ActionData Action      ← 参数（FuncName + Context，已有的 GetInt/GetFloat/GetString）
├── ContextBase Env        ← 环境（"Caster" → CombatNpc / "Subject" → Npc 等）
├── T Get<T>(key)          ← 快捷访问 Env
└── Rng? Rng               ← 快捷访问随机数
```

**理由**：ContextBase 已经是项目中成熟的 key-value 容器，无需发明新容器。ActionData 已有参数读取能力（GetInt/GetFloat/GetString）。ActionContext 组合两者即可。

**替代方案**：让所有函数直接接收 `ContextBase`（把 Action 参数和环境信息混在一起） → 否决，因为 Action 参数来自 JSON 定义而环境来自调用方，职责应分离。

### D3: Attribute 只标记函数名和描述

**决定**：`APIFuncAttribute` 构造参数为 `(string funcName, string desc = "")`，不传 Context 类型。

**理由**：C# Attribute 参数必须是编译期常量，不能 `new ContextBase()`。函数签名（参数名+类型）仍由 `RegisterBuiltinAPIs()` 手动注册，Attribute 只负责标记"这个方法是哪个 funcName 的执行函数"。

### D4: 反射扫描当前 Assembly 的静态方法

**决定**：`APIMgr.Init()` 中新增 `ScanHandlers()` 步骤，扫描当前 Assembly 中所有带 `[APIFunc]` 的**静态方法**，方法签名必须为 `static void Xxx(ActionContext ctx)`，按 FuncName 注册到 `_handlers` 字典。

**理由**：
- 静态方法无状态，Handler 不需要实例
- 当前项目单 Assembly，无需跨 Assembly 扫描
- 方法签名统一为 `Action<ActionContext>`，反射注册简洁

### D5: StoryEffectFunc 变为薄转发层

**决定**：保留 StoryEffectFunc 类及其 `Execute/ExecuteAll` 公开方法（不改变 StoryMgr 调用代码），但内部实现改为：
1. 将 StoryEffectEntry 的 `{funcName, args}` 通过 APIMgr.ParseToContext 解析为 ContextBase
2. 构造 ActionContext（附加 StoryContext 中的环境信息）
3. 转发给 APIMgr.Execute

原有的 10 个 `ExecXxx` 私有方法全部迁移到 `StoryBaseFunc.cs` 中，以 `[APIFunc]` 标记。需要同时在 `RegisterBuiltinAPIs()` 中为这 10 个函数补充签名定义。

**理由**：StoryMgr 只有一处调用 `StoryEffectFunc.ExecuteAll`，保留转发层最小化改动。

### D6: 拼点类 Action 不走 Execute

**决定**：Attack/Shield/Block 在战斗中不通过 APIMgr.Execute 执行，仍走原有的 ContestData → 待发槽 → 对拼路径。CombatBaseFunc 中可以注册它们的 `[APIFunc]` 标记（为未来扩展预留），但 CombatCardFlowHandler 在 ResolveEffectCard 中只对非拼点 Action 调用 Execute。

## Risks / Trade-offs

**[Risk] 反射扫描性能** → 只在 Init() 时扫描一次，运行时 Execute 是字典查找 O(1)，不影响战斗性能。

**[Risk] 函数名冲突** → 全局唯一，同名注册两次会覆盖。通过 ScanHandlers 中的重复检测 + Warning 日志防护。

**[Risk] StoryEffectEntry 的 List\<string\> 参数需要签名才能解析** → 需要为 Story 函数补充 API 签名定义（10 个函数的参数名+类型），这是新增工作量但也带来了参数校验能力。

**[Trade-off] 保留 StoryEffectFunc 转发层** → 多了一层间接，但避免了修改所有 Story 调用方代码。未来可直接删除转发层。
