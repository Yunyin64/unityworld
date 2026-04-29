## Context

当前 Lua 战斗卡牌系统采用"工厂式"架构：
- LuaMgr 按 cardId 存储共享 LuaTable（`_cardEnvironments`），同 ID 的多张卡共享同一个 Lua 环境
- 调用链绕经 `LuaMgr.CallCardHook(cardId, hookName, ctxTable)` 中转
- LuaBridge 作为 LuaTable → ActionContext 的翻译层存在
- CombatCard 只有 `IsLuaCard` 标记，不持有 LuaTable
- CombatBaseFunc.Attack/Shield/Block 是 TODO 占位

核心问题：重复卡 Lua 状态互相污染、调用链冗余、LuaBridge 多余、拼点 API 未实现。

TCA 管线现状：
- ActionData/EffectData/CardEffectData 是 TCA 管线的运行时数据层
- 实际 Card JSON 中未使用 EffectIds（CardDefine.cs 中已注释），TCA 管线是空壳
- 所有卡牌效果逻辑应统一走 Lua，TCA 运行时数据层予以废弃

## Goals / Non-Goals

**Goals:**
- CombatCard 自己持有独立 LuaTable 实例，每张卡互不干扰
- Lua 脚本能直接调用 C# 对象，无需翻译层
- LuaMgr 简化为模板管理器
- Lua 脚本模板标准化（CardBase 元表 + CardData + OnXxx）
- CardData 从 Lua 回写覆盖 Define 默认值
- OnXxx 分为实例方法和 Trigger 钩子两类
- CombatBaseFunc.Attack/Shield/Block 补全（构造拼点数据）

**Non-Goals:**
- 不做 ILuaBindable 通用接口抽象（未来 Buff/Story 复用时再做）
- 不改 CombatContestHandler 的拼点结算逻辑
- 不改 EventMgr 事件系统本身
- 不做 Lua 调试工具

## Decisions

### D1: CombatCard 持有 LuaTable 实例

**选择**：CombatCard 新增 `LuaTable _luaTable` 字段，构造时从 LuaMgr 模板克隆。

**替代方案**：
- A) 保持 LuaMgr 集中管理 → 重复卡状态污染，否决
- B) 每张卡重新 DoString 执行脚本 → 性能差，否决
- C) 克隆模板（setmetatable 继承）→ 实例只存覆盖值，读时 fallback 到模板，✅ 选择

**克隆方式**：在 Lua 侧执行克隆脚本：
```lua
local copy = {}
for k, v in pairs(template) do copy[k] = v end
setmetatable(copy, { __index = template })
```
实例覆盖的值（如 ChargeCount）写在 copy 上，函数从模板继承。

### D2: 删除 LuaBridge

**选择**：删除 LuaBridge.cs，Lua 直接调用 C# 静态方法（CombatBaseFunc 等）。

**理由**：NLua 支持直接调用 C# 对象，LuaBridge 的唯一作用是 LuaTable → ActionContext 翻译，如果直接传 C# 对象则翻译层无存在必要。如需简化 Lua 调用语法，在 Lua 侧写辅助函数即可。

### D3: LuaMgr 简化为模板管理器

**选择**：LuaMgr 只负责：
1. Lua State 生命周期
2. 加载脚本 → 存模板到 `_templates`
3. `CloneTemplate(cardId)` → 复制实例给 CombatCard
4. 注册 C# 类型到 Lua 全局

删除：`CreateContextTable()`、`CallCardHook()`、`_cardEnvironments`（改为 `_templates`）

### D4: Lua 脚本模板标准化

**选择**：每张 Lua 卡牌脚本遵循固定模板：
```lua
local card = setmetatable({}, { __index = CardBase })
card.CardData = { ... }
card.Keywords = { ... }
function card:OnUse(ctx) ... end
return card
```

CardBase 元表由 LuaMgr 在环境初始化时设置 `setmetatable(CardBase, { __index = _G })`，脚本无需重复设置。

### D5: CardData 回写机制

**选择**：CombatCard 构造后，从 `_luaTable["CardData"]` 读取字段覆盖 C# 侧值。

优先级：Lua CardData > Define 默认值。回写字段映射：
- `CardData.Size` → `BaseData.Size`（需副本）
- `CardData.Cooldown` → `CooldownTicks = Cooldown * 10`
- `CardData.CardType` → `BaseData.CardType`
- `CardData.ManaCost` → `BaseData.ManaCost`

CombatCard 必须有自己的 BaseData 副本，避免修改影响大世界原始 Card。

### D6: OnXxx 分类

**实例方法**（C# 在特定时机主动调用）：
- OnUse / OnTick / OnDraw / OnDiscard 等
- 调用方式：`card.OnUse(ctx)` → `_luaTable["OnUse"](_luaTable, ctx)`

**Trigger 钩子**（EventMgr 事件驱动）：
- OnAfterCardUse / OnAttack / OnTakeDamage 等
- 注册方式：构造时扫描 _luaTable，发现 OnXxx → 查 HookToEventId → 注册 LuaEventListener

### D7: Attack/Shield/Block 实现为构造拼点数据

**选择**：CombatBaseFunc.Attack() 构造 ContestData 并放入 caster.PendingSlot，而非直接造成伤害。

**理由**：拼点/直击结算由 CombatContestHandler 统一处理，Attack 只负责"声明意图"。

### D8: 废弃 TCA 管线运行时数据层

**选择**：删除 ActionData.cs、EffectData.cs、CardEffectData.cs，所有卡牌效果逻辑统一走 Lua。

**理由**：
- Card JSON 中未使用 EffectIds（已注释），TCA 管线是空壳
- Lua OnUse/OnTick 等函数完全替代 TCA 的 Trigger→Condition→Action 执行链
- ContestData 不再从 ActionData 构造，改为从 CombatBaseFunc.Attack() 等直接构造

**清理范围**：
- CardMgr: 移除 BuildEffectFromDefine()、TCA 构造逻辑
- CombatCard: 移除 BuildContestData()、IsAttackDefenseCard()
- CombatCardFlowHandler: 移除 TCA 效果结算路径
- CombatScene.Setup: 移除 TCA Trigger 注册
- CombatContestHandler: 移除 TCA 效果结算
- ContestData: 移除 FromActionData()，改为直接构造

**保留**：CardDefine/CardDefineMgr（JSON 基础属性仍需加载）、ActionDefine/TriggerDefine/ConditionDefine（Define 层保留，未来可能复用）

### D9: init.lua 初始化脚本

**选择**：新增 `Data/LuaScripts/init.lua`，在 LuaMgr 初始化时首先执行。

**内容**：
```lua
-- CardBase 基表
CardBase = {}
setmetatable(CardBase, { __index = _G })

-- 默认空实现
function CardBase:OnUse(ctx) end
function CardBase:OnTick(ctx) end
function CardBase:OnDraw(ctx) end
function CardBase:OnDiscard(ctx) end

-- 全局辅助函数（简化 Lua 调用语法）
function Attack(ctx, element, contestType, value)
    CombatBaseFunc.Attack(ctx, element, contestType, value)
end
function Shield(ctx, value)
    CombatBaseFunc.Shield(ctx, value)
end
function Block(ctx, value)
    CombatBaseFunc.Block(ctx, value)
end
function Heal(ctx, value)
    CombatBaseFunc.Heal(ctx, value)
end
function Charge(ctx, cardId, amount)
    CombatBaseFunc.Charge(ctx, cardId, amount)
end
```

**理由**：CardBase 提供默认空实现，实例不写 OnTick 也不会报错；辅助函数让 Lua 脚本更简洁。

### D10: Python 脚本批量生成 Lua 卡牌骨架

**选择**：新增 `tools/gen_lua_card_skeletons.py`，从 CardDefine JSON 批量生成 Lua 卡牌骨架。

**逻辑**：
1. 读取 Data/Card/*.json 中所有卡牌定义
2. 对每张卡生成 `Data/LuaCards/card_{define_id}.lua`
3. 自动填充 CardData（从 JSON 的 Size/Cooldown/CardType/ManaCost 映射）
4. OnUse 等函数生成空骨架（带注释占位）
5. 已存在的 Lua 文件跳过（不覆盖手动编写的内容）

**理由**：50+ 张卡手动写骨架太慢，脚本批量生成后逐个审查完善。

## Risks / Trade-offs

- **[NLua C# 对象暴露范围]** → Lua 可以调任何注册到全局的 C# 类型，需要明确哪些类型注册到 Lua 全局空间，避免误调用。缓解：只注册必要的类型（CombatBaseFunc、枚举等）。
- **[LuaTable 克隆性能]** → 每张卡克隆一次，战斗开始时批量执行。缓解：克隆是浅拷贝 + metatable 继承，开销很小。
- **[CardData 回写时机]** → 必须在 CombatCard 构造之后、战斗 Tick 之前完成。缓解：在 CombatScene.Setup.InitializeLuaCards() 中统一处理。
- **[TCA 废弃的引用清理]** → ActionData/EffectData 被多处引用，清理需全面。缓解：编译器会报引用断裂，逐个修复。
- **[批量生成骨架的准确性]** → py 脚本生成的 CardData 映射可能有遗漏。缓解：生成后逐个审查，脚本可反复运行。
