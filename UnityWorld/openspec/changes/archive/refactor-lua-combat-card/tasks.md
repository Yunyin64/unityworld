## 1. LuaMgr 重构

- [ ] 1.1 将 `_cardEnvironments` 改名为 `_templates`，语义从"环境"改为"模板"
- [ ] 1.2 删除 `CreateContextTable()` 方法
- [ ] 1.3 删除 `CallCardHook()` 方法
- [ ] 1.4 删除 `DiscoverHooks()` 方法（钩子发现改由 CombatCard 负责）
- [ ] 1.5 新增 `CloneTemplate(cardId)` 方法：Lua 侧浅拷贝模板 + setmetatable 继承
- [ ] 1.6 修改 `RegisterCSharpAPIs()`：注册 C# 类型到 Lua 全局空间（CombatBaseFunc 等），替代注册 LuaBridge 方法
- [ ] 1.7 修改 `CreateIsolatedEnvironment()`：创建 CardBase 全局基表并设置 `setmetatable(CardBase, { __index = _G })`
- [ ] 1.8 修改 `LoadCardScript()`：脚本返回值存入 `_templates[cardId]`（脚本 return card）
- [ ] 1.9 新增 init.lua 加载：LuaMgr.Init() 时首先执行 `Data/LuaScripts/init.lua`

## 2. init.lua

- [ ] 2.1 创建 `Data/LuaScripts/init.lua`：CardBase 基表 + 默认空实现 + 全局辅助函数（Attack/Shield/Block/Heal/Charge）

## 3. CombatCard 改造

- [ ] 3.1 新增 `LuaTable? _luaTable` 私有字段
- [ ] 3.2 新增 `InitLuaTable(LuaTable template)` 方法：克隆模板 → 注入 CS_Card → CardData 回写
- [ ] 3.3 实现 CardData 回写逻辑：从 _luaTable["CardData"] 读取字段覆盖 BaseData 副本和 CooldownTicks
- [ ] 3.4 确保 CombatCard 拥有 BaseData 副本（不共享大世界 Card 的 BaseData 引用）
- [ ] 3.5 新增 `OnUse(ActionContext ctx)` 实例方法：IsLuaCard 时调 _luaTable["OnUse"]
- [ ] 3.6 新增 `OnTick(ActionContext ctx)` 实例方法：IsLuaCard 时调 _luaTable["OnTick"]
- [ ] 3.7 新增通用 `CallLuaHook<bool>(string hookName, ActionContext ctx)` 方法供其他 OnXxx 复用
- [ ] 3.8 新增 Trigger 钩子扫描与 EventMgr 注册逻辑

## 4. 删除 LuaBridge

- [ ] 4.1 删除 `Scripts/Core/Systems/LuaBridge.cs` 文件
- [ ] 4.2 清理所有对 LuaBridge 的引用

## 5. CombatBaseFunc 补全

- [ ] 5.1 实现 `Attack(ctx, element, contestType, value)`：直接构造 ContestData 放入 caster.PendingSlot
- [ ] 5.2 实现 `Shield(ctx, shieldValue)`：直接构造 ContestData 放入 caster.PendingSlot
- [ ] 5.3 实现 `Block(ctx, blockValue)`：直接构造 ContestData 放入 caster.PendingSlot

## 6. TCA 管线废弃

- [ ] 6.1 删除 `Scripts/Game/Domain/Object/Card/Data/ActionData.cs`
- [ ] 6.2 删除 `Scripts/Game/Domain/Object/Card/Data/EffectData.cs`
- [ ] 6.3 删除 `Scripts/Game/Domain/Object/Card/Data/CardEffectData.cs`
- [ ] 6.4 移除 `Card.EffectData` 属性和 `Card.Effects` 便捷访问器
- [ ] 6.5 移除 `CombatCard.BuildContestData()` 和 `IsAttackDefenseCard()`
- [ ] 6.6 移除 `ContestData.FromActionData()` 及 Weapon Keyword 辅助方法
- [ ] 6.7 简化 `CardMgr.InstantiateFromDefine()`：只构造 BaseData，移除 BuildEffectFromDefine()
- [ ] 6.8 清理 `CombatCardFlowHandler` 中 TCA 效果结算路径
- [ ] 6.9 清理 `CombatScene.Setup` 中 TCA Trigger 注册
- [ ] 6.10 清理 `CombatContestHandler` 中 TCA 效果结算
- [ ] 6.11 清理 `CombatSlotHandler` 中 IsAttackDefenseCard 引用
- [ ] 6.12 修复所有编译引用断裂

## 7. 战斗流程适配

- [ ] 7.1 修改 `CombatCardFlowHandler.ResolveLuaOnUse()`：改为调 `card.OnUse(ctx)` 而非 `LuaMgr.CallCardHook()`
- [ ] 7.2 修改 `CombatScene.Setup.InitializeLuaCards()`：适配新构造流程（LoadCardScript → CloneTemplate → InitLuaTable）
- [ ] 7.3 修改 `LuaEventListener`：适配新的调用方式（通过 CombatCard 而非 LuaMgr）
- [ ] 7.4 清理 CombatCardFlowHandler 中旧的 Lua 调用路径

## 8. Python 批量生成脚本

- [ ] 8.1 创建 `tools/gen_lua_card_skeletons.py`：从 CardDefine JSON 批量生成 Lua 卡牌骨架
- [ ] 8.2 运行脚本生成所有卡牌的 Lua 骨架文件
- [ ] 8.3 逐个审查生成的 Lua 骨架，补充效果逻辑

## 9. 验证

- [ ] 9.1 编译通过，无引用断裂
- [ ] 9.2 战斗场景启动无报错，Lua 卡牌正常加载
- [ ] 9.3 重复卡 Lua 状态独立验证
- [ ] 9.4 init.lua 正确初始化 CardBase 基表