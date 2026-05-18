## Context

当前 `CombatNpcModifier` 通过 `StatModifiers` 列表在 `ModifierTick` 中写入持有者的 StatBlock。光环需求要求跨对象属性影响（Npc→Npc、Npc→Card），且目标随状态实时变化。

前版设计（combat-aura-system）采用 AuraTarget + IsAuraTarget + 内置选择器，但发现：
- "最上边的卡冷却-1"这类跨类型需求无法用 Npc→Npc 的 CollectAura 解决
- AuraTarget / Self 分流增加了概念复杂度

新方案将判定和效果合一：**属性读取时直接问 Modifier 的 Lua "你要对我加多少？"**

## Goals / Non-Goals

**Goals:**
- 统一属性注入：任何对象（Npc、Card）读任何属性时，从全场 Modifier 收集贡献
- 一个 Lua hook 搞定判定 + 效果：`OnModifierStat{XX}(env, caller)` → 返回修正值（0 = 不生效）
- LuaHooks 预扫描：创建时缓存所有 Lua 函数引用，运行时 `Dictionary.TryGetValue` 一步完成
- 保留老的 env 动态查找路径，做开关 AB 测试
- Self 不特殊处理：传统 Buff 也走 `OnModifierStat{XX}`

**Non-Goals:**
- 不做属性值缓存/脏标记（10 单位以下不需要）
- 不改 StatBlock 核心层
- 本期不改非战斗 Modifier（NpcModifier、TileModifier 等）

## Decisions

### Decision 1: OnModifierStat{XX} 合一判定与效果

**选择**: Lua hook `OnModifierStatAtk(env, caller)` 返回 float，caller 是读属性的对象（Npc 或 Card）。Lua 里自行判断 caller 是不是目标、算出修正值、返回。0 表示不生效。

**替代方案**: IsAuraTarget（判定）+ StatModifiers（效果）分离

**理由**: 合一后 Modder 只写一个函数；跨类型（Npc→Card）天然支持；不需要 AuraTarget 字段和内置选择器。

### Decision 2: LuaHooks / ScanLuaHooks / CallLuaHook 提升到 ILuaBindable 层

**选择**: `ILuaBindable` 接口扩展，包含 `LuaHooks` 字典、`ScanLuaHooks()`、`HasHook()`、`CallLuaHook<bool>()`、`CallLuaHookWithReturn<T>()`。所有实现者（`CombatNpcModifier`、`CombatCard`、`NpcModifier`）创建时调用 `ScanLuaHooks()`。

**替代方案**: 仅在 `CombatNpcModifier` 上实现

**理由**: 
- 避免每次两步（env 取值 + as 转型）
- 判断"有没有某 hook"变成 `ContainsKey`，O(1) 且不涉及 Lua 跨语言
- 适用于所有 hook（OnTick、OnApply 等），不仅限于 OnModifierStat
- 放在接口层，所有 Lua 绑定对象统一受益，不需要每个类自己实现一遍

### Decision 3: 保留老路径开关

**选择**: 通过静态 bool 开关 `LuaBindableConfig.UseLuaHooksCache` 控制，对所有 `ILuaBindable` 实现者生效。true = 新路径（查 LuaHooks 字典），false = 老路径（env 动态查找）。默认 true。

**理由**: 新机制上线后可 AB 测试，确认无回归后再移除老路径。

### Decision 4: 属性读取包装在 CombatNpc / CombatCard 层

**选择**: 
- `CombatNpc.GetStat(statId)` = `Stats.Get(statId)` + 遍历全场 Modifier 的 `OnModifierStat{statId}` 贡献
- `CombatCard.GetCardStat(statId)` = 基础值 + 遍历全场 Modifier 的 `OnModifierStat{statId}` 贡献

**理由**: StatBlock 核心层不改动。包装层在战斗对象上，非战斗代码不受影响。

### Decision 5: 光环判定使用裸值避免递归

**选择**: `OnModifierStat{XX}` 内读属性用 `Stats.Get()`（裸值），不用 `GetStat()`（含光环贡献）。

**理由**: "攻击最高的人+1攻击"如果用含光环值判定会无限递归。Lua 侧通过 `npc.Stats:Get()` 读裸值。

### Decision 6: 遍历范围——全场所有存活 Npc 的所有 Modifier

**选择**: `GetStat` / `GetCardStat` 遍历 CombatScene 中所有存活 Npc 的所有 Modifier，通过 `LuaHooks.ContainsKey` 快速跳过无关 Modifier。

**理由**: 10 单位 × 平均 3 Modifier = 30 次 ContainsKey 检查，极快。只有命中的才调 Lua。

## Risks / Trade-offs

- **[Lua 调用频率]** 如果大量 Modifier 都定义了同一个 OnModifierStat hook，每次读属性会调多次 Lua → 10 单位规模下可接受；未来可加缓存层。
- **[递归风险]** Modder 在 OnModifierStat 里误调 GetStat → 文档明确：hook 内只用 `Stats:Get()`。
- **[Self 无快速路径]** 传统 Buff "+1攻击"也走遍历 → 概念统一的代价，10 单位规模下性能可接受。
- **[老路径维护]** 保留两条路径增加代码复杂度 → AB 测试确认后尽快移除。
