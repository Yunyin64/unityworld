## Why

当前卡牌执行链路经过 6 层间接（CardDefine → EffectIds → EffectDefine → TriggerId/ConditionId → ActionIds → APIMgr 反射），只为了执行两行逻辑。Effect 层是一个"不完全信息"的声明式抽象——它无法表达条件分支、局部状态、跨Effect协作、动态数值计算。

而底层 C# API（`[APIFunc]` 方法如 Attack/Charge/Heal）已经是参数化的、完备的指令集。把执行层从"C# 解释 JSON"迁移到"Lua 直接调用 C# API"成本为零——终点是同一批方法。Lua 化后每张卡的逻辑自包含、人/AI 可读写，同时保留 TCA 模板的随机组合生成能力。

## What Changes

- **BREAKING** 移除 CardDefine 中的 `EffectIds` 字段，卡牌不再通过 Effect 链路执行
- **BREAKING** EffectDefine 不再作为运行时引用，降级为生成工具的中间数据（或废弃）
- 新增 LuaMgr（NLua State 管理、C# API 暴露、脚本加载）
- 新增 `Data/LuaCards/` 目录，每张卡一个 `.lua` 文件
- 重写 ActionDefine 为参数化模板格式（Params 带取值范围 + Score）
- 重写 ConditionDefine 为参数化模板格式
- 修改 CombatScene 的卡牌初始化流程：加载 .lua → 扫描函数名 → EventMgr 注册
- 实现 LuaEventListener 真实桥接（替换当前 stub）
- 建立批量转化工具：现有 TCA 卡牌 → 生成 .lua 文件

## Capabilities

### New Capabilities
- `lua-runtime`: Lua 运行时基础设施（LuaMgr 生命周期、C# API 暴露、脚本加载与函数发现、EventMgr 自动注册）
- `lua-card-script`: Lua 卡牌脚本规范（CombatCard 对象绑定、OnUse/OnAttack 等 Hook 函数、CardData/Keywords 声明）
- `tca-template-format`: TCA 模板新格式（ActionDefine/ConditionDefine 参数化、Score 自动计算、Tag 匹配生成）

### Modified Capabilities
- （无已有 spec 需修改，这是全新的执行架构）

## Impact

- **代码**：Scripts/Core/Systems/（LuaMgr 新建、LuaEventListener 修改）、Scripts/Game/Domain/Combat/（CombatScene 初始化流程修改）
- **数据**：Data/Action/、Data/ConditionDefines.json（重写为参数化格式）、Data/Card/（移除 EffectIds）、Data/LuaCards/（新建）
- **依赖**：NLua 1.7.8（已在 .csproj 中）
- **工具链**：tools/ 下新增批量转化脚本
- **兼容性**：Effect 路径代码保留但不再被新卡使用，渐进迁移