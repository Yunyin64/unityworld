# Hooks 系统详解

> 本文档详细介绍《杀戮尖塔2》项目中的 Hooks 事件驱动系统，帮助开发者理解和使用这套机制。

---

## 一、什么是 Hooks 系统？

Hooks 系统是一个**事件驱动架构**，让游戏中的各种对象（遗物、能力、卡片、药水等）能够响应游戏事件，而不需要彼此知道对方的存在。

### 核心思想

```
游戏事件发生 → 广播信号 → 所有监听者自行响应
```

### 三个核心组件

| 组件 | 类 | 职责 |
|------|-----|------|
| 钩子定义 | `AbstractModel` | 定义所有可响应的事件方法 |
| 广播器 | `Hook` | 静态类，负责触发事件、遍历监听者 |
| 监听者收集器 | `RunState` / `CombatState` | 决定哪些对象能收到事件通知 |

---

## 二、工作流程

### 2.1 完整调用链

```
游戏逻辑
    │
    ▼
Hook.BeforeAttack(combatState, command)  ← 广播器触发事件
    │
    ▼
combatState.IterateHookListeners()  ← 收集当前所有监听者
    │
    ▼
foreach (AbstractModel model in listeners)  ← 遍历列表
{
    await model.BeforeAttack(command);  ← 逐个调用
}
```

### 2.2 关键特性

1. **每次事件都重新收集监听者**：列表是动态生成的，反映"当前这一刻谁在场"
2. **异步执行**：所有钩子方法返回 `Task`，支持动画和等待
3. **顺序执行**：按列表顺序依次调用，不是并行

---

## 三、监听者收集规则

### 3.1 RunState 层级（运行时状态）

在非战斗场景或需要全局监听时使用：

```csharp
public IEnumerable<AbstractModel> IterateHookListeners(CombatState? childCombatState)
{
    // 1. 所有玩家的卡组中的卡片
    foreach (Player player in Players)
        foreach (CardModel card in player.Deck.Cards)
            yield return card;
    
    // 2. 非战斗时：遗物、药水、修改器
    if (childCombatState == null)
    {
        foreach (Player player in Players)
        {
            list.AddRange(player.Relics);
            list.AddRange(player.Potions);
        }
        list.AddRange(Modifiers);
    }
    
    // 3. 战斗时：委托给 CombatState 收集
    if (childCombatState != null)
        foreach (AbstractModel item in childCombatState.IterateHookListeners())
            yield return item;
}
```

### 3.2 CombatState 层级（战斗状态）

在战斗场景中收集战斗相关的监听者：

```csharp
public IEnumerable<AbstractModel> IterateHookListeners()
{
    // 遍历所有生物（盟友 + 敌人）
    for (int i = 0; i < _allies.Count + _enemies.Count; i++)
    {
        Creature creature = ...;
        
        // 1. 生物身上的所有能力
        list.AddRange(creature.Powers);
        
        // 2. 怪物模型
        if (creature.Monster != null)
            list.Add(creature.Monster);
        
        // 3. 玩家特有的监听者
        Player player = creature.Player;
        if (player != null && player.IsActiveForHooks)
        {
            // 遗物（未熔化的）
            list.AddRange(player.Relics.Where(r => !r.IsMelted));
            
            // 药水
            list.AddRange(player.PotionSlots.Where(p => p != null));
            
            // 宝珠
            list.AddRange(player.PlayerCombatState.OrbQueue.Orbs);
            
            // 所有牌堆中的卡
            foreach (CardPile pile in player.PlayerCombatState.AllPiles)
            {
                foreach (CardModel card in pile.Cards)
                {
                    list.Add(card);
                    if (card.Affliction != null) list.Add(card.Affliction);
                    if (card.Enchantment != null) list.Add(card.Enchantment);
                }
            }
        }
    }
    return list;
}
```

### 3.3 监听者类型总结

| 类型 | 战斗中 | 非战斗 | 来源 |
|------|--------|--------|------|
| 遗物 | ✅ | ✅ | `Player.Relics` |
| 药水 | ✅ | ✅ | `Player.PotionSlots` |
| 能力 | ✅ | ❌ | `Creature.Powers` |
| 宝珠 | ✅ | ❌ | `Player.OrbQueue` |
| 卡牌 | ✅ | ✅ | 手牌/弃牌堆/抽牌堆 或 卡组 |
| 附魔/诅咒 | ✅ | ❌ | `CardModel.Enchantment/Affliction` |
| 修改器 | ❌ | ✅ | `RunState.Modifiers` |
| 怪物 | ✅ | ❌ | `Creature.Monster` |

---

## 四、钩子方法分类

### 4.1 事件响应型

**命名模式**：`BeforeXxx` / `AfterXxx` / `OnXxx`

**用途**：响应游戏事件，执行副作用（动画、修改状态、触发效果等）

```csharp
// 示例：战斗胜利后
public virtual Task AfterCombatVictory(CombatRoom room) => Task.CompletedTask;

// 示例：打出卡牌前
public virtual Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;

// 示例：受到伤害后
public virtual Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, ...) => Task.CompletedTask;
```

### 4.2 数值修改型

**命名模式**：`ModifyXxxAdditive` / `ModifyXxxMultiplicative`

**用途**：修改游戏数值，多个修改会叠加计算

```csharp
// 加法修改：伤害 +N
public virtual decimal ModifyDamageAdditive(...) => 0m;

// 乘法修改：伤害 ×N
public virtual decimal ModifyDamageMultiplicative(...) => 1m;

// 整数修改：卡牌打出次数
public virtual int ModifyCardPlayCount(...) => playCount;
```

**计算顺序**：
```
最终值 = (基础值 + 所有加法修改) × 所有乘法修改
```

### 4.3 条件判断型

**命名模式**：`ShouldXxx`

**用途**：决定是否允许某事发生，任一返回 `false` 则阻止

```csharp
// 是否应该死亡
public virtual bool ShouldDie(Creature creature) => true;

// 是否应该抽牌
public virtual bool ShouldDraw(Player player, bool fromHandDraw) => true;

// 是否可以使用卡牌
public virtual bool ShouldBePlayable(CardModel card) => true;
```

### 4.4 尝试修改型

**命名模式**：`TryModifyXxx`

**用途**：尝试修改某个值，返回是否成功

```csharp
// 尝试修改能量消耗
public virtual bool TryModifyEnergyCostInCombat(CardModel card, ref int cost) => false;
```

---

## 五、执行阶段（Early / Normal / Late）

许多钩子方法有三个版本，控制执行顺序：

```csharp
// Early：最先执行
public virtual Task AfterCardDrawnEarly(...) => Task.CompletedTask;

// Normal：中间执行（默认）
public virtual Task AfterCardDrawn(...) => Task.CompletedTask;

// Late：最后执行
public virtual Task AfterCardDrawnLate(...) => Task.CompletedTask;
```

### 5.1 为什么需要阶段区分？

某些效果必须在其他效果之前或之后执行：

```
事件：受到伤害

Early阶段：
  └→ 某遗物记录"受伤前"的状态

Normal阶段：
  └→ 虚弱能力：伤害 × 1.5
  └→ 力量能力：伤害 + 3

Late阶段：
  └→ 虚无能力：如果伤害 > 0，强制设为 1
     （必须最后执行！否则会被其他修改覆盖）
```

### 5.2 Hook 类中的实现

```csharp
public static async Task AfterCardDrawn(CombatState combatState, ...)
{
    // Early 阶段
    foreach (AbstractModel model in combatState.IterateHookListeners())
    {
        await model.AfterCardDrawnEarly(...);
    }
    
    // Normal 阶段
    foreach (AbstractModel model in combatState.IterateHookListeners())
    {
        await model.AfterCardDrawn(...);
    }
    
    // Late 阶段
    foreach (AbstractModel model in combatState.IterateHookListeners())
    {
        await model.AfterCardDrawnLate(...);
    }
}
```

---

## 六、实际开发示例

### 6.1 创建遗物

**需求**：创建一个遗物，在战斗胜利后回复 6 点生命

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Creature;

namespace MyMod.Relics
{
    public sealed class BurningBlood : RelicModel
    {
        // 构造函数设置遗物信息
        public BurningBlood()
        {
            Id = "BurningBlood";
            Name = "燃烧之血";
            Description = "战斗胜利后，回复 6 点生命。";
        }
        
        // 重写钩子方法
        public override async Task AfterCombatVictory(CombatRoom room)
        {
            // 检查玩家是否存活
            if (!Owner.Creature.IsDead)
            {
                Flash();  // 播放遗物闪烁动画
                await CreatureCmd.Heal(Owner.Creature, 6);  // 回复生命
            }
        }
    }
}
```

### 6.2 创建能力

**需求**：创建一个能力，使受到的伤害增加 50%

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Creature;

namespace MyMod.Powers
{
    public sealed class VulnerablePower : PowerModel
    {
        public VulnerablePower()
        {
            Id = "Vulnerable";
            Name = "虚弱";
            Description = "受到的伤害增加 50%。";
        }
        
        // 重写数值修改钩子
        public override decimal ModifyDamageMultiplicative(
            Creature target, 
            DamageSource source, 
            ...)
        {
            // 只有当目标是拥有此能力的生物时才生效
            if (target == Owner)
            {
                return 1.5m;  // 伤害 × 1.5
            }
            return 1m;  // 不修改
        }
    }
}
```

### 6.3 创建卡牌

**需求**：创建一张卡牌，打出时如果手牌中有诅咒，伤害翻倍

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;

namespace MyMod.Cards
{
    public sealed class CursedBlade : CardModel
    {
        public CursedBlade()
        {
            Id = "CursedBlade";
            Name = "诅咒之刃";
            BaseDamage = 6;
            EnergyCost = 1;
        }
        
        // 重写卡牌使用钩子
        public override async Task Use(CardPlayContext context)
        {
            // 检查手牌中是否有诅咒
            bool hasCurse = context.Player.Hand.Cards.Any(c => c.IsCurse);
            
            int damage = BaseDamage;
            if (hasCurse)
            {
                damage *= 2;  // 伤害翻倍
            }
            
            // 造成伤害
            await DamageCmd.DealDamageToEnemy(context, damage);
        }
    }
}
```

### 6.4 创建修改器

**需求**：创建一个修改器，使所有卡牌的费用减少 1

```csharp
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Modifiers
{
    public sealed class EnergyDiscount : ModifierModel
    {
        public EnergyDiscount()
        {
            Id = "EnergyDiscount";
            Name = "能量折扣";
        }
        
        // 重写费用修改钩子
        public override int ModifyEnergyCost(CardModel card, int currentCost)
        {
            // 费用减少 1，但不低于 0
            return Math.Max(0, currentCost - 1);
        }
    }
}
```

---

## 七、开发规范

### 7.1 命名规范

| 类型 | 命名模式 | 示例 |
|------|---------|------|
| 事件响应 | `BeforeXxx` / `AfterXxx` | `BeforeAttack`, `AfterCardPlayed` |
| 数值修改（加法） | `ModifyXxxAdditive` | `ModifyDamageAdditive` |
| 数值修改（乘法） | `ModifyXxxMultiplicative` | `ModifyBlockMultiplicative` |
| 条件判断 | `ShouldXxx` | `ShouldDie`, `ShouldDraw` |
| 尝试修改 | `TryModifyXxx` | `TryModifyEnergyCost` |

### 7.2 返回值规范

| 钩子类型 | 默认返回值 | 含义 |
|---------|-----------|------|
| 事件响应 | `Task.CompletedTask` | 不执行任何操作 |
| 加法修改 | `0m` | 不修改数值 |
| 乘法修改 | `1m` | 不修改数值 |
| 条件判断 | `true` | 允许发生 |
| 尝试修改 | `false` | 未修改 |

### 7.3 异步规范

**所有钩子方法必须返回 `Task`**：

```csharp
// ✅ 正确
public override async Task AfterCardPlayed(...)
{
    await SomeAsyncOperation();
}

// ❌ 错误
public override void AfterCardPlayed(...)
{
    SomeOperation();
}
```

### 7.4 状态修改规范

**不要在钩子中直接修改游戏状态**，应使用命令系统：

```csharp
// ✅ 正确：使用命令
public override async Task AfterCombatVictory(CombatRoom room)
{
    await CreatureCmd.Heal(Owner.Creature, 6);
}

// ❌ 错误：直接修改
public override async Task AfterCombatVictory(CombatRoom room)
{
    Owner.Creature.HP += 6;  // 绕过了伤害计算和事件触发
}
```

### 7.5 条件检查规范

在执行效果前，始终检查前置条件：

```csharp
public override async Task AfterCombatVictory(CombatRoom room)
{
    // 检查所有者是否存在
    if (Owner == null) return;
    
    // 检查生物是否存活
    if (Owner.Creature.IsDead) return;
    
    // 检查战斗是否胜利
    if (!room.IsVictory) return;
    
    // 执行效果
    await CreatureCmd.Heal(Owner.Creature, 6);
}
```

---

## 八、常见问题

### Q1：为什么我的遗物没有触发？

**检查清单**：
1. 遗物是否已添加到玩家身上？
2. 遗物的 `ShouldReceiveCombatHooks` 是否返回 `true`？
3. 是否在正确的场景（战斗/非战斗）？
4. 钩子方法是否正确重写？

### Q2：如何调试钩子执行顺序？

在 `Hook` 类中，每次调用都会通过 `choiceContext.PushModel(model)` 记录执行链：

```csharp
foreach (AbstractModel model in combatState.IterateHookListeners())
{
    choiceContext.PushModel(model);  // 记录当前执行的模型
    await model.BeforeAttack(command);
    choiceContext.PopModel(model);
}
```

可以通过日志或断点查看 `choiceContext` 中的执行链。

### Q3：如何让某个效果优先/延后执行？

使用 Early / Late 版本的钩子：

```csharp
// 优先执行
public override async Task AfterCardDrawnEarly(...) { ... }

// 延后执行
public override async Task AfterCardDrawnLate(...) { ... }
```

### Q4：多个修改器如何叠加？

加法修改直接相加，乘法修改直接相乘：

```
伤害 = (基础伤害 + 修改A + 修改B + ...) × 乘法A × 乘法B × ...
```

### Q5：如何阻止某个事件发生？

在 `ShouldXxx` 钩子中返回 `false`：

```csharp
public override bool ShouldDie(Creature creature)
{
    if (creature == Owner && Owner.HP > 0)
    {
        return false;  // 阻止死亡
    }
    return true;
}
```

---

## 九、架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Hooks 系统架构                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   ┌─────────────┐                                                  │
│   │ 游戏逻辑    │                                                  │
│   └──────┬──────┘                                                  │
│          │ 触发事件                                                 │
│          ▼                                                          │
│   ┌─────────────┐                                                  │
│   │    Hook     │ ← 静态广播器                                      │
│   │  (静态类)   │                                                  │
│   └──────┬──────┘                                                  │
│          │ 调用 IterateHookListeners()                             │
│          ▼                                                          │
│   ┌─────────────────────────────────────────────────────────┐      │
│   │              RunState / CombatState                      │      │
│   │  ┌─────────────────────────────────────────────────┐    │      │
│   │  │           IterateHookListeners()                 │    │      │
│   │  │                                                  │    │      │
│   │  │  yield return 遗物                               │    │      │
│   │  │  yield return 药水                               │    │      │
│   │  │  yield return 能力                               │    │      │
│   │  │  yield return 卡牌                               │    │      │
│   │  │  yield return 宝珠                               │    │      │
│   │  │  yield return 修改器                              │    │      │
│   │  └─────────────────────────────────────────────────┘    │      │
│   └─────────────────────────────────────────────────────────┘      │
│          │                                                          │
│          ▼ 返回监听者列表                                            │
│   ┌─────────────────────────────────────────────────────────┐      │
│   │  foreach (AbstractModel model in listeners)             │      │
│   │  {                                                      │      │
│   │      await model.OnXxx(...);  ← 调用钩子方法             │      │
│   │  }                                                      │      │
│   └─────────────────────────────────────────────────────────┘      │
│          │                                                          │
│          ▼                                                          │
│   ┌─────────────────────────────────────────────────────────┐      │
│   │                   AbstractModel                         │      │
│   │  ┌─────────────────────────────────────────────────┐    │      │
│   │  │  - RelicModel                                    │    │      │
│   │  │  - PowerModel                                    │    │      │
│   │  │  - CardModel                                     │    │      │
│   │  │  - PotionModel                                   │    │      │
│   │  │  - ModifierModel                                 │    │      │
│   │  │  - OrbModel                                      │    │      │
│   │  └─────────────────────────────────────────────────┘    │      │
│   └─────────────────────────────────────────────────────────┘      │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 十、总结

### 核心要点

1. **Hooks 是事件驱动系统**：游戏事件广播，监听者自行响应
2. **监听者动态收集**：每次事件都重新调用 `IterateHookListeners()`
3. **三种钩子类型**：事件响应、数值修改、条件判断
4. **执行阶段控制**：Early / Normal / Late 控制顺序
5. **异步执行**：所有钩子返回 `Task`
6. **使用命令系统**：不直接修改状态，使用 `XxxCmd` 命令

### 设计优势

- **解耦**：各模块不需要知道彼此存在
- **可扩展**：添加新效果只需重写钩子方法
- **统一执行顺序**：避免竞态条件
- **易于调试**：执行链可追踪

---

> 文档版本：1.0  
> 最后更新：2026-04-20  
> 适用项目：SlayTheSpire2_GodotProject
