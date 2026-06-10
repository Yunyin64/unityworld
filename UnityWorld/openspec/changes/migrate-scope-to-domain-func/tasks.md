## 1. APIDomainFunc 填充选卡逻辑

- [x] 1.1 读取用户已手动修改的 APIDomainFunc.cs，补全所有 Domain key 的真实实现（Self/Random/Other/Adjacent/AboveOne/AboveAll/BelowOne/BelowAll/All/TargetAll/TargetRandom）
- [x] 1.2 补全 Npc 维度的 GetSelfNpc / GetTargetNpc 实现（CombatNpc 需暴露 GetTarget() 方法）
- [x] 1.3 在 APIMgr 中补全 GetTargetNpc 的转发方法（类似 GetTargetCard）

## 2. C# Action 侧对齐

- [x] 2.1 确认 CombatCDAction.Charge 从 ctx 取 "Domain" 字符串并调用 APIMgr.GetTargetCard，逻辑正确
- [x] 2.2 改造 Freeze：加入 Domain 选卡逻辑（从 ctx 取 Domain → GetTargetCard → 遍历执行）
- [x] 2.3 改造 Slow：加入 Domain 选卡逻辑
- [x] 2.4 改造 Haste：加入 Domain 选卡逻辑
- [x] 2.5 检查 AddCardStatBuff / Displace 等是否需要同步改造

## 3. Lua Action 包装函数改造

- [x] 3.1 修改 Action.lua 中 Charge 函数签名为 `(ctx, domain, reduceTick)`，内部 `ctx:Set("Domain", domain)`
- [x] 3.2 修改 Action.lua 中 Freeze 函数签名加入 domain 参数
- [x] 3.3 修改 Action.lua 中 Slow 函数签名加入 domain 参数
- [x] 3.4 修改 Action.lua 中 Haste 函数签名加入 domain 参数
- [x] 3.5 检查 Action.lua 中其他函数是否也需要 Domain 化（AddCardStatBuff / Displace 等涉及目标卡的）

## 4. ActionTemplate JSON 更新

- [x] 4.1 更新 ActionTemplate_CardCD.json 的 LuaTemplate 和 ParamDefs
- [x] 4.2 更新 ActionTemplate_Contest.json 的 LuaTemplate 和 ParamDefs
- [x] 4.3 更新 ActionTemplate_Mana.json 的 LuaTemplate 和 ParamDefs
- [x] 4.4 更新 ActionTemplate_Reserve.json 的 LuaTemplate 和 ParamDefs
- [x] 4.5 更新 ActionTemplate_Deck.json 的 LuaTemplate 和 ParamDefs
- [x] 4.6 更新 ActionTemplate_Buff.json 的 LuaTemplate 和 ParamDefs
- [x] 4.7 更新 ActionTemplate_Effect.json 的 LuaTemplate 和 ParamDefs
- [x] 4.8 所有 DoMain Value 列表统一使用 Above/Below 命名

## 5. 手写 Lua 卡牌脚本迁移

- [x] 5.1 改造 card_jin_charge.lua：去掉 AdjacentCards 调用，改为 `Charge(ctx, "LeftAll", 10)`
- [x] 5.2 检查 card_huo_burst.lua / card_shui_freeze.lua / card_tu_slow.lua 是否需要同步改造

## 6. 标记废弃

- [x] 6.1 CombatBaseScope.cs 顶部加废弃注释
- [x] 6.2 Scope.lua 顶部加废弃注释
