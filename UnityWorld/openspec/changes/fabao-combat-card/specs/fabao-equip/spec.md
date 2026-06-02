## ADDED Requirements

### Requirement: NpcCardData.EquippedFaBao 字段
NpcCardData SHALL 拥有 `List<int> EquippedFaBao` 字段，记录当前已装备的法宝卡 Id 列表。

#### Scenario: 初始无装备
- **WHEN** NpcCardData 初始化
- **THEN** EquippedFaBao SHALL 为空列表

### Requirement: 装备法宝方法
NpcSystemCard SHALL 提供 `EquipFaBao(NpcCardData data, int fabaoCardId)` 方法。装备时 SHALL：
1. 将法宝卡 Id 加入 EquippedFaBao 列表
2. 标记该卡为已装备状态

招式卡的实例化不在此方法中完成，而在 AssignAllToField / 分配卡组流程中处理。

#### Scenario: 装备法宝标记
- **WHEN** 调用 EquipFaBao(data, 100)
- **THEN** data.EquippedFaBao SHALL 包含 100

### Requirement: AssignAllToField 附带招式卡
AssignAllToField（及相关分配流程）SHALL 在将已装备法宝卡分配入 Field 时：
1. 取对应 Equip.FormList
2. 实例化 FormList 中每个 CardDefineId 为 Card
3. 设新招式卡的 ParentCardId = 法宝卡 Id
4. 将招式卡加入 AllCards + AllCardIds + Field

#### Scenario: 分配时法宝带招式
- **WHEN** AssignAllToField 执行，NpcCardData 中有已装备法宝(Id=100, Equip.FormList=["card_剑刺","card_横斩"])
- **THEN** SHALL 实例化两张招式卡，ParentCardId=100，均入 Field

#### Scenario: 重复分配不重复创建
- **WHEN** AssignAllToField 连续执行两次
- **THEN** 招式卡 SHALL 只在第一次创建（通过检查是否已存在同 ParentCardId 的招式卡来避免重复）

### Requirement: InitDeck 继承 ParentCardId
CombatCard.CreateFromData SHALL 将源 Card 的 ParentCardId 复制到 CombatCard 实例上。InitDeck 无需额外逻辑处理关联。

#### Scenario: 战斗初始化继承
- **WHEN** Card(ParentCardId=100) 经 CreateFromData 生成 CombatCard
- **THEN** CombatCard.ParentCardId SHALL 为 100
