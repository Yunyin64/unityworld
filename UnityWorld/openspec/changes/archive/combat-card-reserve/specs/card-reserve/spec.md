## ADDED Requirements

### Requirement: NpcCardData 拥有 Field 和 Reserve 分配列表
NpcCardData SHALL 包含 `Field: List<int>` 和 `Reserve: List<int>` 两个字段，存储 cardId 引用。AllCards 逻辑不变。

#### Scenario: Npc 分配卡到 Field
- **WHEN** 卡被分配到 Field 列表
- **THEN** 该 cardId 存在于 Field 中，对应卡占用 SP

#### Scenario: Npc 分配卡到 Reserve
- **WHEN** 卡被分配到 Reserve 列表
- **THEN** 该 cardId 存在于 Reserve 中，对应卡不占用 SP

#### Scenario: 卡未分配到任何列表
- **WHEN** 卡存在于 AllCards 但不在 Field 也不在 Reserve
- **THEN** 该卡不参与战斗

### Requirement: 大世界 SP 只统计 Field
Npc 的 SP 占用 SHALL 只计算 Field 列表中卡的 Size 总和。

#### Scenario: SP 计算
- **WHEN** 查询 Npc 当前 SP 占用
- **THEN** 返回值 = Σ Field 中各 cardId 对应卡的 Size

### Requirement: CombatNpc 拥有 Reserve 静默池
CombatNpc SHALL 拥有 `Reserve: List<CombatCard>` 字段。Reserve 中的卡不参与 Tick、不参与 CD 循环、不占 SP。

#### Scenario: Reserve 中的卡不 Tick
- **WHEN** CombatNpc.Tick() 执行
- **THEN** 仅 CardDeck 中的卡执行 Tick，Reserve 中的卡不执行

#### Scenario: Reserve 中的卡不占 SP
- **WHEN** GetSp() 计算时
- **THEN** 仅统计 CardDeck 中卡的 Size，Reserve 中的卡不计入

### Requirement: 战斗初始化按 Field/Reserve 分流加载
InitDeck() SHALL 将大世界 NpcCardData.Field 中的卡加载到 CombatNpc.CardDeck，将 NpcCardData.Reserve 中的卡加载到 CombatNpc.Reserve。

#### Scenario: 正常分流加载
- **WHEN** 进入战斗执行 InitDeck()
- **THEN** Field 卡 → CardDeck，Reserve 卡 → Reserve

#### Scenario: 旧数据兼容（Field 和 Reserve 均为空）
- **WHEN** NpcCardData.Field 和 Reserve 都为空
- **THEN** fallback 到原逻辑：AllCards 全部加载到 CardDeck

### Requirement: Deploy 操作将卡从 Reserve 移入 CardDeck
CombatNpc SHALL 提供 `Deploy(CombatCard card)` 方法，将指定卡从 Reserve 移入 CardDeck。移入后卡的 CD 清零，Phase 设为 Waiting，触发 Lua hook `OnDeploy`。

#### Scenario: 成功 Deploy
- **WHEN** 调用 Deploy(card) 且 card 在 Reserve 中
- **THEN** card 从 Reserve 移除，加入 CardDeck，CD=0，Phase=Waiting，触发 OnDeploy hook

#### Scenario: Deploy 不存在于 Reserve 的卡
- **WHEN** 调用 Deploy(card) 且 card 不在 Reserve 中
- **THEN** 操作无效，输出警告日志

### Requirement: Recall 操作将卡从 CardDeck 移入 Reserve
CombatNpc SHALL 提供 `Recall(CombatCard card)` 方法，将指定卡从 CardDeck 移入 Reserve。移入后卡的 CD 清零，触发 Lua hook `OnRecall`。

#### Scenario: 成功 Recall
- **WHEN** 调用 Recall(card) 且 card 在 CardDeck 中
- **THEN** card 从 CardDeck 移除，加入 Reserve，CD=0，触发 OnRecall hook

#### Scenario: Recall 不存在于 CardDeck 的卡
- **WHEN** 调用 Recall(card) 且 card 不在 CardDeck 中
- **THEN** 操作无效，输出警告日志

### Requirement: Deploy/Recall 通过 Action API 暴露
Deploy 和 Recall SHALL 作为 Combat Action API 暴露，可被 Lua/Keyword 调用。

#### Scenario: Action 调用 Deploy
- **WHEN** Lua/Keyword 通过 Action API 调用 Deploy 指定一张 Reserve 中的卡
- **THEN** 执行 Deploy 流程

#### Scenario: Action 调用 Recall
- **WHEN** Lua/Keyword 通过 Action API 调用 Recall 指定一张 CardDeck 中的卡
- **THEN** 执行 Recall 流程
