# 对齐现有战斗 TCA

## Why

背包扫描上线后，TCA 目录仍含旧事件、独立 CD 选卡和已禁用动作。部分模板与实际 Lua 签名或结算不同，策划按目录配效果会得到错误结果。

## What Changes

- 触发配置跟随实际战斗广播，明确发动、效果结算、对拼胜负与角色事件的发生时机。
- **BREAKING** 选择模板共用现行卡牌 Domain 和统一的 Lua 返回值。旧 CD 选择、线性位移和候补动作退出可选目录。
- 修正现有动作参数与结算。治疗受最大 HP 限制，浩瀚在基础补灵前按层恢复 MP。
- 检验目录样例能通过运行时 Lua 接口执行。卡牌内容和新增战斗机制留待后续。

## Capabilities

### New Capabilities

- `combat-tca-catalog` — 规定可复用触发、选择与动作模板的运行契约。

### Modified Capabilities

- `card-target-domain` — 从 Lua 使用 Domain 选卡，明确范围与返回值。
- `element-buff-defines` — 修正浩瀚调用签名，并核对基础补灵时序。

## Impact

涉及 `Data/TriggerDefines.json`、条件和动作模板、Lua 包装导出、浩瀚 Modifier、战斗治疗和控制台测试。由现有战斗事件和 C# API 提供执行能力。
