## Context

当前战斗卡牌执行架构：CardDefine(JSON) → EffectIds → EffectDefine → Trigger/Condition/Action → APIMgr 反射调用 CombatBaseFunc。这条链路本质是一个受限的"JSON 解释器"，而底层 CombatBaseFunc 的 `[APIFunc]` 方法已经是完备的参数化指令集。

NLua 1.7.8 已在项目依赖中，LuaEventListener stub 已存在，IEventListener 接口统一了 C# 和 Lua 监听者。项目使用 .NET 8.0，无 Unity 依赖。

约束：
- Rng 必须用项目自己的，禁止 System.Random
- EventMgr 的 ScopeKey 精准投递机制保留
- CombatTestRunner.RunBasicTest() 是当前验证入口
- 卡牌随机组合生成能力必须保留（TCA 作为模板）

## Goals / Non-Goals

**Goals:**
- 卡牌逻辑执行完全迁移到 Lua 层（OnUse/OnAttack 等 Hook 函数直接调 C# API）
- 每张卡 = CardDefine.json（元数据）+ card_xxx.lua（逻辑），自包含
- TCA Define 重写为参数化模板（支持 Tag 匹配 + Score 自动计算）
- 保留运行时随机造卡能力（模板参数组合 → 生成 .lua）
- CombatTestRunner 验证 Lua 卡牌与原 C# 路径产出相同结果

**Non-Goals:**
- 不重写 CombatBaseFunc 的 API 方法（它们是不变的指令集）
- 不改变 EventMgr 的核心机制（注册/投递/ScopeKey）
- 不实现 Lua 沙箱安全机制（开发期信任脚本）
- 不做 Early/Normal/Late 执行优先级（未来方向，本次不做）
- 不处理非战斗卡牌的 Lua 化（如大世界行为卡）

## Decisions

### D1: Lua 卡牌对象模型 — 每张卡一个独立 Lua 环境

**选择**：每张 CombatCard 在加载时获得独立的 Lua 环境（通过 NLua 的 table-as-environment 机制），`self` 绑定到卡牌实例。

**替代方案**：
- 全局 State + 函数命名空间（`card_jin_whirlwind_OnUse`）→ 命名冲突风险，不支持局部状态
- 每张卡一个 Lua State → 内存开销过大

**理由**：独立环境允许卡牌有局部变量（状态记忆），同时共享一个全局 State 的 C# API 注册。

### D2: 函数名 = Trigger 注册（约定优于配置）

**选择**：框架加载 .lua 后扫描所有 `CombatCard:OnXxx` 方法名，对照内置映射表自动注册到 EventMgr。

**映射表**：
| 函数名 | 触发方式 | 对应 EventId |
|---|---|---|
| `OnUse` | 框架主动调用 | 无需注册 |
| `OnAttack` | EventMgr 事件 | `trigger_on_attack` |
| `ContestWin` | EventMgr 事件 | `trigger_on_contest_win` |
| `ContestLose` | EventMgr 事件 | `trigger_on_contest_lose` |
| `OnDominate` | EventMgr 事件 | `trigger_on_dominate` |
| `OnDominated` | EventMgr 事件 | `trigger_on_dominated` |
| `OnHitBody` | EventMgr 事件 | `trigger_on_hit_body` |
| `OnAfterCardUse` | EventMgr 事件 | `trigger_after_card_use` |

**理由**：卡牌作者（人/AI）只需定义函数，不需要写注册代码。错误更少，可读性更好。

### D3: C# API 暴露方式 — 全局函数而非对象方法

**选择**：在 Lua 全局空间暴露 `Attack()`、`Charge()`、`Heal()` 等函数（不需要 `CombatCmd.` 前缀）。

**替代方案**：
- `CombatCmd.Attack()` 命名空间 → 多打字，AI 容易漏
- `ctx:Attack()` 挂在 context 上 → 语义不清

**理由**：全局函数最简洁，和现有 ActionDefine 的 FuncName 字段直接对应。卡牌脚本只有战斗 scope，不存在命名冲突。

### D4: ActionDefine 参数化格式

**选择**：每个 ActionDefine 的 Params 变为数组，每项包含 Name/Type/Value[]/Score[]。

```json
{
  "ID": "action_atk",
  "FuncName": "Attack",
  "Tags": ["攻击"],
  "Params": [
    {"Name": "Element", "Type": "String", "Value": ["Jin","Mu","Shui","Huo","Tu"], "Score": [0,0,0,0,0]},
    {"Name": "PhysicalType", "Type": "String", "Value": ["Zhan","Da","Ci","SheJi"], "Score": [0,0,0,1]},
    {"Name": "AttackValue", "Type": "Int", "Value": [1,2,3,4,5], "Score": [1,2,3,4,5]}
  ]
}
```

**理由**：一条 Define 覆盖所有参数排列组合，分数自动从各 Param 的选择项累加。不再需要 action_atk_1, action_atk_2...。

### D5: CombatScene 集成策略 — Lua 卡优先，无 .lua 回退 Effect 路径

**选择**：CombatScene 加载卡牌时，检查 `Data/LuaCards/{cardId}.lua` 是否存在。存在则走 Lua 路径，不存在则回退原 Effect 解释路径。

**理由**：允许渐进迁移，不需要一次性转换所有卡。

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| NLua 性能（大量卡同时 Tick）| 战斗卡数量有限（<20张/人），Lua 调用频率低（CD 制），可接受 |
| Lua 脚本错误导致战斗崩溃 | LuaMgr 包裹 try-catch，错误时 LogMgr.Instance.Err + 跳过该效果 |
| 现有卡牌数据迁移成本 | 建立自动转化脚本（tools/），旧 TCA 数据可机械翻译为 .lua |
| 随机造卡质量 | Score 系统保留，参数化后组合空间更大但分数可控 |
| self 状态泄漏/持久化 | 卡牌 Lua 环境随 CombatScene 生命周期创建销毁，不跨战斗 |