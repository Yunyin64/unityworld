# Proposal

## Why

战斗配卡目录把治疗量、灵盾获得量、MP 回复量各拆成四个 Action。策划要从十二个名字里找同一类效果的目标与时机，实际结算已经共用 `AddNpcBuff` 和各自的 Lua Buff。法宝供能也已有 Buff 定义，但角色轮首仍由 C# 按定义名前缀搜出状态并执行供能。

## What Changes

- **BREAKING（可选模板 ID）** 将上述十二条目录入口合并为「治疗量调整」「灵盾获得量调整」「MP 回复量调整」。每条通过用途、点数和有效期配置原有选项。按原 ID 读取的配卡资料可迁移到等价参数。
- 法宝供能由通用 Buff 施加流程挂载，由 Buff Lua 在持有者轮首执行。现有 `action_add_artifact_power` 和卡牌 Lua 的 `AddArtifactPower(ctx, Domain, Element, Amount, Rounds)` 继续提供同一套参数。
- 旧 Lua 调用及底层派发名提供兼容，施加相同的按轮 Modifier，由同一套供能 Lua 执行。
- 复用现有数值 Buff 的定义和结算钩子。法宝供能遵循现有轮次、候选选择与蓄灵事件规则。

## Capabilities

### New Capabilities

### Modified Capabilities

- `combat-tca-catalog` 增加三类数值修正 Action 的参数、旧模板映射和生成结果要求。

## Impact

- 配卡目录、Lua 包装与抽样描述需要一起更新，使配卡预览和实际施加的状态一致。
- 轮首接入通用 Modifier 回调。供能 Lua 调用战斗已有的轮期限、场景随机和蓄灵能力。
- TCA 目录和轮次回归需要覆盖合并前后的效果等价性，以及法宝供能的顺序、到期和按种子复现。
