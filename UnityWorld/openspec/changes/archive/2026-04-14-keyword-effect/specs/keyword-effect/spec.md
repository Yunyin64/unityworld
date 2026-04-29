## ADDED Requirements

### Requirement: EffectDefine 支持 Keyword 模式
EffectDefine SHALL 新增 `IsKeyword`（bool）和 `KeywordParams`（Dictionary<string,string>）两个 JSON 属性。
当 `IsKeyword` 为 true 时，`TriggerId`/`ConditionId`/`ActionIds` 字段 SHALL 被忽略。
当 `IsKeyword` 为 false 或缺省时，`KeywordParams` 字段 SHALL 被忽略。
Keyword 的类别由 EffectDefine 的 ID 本身标识（如 `"kw_initial"`），无需额外 KeywordId 字段。

#### Scenario: Keyword EffectDefine JSON 解析
- **WHEN** JSON 中一条 ID 为 `"kw_initial"` 的 EffectDefine 设置 `"IsKeyword": true, "KeywordParams": {}`
- **THEN** EffectDefineMgr 成功加载该条目，`IsKeyword == true`，`KeywordParams` 为空字典

#### Scenario: TCA EffectDefine 向后兼容
- **WHEN** JSON 中一条 EffectDefine 未设置 `IsKeyword` 字段（或设为 false）
- **THEN** 该条目的 `IsKeyword == false`，TCA 字段（TriggerId/ConditionId/ActionIds）正常工作

### Requirement: EffectData 支持 Keyword 模式
EffectData SHALL 新增 `IsKeyword`（bool）和 `KeywordParams`（Dictionary<string,string>）两个运行时字段。
Keyword 模式的 EffectData，其 `Actions` 列表 SHALL 为空。
Keyword 的类别通过 EffectData 已有的 `DefineId` 字段标识。

#### Scenario: Keyword EffectData 构建
- **WHEN** CardMgr.BuildEffectFromDefine 遇到 IsKeyword=true 的 EffectDefine
- **THEN** 构建出的 EffectData 中 IsKeyword=true，DefineId 保留原 EffectDefine 的 ID，KeywordParams 从 Define 拷贝，Actions 为空列表

### Requirement: CardMgr 构建 Keyword Effect 分支
CardMgr.BuildEffectFromDefine SHALL 在检测到 `IsKeyword == true` 时走 Keyword 构建路径：
跳过 Trigger/Condition/Action 的加载，拷贝 KeywordParams，
仍然计算 PowerScore（使用 Define 覆盖值或默认 0）和 Tags。

#### Scenario: 构建 Initial Keyword Effect
- **WHEN** 调用 BuildEffectFromDefine 传入 ID 为 `"kw_initial"` 且 IsKeyword=true 的 EffectDefine
- **THEN** 返回的 EffectData 中 IsKeyword=true，DefineId="kw_initial"，Actions 为空，Tags 包含 Define 中配置的 Tag

### Requirement: CombatCardState 支持 CD 满值设置
CombatCardState SHALL 新增 `SetCdFull()` 公开方法，将 `CurrentCdTick` 设为 `Card.Cooldown` 的整数值。

#### Scenario: 调用 SetCdFull
- **WHEN** 一张 Cooldown=5 的卡调用 SetCdFull()
- **THEN** CurrentCdTick 变为 5，下次 CollectReadyCards 时该卡被识别为就绪

### Requirement: 战斗初始化阶段执行 Keyword
CombatScene.Start() SHALL 在设置 phase=Running 之前，遍历所有 CombatNpc 的 CardStates，
对每张卡的每个 Effect 检查 IsKeyword，按 DefineId 分发执行。
`"kw_initial"` 的处理逻辑：调用该卡的 CombatCardState.SetCdFull()。

#### Scenario: Initial 卡第一个 Tick 就绪
- **WHEN** 一张带 Initial keyword 的卡参与战斗，战斗 Start 完成后执行第一个 Tick
- **THEN** 该卡在第一个 Tick 的 CollectReadyCards 中被收集为就绪卡

#### Scenario: 非 Initial 卡正常 CD 循环
- **WHEN** 一张不带 Initial keyword 的卡参与战斗，Cooldown=5
- **THEN** 该卡在第 5 个 Tick 才会被 CollectReadyCards 收集为就绪

### Requirement: Initial Keyword 的 EffectDefine JSON 数据
Data/Effect/ 下 SHALL 存在一条 ID 为 `"kw_initial"` 的 EffectDefine JSON 条目，
IsKeyword=true，KeywordParams={}，
PowerScore=0，Tags=["初始"]。

#### Scenario: 数据完整性
- **WHEN** GameDataMgr 加载完成后查询 EffectDefineMgr.Instance.Get("kw_initial")
- **THEN** 返回非 null，IsKeyword=true，KeywordId="Initial"