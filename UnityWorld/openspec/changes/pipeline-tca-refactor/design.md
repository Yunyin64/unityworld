# Pipeline 管线设计思考

## 核心概念

卡牌效果不是单一的 TCA（Trigger-Condition-Action），而是一条**管线**，每个节点都有独立的 **Scope**（作用域/目标选择器）。

存在两条管线：

```
ActionPipeline（事件驱动，瞬间执行，必有 Trigger）
┌──────────┐   ┌───────────┐   ┌───────┐   ┌────────────────┐
│ Trigger  │──▶│Condition? │──▶│Scale? │──▶│ List<Action>   │
└──────────┘   └───────────┘   └───────┘   └────────────────┘
  有Scope        有Scope         有Scope      每个Action有Scope


AuraPipeline（状态驱动，持续生效，无 Trigger）
┌───────────┐   ┌───────┐   ┌────────────────┐
│Condition? │──▶│Scale? │──▶│ List<Aura>     │
└───────────┘   └───────┘   └────────────────┘
  有Scope        有Scope      每个Aura有Scope
```

## 节点语义

| 节点 | 关键词 | 返回/职责 |
|------|--------|-----------|
| Trigger | "当X时" | 事件触发时机，ActionPipeline 必有 |
| Condition | "如果X" | 布尔门控，返回 0/1 |
| Scale | "每有X" | 返回数值 N，乘以后续 Value |
| Action | "造成X" | 瞬间执行，做完就完 |
| Aura | "持续X" | 持续生效，条件不满足就消失 |

## Action vs Aura 的本质区别

不是"加 buff vs 造伤害"，而是**生命周期**：

```
Action：触发 → 执行 → 结束（不可逆，已发生）
         "造成3点伤害" "获得2点护甲" "抽1张牌" ← 都是 Action

Aura：  存在 → 持续生效 → 条件不满足/来源消失 → 效果消失
         "持续+2攻击力" "持续减少1点消耗" ← 只要在就有，走了就没
```

## Scope：通用目标选择器

Scope 不是管线某一层的东西，**每一层都有自己的 Scope**：

- Trigger 的 Scope：监听谁的事件
- Condition 的 Scope：检查谁的状态
- Scale 的 Scope：统计谁的数量
- Action/Aura 的 Scope：对谁执行

同一条管线内不同节点可以指向不同目标：
```
"当【对手的法术卡】充能时，每有一张【自己的装备卡】，对【对手随机一张卡】造成2伤害"

Trigger:  Scope(Enemy, IsSpell)
Scale:    Scope(Self, IsEquip)
Action:   Scope(Enemy, Any, Random(1))
```

Scope 本身是组合查询：
```
Scope = 归属 + 条件 + 选择方式

归属:    Self / Enemy / Any / None(全局)
条件:    HasCD / IsEquip / IsSpell / HasTag("火") / ...（可多个）
选择方式: All / Random(N) / Adjacent / ...
```

例子：
- "自己随机一张有CD的卡" → Scope(Self, HasCD, Random(1))
- "自己所有法术卡" → Scope(Self, IsSpell, All)
- "对手上下两张卡" → Scope(Enemy, Any, Adjacent)
- "抽牌" → Scope(Self)（目标是 Npc 自己）
- "改变天气" → Scope(None)（全局效果，无具体目标）

## Action = Scope + Func

Action 本质是 "选谁 + 做什么"：
```
Func 是纯动词：Charge(X) / Damage(X) / Draw(X) / Discard / ...
Scope 负责选目标

组合：
  Scope(Self, HasCD, Random(1)) + Func(Charge, 2) = "自己随机一张有CD的卡充能2"
  Scope(Enemy, Any, Random(1)) + Func(Damage, 3)  = "对手随机一张卡造成3伤害"
```

这样 Func 数量很少（纯动词），复杂度全在 Scope 组合上。

## Func 的前置要求（Requires）

有些 Func 对目标有要求：
```
Charge(X) → 目标必须有 CD
Discard   → 目标必须在手牌中
```

Scope 的 Filter 应该包含对应条件来保证选出来的目标合法。运行时如果目标不满足 Requires，跳过不报错。

## 一个机制展开为原子 API

一个"机制"关键词（如"充能"），展开后就是管线各位置上的原子 API 集合：

```
机制"充能" 展开：

Trigger:  OnSelfCharge / OnEnemyCharge
Scale:    (无)
Condition: (无)
Action:   Scope(Self, HasCD, Random(1)) + Charge(X)
          Scope(Enemy, HasCD, Random(1)) + Charge(X)
          Scope(Self, HasCD, Adjacent) + Charge(X)
Aura:     ChargeEfficiency(+X%)
```

设计新机制 = 填表：
```
新机制: ____
  T:  当____时
  S:  每有____
  C:  如果____
  A:  Scope(____) + Func(____)
  Aura: Scope(____) + Func(____)
```

## 合法组合

Condition 和 Scale 可叠加，不互斥：

```
Action 路径（必有 Trigger）：
  T + Action
  T + Scale + Action
  T + Condition + Action
  T + Condition + Scale + Action

Aura 路径（无 Trigger）：
  Aura
  Scale + Aura
  Condition + Aura
  Condition + Scale + Aura
```

一张卡可同时带多条管线：
```
CardDefine {
  ActionPipelines: List<ActionPipeline>
  AuraPipelines:   List<AuraPipeline>
}
```
