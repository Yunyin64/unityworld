## Context

战斗系统中卡牌行为由 Lua 脚本定义。现有基础设施：
- `LuaMgr`：持有 NLua State，可加载脚本、注册 C# 函数
- `CombatCard`：实现 `ILuaBindable`，持有 `LuaTable env`
- `APIMgr`：通过 `[APIFunc]` 反射注册，提供 `Execute(funcName, ctx)` 执行
- `CombatBaseFunc`：已实现 Attack/Shield/Block/Heal 等静态方法

当前状态：链路各节点存在但未连通，脚本无法实际驱动战斗行为。

## Goals / Non-Goals

**Goals:**
- 跑通 Init.lua → 卡牌脚本加载 → OnContest/OnApply Lua 调用 → APIMgr.Execute 的完整管线
- 多卡实例互不污染
- Lua 脚本作者可用简洁语法调用战斗 API

**Non-Goals:**
- OnTick Lua 调用（后续）
- 被动 Hook 事件注册（后续）
- Keywords 系统（后续）
- CardData 覆写（后续）

## Decisions

### 1. Init.lua 全局注册方案

Lua 侧通过 `Init.lua` 定义全局包装函数（Attack、Shield 等），内部调 `APIMgr:Execute(funcName, ctx)`。

**理由：** 保持 APIMgr 作为唯一执行入口，所有校验/日志统一走 C# 管线。Lua 侧啰嗦一点但清晰可控。

### 2. ctx 传递方案：C# APIContext 直接传入 Lua

C# 侧构造 `APIContext { SourceCard=this, Caster=Owner, Scene=null }`，作为参数传给 Lua 函数。Lua 包装函数通过 `ctx:Set(key, value)` 填充参数后调 `API:Execute`。

**理由：** 走标准管线，类型安全，Lua 侧不需要直接操作 C# 对象方法。

### 3. env 存储方案：env = Lua 脚本 return 的 card table

不存隔离环境表。`LoadCardScript` 执行脚本后捕获 `return` 值，直接作为 `env`。调用时 `env["OnContest"]` 即可取到函数。

**理由：** 简单直接，card table 上就挂着所有 Hook 函数。

### 4. 不缓存，每次实例化独立执行

同一 DefineId 的多张卡各自执行一遍 Lua 脚本，获得独立的 card table。

**理由：** 避免多实例共享 table 导致 local 状态污染。Lua 脚本轻量，执行开销可忽略。

### 5. APIMgr.ScanHandlers 签名修复

放宽校验：支持返回 `APIContext`（不仅 void）和参数类型 `APIContext`（不仅 ContextBase）。

**理由：** 现有 `CombatBaseFunc.Attack` 等方法签名是 `static APIContext Attack(APIContext ctx)`，当前校验会跳过它们。

## Risks / Trade-offs

- [每卡执行一次脚本] → 文件 IO 可考虑缓存脚本字符串（读一次文件，多次 load），暂不优化
- [Scene 为 null] → 如果后续 API 需要 Scene 引用再补，当前不阻塞
- [NLua 类型映射] → `ctx:Set()` 需要 NLua 正确暴露 ContextBase 的方法，如有问题需加 `[LuaGlobalAttribute]` 或在 Init.lua 中桥接