## Context

当前 `CombatNpcFunc.cs` 中 `ResolveContest` / `ContestWin` / `ContestLose` / `Straight` 共约 120 行，按 ContestType 做分支处理（攻击→伤害，Shield→叠甲，Block→消失）。新增防御类型需要同时改底层 C# + 所有相关分支。

新设计将底层简化为一条判定规则 + 一次事件广播，所有差异化行为由功法卡（Passive 相位的 CombatCard）在 Lua hook 中实现。

## Goals / Non-Goals

**Goals:**
- 拼点结算底层只有一个 if：攻击赢→差值伤害，其他→广播 `OnContestOverflow`
- ContestType（Zhan/Da/Ci/SheJi/Shield/Block/Dodge）作为纯标签，不决定基础行为
- 功法卡通过 `OnContestOverflow` hook 消费溢出值，实现"溢出转甲/反伤/充能"等效果
- 移除通吃规则，攻击对攻击也走差值制
- 新增 Dodge 枚举值

**Non-Goals:**
- 不重构卡牌 CD 系统 / 灵元系统 / PendingSlot 机制
- 不改动 DamageInfo → ApplyDamage → Shield吸收 的伤害流水线
- 不在本次实现具体功法卡（只建立机制，功法卡另开 change）
- 不改动 CombatCardPhase 状态机

## Decisions

### 1. 统一差值制，移除通吃

**选择**：攻击 vs 攻击（同类型）不再通吃，统一为差值伤害。

**理由**：
- 通吃是唯一打破"比大小出差值"统一规则的特例
- 若需要通吃效果，可由功法 hook 实现（检测同类型 → 将伤害覆写为赢方全额值）
- 减少底层分支

**替代方案**：保留通吃作为基础规则 → 拒绝，因为破坏统一性

### 2. 防御赢/防御对撞 → 广播事件而非直接效果

**选择**：基础层防御赢 = 什么都不做 + 广播 `OnContestOverflow`。

**理由**：
- 功法定义效果 = 数据驱动 = Mod友好
- 底层代码量从 ~120 行降到 ~20 行
- 新防御类型（Dodge/Parry/...）加入零代码改动，只加枚举值

### 3. 事件载体设计

**选择**：`OnContestOverflow` 事件通过现有 `DispatchHookToAll` 通道广播，携带 APIContext 扩展字段。

APIContext 扩展：
```
ctx.Winner      = winnerNpc
ctx.Loser       = loserNpc  
ctx.Overflow    = float（差值）
ctx.WinnerType  = ContestType（赢家卡的类型）
ctx.LoserType   = ContestType（输家卡的类型）
ctx.WinnerCard  = CombatCard
ctx.LoserCard   = CombatCard
```

**替代方案**：新建 ContestResultContext 类 → 拒绝，复用现有 APIContext 减少类型膨胀。

### 4. Straight（直击）统一处理

**选择**：直击走同一套逻辑。攻击直击=全额伤害+广播；防御直击=全额溢出+广播（默认无事）。

### 5. ContestType.Dodge 加入枚举

**选择**：直接加入 EnumTypes.cs，与 Shield/Block 并列，基础行为完全一致。

## Risks / Trade-offs

- **[现有 Lua 适配]** 如有卡牌 Lua 依赖通吃规则（检测"同类型攻击赢→全额伤害"），需要排查适配 → 排查 Data/ 下 Lua 文件中是否有相关逻辑
- **[功法缺失过渡期]** 改完底层后，Shield/Block 赢了会暂时"什么都不做"直到功法卡实现 → 可接受，本次只建机制
- **[APIContext 字段膨胀]** 加了 Winner/Loser/Overflow 等字段 → 可接受，APIContext 本身就是通用上下文袋
