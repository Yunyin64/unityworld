# 保留待实现的 TCA 配置

## Why

部署、召回、位移及部分触发和选择器仍有设计价值。上次对齐从配置中移除了这些条目，导致后续开发失去原 ID、参数和评估数据。

## What Changes

- 恢复原有设计条目，以 `Pending` 记录其开发状态。
- 继续用 `Ready` 表示已接入运行规则的条目。现行 Domain 选择器使用自己的 ID。
- 在目录、Lua 触发映射和回归中按状态处理，保留现有战斗行为。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `combat-tca-catalog` — 保留未完设计及其原始配置。

## Impact

涉及触发、条件和动作配置及其加载类型、Lua 触发映射、接口目录和 TCA 回归。
