## Context

当前战斗系统有两套"选取目标卡牌"机制：
1. **CombatBaseScope**：通过 `[APIFunc]` 反射注册，Lua 侧用 `API:Execute("AllCard", ctx)` 调用，结果通过 `ctx.Set("Result", ...)` 返回
2. **APIDomainFunc**：字典分发 `_cardFuncs[key](ctx)`，直接返回 `List<CombatCard>`，但当前实现全是空壳

Action（如 Charge）已经在 C# 侧调用 `APIMgr.GetTargetCard(domain, ctx)`，但 Lua 侧的 Action.lua 包装函数还在接收已选好的 cards 列表（由 Scope 预先选取）。

## Goals / Non-Goals

**Goals:**
- APIDomainFunc 成为唯一的选卡实现层
- Action 自带 Domain 参数，一步完成"选卡+执行"
- Lua Action 包装函数签名统一为 `(ctx, domain, ...params)`
- 补全所有 Domain key：All / Random / LeftOne / LeftAll / RightOne / RightAll / Adjacent / Self

**Non-Goals:**
- 不删除 CombatBaseScope.cs（保留标记废弃）
- 不重构 APIMgr 的反射扫描机制

## Decisions

### 1. Domain key 与选卡逻辑映射

| # | Domain Key | 含义 | 实现逻辑（相对于 ctx.SourceCard） |
|---|---|---|---|
| 0 | `Self` | 自身卡牌 | `[ctx.SourceCard]` |
| 1 | `Random` | 随机一张 CD 中的卡 | `Field.Where(InCD).RandomOne()` |
| 2 | `Other` | 自己除 SourceCard 外所有卡 | `Field.Where(c => c != SourceCard)` |
| 3 | `Adjacent` | 上下各取（邻居） | `index±1` |
| 4 | `AboveOne` | 上方一张 | `Field[index - 1]` |
| 5 | `AboveAll` | 上方所有 | `Field[0..index)` |
| 6 | `BelowOne` | 下方一张 | `Field[index + 1]` |
| 7 | `BelowAll` | 下方所有 | `Field(index+1..]` |
| 8 | `All` | 自己所有卡 | `caster.GetField()` |
| 9 | `TargetAll` | 对方所有卡 | `target.GetField()` |
| 10 | `TargetRandom` | 对方随机一张卡 | `target.GetField().RandomOne()` |

**定位基准**：所有相对位置都以 `ctx.SourceCard` 为锚点。如果 SourceCard 为 null，fallback 到 caster 的整个 Field。

### 2. Lua Action 签名变更

Charge 为例：
```lua
-- 旧
Charge = function(ctx, cards, reduceTick)
    ctx:Set("TargetCard", cards)
    ...

-- 新
Charge = function(ctx, domain, reduceTick)
    ctx:Set("Domain", domain)
    ...
```

C# 侧 `CombatCDAction.Charge()` 已经从 ctx 取 Domain 字符串去调 `APIMgr.GetTargetCard`，无需改动。

### 3. LuaTemplate 模板格式

```json
"LuaTemplate": "Charge(ctx, \"{DoMain}\", {ReduceTick})"
```

Domain 是字符串类型，模板展开时需要引号包裹。

### 4. Npc Domain key 映射

| Domain Key | 含义 | 实现逻辑 |
|---|---|---|
| `Self` | 施法者自身 | `[ctx.Caster]` |
| `Target` | 当前目标 | `[ctx.Caster.Target]`（受保护字段，需通过 GetTarget() 暴露） |

调用入口：`APIMgr.GetTargetNpc(key, ctx)` → `APIDomainFunc._npcFuncs[key](ctx)`

### 5. CombatBaseScope 废弃策略

在文件顶部加注释标记 `// [Deprecated] 已迁移至 APIDomainFunc，保留供参考`，不加 `[Obsolete]` Attribute（避免编译警告噪音，因为 Lua 还在间接引用）。

## Risks / Trade-offs

- **SourceCard 为 null 时的边界**：部分 Action 可能没有 SourceCard（如 Npc 级别的效果触发）。→ 在 APIDomainFunc 中对 null 做 fallback，返回空列表并 log 警告。
- **手写 Lua 脚本改造不完全**：可能遗漏某些卡牌脚本。→ grep 全局搜索 Scope 函数调用，逐一改造。
- **Random 需要确定性随机源**：使用 `caster.Scene.Soul.Random()` 保证战斗回放一致性。
