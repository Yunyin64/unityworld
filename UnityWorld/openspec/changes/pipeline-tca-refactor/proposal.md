## Why

现有 EffectDefine 采用扁平的 Trigger + Condition + ActionIds 结构，缺少 Scale（倍率计算）、Scope（目标选择器）、Aura（持续效果）三个维度。这导致：
1. 无法表达 "每有X" 类倍率叠加效果，只能硬编码到 Lua
2. Action/Condition/Trigger 各自隐含目标选择逻辑，无法复用
3. 瞬间效果（Action）和持续效果（Aura）混在一起，生命周期管理不清晰

需要将卡牌效果描述重构为两条标准管线：ActionPipeline（事件驱动，瞬间执行）和 AuraPipeline（状态驱动，持续生效），并引入 Scope 作为所有节点通用的目标选择器。

## What Changes

- **新增 Scope 数据结构**：通用目标选择器（Owner + Filter + Selector），所有管线节点共享
- **新增 Scale 节点类型**：返回数值倍率，用于 "每有X" 类效果
- **新增 AuraPipeline 概念**：Condition? → Scale? → List\<Aura\>，无 Trigger，持续生效
- **重构 EffectDefine → ActionPipeline**：Trigger(必有) → Condition? → Scale? → List\<Action\>，每个节点携带独立 Scope
- **Trigger/Condition/Scale/Action 各自携带 Scope**：不再隐含目标，显式声明作用域
- **Func 前置要求声明**：Action/Aura 的 Func 定义中声明 Requires（如 HasCooldown），用于设计时校验

## Capabilities

### New Capabilities
- `pipeline-scope`: 通用目标选择器 Scope（Owner/Filter/Selector），可被所有管线节点引用
- `pipeline-scale`: Scale 节点类型定义与运行时求值逻辑
- `pipeline-action`: ActionPipeline 数据结构定义与执行流程（Trigger → Condition? → Scale? → List\<Action\>）
- `pipeline-aura`: AuraPipeline 数据结构定义与持续生效/失效逻辑（Condition? → Scale? → List\<Aura\>）

### Modified Capabilities
- `action-card`: CardDefine 不再引用 EffectDefine，改为持有 ActionPipeline[] + AuraPipeline[]
- `passive-card`: Passive 卡的被动效果改用 AuraPipeline 表达

## Impact

- **Data 层**: EffectDefine 结构重构；TriggerDefine / ConditionDefine / ActionDefine 增加 Scope 字段；新增 ScaleDefine、AuraDefine
- **Domain 层**: CombatCard 执行逻辑需适配新管线；API 层需新增 Scale 求值 API
- **JSON 数据**: 现有 Effect/*.json 需迁移为 ActionPipeline/AuraPipeline 格式
- **Lua 层**: 生成模板需适配新管线结构和 Scope 参数
