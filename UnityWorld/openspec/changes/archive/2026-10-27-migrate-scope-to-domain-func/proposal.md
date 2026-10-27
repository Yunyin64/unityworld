## Why

战斗系统中"选取目标卡牌"存在两套并行机制：CombatBaseScope（[APIFunc] 反射注册，通过 ctx.Set 返回）和 APIDomainFunc（字典分发，直接返回 List）。两套做同一件事，维护成本翻倍，且 APIDomainFunc 当前是空壳实现。需要统一为 APIDomainFunc 一条路径，让 Action 自带选卡语义（Domain 参数），消除 Scope 中间层。

## What Changes

- **填充 APIDomainFunc 真实选卡逻辑**：All / Random / LeftOne / LeftAll / RightOne / RightAll / Adjacent / Self
- **改造 Action.lua 的 Charge 包装函数**：签名从 `(ctx, cards, reduceTick)` 改为 `(ctx, domain, reduceTick)`，内部通过 Domain 参数选卡
- **更新 ActionTemplate_CardCD.json 的 LuaTemplate**：DoMain 作为字符串参数传入
- **更新手写 Lua 卡牌脚本**：card_jin_charge.lua 等改为直接调用 Action + Domain，不再调 Scope
- **标记 CombatBaseScope.cs 废弃**：代码保留，加 `[Obsolete]` 注释，不删除
- **补全 Npc 维度的 DomainFunc**：Self / Target（已有注册，填充实现）

## Capabilities

### New Capabilities
- `card-target-domain`: APIDomainFunc 统一选卡逻辑，

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/!Global/API/APIDomainFunc.cs` — 填充实现
- `Scripts/Game/Domain/!Global/API/Combat/Action/CombatCDAction.cs` — Charge 确认对齐
- `Scripts/Game/Domain/!Global/API/Combat/Scope/CombatBaseScope.cs` — 标记废弃
- `Data/LuaScripts/Action.lua` — Charge 签名变更
- `Data/LuaScripts/Scope.lua` — 标记废弃
- `Data/Action/ActionTemplate_CardCD.json` — LuaTemplate 更新
- `Data/Card/Lua/card_jin_charge.lua` 等手写脚本 — 改用新写法
