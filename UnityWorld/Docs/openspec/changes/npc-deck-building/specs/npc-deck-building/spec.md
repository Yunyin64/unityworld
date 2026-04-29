## ADDED Requirements

### Requirement: NPC 持有候选卡池（CardPool）
NPC SHALL 持有一个候选卡池（CardPool），包含该 NPC 所有拥有的 Card 实例。CardPool 中的 Card 是实例化对象，同一 DefineID 可存在多份。

#### Scenario: 功法解锁卡牌进入候选池
- **WHEN** NPC 修炼功法达到某节点阈值，且该节点类型为 Card
- **THEN** 一个新的 Card 实例 SHALL 被创建并加入该 NPC 的 CardPool

#### Scenario: 重复 DefineID 的卡牌共存
- **WHEN** NPC 从两本不同功法中各解锁了一张同 DefineID 的卡牌
- **THEN** CardPool 中 SHALL 存在两个独立的 Card 实例

### Requirement: NPC 构建战斗卡组（BattleDeck）
NPC SHALL 从 CardPool 中选取子集构成 BattleDeck，BattleDeck 中所有卡牌的 Cost 总和 MUST 不超过 NPC 的 SP 上限。

#### Scenario: 正常构建卡组
- **WHEN** NPC 需要进入战斗
- **THEN** 系统 SHALL 从该 NPC 的 CardPool 中选取卡牌组成 BattleDeck，且 Cost 总和 ≤ SP

#### Scenario: 候选卡不足以填满 SP
- **WHEN** NPC 的 CardPool 中所有卡牌的 Cost 总和 < SP
- **THEN** BattleDeck SHALL 包含 CardPool 中的全部卡牌

### Requirement: 选卡策略可替换
卡组构建 SHALL 通过可替换的选卡策略模块执行。当前阶段使用随机策略，未来可替换为其他策略（如实战水平策略）而无需修改构建流程。

#### Scenario: 随机策略构建
- **WHEN** 使用随机选卡策略构建 BattleDeck
- **THEN** 系统 SHALL 从 CardPool 中随机选取卡牌，逐张加入直到 SP 用满或无法再加入更多卡牌

#### Scenario: 策略替换不影响构建流程
- **WHEN** 选卡策略从"随机"替换为"实战水平"
- **THEN** 构建流程的调用方式 SHALL 保持不变

### Requirement: 卡牌排序是构建决策的一部分
BattleDeck 中卡牌的排列顺序 SHALL 由构建策略决定，战斗引擎按此顺序读取。当前阶段排序为随机。

#### Scenario: 随机排序
- **WHEN** 使用随机策略构建 BattleDeck
- **THEN** 卡牌排列顺序 SHALL 为随机

#### Scenario: 排序影响战斗行为
- **WHEN** BattleDeck 中卡牌顺序不同
- **THEN** 战斗引擎中待发槽的挤出逻辑等 SHALL 受到排序影响

### Requirement: 伤势卡不进入候选池
战斗中产生的伤势卡 SHALL 直接塞入 BattleDeck（战后持续存在），但 MUST NOT 进入 NPC 的 CardPool。

#### Scenario: 战斗中获得伤势卡
- **WHEN** NPC 在战斗中 HP 清零触发伤势
- **THEN** 伤势卡 SHALL 塞入当前 BattleDeck，但不加入 CardPool

#### Scenario: 战后伤势卡持续占用空间
- **WHEN** 战斗结束后 NPC 身上仍有伤势卡
- **THEN** 伤势卡 SHALL 继续占用 SP 空间，在下一次构建 BattleDeck 时自动包含

### Requirement: 实战水平概念预留
设计 SHALL 预留"实战水平"机制的概念定义：NPC 通过生成 K 种随机卡组方案、每种与标准模型对战 N 轮来选出最优卡组。K 和 N 为 NPC 属性，代表推演能力。本次不实现。

#### Scenario: 概念存在但不执行
- **WHEN** 当前阶段构建卡组
- **THEN** 系统 SHALL 使用随机策略，实战水平策略不参与执行
