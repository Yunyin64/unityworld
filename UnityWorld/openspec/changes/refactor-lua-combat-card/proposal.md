## Why

当前 Lua 战斗卡牌系统采用"工厂式"架构：LuaMgr 持有按 cardId 共享的 LuaTable，所有同 ID 卡牌共享同一个 Lua 环境，导致重复卡状态互相污染；调用链绕经 LuaMgr.CallCardHook() 中转，CombatCard 自身无法直接执行 Lua 逻辑。同时 LuaBridge 作为多余的翻译层存在，而 Lua 完全可以直接调用 C# 对象。整体代码冗余、职责混乱，需要重构为"实例式"架构。

此外，传统 TCA 管线（Trigger→Condition→Action）的运行时数据层（ActionData/EffectData/CardEffectData）在实际卡牌 JSON 中并未使用（EffectIds 已被注释），所有卡牌效果逻辑应统一迁移到 Lua 脚本，TCA 管线代码予以废弃。

## What Changes

- **BREAKING**: CombatCard 自己持有独立的 LuaTable 实例（从模板克隆），每张卡互不干扰
- **BREAKING**: 删除 LuaBridge，Lua 直接调用 C# 对象（CombatBaseFunc 等）
- **BREAKING**: LuaMgr 简化为模板管理器，不再负责 ctx 构造和 hook 调用中转
- **BREAKING**: CombatCard.OnUse/OnTick 等方法直接走自己的 _luaTable，不经过 LuaMgr
- Lua 脚本模板标准化：继承 CardBase 元表 + CardData 数据表 + OnXxx 函数
- CardData 从 Lua 回写覆盖 CombatCard 的 Define 默认值（Lua 优先，Define 保底）
- OnXxx 分为两类：实例方法（C# 直接调）和 Trigger 钩子（EventMgr 事件驱动）
- CombatBaseFunc.Attack/Shield/Block 补全实现（构造拼点数据放入代发槽，而非直接造成伤害）
- **BREAKING**: 废弃 TCA 管线运行时数据层（ActionData/EffectData/CardEffectData），所有卡牌效果逻辑统一走 Lua
- 新增 init.lua：创建 CardBase 基表、默认空实现、全局辅助函数
- 用 Python 脚本批量从 CardDefine JSON 生成 Lua 卡牌骨架

## Capabilities

### New Capabilities
- `lua-card-instance`: CombatCard 持有独立 LuaTable 实例，模板克隆机制，CardData 回写，OnXxx 实例方法直接调用
- `lua-script-template`: Lua 卡牌脚本标准模板（CardBase 元表继承、CardData、Keywords、OnXxx 函数定义规范）
- `tca-pipeline-deprecation`: 废弃 TCA 管线运行时数据层，清理 ActionData/EffectData/CardEffectData 及相关引用

### Modified Capabilities
- `combat-base-func`: Attack/Shield/Block 从 TODO 占位改为构造 ContestData 放入 PendingSlot

## Impact

- **LuaMgr.cs**: 大幅简化，删除 CreateContextTable/CallCardHook，新增 CloneTemplate
- **CombatCard.cs**: 新增 _luaTable 字段、OnUse/OnTick 等实例方法、CardData 回写逻辑；移除 BuildContestData/IsAttackDefenseCard 等 TCA 相关方法
- **LuaBridge.cs**: 删除
- **ActionData.cs / EffectData.cs / CardEffectData.cs**: 废弃删除
- **CardMgr.cs**: 移除 BuildEffectFromDefine 等 TCA 构造逻辑
- **CombatCardFlowHandler.cs**: 改为调 card.OnUse()，移除 TCA 效果结算路径
- **CombatScene.Setup.cs**: 适配新构造流程，移除 TCA Trigger 注册
- **CombatContestHandler.cs**: 移除 TCA 效果结算
- **ContestData.cs**: 移除 FromActionData 工厂方法，改为从 Lua 调用路径构造
- **CombatBaseFunc.cs**: 补全 Attack/Shield/Block 实现
- **Data/LuaScripts/init.lua**: 新增 CardBase 基表初始化
- **Data/LuaCards/*.lua**: 按新模板批量生成 + 逐个完善
- **tools/gen_lua_card_skeletons.py**: 新增批量生成脚本
