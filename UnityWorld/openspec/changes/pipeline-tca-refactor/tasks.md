## 1. Scope 基础设施

- [ ] 1.1 创建 `ScopeOwner` 枚举（Self/Enemy/Any/None）于 Scripts/Game/Data/Enum/
- [ ] 1.2 创建 `ScopeSelector` 类（Type + Count）于 Scripts/Game/Data/Defines/
- [ ] 1.3 创建 `Scope` 类（Owner + Filters + Selector）于 Scripts/Game/Data/Defines/
- [ ] 1.4 实现 Scope 求值逻辑（按 Owner→Filters→Selector 顺序筛选目标）于 Domain/Combat/ 中

## 2. 管线节点数据结构

- [ ] 2.1 创建 `TriggerNode` 类（EventId + Scope）于 Scripts/Game/Data/Defines/
- [ ] 2.2 创建 `ConditionNode` 类（Check + Scope + ParamValues）于 Scripts/Game/Data/Defines/
- [ ] 2.3 创建 `ScaleNode` 类（Query + Scope）于 Scripts/Game/Data/Defines/
- [ ] 2.4 创建 `ActionNode` 类（FuncId + Value + Scope + Requires）于 Scripts/Game/Data/Defines/
- [ ] 2.5 创建 `AuraNode` 类（FuncId + Value + Scope + Requires）于 Scripts/Game/Data/Defines/

## 3. 管线数据结构

- [ ] 3.1 创建 `ActionPipeline` 类（Trigger + Condition? + Scale? + List\<ActionNode\>）
- [ ] 3.2 创建 `AuraPipeline` 类（Condition? + Scale? + List\<AuraNode\>）
- [ ] 3.3 CardDefine 增加 `ActionPipelines` 和 `AuraPipelines` 字段（List，默认空）

## 4. 管线执行引擎

- [ ] 4.1 实现 ActionPipeline 执行器：事件匹配 → Condition 求值 → Scale 求值 → 遍历 Actions 执行
- [ ] 4.2 实现 AuraPipeline 执行器：Tick 检测 Condition → Scale 求值 → 挂载/更新/移除 Modifier
- [ ] 4.3 ActionPipeline 执行器集成到 CombatCard 事件响应流程中
- [ ] 4.4 AuraPipeline 执行器集成到 CombatCard Tick 流程中

## 5. Modifier 集成

- [ ] 5.1 AuraNode 生效时通过 Modifier 系统创建对应 Modifier 实例
- [ ] 5.2 AuraNode 失效时（条件不满足/来源消失）移除对应 Modifier
- [ ] 5.3 Scale 值变化时更新 Modifier 的效果数值

## 6. 旧系统兼容

- [ ] 6.1 EffectDefine 标记 `[Obsolete]` 但保留功能
- [ ] 6.2 CombatCard 运行时判断：有 Pipeline 走新逻辑，无则走旧 Keyword/Lua 逻辑
- [ ] 6.3 验证现有卡牌（无 Pipeline 字段）行为不变

## 7. 数据模板与验证

- [ ] 7.1 创建示例 ActionPipeline JSON 数据文件（1-2 张示例卡）
- [ ] 7.2 创建示例 AuraPipeline JSON 数据文件（1-2 张示例被动卡）
- [ ] 7.3 实现管线 JSON 校验逻辑（Func.Requires vs Scope.Filters 兼容性检查，仅警告不报错）
