# card-target-domain Specification

## Purpose

规定战斗效果通过 Domain 字符串选择卡牌或角色的统一入口，明确空目标、随机候选与空间选择的行为，并区分背包扫描机制与保留的旧线性兼容接口，避免调用者继续依赖废弃的单卡冷却逻辑。

## Requirements

### Requirement: APIDomainFunc 提供完整的 Card Domain 选卡能力
APIDomainFunc SHALL 支持以下 Domain key，按施放者、对手或来源卡选择目标并返回 `List<CombatCard>`。AboveAll 使用背包二维占格；AboveOne、BelowOne、BelowAll 和 Adjacent 保留旧 Field 线性兼容语义，不代表已实现二维邻接设计。

#### Scenario: Domain "All" 返回 caster 所有场上卡
- **WHEN** 调用 `GetTargetCard("All", ctx)` 且 ctx.Caster 不为 null
- **THEN** 返回 `caster.GetField()` 的全部卡牌

#### Scenario: Domain "Random" 返回一张随机场上卡
- **WHEN** 调用 `GetTargetCard("Random", ctx)` 且 caster 有场上卡
- **THEN** 按实例 ID 稳定排序候选后，返回包含 1 张随机场上卡的列表，使用 `Scene.Soul.Random()` 确定性随机，不要求 InCD 状态

#### Scenario: Domain "Random" 无场上卡时返回空
- **WHEN** 调用 `GetTargetCard("Random", ctx)` 且 caster 无场上卡
- **THEN** 返回空列表

#### Scenario: Domain "AboveOne" 返回上方一张卡
- **WHEN** 调用 `GetTargetCard("AboveOne", ctx)` 且 SourceCard 不在首位
- **THEN** 返回 SourceCard 在 Field 中的前一张卡（index - 1）

#### Scenario: Domain "AboveOne" 在首位时返回空
- **WHEN** 调用 `GetTargetCard("AboveOne", ctx)` 且 SourceCard 在 Field 首位（index == 0）
- **THEN** 返回空列表

#### Scenario: Domain "AboveAll" 返回上方所有卡
- **WHEN** 调用 `GetTargetCard("AboveAll", ctx)` 且 SourceCard 有有效背包占格
- **THEN** 返回整个本方背包中完全位于来源卡上方的实例：目标最下方 Y 严格小于来源最上方 Y，不限列且按实例去重；无有效来源占格时返回空列表

#### Scenario: Domain "BelowOne" 返回下方一张卡
- **WHEN** 调用 `GetTargetCard("BelowOne", ctx)` 且 SourceCard 不在末位
- **THEN** 返回 SourceCard 在 Field 中的后一张卡（index + 1）

#### Scenario: Domain "BelowAll" 返回下方所有卡
- **WHEN** 调用 `GetTargetCard("BelowAll", ctx)` 且 SourceCard 不在末位
- **THEN** 返回 Field 中 SourceCard 之后的所有卡（index+1 到末尾）

#### Scenario: Domain "Adjacent" 返回相邻卡
- **WHEN** 调用 `GetTargetCard("Adjacent", ctx)` 且 SourceCard 有邻居
- **THEN** 返回 SourceCard 上方和下方各一张卡（最多2张）

#### Scenario: Domain "Self" 返回自身卡牌
- **WHEN** 调用 `GetTargetCard("Self", ctx)` 且 ctx.SourceCard 不为 null
- **THEN** 返回仅包含 ctx.SourceCard 的列表

#### Scenario: SourceCard 为 null 时 fallback
- **WHEN** 任何相对位置 Domain（AboveOne/AboveAll/BelowOne/BelowAll/Adjacent/Self）被调用且 ctx.SourceCard 为 null
- **THEN** 返回空列表

### Requirement: Lua Action 包装函数接收 Domain 字符串参数
Lua Action 包装函数 SHALL 接收 Domain 字符串，通过 `ctx:Set("Domain", Domain)` 传入 C#。Charge 的 Self/Target 选择角色。兼容卡选择器按 Owner 去重后作用于整包，ReduceTick 表示正常速度 Tick 进度。

#### Scenario: Charge Lua 包装函数签名
- **WHEN** Lua 调用 `Charge(ctx, "Self", 10)`
- **THEN** ctx 中设置 Domain="Self"、ReduceTick=10，C# Charge 对本方背包增加 10 个基准 Tick 进度，不受加减速倍率影响

#### Scenario: LuaTemplate 展开为正确调用
- **WHEN** ActionTemplate 中 `Charge(ctx, "{Domain}", {ReduceTick})` 选择 Domain 为 Target，ReduceTick 为 10
- **THEN** Lua 调用为 `Charge(ctx, "Target", 10)`，为施放者的当前对手增加 10 个基准 Tick 进度

### Requirement: CombatBaseScope 标记废弃
CombatBaseScope.cs SHALL 保留代码但在文件顶部加废弃注释，不影响编译。

#### Scenario: Scope 代码保留但不再是主路径
- **WHEN** 系统正常运行
- **THEN** CombatBaseScope 中的函数仍可通过 API:Execute 调用（兼容），但新代码不再使用此路径

### Requirement: Lua 选择模板返回统一的卡牌列表

`SelectCards(ctx, Domain)` SHALL 使用 `APIDomainFunc`，返回独立的 `List<CombatCard>`。找不到符合条件的卡时返回空列表。目录 SHALL 使用 `All`、`TargetAll`、`Random`、`TargetRandom` 和 `Adjacent` 提供选卡模板。随机选择至多一张，采用本场随机数生成器。

#### Scenario: 选取对手场上卡牌

- **WHEN** Lua 调用 `SelectCards(ctx, "TargetAll")`
- **THEN** 返回实际对手的场上卡牌列表，本方卡和候补卡不进入结果，无对手时返回空列表

#### Scenario: 沿场上顺序选相邻卡

- **WHEN** Lua 调用 `SelectCards(ctx, "Adjacent")` 且来源卡位于场上顺序中间
- **THEN** 返回场上顺序中紧邻来源卡的前后各一张，边界仅返回存在的一侧，无有效来源时返回空列表

### Requirement: 二维方位选择、匹配与计数使用实际共边

系统 SHALL 从来源卡占格按上、下、左、右、邻选择同一背包场上卡。上、下、左、右要求指定方向上有至少一对占格正交共边，邻取四向并集。结果每卡只出现一次，按行、列、实例排序。选择、存在判断和计数 SHALL 支持按 Keyword 过滤。空来源或空候选返回空集合、假及零。

#### Scenario: 一张横卡接触多个格

- **WHEN** A 占 (2,2)(3,2)，B 占 (2,1)(3,1)，C 占 (4,2)，D 只与 A 的角相接
- **THEN** A 的上方匹配 B，右方匹配 C，邻居数为 2，D 不计入

### Requirement: 位置规则可限定动作和邻卡成功发动监听

TCA SHALL 允许以二维选择结果作为动作目标，并以监听卡为参考判断实际使用卡是否位于指定方向。成功发动的监听 SHALL 使用本次提交时的占格，按一次成功 Use 派发一次。仅点亮、位置移动和失败扫描均不触发使用。

#### Scenario: 发动过程中移动

- **WHEN** A 的右邻 B 成功发动，B 的效果把自己移到 A 左边
- **THEN** A 的右侧使用条件对这次事件成立。下一次发动按当时布局重新判断
