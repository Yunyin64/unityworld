# 基础法术的背包效果

## Why

这五张法术分别改变背包的扫描速度、进度和卡牌位置。回旋斩和流动可以复用现有动作，眩晕需要在背包时钟中暂停自然推进。

## What Changes

- 加速占 1 格，让己方背包双速扫描 10 个真实 Tick。爆燃给己方背包增加 20 个基准 Tick 进度，按默认间隔相当于两行。
- 回旋斩占 2 格，消耗 1 金和 1 个任意五行灵元，发起 3 点金系斩击。持有者每次发动攻击，为自己的背包充能 1 Tick。
- 流动占 2 格，消耗 2 水，从能下滑的敌方卡中随机选一张，向下滑至障碍或边界前。
- 眩晕使敌方背包停止自然推进 5 Tick。进度一旦达到一行的要求，仍可扫描。

## Capabilities

### Modified Capabilities

- `combat-backpack-scan` 规定法术对扫速、自然推进和充能的影响。
- `card-target-domain` 规定流动复用已有随机目标和滑移时的选牌结果。

## Impact

涉及背包扫描时钟、眩晕 Action 和对应卡牌 JSON、Lua。冻结迁移暂缓，原规格草案保存在 [冻结规则](deferred/combat-status-effects.md) 和 [战斗快照](deferred/battle-observer-session.md)。
