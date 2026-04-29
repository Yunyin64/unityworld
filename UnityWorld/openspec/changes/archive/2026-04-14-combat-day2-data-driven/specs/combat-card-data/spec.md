## MODIFIED Requirements

### Requirement: CardDefine 数据结构
CardDefine SHALL 包含以下字段（新增 cardType 和 manaCost）：
- ID（string）：唯一标识
- DisplayName（string）：显示名称
- Desc（string）：描述
- Rarity（int）：稀有度
- Cost（int）：卡组空间占用
- Cooldown（float）：冷却时间
- CardType（string）：卡牌类型（"ZhaoShi"/"FaShu"/"FaBao"/"DanYao"/"ZhenFa"/"ShenTong"），默认 "ZhaoShi"
- ManaCost（Dictionary<string,int>）：灵元需求，如 {"Huo":1}，默认空字典
- EffectIds（List<string>）：引用的 EffectDefine ID 列表
- Tags（List<string>）：标签列表

#### Scenario: CardDefine JSON 解析含 cardType 和 manaCost
- **WHEN** 加载 CardDefine JSON `{"id":"fireball", "cardType":"FaShu", "manaCost":{"Huo":1}, "size":1, "cooldown":5, "effectIds":["eff_fireball"]}`
- **THEN** CardDefine.CardType 为 "FaShu"，ManaCost 为 {"Huo":1}

#### Scenario: CardDefine JSON 不含 cardType 时默认
- **WHEN** 加载不含 cardType 字段的 CardDefine JSON
- **THEN** CardType 默认为 "ZhaoShi"，ManaCost 默认为空字典

### Requirement: CardData 运行时数据
CardData SHALL 包含以下字段（移除临时占位字段，新增 ManaCost）：
- Id（int）
- DefineId（string）
- Cost（int）
- Cooldown（float）
- CardType（CardType 枚举）
- ManaCost（Dictionary<string,int>）：灵元需求
- Effects（List<EffectData>）
- Tags（List<string>）

**移除** 以下临时字段：ContestValue、ContestType、PhysicalType。

#### Scenario: CardData 无临时占位字段
- **WHEN** 访问 CardData 实例
- **THEN** 不存在 ContestValue、ContestType、PhysicalType 属性

#### Scenario: CardData 含 ManaCost
- **WHEN** 从 CardDefine（manaCost={"Huo":1}）构造 CardData
- **THEN** CardData.ManaCost 为 {"Huo":1}

### Requirement: EffectData 持有 ActionData 实例列表
EffectData SHALL 将 `List<string> ActionIds` 替换为 `List<ActionData> Actions`。同时保留 ActionIds 作为只读引用（从 Actions 提取）以兼容日志和序列化需求。

#### Scenario: EffectData 初始化时实例化 ActionData
- **WHEN** 从 EffectDefine 构造 EffectData，EffectDefine.ActionIds 为 ["attack_huo_shot_3"]
- **THEN** EffectData.Actions 包含一个 ActionData 实例，其 FuncName 为 "Attack"

#### Scenario: 通过 EffectData.Actions 修改运行时数值
- **WHEN** 访问 EffectData.Actions[0].Context 并修改 "AttackValue"
- **THEN** 修改立即生效，下次读取返回新值