## ADDED Requirements

### Requirement: Lua 选择模板返回统一的卡牌列表

`SelectCards(ctx, Domain)` SHALL 使用 `APIDomainFunc`，返回独立的 `List<CombatCard>`。找不到符合条件的卡时返回空列表。目录 SHALL 使用 `All`、`TargetAll`、`Random`、`TargetRandom` 和 `Adjacent` 提供选卡模板。随机选择至多一张，采用本场随机数生成器。

#### Scenario: 选取对手场上卡牌

- **WHEN** Lua 调用 `SelectCards(ctx, "TargetAll")`
- **THEN** 返回实际对手的场上卡牌列表，本方卡和候补卡不进入结果，无对手时返回空列表

#### Scenario: 沿场上顺序选相邻卡

- **WHEN** Lua 调用 `SelectCards(ctx, "Adjacent")` 且来源卡位于场上顺序中间
- **THEN** 返回场上顺序中紧邻来源卡的前后各一张，边界仅返回存在的一侧，无有效来源时返回空列表

## MODIFIED Requirements

### Requirement: Lua Action 包装函数接收 Domain 字符串参数
Lua Action 包装函数 SHALL 接收 Domain 字符串，通过 `ctx:Set("Domain", Domain)` 传入 C#。Charge 的 Self/Target 选择角色。兼容卡选择器按 Owner 去重后作用于整包，ReduceTick 表示正常速度 Tick 进度。

#### Scenario: Charge Lua 包装函数签名
- **WHEN** Lua 调用 `Charge(ctx, "Self", 10)`
- **THEN** ctx 中设置 Domain="Self"、ReduceTick=10，C# Charge 对本方背包增加 10 个基准 Tick 进度，不受加减速倍率影响

#### Scenario: LuaTemplate 展开为正确调用
- **WHEN** ActionTemplate 中 `Charge(ctx, "{Domain}", {ReduceTick})` 选择 Domain 为 Target，ReduceTick 为 10
- **THEN** Lua 调用为 `Charge(ctx, "Target", 10)`，为施放者的当前对手增加 10 个基准 Tick 进度
