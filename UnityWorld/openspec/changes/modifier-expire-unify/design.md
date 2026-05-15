## Context

当前 Modifier 体系有三种运行时实现（CombatNpcModifier、NpcModifier、CardModifier），它们共享 `IModifierBase` 接口定义数据字段，但过期判定各自为政：

- 基线扩展方法 `IModifierBaseExt.IsExpired()` 只看 `Duration > 0 && RemainingTime <= 0`，不考虑层数
- `TileModifier` 声明了 `public bool IsExpired = false` 字段遮蔽扩展方法，永远不过期
- `CardModifier` 声明了 `isExpired` 属性，逻辑写反了（`RemainingTime >= Duration`）
- `NpcModifier` 完全没有 Tick 驱动和过期检查
- `CombatNpcModifierFunc.ModifierTick()` 内联判定 `RemainingTime <= 0` 而不调用 `IsExpired()`
- `CombatNpcModifierFunc.GetAllModifiers()` 过滤条件反了

Modifier 分两种形态：
- **智能 Modifier**（CombatNpcModifier、NpcModifier）：实现 `ILuaBindable`，有 Lua env 和 hooks
- **哑 Modifier**（CardModifier）：纯 `StatModifiers` 数据容器，无脚本行为

项目已有 `TriggerDefine` 数据体系，用 ID + Tags 描述触发时机，可复用为 Modifier 的移除触发点。

## Goals / Non-Goals

**Goals:**
- 在 `IModifierBase` 接口层统一过期判定，所有 Modifier 子类使用同一套 `IsExpired()` 逻辑
- 引入 `ExpirePolicy` 枚举描述"满足什么条件算过期"
- 引入 `RemoveTriggerId` 字段引用 `TriggerDefine`，描述"什么事件触发层数消耗"
- 提供 `ReduceStack` / `AddStack` 扩展方法作为层数操作的统一 API
- 修复所有已知 bug（TileModifier 遮蔽、CardModifier 逻辑反、GetAllModifiers 语义反）

**Non-Goals:**
- 不改造 TileModifier 的整体设计（仅删除遮蔽字段修 bug）
- 不改造 Trait 体系（Trait 独立于 Modifier）
- 不新增 NpcModifier 的 Tick 驱动入口（本次只统一接口和判定，驱动入口后续迭代）
- 不改动 Lua hook 签名

## Decisions

### 1. ExpirePolicy 枚举设计

```csharp
public enum ExpirePolicy
{
    Never,        // 永不自动过期，只能手动移除
    TimeBased,    // RemainingTime ≤ 0 时过期
    StackBased,   // CurrentStack ≤ 0 时过期
    TimeOrStack,  // 任一满足即过期
    TriggerBased, // 纯靠 RemoveTriggerId 指定的事件直接移除
}
```

**为什么不用 `TimeAndStack`（两者都满足才过期）？**
这等同于 StackBased + 由时间驱动 ReduceStack，两个维度组合已覆盖该场景，无需额外枚举值。

### 2. RemoveTriggerId 引用 TriggerDefine 而非自定义字符串协议

**决策**：`RemoveTriggerId` 存储 `TriggerDefine.ID`，引擎在事件点广播 triggerId 时，匹配 Modifier 的 `RemoveTriggerId` 执行 `ReduceStack(1)`。

**为什么不用自定义字符串？**
- TriggerDefine 已有成熟的 ID + Tags + Weight 数据体系
- 未来可利用 Tags 做模糊匹配（"所有带 Combat.Hit tag 的触发点"）
- Mod 友好：modder 在 JSON 加一条 TriggerDefine 即可定义新触发时机

### 3. 删除 StackReduceType 枚举

**决策**：不再由 Modifier 自身描述"层数什么时候减"，层数变化完全由外部驱动。

**理由**：
- 在并行 CD 制战斗中，"PerTick 减层"不是合理的游戏机制
- 层数减少的时机取决于游戏设计（受击、出牌、对拼等），无法用枚举穷举
- 智能 Modifier 由 Lua hook 自行控制减层
- 哑 Modifier 由 `RemoveTriggerId` + 引擎广播自动触发减层

### 4. IsExpired 统一判定逻辑

```csharp
public static bool IsExpired(this IModifierBase self) => self.ExpirePolicy switch
{
    ExpirePolicy.Never      => false,
    ExpirePolicy.TimeBased  => self.Duration > 0 && self.RemainingTime <= 0f,
    ExpirePolicy.StackBased => self.CurrentStack <= 0,
    ExpirePolicy.TimeOrStack => (self.Duration > 0 && self.RemainingTime <= 0f)
                                || self.CurrentStack <= 0,
    ExpirePolicy.TriggerBased => false, // 由触发器事件直接移除，不靠轮询判定
    _ => false,
};
```

`TriggerBased` 返回 false 是因为它不靠 Tick 末尾的轮询检查——引擎在事件广播时直接移除该 Modifier。

### 5. RemoveTriggerId 的广播机制

事件点广播 triggerId 时的处理流程：
1. 遍历目标实体身上所有 Modifier
2. `modifier.RemoveTriggerId == triggerId` 的：
   - 如果 `ExpirePolicy == TriggerBased`：直接标记移除
   - 否则：调 `ReduceStack(1)`，后续由正常 IsExpired 检查清理

### 6. ReduceStack / AddStack 作为扩展方法

放在 `IModifierBaseExt` 中，统一提供层数操作，内部处理 MaxStack 上限约束。这样 Lua 和引擎代码都通过同一个入口操作层数。

## Risks / Trade-offs

- **[接口改动影响面]** `IModifierBase` 新增字段是 BREAKING 改动，所有实现类必须跟着加 → 改动文件多但都是机械性的，逐个补上即可
- **[TriggerBased 的直接移除]** 事件广播时直接移除 Modifier 而非等 Tick 末尾，需注意遍历中不要修改集合 → 使用 toRemove 临时列表
- **[NpcModifier 暂无驱动]** 本次只统一接口不加 Tick 驱动，TimeBased 的 NpcModifier 暂时不会自动过期 → 接受，后续迭代补上
- **[TileModifier 删字段]** 删除 `IsExpired` 字段后，`TileSystemAura` 的 `modifier.IsExpired` 会走扩展方法，行为变为正确 → 需确认 TileModifier 构造时 Duration 赋值正确
