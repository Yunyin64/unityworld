## ADDED Requirements

### Requirement: StatEntry 存储结构
`StatEntry` SHALL 存储以下字段：
- `_statId: string`：属性 ID，用于查询 Define
- `_addValue: float`：累加账本值（默认 0）
- `_modifiers: List<StatModifier>`：修正项列表
- `_cachedFinalValue: float`：缓存的最终值
- `_isDirty: bool`：脏标记

StatEntry **不** 存储 `_baseValue` 字段。

#### Scenario: 构造时传入 statId
- **WHEN** 创建 `new StatEntry("reputation")`
- **THEN** `_statId = "reputation"`，`_addValue = 0`，`_modifiers` 为空

---

### Requirement: 三层值来源计算公式
StatEntry 的 `FinalValue` SHALL 按以下顺序计算：

```
① base = StatDefineMgr.Get(_statId)?.DefaultValue ?? 0
② base = (base + flatSum) × (1 + percentSum)
③ base = base + _addValue
④ 如果存在 Override Modifier → base = overrideValue
⑤ 应用 Modifier 的 ClampMin/ClampMax
⑥ 应用 Define 的 MinValue/MaxValue（最终硬夹紧）
```

#### Scenario: 纯 Modifier 驱动的属性
- **GIVEN** StatDefine `reputation` DefaultValue=0, MaxValue=9999
- **AND** StatEntry 有 Modifier `[Flat +20, Percent +0.5]`
- **WHEN** 计算 `FinalValue`
- **THEN** 结果为 `(0 + 20) × 1.5 + 0 = 30`

#### Scenario: 累加型属性
- **GIVEN** StatDefine `wealth` DefaultValue=0
- **AND** StatEntry `_addValue = 1000`
- **WHEN** 计算 `FinalValue`
- **THEN** 结果为 `(0 + 0) × 1 + 1000 = 1000`

#### Scenario: 混合计算
- **GIVEN** StatDefine DefaultValue=10
- **AND** StatEntry 有 Modifier `[Flat +5, Percent +0.2]`，`_addValue = 100`
- **WHEN** 计算 `FinalValue`
- **THEN** 结果为 `(10 + 5) × 1.2 + 100 = 118`

#### Scenario: Override 覆盖
- **GIVEN** StatEntry 有 Modifier `[Override value=50]`，`_addValue = 100`
- **WHEN** 计算 `FinalValue`
- **THEN** 结果为 `50`（Override 在 AddValue 之后）

#### Scenario: 两层夹紧
- **GIVEN** StatDefine MinValue=0, MaxValue=100
- **AND** StatEntry 有 Modifier `[ClampMin 20, ClampMax 80]`
- **AND** 计算中间值为 `110`
- **WHEN** 计算 `FinalValue`
- **THEN** 先被 Modifier Clamp 到 `80`，再被 Define Clamp 到 `100` 不变

---

### Requirement: AddValue 操作接口
StatEntry SHALL 提供 `Add(float amount)` 和 `SetAdd(float value)` 方法：
- `Add(amount)`：`_addValue += amount`，标记 dirty
- `SetAdd(value)`：`_addValue = value`，标记 dirty

#### Scenario: 累加财富
- **WHEN** 调用 `entry.Add(100)` 后再 `entry.Add(-50)`
- **THEN** `_addValue = 50`

---

### Requirement: Modifier 操作接口
StatEntry SHALL 提供现有 Modifier 操作：
- `AddModifier(StatModifier modifier)`
- `RemoveModifiersBySource(string sourceId)`
- `ClearModifiers()`

这些方法在执行后 SHALL 标记 `_isDirty = true`。

#### Scenario: 添加 Trait Modifier
- **WHEN** 调用 `entry.AddModifier(StatModifier.Flat(20, "trait_ brave"))`
- **THEN** Modifier 被添加，`_isDirty = true`

---

### Requirement: StatBlock 惰性创建 Entry
`StatBlock.Get(statId, defaultValue)` SHALL 按以下逻辑：

1. 查找 `_stats[statId]` 的 StatEntry
2. 若存在 → 计算 `ApplyDefineClamp(statId, entry.FinalValue)` 并返回
3. 若不存在 → 从 StatDefineMgr 查询 Define
   - 若 Define 存在 → 返回 `ApplyDefineClamp(statId, define.DefaultValue)`
   - 若 Define 不存在 → 返回 `defaultValue`

#### Scenario: 无 Entry 时返回 Define 默认值
- **GIVEN** StatBlock 为空（`_stats = {}`）
- **AND** StatDefine `reputation` DefaultValue=0
- **WHEN** 调用 `statBlock.Get("reputation")`
- **THEN** 返回 `0`（不创建 Entry）

#### Scenario: AddModifier 时创建 Entry
- **GIVEN** StatBlock 为空
- **WHEN** 调用 `statBlock.AddModifier("reputation", modifier)`
- **THEN** 自动创建 `StatEntry("reputation")` 并存入 `_stats`

---

### Requirement: StatBlock 的 Define 夹紧
StatBlock SHALL 在 `Get()` 返回前应用 Define 的 MinValue/MaxValue 硬夹紧。

#### Scenario: 超出 Define 最大值
- **GIVEN** StatDefine MaxValue=100，计算中间值=150
- **WHEN** 调用 `statBlock.Get("statId")`
- **THEN** 返回 `100`

---

### Requirement: 删除 SetBase 方法
`StatBlock.SetBase(statId, value)` 方法 SHALL 被删除，因为 Base 值现在从 Define.DefaultValue 实时读取。

#### Scenario: 编译时错误
- **WHEN** 代码尝试调用 `statBlock.SetBase("age", 10)`
- **THEN** 编译错误（方法不存在）

---

### Requirement: Define 热重载支持
当 `stat_defines.json` 被重新加载时，StatEntry 的计算 SHALL 自动使用新的 `DefaultValue`、`MinValue`、`MaxValue`，无需遍历同步。

#### Scenario: DefaultValue 变更
- **GIVEN** StatEntry 已存在，当前 Define DefaultValue=10
- **WHEN** Define 重载后 DefaultValue 变更为 20
- **THEN** 下次 `Get()` 自动返回基于 20 计算的结果

#### Scenario: MinValue 变更
- **GIVEN** 当前值=15，Define MaxValue=20
- **WHEN** Define 重载后 MaxValue 变更为 10
- **THEN** 下次 `Get()` 返回 `10`（被新 MaxValue 夹紧）
