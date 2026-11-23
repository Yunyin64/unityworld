# 统一拼点与伤害数值 Buff

## Why

拼点强化已有 28 个状态，但弱化缺少对应规则。伤害方向已有少量旧定义，配卡时也缺少按出伤、入伤和生效时机划分的动作。统一正负层数后，同一种规则可以兼顾强化和弱化，策划仍能直接选到目标和用途明确的动作。

## What Changes

- 将 14 类拼点强化改为 `<类型>_Changed` 和 `<类型>_Changed_Once`，共 28 个状态。正层加点，负层减点，适用于全攻击、物理类型、五行攻击、全防御和防御类型。
- 将通用配卡动作 `action_add_npc_buff` 的目录入口拆成八个拼点 Action。由动作固定己方强化或对手弱化、攻击或防御、持续或单次，配置只需选范围和强度等参数。
- 伤害沿用相同的正负层数与持续、单次结构。出伤覆盖增伤和削弱输出，入伤覆盖减伤和易伤。拟按全部、四种现有物理类型和五行分别配，共 40 个状态与八个伤害 Action。
- 继续使用 `AddNpcBuff` 运行时接口。迁移现有内置调用，并为原内置状态 ID 保留兼容映射，避免旧 Lua 的强化或斩击抗性失效。五行状态、卸力、护甲、坚甲和灵盾继续按各自现行规则结算。

## Capabilities

### New Capabilities

### Modified Capabilities

- `combat-contest-scheduling`：在持有者生成拼点时支持有符号修正，保留持续和单次的生效时机。
- `combat-status-effects`：规定出伤与入伤修正、有符号层数及消耗。
- `combat-tca-catalog`：提供八个拼点动作和八个伤害动作，固定目标、方向和生命周期。

## Impact

涉及 `ActionTemplate_Buff.json`、`Contest_Improved.json`、`Damage_Add.json`、战斗 Modifier 生命周期、拼点及伤害结算入口、旧 Lua 和内置调用与回归用例。**BREAKING（配卡模板 ID）**。通用动作目录 ID `action_add_npc_buff` 由八条拼点模板替代，Lua 接口签名保留。
