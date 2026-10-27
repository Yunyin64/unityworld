## Why

战斗系统需要五行元素的正/负面 Buff 机制，为灵元抽取（ManaDraw）环节提供持续性的元素互动效果。五行相生相克通过 buff 的扩散与清除形成策略循环。

## What Changes

- 新增 2 个战斗 API 函数：`AddElementBuff` 和 `RemoveElementBuff`，放在 `CombatManaAction.cs`
- 在 `Element_Buff.json` 中定义 10 条 CombatNpcModifierDefine（金木水火土 × 正/负面）
- 创建 10 个 Lua 脚本，通过 `OnBaseManaDraw` hook 触发各自效果
- 所有 Buff 永久存在（Duration:-1），可叠层（MaxStack:99），效果强度 = CurrentStack

## Capabilities

### New Capabilities
- `element-buff-api`: 两个新 API（AddElementBuff / RemoveElementBuff），支持循环 N 次随机或指定元素的 buff 添加/清除
- `element-buff-defines`: 10 条五行元素 Buff 的 JSON 定义和 Lua 脚本实现

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs` — 新增 2 个 APIFunc
- `Data/Modifier/CombatModifierDefines/Element_Buff.json` — 写入 10 条定义
- `Data/Modifier/CombatModifierDefines/Lua/` — 新增 10 个 Lua 脚本
- 依赖已有：`RandomBaseElementBuff`、`AddModifier`、`ReduceStack`、`Heal`、`SelfDamage`、`Haste`、`Slow`、`ManaConvert`
