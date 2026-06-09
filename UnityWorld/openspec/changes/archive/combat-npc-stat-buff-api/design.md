## Context

CombatNpc 在战斗开始时通过 `npc.Stats.Snapshot()` 获得一份独立的 StatBlock 副本，战斗结束即丢弃。当前对战斗属性的临时修正只能通过 CombatNpcModifier（重量级：Define + Lua + 生命周期管理）来做。StatBlock 本身已有完善的 `AddModifier(statId, StatModifier)` 和 `RemoveModifiersBySource(sourceId)` 机制，只是没有在 CombatNpc 层暴露便捷入口。

## Goals / Non-Goals

**Goals:**
- 提供 `AddStatBuff` / `RemoveStatBuff` 两个方法，让卡牌效果和 Lua 脚本能一行代码施加永久属性修正
- 无需新建 Define、无需 JSON 配置、无需计时器
- sourceId 可选——不传时自动生成，传了可用于精准移除
- Lua 侧可直接调用（CombatNpc 已是 ILuaBindable）

**Non-Goals:**
- 不做有限时长支持（需要时限的走 CombatNpcModifier + Define）
- 不做 UI 展示/图标/tooltip（这是匿名修正，不需要面向玩家呈现）
- 不修改现有 CombatNpcModifier 的 StatModifiers 接线逻辑（那是另一个独立课题）

## Decisions

### 1. 直接封装 StatBlock 已有 API，不引入新数据结构

**选择**：`AddStatBuff` 内部直接调用 `Stats.AddModifier(statId, new StatModifier(type, value, sourceId))`，不维护额外列表。

**理由**：
- StatBlock 已经能按 SourceId 精准移除，不需要再包一层
- 战斗 Snapshot 结束即丢，不需要追踪"有哪些轻量 buff"
- 最少代码、最少状态、最少出 bug 的可能

**替代方案**：维护一个 `List<StatBuffTimer>` 跟踪所有轻量修正 → 既然 Duration 固定 -1 不需要计时，多此一举。

### 2. sourceId 缺省策略

**选择**：sourceId 不传时使用 `$"StatBuff_{statId}_{Guid.NewGuid():N8}"`（前8位 GUID）。

**理由**：
- 确保不会和其他修正冲突
- 不传 sourceId = 调用者不打算主动移除，属于"放火不管"的永久增益
- 如果调用者要移除，必须自己传 sourceId 并保存

### 3. 方法放在哪个 partial file

**选择**：新建 `CombatNpcStatBuffFunc.cs`（partial class CombatNpc）。

**理由**：
- 与现有 `CombatNpcModifierFunc.cs`（重量级 Modifier）、`CombatNpcData.cs`（属性/血量）平行
- 职责单一：这个文件只管轻量 StatBuff 的增删

## Risks / Trade-offs

- **[Risk] sourceId 不传时无法移除** → 这是设计意图（永久存活），文档注释中明确说明。如果调用者需要后续移除，必须显式传 sourceId。
- **[Trade-off] 不跟踪已施加的轻量 buff 列表** → 无法查询"当前有哪些轻量 buff"。如未来需要此能力，可追加一个只读查询接口，但目前 YAGNI。
- **[Risk] Lua 侧 ModifierType enum 传参** → Lua 传字符串 "Flat"/"Percent"，C# 侧需做 Enum.Parse 转换。实现时加一个字符串重载或在方法内 parse。
