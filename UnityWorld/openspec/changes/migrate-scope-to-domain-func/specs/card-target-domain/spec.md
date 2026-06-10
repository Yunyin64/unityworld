## ADDED Requirements

### Requirement: APIDomainFunc 提供完整的 Card Domain 选卡能力
APIDomainFunc SHALL 支持以下 Domain key，每个 key 对应一种相对于 ctx.SourceCard 的选卡策略，返回 `List<CombatCard>`。

#### Scenario: Domain "All" 返回 caster 所有场上卡
- **WHEN** 调用 `GetTargetCard("All", ctx)` 且 ctx.Caster 不为 null
- **THEN** 返回 `caster.GetField()` 的全部卡牌

#### Scenario: Domain "Random" 返回一张随机 CD 中的卡
- **WHEN** 调用 `GetTargetCard("Random", ctx)` 且 caster 有 InCD 状态的卡
- **THEN** 返回包含 1 张随机 InCD 卡牌的列表，使用 `Scene.Soul.Random()` 确定性随机

#### Scenario: Domain "Random" 无 InCD 卡时返回空
- **WHEN** 调用 `GetTargetCard("Random", ctx)` 且 caster 无 InCD 状态的卡
- **THEN** 返回空列表

#### Scenario: Domain "LeftOne" 返回上方一张卡
- **WHEN** 调用 `GetTargetCard("LeftOne", ctx)` 且 SourceCard 不在首位
- **THEN** 返回 SourceCard 在 Field 中的前一张卡（index - 1）

#### Scenario: Domain "LeftOne" 在首位时返回空
- **WHEN** 调用 `GetTargetCard("LeftOne", ctx)` 且 SourceCard 在 Field 首位（index == 0）
- **THEN** 返回空列表

#### Scenario: Domain "LeftAll" 返回上方所有卡
- **WHEN** 调用 `GetTargetCard("LeftAll", ctx)` 且 SourceCard 不在首位
- **THEN** 返回 Field 中 SourceCard 之前的所有卡（index 0 到 index-1）

#### Scenario: Domain "RightOne" 返回下方一张卡
- **WHEN** 调用 `GetTargetCard("RightOne", ctx)` 且 SourceCard 不在末位
- **THEN** 返回 SourceCard 在 Field 中的后一张卡（index + 1）

#### Scenario: Domain "RightAll" 返回下方所有卡
- **WHEN** 调用 `GetTargetCard("RightAll", ctx)` 且 SourceCard 不在末位
- **THEN** 返回 Field 中 SourceCard 之后的所有卡（index+1 到末尾）

#### Scenario: Domain "Adjacent" 返回相邻卡
- **WHEN** 调用 `GetTargetCard("Adjacent", ctx)` 且 SourceCard 有邻居
- **THEN** 返回 SourceCard 上方和下方各一张卡（最多2张）

#### Scenario: Domain "Self" 返回自身卡牌
- **WHEN** 调用 `GetTargetCard("Self", ctx)` 且 ctx.SourceCard 不为 null
- **THEN** 返回仅包含 ctx.SourceCard 的列表

#### Scenario: SourceCard 为 null 时 fallback
- **WHEN** 任何相对位置 Domain（LeftOne/LeftAll/RightOne/RightAll/Adjacent/Self）被调用且 ctx.SourceCard 为 null
- **THEN** 返回空列表

### Requirement: Lua Action 包装函数接收 Domain 字符串参数
Action.lua 中的 Charge 等函数 SHALL 接收 domain 字符串参数，通过 `ctx:Set("Domain", domain)` 传入 C# 侧，由 C# Action 内部调用 APIDomainFunc 选卡。

#### Scenario: Charge Lua 包装函数签名
- **WHEN** Lua 调用 `Charge(ctx, "LeftAll", 10)`
- **THEN** ctx 中设置 Domain="LeftAll"、ReduceTick=10，C# Charge 通过 Domain 选卡并执行充能

#### Scenario: LuaTemplate 展开为正确调用
- **WHEN** ActionTemplate 中 `"LuaTemplate": "Charge(ctx, \"{DoMain}\", {ReduceTick})"` 被展开
- **THEN** DoMain 值（如 "All"）作为带引号的字符串传入 Charge 函数

### Requirement: CombatBaseScope 标记废弃
CombatBaseScope.cs SHALL 保留代码但在文件顶部加废弃注释，不影响编译。

#### Scenario: Scope 代码保留但不再是主路径
- **WHEN** 系统正常运行
- **THEN** CombatBaseScope 中的函数仍可通过 API:Execute 调用（兼容），但新代码不再使用此路径
