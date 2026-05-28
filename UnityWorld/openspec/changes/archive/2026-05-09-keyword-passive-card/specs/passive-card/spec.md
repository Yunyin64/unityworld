## ADDED Requirements

### Requirement: CombatCardPhase.Passive 枚举值
`CombatCardPhase` 枚举 SHALL 包含 `Passive` 值，表示卡牌处于被动模式，不参与 CD 循环。

#### Scenario: Passive 枚举值存在
- **WHEN** 代码引用 `CombatCardPhase.Passive`
- **THEN** 编译通过且枚举值可正常使用

### Requirement: CardDefine Keywords 字段
`CardDefine` SHALL 包含 `List<string> Keywords` 字段，支持 JSON 反序列化（JsonPropertyName "keywords"）。默认值为空列表。

#### Scenario: JSON 中配置 Keywords
- **WHEN** CardDefine JSON 包含 `"keywords": ["Passive"]`
- **THEN** 反序列化后 CardDefine.Keywords 包含 "Passive"

#### Scenario: JSON 中未配置 Keywords
- **WHEN** CardDefine JSON 不包含 "keywords" 字段
- **THEN** CardDefine.Keywords 为空列表

### Requirement: CardBaseData Keywords 字段
`CardBaseData` SHALL 包含 `List<string> Keywords` 字段。`Clone()` 方法 SHALL 深拷贝该列表。

#### Scenario: Clone 深拷贝 Keywords
- **WHEN** 对一个含有 Keywords = ["Passive"] 的 CardBaseData 调用 Clone()
- **THEN** 克隆体的 Keywords 列表内容相同但引用不同（修改克隆体不影响原体）

### Requirement: CombatCard.Tick() 跳过 Passive 卡的 CD 循环
CombatCard.Tick() SHALL 在 Phase == Passive 时，仅调用 keyword hooks 的 OnTick 和卡自身 Lua env 的 `OnPassiveTick`（如果定义了），然后 return，不执行 Waiting/InCD/Ready 的状态转换逻辑。

#### Scenario: Passive 卡的 Tick
- **WHEN** 一张 Phase == Passive 的卡执行 Tick()
- **THEN** 不调用 CheckMana()、不调用 ResetCD()、不改变 Phase、不增加 CD 计时器

#### Scenario: 主动卡的 Tick 不受影响
- **WHEN** 一张 Phase == Waiting 的卡执行 Tick()
- **THEN** 正常走 CheckMana → InCD → ResetCD → Ready 流程，行为与改动前完全一致

### Requirement: Passive.lua 实现
`Keywords/Passive.lua` SHALL 返回一个 table，其中定义 `OnPreStart(card, ctx)` 函数。该函数 SHALL 调用 `card:SetPhase("Passive")` 将卡牌设为被动模式。

#### Scenario: Passive keyword 生效
- **WHEN** 一张 Keywords 包含 "Passive" 的卡执行 PreStart()
- **THEN** 该卡的 Phase 被设置为 CombatCardPhase.Passive，后续 Tick 不走 CD 循环
