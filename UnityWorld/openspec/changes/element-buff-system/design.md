## Context

战斗系统已有 CombatNpcModifier 机制（JSON Define + Lua hook），以及完整的 API 函数体系（`[APIFunc]` 标注 → `APIMgr` 反射注册）。`OnManaDraw` 事件通过 `DispatchHookToAll` 自动分发到全场 Modifier Lua 脚本。现有 `RandomBaseElementBuff(isDebuff)` 方法已实现随机选取五行 buff/debuff ID。

## Goals / Non-Goals

**Goals:**
- 提供通用的五行 Buff 添加/清除 API，供 Lua 脚本和未来卡牌使用
- 实现 10 个五行元素 Buff（金木水火土 × 正/负面），形成互克循环
- 所有效果通过 OnManaDraw hook 触发，强度 = CurrentStack

**Non-Goals:**
- 不做 UI 表现层
- 不做五行相克的自动触发（如火克金自动加成），留给未来
- 不修改现有 Modifier 框架的核心逻辑

## Decisions

### 1. 新 API 放在 CombatManaAction.cs

与灵元相关的操作集中管理。两个 API 都接收 `Element:String` 参数，"None" = 随机，否则指定元素。循环 N 次逻辑写在 C# 侧，Lua 只需一行调用。

### 2. Buff 全部 Duration:-1 + ExpirePolicy 默认(Never)

五行 Buff 不靠时间过期，而是通过载德/灼烧的清除机制对抗。形成"越打越重，靠克制解除"的策略节奏。

### 3. 效果通过 OnManaDraw hook 触发

不用 OnTick（太频繁），绑定灵元抽取节奏。已验证 `DispatchHookToAll` 会遍历全场 Modifier 的 Lua hook，无需额外注册。

### 4. RemoveElementBuff 的清除逻辑

循环 N 次，每次从目标已有的匹配类型 Modifier 中随机选一个 ReduceStack(1)。若该 Modifier 层数归零，由现有 `ModifierTick` 的 `IsExpired()` 自动移除（需设 ExpirePolicy = StackBased）。

### 5. 石化/浩瀚走 ManaConvert 逻辑

石化扣 mp：调用 `DrawMana(n)` 但不产灵元（或直接减 Mp）。浩瀚回 mp：调用 `RecoverMP(n)`。具体实现参考现有 ManaConvert 模式。

## Risks / Trade-offs

- [递归风险] 锐意/中毒给自己加 buff → 新 buff 的 OnManaDraw 不会在同一次事件中再触发（DispatchHookToAll 遍历的是事件触发时的快照）→ 安全
- [层数爆炸] MaxStack:99 + 锐意/中毒每次 ManaDraw 扩散 → 需要载德/灼烧作为自然制衡；如果失衡后续可调 MaxStack
- [ExpirePolicy] 需要将这 10 个 Buff 的 ExpirePolicy 设为 StackBased（被清除时归零过期），而非默认 TimeBased
