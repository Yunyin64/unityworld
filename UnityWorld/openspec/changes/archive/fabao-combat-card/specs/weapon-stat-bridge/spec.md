## ADDED Requirements

### Requirement: Card ParentCardId 字段
Card SHALL 拥有 `int ParentCardId` 字段，默认值为 -1（表示无父卡/非招式卡）。该字段在大世界卡组分配阶段（装备法宝 → 实例化招式卡时）设置，战斗层 CreateFromData 时继承到 CombatCard。

#### Scenario: 普通卡无父卡
- **WHEN** 非招式卡被创建
- **THEN** ParentCardId SHALL 为 -1

#### Scenario: 招式卡在大世界设置父卡
- **WHEN** 法宝装备流程中实例化 FormList 招式卡
- **THEN** 招式卡的 ParentCardId SHALL 被设为法宝卡的 Card.Id

#### Scenario: CombatCard 继承 ParentCardId
- **WHEN** CombatCard.CreateFromData(card) 执行
- **THEN** CombatCard.ParentCardId SHALL 等于源 Card.ParentCardId

### Requirement: GetEquipData 方法
Card/CombatCard SHALL 提供 `GetEquipData(): ContextBase` 方法。该方法 SHALL：
1. 通过 ParentCardId 从 EquipMgr 获取 Equip 实例
2. 若 Equip 存在，构造 ContextBase 并填入 Attack、Defend、Speed、Amount、DisplayName
3. 若 ParentCardId = -1 或 Equip 不存在，返回空 ContextBase

#### Scenario: 招式卡获取装备数据
- **WHEN** 招式卡 ParentCardId 有效且对应 Equip 存在（Attack=15, Defend=5, Speed=1.2）
- **THEN** GetEquipData() SHALL 返回 ContextBase，其中 GetValue<int>("Attack") = 15, GetValue<int>("Defend") = 5, GetValue<float>("Speed") = 1.2

#### Scenario: 无父装备获取装备数据
- **WHEN** ParentCardId = -1 的卡调用 GetEquipData()
- **THEN** SHALL 返回空 ContextBase（Get 任何 key 返回默认值）

#### Scenario: Lua 侧取值
- **WHEN** Lua 调用 `local eq = card:GetEquipData(); local atk = eq:GetValue("Attack", 0)`
- **THEN** SHALL 正确通过 ContextBase 的 GetValue 泛型方法返回对应数值

### Requirement: 装备法宝时实例化招式卡并设 ParentCardId
NpcSystemCard SHALL 提供装备法宝方法。当 Npc 装备法宝后调用 AssignAllToField（或类似分配流程）时 SHALL：
1. 识别已装备的法宝卡
2. 取对应 Equip.FormList
3. 实例化 FormList 中每个 CardDefineId 为 Card
4. 设新卡的 ParentCardId = 法宝卡 Id
5. 将招式卡加入 NpcCardData.AllCards + Field

#### Scenario: 法宝带两张招式卡装备并分配
- **WHEN** Npc 装备法宝(Id=100, Equip.FormList=["card_剑刺","card_横斩"]) 后执行 AssignAllToField
- **THEN** AllCards SHALL 新增两张卡(DefineId="card_剑刺"/"card_横斩")，其 ParentCardId SHALL 均为 100，且均在 Field 中

#### Scenario: 法宝无招式卡
- **WHEN** Npc 装备法宝(FormList=[]) 后执行 AssignAllToField
- **THEN** 卡组不变，仅法宝卡本身在 Field 中

#### Scenario: 多法宝各自独立
- **WHEN** Npc 装备法宝A(Id=100, FormList=["card_横斩"]) 和 法宝B(Id=200, FormList=["card_突刺"])
- **THEN** card_横斩.ParentCardId SHALL 为 100，card_突刺.ParentCardId SHALL 为 200
