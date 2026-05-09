## Context

CombatCard 已有完整的 Lua 驱动生命周期：`InitializeLuaCards()` 加载脚本 → `CallLuaHook()` 在 PreStart/Start/Tick/End 调用 Lua 函数。CombatNpcModifier 虽实现了 `ILuaBindable`（有 `env` 属性），但从未被赋值或使用。战斗 Buff 目前只是挂在 `CombatNpc.Buffs` 列表上的死数据，没有 Tick 衰减、没有行为逻辑。

现有代码约束：
- LuaMgr 共享单一 `Lua _luaState`，所有 DoFile 在同一全局环境中执行
- CombatNpcModifier.CreateModifier() 是纯数据工厂，不涉及 Lua
- CombatNpc 使用 partial class 拆分（CombatNpc.cs / CombatNpcFunc.cs / CombatNpcData.cs / CombatNpcManaFunc.cs）

## Goals / Non-Goals

**Goals:**
- CombatNpcModifier 支持独立 Lua env，生命周期 Hook：OnApply / OnTick / OnStack / OnRemove
- AddBuff 支持同 ID 叠层（CurrentStack + MaxStack + RefreshOnStack 逻辑）
- BuffTick 每战斗 Tick 遍历所有 Buff，调用 OnTick，衰减 Duration，移除过期 Buff
- Lua 脚本按约定路径加载：DefineId → `CombatModifiers/{DefineId}.lua`
- 每个 Modifier 实例独立 env，不缓存，不共享状态

**Non-Goals:**
- 不做 NpcModifier（大世界 Buff）的 Lua 化
- 不做 CardModifier / TileModifier 的 Lua 化
- 不做 Buff 的 EventMgr 事件触发（Trigger 体系）
- 不做 Buff UI / 视觉效果

## Decisions

### 1. Lua 加载策略：每实例独立 DoFile，不缓存

**选择**：每次 AddBuff 都调用 `LuaMgr.DoFile()` 获取全新 LuaTable 赋给 `modifier.env`。

**替代方案**：预注册缓存（像 Keyword 那样启动时全量加载）。

**理由**：不同 NPC 的同 ID Buff 必须独立运行（各自有不同的 CurrentStack、计时器、Lua 局部变量）。缓存 LuaTable 会导致状态共享，破坏独立性。按需加载也避免了加载不需要的脚本。

### 2. 脚本路径：DefineId 即文件名，无额外字段

**选择**：`DefineId = "Burn"` → `Data/LuaScripts/CombatModifiers/Burn.lua`。

**替代方案**：Define 中加 LuaScriptPath 字段显式声明路径。

**理由**：简单，零配置。CardDefine 的 LuaScriptPath 是历史设计，新系统直接走约定，符合项目"没有就是同名"的惯例。Lua 文件不存在时静默跳过（纯数值 Buff 不需要脚本）。

### 3. 叠层逻辑在 C# 侧完成，Lua 只收通知

**选择**：C# 负责判断同 DefineId Buff 是否已存在、CurrentStack 累加、MaxStack 上限、RefreshOnStack 刷新。叠层完成后调用已有 Buff 的 `env.OnStack(mod, npc)`。

**理由**：叠层是通用机制，不应该每个 Lua 脚本都重写一遍。Lua 的 OnStack hook 用于叠层时的附加效果（如叠满触发特殊事件）。

### 4. Buff Tick 驱动位置：CombatNpcBuffFunc.cs partial

**选择**：新增 `CombatNpcBuffFunc.cs` 作为 `CombatNpc` 的 partial class，包含所有 Buff 管理逻辑。CombatNpc.Tick() 中调用 `BuffTick()`。

**理由**：遵循现有 partial class 拆分模式（ManaFunc、Func、Data 已有先例），保持单一职责。

### 5. Hook 参数：(mod, npc)

**选择**：Lua Hook 签名为 `function OnXxx(mod, npc)`，其中 mod 是 CombatNpcModifier 实例，npc 是宿主 CombatNpc。

**理由**：Modifier 需要访问自身状态（CurrentStack、RemainingTime）和宿主（Stats、Hp、其他 Buff）。与 CombatCard 的 `(card, ctx)` 模式类似但更简单——Modifier 不需要 APIContext 的完整上下文。

### 6. 没有 Lua 脚本的 Buff 正常工作

**选择**：如果 `CombatModifiers/{DefineId}.lua` 不存在，env 为 null，所有 Hook 调用静默跳过。纯数值 Buff（只靠 StatModifiers）不需要 Lua 脚本即可工作。

**理由**：向后兼容。很多简单 Buff（+10% 攻击力）只需要 StatModifiers 数据，不需要每 Tick 执行逻辑。

## Risks / Trade-offs

- **[性能] 每次 AddBuff 都 DoFile** → 如果战斗中频繁叠加/移除同一 Buff（如每 Tick 刷燃烧），DoFile 开销可能累积。缓解：战斗 Buff 种类有限，单场战斗 AddBuff 频率不高。如果未来成为瓶颈，可在 LuaMgr 层加 Script Template 缓存（缓存编译结果，不缓存实例）。
- **[过期移除中的集合修改]** → BuffTick 遍历时不直接移除，用 toRemove 临时列表收集后批量移除（遵循项目规则）。
- **[Lua 异常不应崩溃战斗]** → 所有 Hook 调用包在 try-catch 中，异常只打日志不中断。
