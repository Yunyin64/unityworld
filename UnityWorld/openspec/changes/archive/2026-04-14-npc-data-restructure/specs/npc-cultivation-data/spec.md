## ADDED Requirements

### Requirement: NpcCultivationData 道途与境界字段
`NpcCultivationData` SHALL 包含以下道途与境界字段：
- `Path` (PracticePath)：道途类型（None/Ling/Wu/Hun 等）
- `CurrentRealmLevel` (int)：当前境界等级（0=凡人，1+=修士）
- `RealmProgress` (int)：境界进度值（用于突破判定）
- `IsInCultivation` (bool)：是否正在闭关修炼

#### Scenario: 凡人注册修行数据
- **WHEN** 凡人 NPC 被创建并注册到 NpcSystemPractice
- **THEN** Path MUST 为 PracticePath.None，CurrentRealmLevel MUST 为 0，IsInCultivation MUST 为 false

#### Scenario: 修士注册修行数据
- **WHEN** 修士 NPC 被创建，指定 Path=Ling, RealmLevel=2
- **THEN** NpcCultivationData 的 Path MUST 为 Ling，CurrentRealmLevel MUST 为 2

### Requirement: NpcCultivationData 寿元上限
`NpcCultivationData` SHALL 包含 `LifespanMax` (float) 字段，表示当前寿元上限（含修行延寿后的总值）。
凡人 NPC 的 LifespanMax 由 NpcDefine 模板的基础值赋予。修行者的 LifespanMax 随境界提升而增长。

#### Scenario: 凡人寿元上限
- **WHEN** 凡人 NPC 被创建，NpcDefine.BaseLifespanMax = 80
- **THEN** NpcCultivationData.LifespanMax MUST 为 80

#### Scenario: 寿元耗尽判定
- **WHEN** NPC 的 AgeAccumulated（从 BioData 读取）>= LifespanMax
- **THEN** NpcSystemPractice MUST 判定该 NPC 寿元耗尽

### Requirement: NpcCultivationData 灵根
`NpcCultivationData` SHALL 包含 `SpiritRoot` (string) 字段，表示灵根类型（如「火灵根」「双灵根」）。
凡人 NPC 的 SpiritRoot 为 null。

#### Scenario: 凡人无灵根
- **WHEN** 凡人 NPC 被创建
- **THEN** SpiritRoot MUST 为 null

### Requirement: NpcCultivationData 八大基础属性
`NpcCultivationData` SHALL 包含 `Properties` 字段，类型为 `BaseProperty` struct。
`BaseProperty` SHALL 包含以下 8 个 int 字段：
- `QiXue`（气血）：身体气血充盈程度
- `TiPo`（体魄）：身体强度与耐力
- `QiGan`（炁感）：对天地灵气的感知能力
- `LingJi`（灵机）：灵感与直觉敏锐度
- `ShenShi`（神识）：精神力强度与探知范围
- `WuXing`（悟性）：领悟功法与道理的速度
- `JiYuan`（机缘）：遇到机缘的概率修正
- `MeiLi`（魅力）：社交与影响力加成

所有字段默认值为 0。

#### Scenario: 创建修士时设置基础属性
- **WHEN** 修士 NPC 被创建并指定 BaseProperty 各字段值
- **THEN** NpcCultivationData.Properties 的每个字段 MUST 等于指定值

#### Scenario: 通过 Get 方法读取单个属性
- **WHEN** 调用 NpcSystemPractice 的 GetQiXue(npc) 等便捷方法
- **THEN** MUST 返回对应 BaseProperty 字段的值

### Requirement: NpcCultivationData 战斗三维
`NpcCultivationData` SHALL 包含以下战斗属性字段（直接平铺，不封装 struct）：
- `HpMax` (int)：HP 上限
- `MpMax` (int)：MP 上限（灵力）
- `SpMax` (int)：SP 上限（魂力）

所有字段默认值为 0。

#### Scenario: 创建修士时设置战斗三维
- **WHEN** 修士 NPC 被创建并指定 HpMax=100, MpMax=50, SpMax=30
- **THEN** NpcCultivationData 的 HpMax MUST 为 100, MpMax MUST 为 50, SpMax MUST 为 30

### Requirement: NpcCultivationData 五行亲和
`NpcCultivationData` SHALL 包含 `Affinity` 字段，类型为 `ElementalAffinity` struct。
`ElementalAffinity` SHALL 包含以下 5 个 int 字段：
- `Jin`（金）
- `Mu`（木）
- `Shui`（水）
- `Huo`（火）
- `Tu`（土）

所有字段默认值为 0。

#### Scenario: 创建修士时设置五行亲和
- **WHEN** 修士 NPC 被创建并指定五行亲和值
- **THEN** NpcCultivationData.Affinity 的每个字段 MUST 等于指定值

### Requirement: NpcCultivationData 功法槽位（保留已有）
`NpcCultivationData` SHALL 保留已有的功法槽位相关字段：
- `CoreCultivationId` (string)：核心功法定义 ID
- `GongFaDatas` (List\<GongFa\>)：功法槽位列表
- `ActiveSlotIndex` (int)：当前激活的槽位索引
- `GetCoreSlot()`：获取核心功法槽位
- `GetActiveSlot()`：获取当前激活的修炼槽位

`GongFa` 类定义保持不变，与 `NpcCultivationData` 一起迁移到 `Npc/Data/NpcCultivationData.cs`。

#### Scenario: 功法槽位功能不变
- **WHEN** 通过 CultivationMgr.AddCultivation 添加功法
- **THEN** NpcCultivationData.GongFaDatas MUST 正确记录功法，GetCoreSlot() MUST 返回核心功法

### Requirement: NpcSystemPractice 管理 NpcCultivationData
`NpcSystemPractice` SHALL 提供以下能力：
- `Register(Npc, NpcCultivationData)`：注册 NPC 的修行数据
- `GetCultivation(int id)`：查询 NPC 的修行数据
- `OnTick(Npc, float)`：每 Tick 执行修行逻辑（寿元耗尽判定等）
- `IsLifespanExhausted(Npc)`：判断寿元是否耗尽（读取 BioData.AgeAccumulated 对比 LifespanMax）
- `GetLifespanMax(Npc)`：获取寿元上限
- `GetLifespanRatio(Npc)`：获取寿元消耗比例
- 八大基础属性的 Get 便捷方法（GetQiXue, GetTiPo, GetQiGan, GetLingJi, GetShenShi, GetWuXing, GetJiYuan, GetMeiLi）

#### Scenario: 注册并查询修行数据
- **WHEN** 调用 Register 注册 NPC 修行数据后调用 GetCultivation
- **THEN** MUST 返回注册时的 NpcCultivationData 实例

#### Scenario: 寿元耗尽判定
- **WHEN** NPC 的 BioData.AgeAccumulated >= CultivationData.LifespanMax
- **THEN** IsLifespanExhausted MUST 返回 true

### Requirement: NpcCultivationData 独立文件
`NpcCultivationData` 和 `GongFa` SHALL 定义在 `Scripts/Game/Domain/Object/Npc/Data/NpcCultivationData.cs` 中。
`BaseProperty` struct SHALL 定义在同一文件中。
`ElementalAffinity` struct SHALL 定义在同一文件中。
原 `Scripts/Game/Domain/GamePlay/Practice/NpcCultivationData.cs` 的内容 SHALL 迁移至此。

#### Scenario: 文件结构
- **WHEN** 检查文件系统
- **THEN** `Npc/Data/NpcCultivationData.cs` MUST 存在且包含 NpcCultivationData、GongFa、BaseProperty、ElementalAffinity
- **THEN** 原 `GamePlay/Practice/NpcCultivationData.cs` 内容 MUST 被清空或重定向（不删除文件）