# Power 系统技术文档

> 面向策划和程序的完整参考手册
> 
> 最后更新：2026-04-24

---

## 目录

1. [系统概述](#1-系统概述)
2. [核心概念](#2-核心概念)
3. [PowerModel 基类 API](#3-powermodel-基类-api)
4. [PowerCmd 命令接口](#4-powercmd-命令接口)
5. [Power 生命周期](#5-power-生命周期)
6. [Hook 系统集成](#6-hook-系统集成)
7. [临时 Power 机制](#7-临时-power-机制)
8. [所有 Power 一览表](#8-所有-power-一览表)
9. [如何新增一个 Power](#9-如何新增一个-power)

---

## 1. 系统概述

Power 系统是战斗中的核心状态效果机制，管理所有附着在生物（Creature）上的 Buff 和 Debuff。

```
┌─────────────────────────────────────────────────────┐
│                    战斗场景                           │
│                                                      │
│   ┌──────────┐                    ┌──────────┐      │
│   │ Player   │                    │ Enemy    │      │
│   │ Creature │                    │ Creature │      │
│   │          │                    │          │      │
│   │ Powers:  │   PowerCmd.Apply   │ Powers:  │      │
│   │ ┌──────┐ │ ◄────────────────► │ ┌──────┐ │      │
│   │ │Str +3│ │                    │ │Poison│ │      │
│   │ │Dex +2│ │                    │ │Weak  │ │      │
│   │ │Blur  │ │                    │ │Vuln  │ │      │
│   │ └──────┘ │                    │ └──────┘ │      │
│   └──────────┘                    └──────────┘      │
│                                                      │
│   ┌──────────────────────────────────────────┐      │
│   │            Hook System                    │      │
│   │  所有 Power/Relic/Card 都可以监听和拦截    │      │
│   └──────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────┘
```

### 关键文件位置

| 文件/目录 | 说明 |
|-----------|------|
| `src/Core/Models/PowerModel.cs` | Power 抽象基类 |
| `src/Core/Commands/PowerCmd.cs` | Power 操作命令（施加/移除/修改） |
| `src/Core/Models/Powers/` | 所有具体 Power 实现（~250+ 个） |
| `src/Core/Models/Powers/Mocks/` | 测试用 Mock Power（8 个） |
| `src/Core/Models/ITemporaryPower.cs` | 临时 Power 接口 |
| `src/Core/Models/Entities/Powers/PowerType.cs` | 类型枚举 |
| `src/Core/Models/Entities/Powers/PowerStackType.cs` | 叠加方式枚举 |
| `src/Core/Nodes/Combat/NPower.cs` | Power UI 节点 |
| `src/Core/Nodes/Combat/NPowerContainer.cs` | Power 容器 UI |

---

## 2. 核心概念

### 2.1 PowerType — 效果类型

```csharp
public enum PowerType
{
    None,    // 中性效果（无颜色标识）
    Buff,    // 增益效果（绿色标识）
    Debuff   // 减益效果（红色标识）
}
```

### 2.2 PowerStackType — 叠加方式

```csharp
public enum PowerStackType
{
    None,     // 不叠加
    Counter,  // 计数器型：数值可增减，可归零或为负
    Single    // 单例型：同一生物上最多1个实例
}
```

### 2.3 Canonical vs Mutable — 原型模式

```
┌───────────────┐     ToMutable()     ┌───────────────┐
│   Canonical   │ ──────────────────► │    Mutable    │
│  (不可变模板)  │                     │  (可变实例)    │
│               │                     │               │
│  存在 ModelDb  │                     │ Owner = 某生物 │
│  全局唯一      │                     │ Amount = 具体值│
│  只读          │                     │ 可修改         │
└───────────────┘                     └───────────────┘
```

- **Canonical**：存储在 `ModelDb` 中的只读模板，代表 Power 的定义
- **Mutable**：通过 `ToMutable()` 克隆出的实例，绑定到具体生物上

### 2.4 Instanced vs Non-Instanced

| 模式 | 说明 | 例子 |
|------|------|------|
| **Non-Instanced**（默认） | 同一生物上只能有一个实例，再次施加会叠加数值 | StrengthPower: 先+3再+2 = 5 |
| **Instanced** | 同一生物上可以有多个独立实例，各自独立计时和移除 | TemporaryStrengthPower 的子类 |

### 2.5 AllowNegative — 允许负数

- 默认 `false`：数值降到 0 时自动移除
- 设为 `true`：数值可以为负（如 Strength 可以被减到 -3）
- 目前只有 `StrengthPower` 和 `DexterityPower` 设置了 `AllowNegative = true`

### 2.6 Amount 的多重语义 — "层数"不止一个含义

`Amount` 字段在不同 Power 中承载了不同的语义。理解这一点对策划设计新 Power 至关重要。

#### 三种 Amount 用法

| 用法 | Amount 含义 | DisplayAmount | 典型 Power |
|------|-------------|---------------|-----------|
| **效果强度** | 数值越大效果越强 | = Amount（默认） | StrengthPower (Amount=3 → +3伤害) |
| **持续回合数** | 倒计时，每回合 -1 | = Amount（默认） | FrailPower (Amount=2 → 持续2回合) |
| **内部计数器** | 内部逻辑用途，UI 显示另一个值 | ≠ Amount（覆写） | PanachePower (Amount=伤害值, 显示=剩余牌数) |

#### Amount vs DisplayAmount vs DynamicVars vs InternalData

```
┌──────────────────────────────────────────────────────────────────┐
│                     Power 的数值体系                               │
│                                                                   │
│  ┌─────────┐   核心数值，参与叠加/移除判定/Hook 修改               │
│  │ Amount  │   所有 PowerCmd 操作都围绕这个值                      │
│  └─────────┘                                                      │
│       │                                                           │
│  ┌──────────────┐   UI 上实际显示的数字                            │
│  │ DisplayAmount│   默认 = Amount，可覆写为任意值                   │
│  └──────────────┘                                                  │
│       │                                                            │
│  ┌─────────────┐   本地化描述中的动态占位符                         │
│  │ DynamicVars │   如 "{CardsLeft} 张牌后触发"                     │
│  └─────────────┘   可在运行时修改，自动反映到描述文本               │
│       │                                                            │
│  ┌──────────────┐   Power 私有的运行时状态                         │
│  │ InternalData │   不参与序列化，克隆时通过 InitInternalData() 重建│
│  └──────────────┘   适合存储触发标记、累计计数等                    │
│       │                                                            │
│  ┌────────────────┐   回合开始时的 Amount 快照                     │
│  │AmountOnTurnStart│  用于判断"本回合是否有变化"                    │
│  └────────────────┘                                                │
└──────────────────────────────────────────────────────────────────┘
```

#### 实际案例对照

| Power | Amount 含义 | DisplayAmount 显示 | 额外状态 |
|-------|------------|-------------------|---------|
| **StrengthPower** | 伤害加成 | Amount（直接） | 无 |
| **FrailPower** | 剩余回合数 | Amount（直接） | 无 |
| **PanachePower** | 伤害值 | DynamicVars["CardsLeft"]（剩余牌数 5→4→3→2→1→5 循环） | InternalData: 是否已触发首张 |
| **SlowPower** | 层数计数器 | DynamicVars["SlowAmount"] × 10（百分比显示） | 无 |
| **HardenedShellPower** | 本回合最大吸收量 | Max(0, Amount - 已受伤害)（剩余吸收量） | InternalData: 本回合已受伤害 |
| **FeralPower** | 免费攻击次数 | Max(0, Amount - 已打出)（剩余免费次数） | InternalData: 已打出零费攻击数 |
| **OrbitPower** | 伤害值 | 4 - 已花费能量 % 4（距触发剩余能量） | InternalData: 已花费能量 |
| **OutbreakPower** | 伤害值 | InternalData.timesPoisoned（中毒次数） | InternalData: 中毒计数 |
| **TagTeamPower** | 内部效果值 | 固定 = 1 | 无 |
| **TenderPower** | 效果值 | 本回合已打出牌数 | 内部计数器 |
| **MonologuePower** | 每回合力量值 | DynamicVars["StrengthApplied"]（已施加总量） | 无 |

#### 策划设计新 Power 时的决策清单

1. **Amount 代表什么？**
   - 效果强度（如 +X 伤害）？
   - 持续回合数（自动倒计时）？
   - 触发条件的阈值？
   - 还是纯内部使用？

2. **UI 需要显示什么数字？**
   - 如果就是 Amount → 不需要处理（默认行为）
   - 如果是另一个值 → 覆写 `DisplayAmount` 属性
   - 别忘了在值变化时调用 `InvokeDisplayAmountChanged()`

3. **是否需要额外的运行时状态？**
   - 需要 → 定义私有 `Data` 类 + 覆写 `InitInternalData()`
   - 状态在回合开始/结束时重置？→ 在对应 Hook 中处理

4. **描述文本是否需要动态数值？**
   - 需要 → 使用 `DynamicVars` + 本地化 key 中用 `{VarName}` 占位

---

## 3. PowerModel 基类 API

### 3.1 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `PowerType` | **抽象**，子类必须实现。Buff / Debuff / None |
| `StackType` | `PowerStackType` | **抽象**，子类必须实现。Counter / Single |
| `Amount` | `int` | 当前数值（层数/持续回合等） |
| `Owner` | `Creature` | 拥有此 Power 的生物（施加后不可转移） |
| `Applier` | `Creature?` | 施加者（可选） |
| `Target` | `Creature?` | 目标（可选，用于上下文） |
| `IsInstanced` | `bool` | 是否可多实例（默认 false） |
| `AllowNegative` | `bool` | 数值是否允许为负（默认 false） |

### 3.2 显示与本地化属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 本地化标题，key: `powers/{id}.title` |
| `Description` | `LocString` | 本地化描述，key: `powers/{id}.description` |
| `SmartDescription` | `LocString` | 动态描述（根据 Amount 变化） |
| `RemoteDescription` | `LocString` | 多人模式下从施加者视角看到的描述 |
| `DisplayAmount` | `int` | 显示用数值（可覆写自定义） |
| `AmountLabelColor` | `Color` | 数值标签颜色（Buff=奶白, Debuff=红） |
| `Icon` | `Texture2D` | 小图标 |
| `BigIcon` | `Texture2D` | 大图标 |

### 3.3 可见性属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsVisible` | `bool` | 是否在 UI 中显示（检查玩家上下文） |
| `IsVisibleInternal` | `bool` | 内部可见性逻辑（默认 true，可覆写） |
| `ShouldPlayVfx` | `bool` | 是否播放特效（检查 Owner 存活状态） |

### 3.4 生命周期虚方法（子类可覆写）

```csharp
// 施加前回调
virtual Task BeforeApplied(Creature target, decimal amount, 
    Creature? applier, CardModel? cardSource)

// 施加后回调
virtual Task AfterApplied(Creature? applier, CardModel? cardSource)

// 移除后回调
virtual Task AfterRemoved(Creature oldOwner)

// 数值变化后回调
virtual Task AfterPowerAmountChanged(PowerModel power, decimal amount, 
    Creature? applier, CardModel? cardSource)
```

### 3.5 回合相关虚方法

```csharp
// 回合结束前
virtual Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)

// 回合结束后
virtual Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)

// 阵营回合开始后
virtual Task AfterSideTurnStart(CombatSide side, CombatState state)
```

### 3.6 伤害修改虚方法

```csharp
// 加算修改（如 Strength: +3 伤害）
virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, 
    ValueProp props, Creature? dealer, CardModel? cardSource)

// 乘算修改（如 Vulnerable: ×1.5 伤害）
virtual decimal ModifyDamageMultiplier(Creature? target, decimal amount, 
    ValueProp props, Creature? dealer, CardModel? cardSource)
```

### 3.7 移除判定虚方法

```csharp
// 数值归零时是否移除（默认：Amount ≤ 0 时移除）
virtual bool ShouldRemoveDueToAmount()

// Owner 死亡时是否移除（默认 true）
virtual bool ShouldPowerBeRemovedAfterOwnerDeath()

// Owner 死亡是否触发致命逻辑（默认 true）
virtual bool ShouldOwnerDeathTriggerFatal()
```

### 3.8 内部数据存储

```csharp
// 初始化自定义内部数据（克隆时调用）
protected virtual object InitInternalData()

// 获取已存储的内部数据
protected T GetInternalData<T>()
```

用于 Power 内部需要跟踪额外状态的场景（如 CurlUpPower 需要记录已触发状态）。

### 3.9 事件

| 事件 | 签名 | 触发时机 |
|------|------|----------|
| `PulsingStarted` | `Action` | 开始脉冲高亮 |
| `PulsingStopped` | `Action` | 停止脉冲 |
| `Flashed` | `Action<PowerModel>` | 闪烁效果（数值变化时） |
| `DisplayAmountChanged` | `Action` | 显示数值变更 |
| `Removed` | `Action` | Power 被移除 |

---

## 4. PowerCmd 命令接口

`PowerCmd` 是操作 Power 的唯一入口，所有方法均为 `static async`。

### 4.1 施加 Power

```csharp
// 按类型施加（最常用）
static async Task<T?> Apply<T>(
    Creature target,          // 目标生物
    decimal amount,           // 数值
    Creature? applier,        // 施加者（可选）
    CardModel? cardSource,    // 来源卡牌（可选）
    bool silent = false       // 静默模式（不触发 UI 事件）
) where T : PowerModel

// 批量施加给多个目标
static async Task<IReadOnlyList<T>> Apply<T>(
    IEnumerable<Creature> targets,
    decimal amount,
    Creature? applier,
    CardModel? cardSource,
    bool silent = false
) where T : PowerModel

// 施加已有实例
static async Task Apply(
    PowerModel power,
    Creature target,
    decimal amount,
    Creature? applier,
    CardModel? cardSource,
    bool silent = false
)
```

### 4.2 修改数值

```csharp
// 修改已有 Power 的数值（增减偏移量）
static async Task<int> ModifyAmount(
    PowerModel power,
    decimal offset,           // 偏移量（+增 -减）
    Creature? applier,
    CardModel? cardSource,
    bool silent = false
)

// 设置为精确值（不存在则施加，存在则修改差值）
static async Task<T?> SetAmount<T>(
    Creature target,
    decimal amount,           // 目标值
    Creature? applier,
    CardModel? cardSource
) where T : PowerModel

// 减少 1 层
static async Task Decrement(PowerModel power)

// 持续时间递减（遵循 SkipNextDurationTick）
static async Task TickDownDuration(PowerModel power)
```

### 4.3 移除 Power

```csharp
// 按类型移除
static async Task Remove<T>(Creature creature) where T : PowerModel

// 按实例移除
static async Task Remove(PowerModel? power)
```

### 4.4 查询 Power（Creature 上的方法）

```csharp
// 检查是否拥有某 Power
bool HasPower<T>() where T : PowerModel
bool HasPower(ModelId id)

// 获取 Power 实例
T? GetPower<T>() where T : PowerModel
PowerModel? GetPower(ModelId id)

// 获取数值
int GetPowerAmount<T>() where T : PowerModel

// 获取多实例（用于 Instanced Power）
IEnumerable<T> GetPowerInstances<T>() where T : PowerModel
```

---

## 5. Power 生命周期

### 5.1 施加流程

```
PowerCmd.Apply<StrengthPower>(target, 3, applier, card)
│
├─ 1. 前置检查
│     ├─ 战斗是否已结束？ → 是则跳过
│     └─ 目标能否接收 Power？ → 否则跳过
│
├─ 2. 获取 Canonical 模板
│     └─ ModelDb.Power<StrengthPower>()
│
├─ 3. 判断是否已存在
│     ├─ YES & Non-Instanced → 走 ModifyAmount 流程（叠加）
│     └─ NO 或 Instanced → 继续新建流程
│
├─ 4. 创建 Mutable 实例
│     └─ canonical.ToMutable()
│
├─ 5. Hook 拦截链（详见第6节）
│     ├─ BeforePowerAmountChanged     ← 全局通知
│     ├─ ModifyPowerAmountGiven       ← 施加者侧修改
│     └─ ModifyPowerAmountReceived    ← 接收者侧修改 ⭐
│
├─ 6. power.BeforeApplied()           ← Power 自身前处理
│
├─ 7. ApplyInternal()
│     ├─ Owner = target
│     ├─ SetAmount(amount)
│     └─ target._powers.Add(power)
│
├─ 8. 等待动画（0.1 ~ 0.25s）
│
├─ 9. 特殊处理
│     └─ 对玩家施加 Debuff → SkipNextDurationTick = true
│
├─ 10. 后续 Hook
│      ├─ AfterModifyingPowerAmountGiven
│      ├─ AfterModifyingPowerAmountReceived
│      ├─ power.AfterApplied()
│      └─ AfterPowerAmountChanged
│
└─ 11. 返回 Power 实例（或 null）
```

### 5.2 数值修改流程

```
PowerCmd.ModifyAmount(power, +2, applier, card)
│
├─ Hook: BeforePowerAmountChanged
├─ Hook: ModifyPowerAmountGiven（施加者侧）
├─ Hook: ModifyPowerAmountReceived（接收者侧）
│
├─ power.SetAmount(newAmount)
│     ├─ 计算 delta = newAmount - oldAmount
│     ├─ 触发 DisplayAmountChanged 事件
│     └─ 触发 PowerIncreased / PowerDecreased 事件
│
├─ Hook: AfterModifyingPowerAmountGiven
├─ Hook: AfterModifyingPowerAmountReceived
│
├─ 检查: ShouldRemoveDueToAmount()？
│     └─ YES → PowerCmd.Remove(power)
│
└─ Hook: AfterPowerAmountChanged
```

### 5.3 移除流程

```
PowerCmd.Remove(power)
│
├─ power.RemoveInternal()
│     ├─ 触发 Removed 事件
│     └─ creature._powers.Remove(power)
│
├─ 等待动画（0.2 ~ 0.4s）
│
└─ power.AfterRemoved(oldOwner)
```

### 5.4 持续时间递减

```
PowerCmd.TickDownDuration(power)
│
├─ SkipNextDurationTick == true？
│     ├─ YES → 清除标记，本次不递减（返回）
│     └─ NO  → PowerCmd.Decrement(power)
│               └─ ModifyAmount(power, -1)
│                    └─ 如果 Amount ≤ 0 → 自动移除
```

> **设计意图**：对玩家施加 Debuff 时设置 `SkipNextDurationTick = true`，确保 Debuff 至少持续一个完整回合。

---

## 6. Hook 系统集成

### 6.1 Hook 执行机制

Power 继承自 `AbstractModel`，所有 Hook 通过战斗中的监听器列表统一调度：

```csharp
// 遍历战斗中所有模型（Power + Relic + Card + Modifier）
foreach (AbstractModel item in combatState.IterateHookListeners())
{
    item.SomeHookMethod(...);
}
```

### 6.2 Power 施加相关 Hook 链

```
施加 Power 时的 Hook 调用顺序：

  ① BeforePowerAmountChanged          全局通知，所有监听者
  ② ModifyPowerAmountGiven            施加者身上的 Power/Relic 可修改数量
  ③ TryModifyPowerAmountReceived      接收者身上的 Power/Relic 可拦截/修改 ⭐
  ④ AfterModifyingPowerAmountGiven    施加者侧后处理
  ⑤ AfterModifyingPowerAmountReceived 接收者侧后处理
  ⑥ AfterPowerAmountChanged           全局通知
```

**典型用例**：

| Hook | 使用者 | 效果 |
|------|--------|------|
| `TryModifyPowerAmountReceived` | `ArtifactPower` | 拦截 Debuff，消耗 1 层 Artifact |
| `ModifyPowerAmountGiven` | 某些 Relic | 增加施加的 Debuff 层数 |
| `AfterPowerAmountChanged` | 连锁效果 Power | 在某 Power 数值变化时触发额外效果 |

### 6.3 常用 Hook 一览

#### 伤害相关

| Hook | 时机 | 用途 |
|------|------|------|
| `ModifyDamageAdditive` | 伤害计算-加算阶段 | StrengthPower (+X), AccuracyPower (+X) |
| `ModifyDamageMultiplicative` | 伤害计算-乘算阶段 | VulnerablePower (×1.5), WeakPower (×0.75) |
| `ModifyDamageCap` | 伤害上限修改 | IntangiblePower (限制为1) |
| `ModifyHpLostBeforeOstyLate` | HP 扣除前 | HardenedShellPower |
| `ModifyHpLostAfterOstyLate` | HP 扣除后 | BufferPower (减免) |

#### 格挡相关

| Hook | 时机 | 用途 |
|------|------|------|
| `ModifyBlockAdditive` | 格挡计算-加算 | DexterityPower (+X) |
| `ModifyBlockMultiplicative` | 格挡计算-乘算 | FrailPower (×0.75) |
| `ShouldClearBlock` | 回合开始时是否清除格挡 | BarricadePower (保留), BlurPower (保留) |
| `AfterBlockGained` | 获得格挡后 | JuggernautPower (反弹) |
| `AfterBlockBroken` | 格挡被击破后 | BurrowedPower |

#### 卡牌相关

| Hook | 时机 | 用途 |
|------|------|------|
| `BeforeCardPlayed` | 打出卡牌前 | DanseMacabrePower, GravityPower |
| `AfterCardPlayed` | 打出卡牌后 | EnragePower, AfterimagePower |
| `AfterCardDrawn` | 抽卡后 | AutomationPower, ConfusedPower |
| `AfterCardExhausted` | 卡牌消耗后 | DarkEmbracePower, FeelNoPainPower |
| `TryModifyEnergyCostInCombat` | 修改卡牌费用 | CorruptionPower (技能牌免费) |
| `ModifyCardPlayCount` | 修改卡牌打出次数 | BurstPower, DuplicationPower, EchoFormPower |
| `ModifyCardPlayResultPileTypeAndPosition` | 修改卡牌打出后去向 | CorruptionPower (消耗), FeralPower |

#### 回合相关

| Hook | 时机 | 用途 |
|------|------|------|
| `BeforeHandDraw` | 抽牌阶段前 | CreativeAiPower, HelloWorldPower |
| `ModifyHandDraw` | 修改抽牌数量 | ClarityPower (+X), MindRotPower (-X) |
| `AfterEnergyReset` | 能量重置后 | EnergyNextTurnPower, GenesisPower |
| `ModifyMaxEnergy` | 修改最大能量 | DemesnePower, FriendshipPower |

#### 生死相关

| Hook | 时机 | 用途 |
|------|------|------|
| `BeforeDeath` | 死亡前 | HeistPower, DoorRevivalPower |
| `AfterDeath` | 死亡后 | CrabRagePower, HexPower |
| `AfterDamageReceived` | 受到伤害后 | AsleepPower (唤醒), CurlUpPower |
| `AfterDamageGiven` | 造成伤害后 | EnvenomPower (施毒), ImbalancedPower |
| `ShouldAllowHitting` | 是否允许被命中 | DieForYouPower, IllusionPower |
| `ShouldCreatureBeRemovedFromCombatAfterDeath` | 死后是否移出战场 | DieForYouPower |

---

## 7. 临时 Power 机制

### 7.1 ITemporaryPower 接口

```csharp
public interface ITemporaryPower
{
    AbstractModel OriginModel { get; }          // 来源模型（卡牌/药水等）
    PowerModel InternallyAppliedPower { get; }  // 内部实际施加的 Power
    void IgnoreNextInstance();                   // 忽略下一个同类实例
}
```

### 7.2 工作原理

```
┌──────────────────────────────────────────────────────┐
│  示例：FlexPotionPower（药水给的临时力量）              │
│                                                       │
│  继承: TemporaryStrengthPower → PowerModel             │
│  实现: ITemporaryPower                                │
│                                                       │
│  施加时 (AfterApplied):                               │
│    └─ 内部施加 StrengthPower +3 给 Owner              │
│                                                       │
│  回合结束时 (BeforeTurnEnd / AfterTurnEnd):            │
│    └─ 移除自身 → 触发 AfterRemoved                     │
│      └─ 内部移除 StrengthPower -3 从 Owner            │
│                                                       │
│  结果：本回合临时获得 +3 力量，回合结束恢复              │
└──────────────────────────────────────────────────────┘
```

### 7.3 三个抽象基类

| 基类 | 目标 Power | 已知子类 |
|------|-----------|----------|
| `TemporaryStrengthPower` | `StrengthPower` | FlexPotionPower, CoordinatePower, CrushUnderPower, DarkShacklesPower, DyingStarPower, EnfeeblingTouchPower, FeedingFrenzyPower, ManglePower |
| `TemporaryDexterityPower` | `DexterityPower` | AnticipatePower, HelicalDartPower, SpeedPotionPower |
| `TemporaryFocusPower` | `FocusPower` | FocusedStrikePower, HotfixPower |

### 7.4 临时 Power 的特殊规则

- **SkipNextDurationTick**：对玩家施加时跳过首次 Tick，确保至少持续 1 回合
- **IgnoreNextInstance**：防止同一来源的临时 Power 重复叠加
- **归属追踪**：通过 `OriginModel` 可追溯到具体的卡牌或药水
