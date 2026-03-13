## ADDED Requirements

### Requirement: ModifierBase 是所有修正源的抽象基类
系统 SHALL 提供 `ModifierBase` 抽象基类，定义所有修正源的公共属性：唯一 ID、来源标识（SourceId）、持续时间（Duration，-1 表示永久）、剩余时间（RemainingTime）。所有具体修正类型 MUST 继承此基类。

#### Scenario: 有限时修正源可追踪剩余时间
- **WHEN** 创建一个 Duration = 10 的 ModifierBase 子类实例
- **THEN** RemainingTime 初始为 10，IsExpired 返回 false

#### Scenario: 永久修正源永不过期
- **WHEN** 创建一个 Duration = -1 的 ModifierBase 子类实例
- **THEN** IsExpired 始终返回 false，无论经过多少 Tick

#### Scenario: 有限时修正源倒计时归零后标记过期
- **WHEN** RemainingTime 减少至 0 或以下
- **THEN** IsExpired 返回 true

### Requirement: TileModifier 继承 ModifierBase 并携带元气修正数据
`TileModifier` SHALL 继承 `ModifierBase`，额外持有 `AuraData`（五行各元素的每 Tick 变化量，可正可负），用于表达对 Tile CurrentAura 的修正强度。

#### Scenario: TileModifier 可描述单一元素的正向修正
- **WHEN** 创建 AuraData.Huo = +3.0、其余为 0 的 TileModifier
- **THEN** 该 modifier 每 Tick 使所在 Tile 的火元气增加 3.0，其他元素不变

#### Scenario: TileModifier 可描述多元素的复合修正
- **WHEN** 创建 AuraData.Huo = +2.0、AuraData.Shui = -1.0 的 TileModifier
- **THEN** 每 Tick 火元气 +2.0，水元气 -1.0

#### Scenario: AuraData 全为零的 TileModifier 不改变元气
- **WHEN** TileModifier.AuraData 五行全为 0，经过一次 Tick
- **THEN** Tile.CurrentAura 数值不发生变化

### Requirement: NpcModifier 和 CardModifier 预留为空扩展点
系统 SHALL 提供 `NpcModifier` 和 `CardModifier` 继承自 `ModifierBase` 的空类占位，用于标明扩展意图，本期不实现具体数据字段。

#### Scenario: NpcModifier 可被实例化而不报错
- **WHEN** 实例化 NpcModifier（使用 ModifierBase 构造参数）
- **THEN** 不抛出异常，IsExpired 行为与基类一致

#### Scenario: CardModifier 可被实例化而不报错
- **WHEN** 实例化 CardModifier（使用 ModifierBase 构造参数）
- **THEN** 不抛出异常，IsExpired 行为与基类一致
