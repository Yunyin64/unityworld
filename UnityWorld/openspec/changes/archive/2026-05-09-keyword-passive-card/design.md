## Context

当前战斗卡牌系统中，`CombatCard` 统一走 `WaitResource → InCD → Ready → InPending → Finished` 的 Phase 循环。所有卡牌行为由 Lua env 中的 `OnContest`/`OnApply` 等 Hook 驱动。`LuaMgr` 负责管理 Lua State 生命周期及卡牌脚本加载，卡牌脚本目录为 `Data/LuaCards/`，初始化脚本为 `Data/LuaScripts/Init.lua`。

现在需要支持"被动卡"——一种不走 CD 循环、通过 keyword 声明行为模式的卡牌。为了 Mod 友好，C# 侧不硬编码任何 keyword 的语义，只负责加载 keyword Lua 脚本并在卡牌生命周期节点遍历调用。

## Goals / Non-Goals

**Goals:**
- Keyword 注册表：LuaMgr 启动时加载 `Keywords/Keyword.lua` 索引文件，按映射加载各 keyword Lua 脚本，缓存为 `Dictionary<string, LuaTable>`
- CombatCard 生命周期节点遍历卡牌 keywords，查注册表调用对应 Lua Hook
- keyword 未注册 → 报错日志；keyword 存在但没有某个 hook → 静默跳过
- 落地 Passive keyword：`Passive.lua` 在 `OnPreStart` 中调用 `card:SetPhase("Passive")`，CombatCard.Tick() 中 Phase==Passive 跳过 CD 循环
- CardDefine / CardBaseData 支持 `Keywords` 字段，JSON 可配、Lua 可覆盖

**Non-Goals:**
- 不实现 Passive 以外的任何 keyword（OnHit、Counter、Aura 等留给未来）
- 不改变现有主动卡的任何行为
- 不涉及 Modifier 的 Lua env 改造（独立议题）

## Decisions

### D1: Keyword 注册表放在 LuaMgr 中

**选择**: 在 `LuaMgr` 中新增 `Dictionary<string, LuaTable> _keywordRegistry`，Init 阶段加载。

**理由**: LuaMgr 已经是 Lua 脚本加载的唯一入口（`LoadCardScript`、`LoadInitScript`），keyword 脚本的加载逻辑与之同构。不需要新建 Manager。

**替代方案**: 新建 KeywordMgr —— 引入额外的 Manager 注册/初始化开销，且 keyword 脚本加载仍需调用 LuaMgr 的 Lua State，不如直接内聚。

### D2: Keyword.lua 索引文件格式

**选择**: `Data/LuaScripts/Keywords/Keyword.lua` 返回一个 `{ name = "相对路径" }` 的 table。LuaMgr 遍历此 table，对每个 entry 加载对应 Lua 文件并缓存。

```lua
-- Keywords/Keyword.lua
return {
    Passive = "Passive",  -- 相对于 Keywords/ 目录
}
```

**理由**: 单一索引文件作为注册入口，Modder 只需修改此文件并添加对应 lua 脚本即可扩展。路径拼接逻辑统一在 LuaMgr 中处理。

### D3: C# 不认识任何 keyword 的语义

**选择**: C# 侧只做三件事——(1) 读取 Keywords 列表 (2) 查注册表 (3) 调 Lua Hook。不对任何 keyword 字符串做 if/switch 判断。

**理由**: Mod 友好。新增 keyword 不需要改 C#，只需要加 Lua 文件并在 Keyword.lua 中注册。

**Passive 的 SetPhase 怎么办**: 暴露 `CombatCard.SetPhase(string phaseName)` 方法给 Lua 调用。Lua 侧 `card:SetPhase("Passive")`，C# 侧解析字符串为 `CombatCardPhase` 枚举。这样 `Passive` 这个枚举值存在于 C#，但 C# 不会在 keyword 处理逻辑中硬编码它。

### D4: Keyword Hook 在 CombatCard 生命周期中的调用时机

**选择**: 在以下节点插入 keyword hook 遍历：

| CombatCard 方法 | Keyword Hook 名 | 调用时机 |
|---|---|---|
| `PreStart()` | `OnPreStart` | 卡牌初始化后、Lua env 加载后 |
| `Start()` | `OnStart` | 战斗开始时 |
| `Tick()` | `OnTick` | 每 Tick（Passive 卡也会调到这里，然后 return 跳过 CD） |
| `End()` | `OnEnd` | 战斗结束清理时 |

调用顺序：先遍历所有 keyword hooks，再调用卡自身的 Lua env hooks。这样 keyword 可以在卡逻辑之前修改卡的状态（如 SetPhase）。

### D5: CombatCard.Tick() 中 Passive 的处理

**选择**: Tick() 开头检查 `Phase == Passive`，如果是，只调用 keyword OnTick hooks 和卡自身的 `OnPassiveTick`（可选），然后 return，不进入 CD 循环。

```
Tick():
  keyword hooks (OnTick)        ← 所有卡都走
  if Phase == Passive:
    CallLuaHook<bool>("OnPassiveTick") ← 可选，卡自身 Lua 定义
    return                       ← 跳过 CD 循环
  ... 原有 CD 循环逻辑 ...
```

### D6: Keywords 字段的数据来源与覆盖规则

**选择**: `CardDefine` 上有 `List<string> Keywords`（JSON 配置），`CardBaseData` 上也有对应字段（运行时克隆）。Lua env 中如果定义了 `card.Keywords`，则覆盖 JSON 的值。

**理由**: 与你现有的"Lua 覆盖 JSON"惯例一致。

## Risks / Trade-offs

- **[性能] 每 Tick 遍历 keywords 列表并查字典调 Lua** → 被动卡通常数量有限（1-3张），且 Dictionary 查询 O(1)，影响可忽略。如果未来 keyword 数量爆炸，可考虑 PreStart 时预缓存 keyword hook 函数引用。
- **[错误静默] keyword 存在但某 hook 不存在时静默跳过** → 可能掩盖 Lua 脚本的拼写错误。缓解：在 Debug 模式下可加 Warn 日志。
- **[枚举字符串解析] SetPhase 接收字符串** → 拼写错误不会编译期发现。缓解：解析失败时报错日志。
