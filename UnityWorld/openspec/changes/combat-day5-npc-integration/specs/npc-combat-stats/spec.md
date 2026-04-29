## ADDED Requirements

### Requirement: 八大属性默认值为 10
NPC 创建时，BaseProperty 的所有属性（QiXue/TiPo/QiGan/LingJi/ShenShi/WuXing/JiYuan/MeiLi）SHALL 初始化为 10。

#### Scenario: 凡人 NPC 创建后属性值
- **WHEN** NPC 被创建且未受任何修正
- **THEN** 八大属性均为 10

### Requirement: 战斗三维由八大属性公式驱动
NpcCultivationData SHALL 提供 `RecalcCombatStats()` 方法，按以下公式计算战斗三维：
- `HpMax = Properties.QiXue`
- `MpMax = Properties.QiGan × 3`
- `SpMax = Properties.ShenShi`

#### Scenario: 凡人基准线
- **WHEN** NPC 八大属性均为 10
- **THEN** HpMax=10, MpMax=30, SpMax=10

#### Scenario: 属性被外部修改后重算
- **WHEN** NPC 的 QiXue 被修改为 20
- **THEN** 调用 RecalcCombatStats() 后 HpMax=20，其他不变

### Requirement: 五行亲和从 SoulData 映射
NpcCultivationData SHALL 提供 `CalcAffinityFromSoul(SoulData soul)` 方法，按以下规则映射：
- 水(Shui) = Soul.FI + Soul.FE
- 火(Huo) = Soul.NI + Soul.NE
- 金(Jin) = Soul.TI + Soul.TE
- 木(Mu) = Soul.SI + Soul.SE
- 土(Tu) = Soul.MI + Soul.ME

#### Scenario: Soul 驱动五行亲和
- **WHEN** NPC 的 SoulData 为 NI=72, NE=31, TI=45, TE=88, FI=60, FE=25, SI=15, SE=90, MI=40, ME=55
- **THEN** 五行亲和为 Shui=85, Huo=103, Jin=133, Mu=105, Tu=95

### Requirement: NPC 创建时自动计算属性和亲和
NPC 在 OnEntityBorn 时 SHALL 自动调用 RecalcCombatStats() 和 CalcAffinityFromSoul()，保证出生后即有正确的战斗三维和五行亲和。

#### Scenario: NPC 出生后属性完整
- **WHEN** NpcMgr.Create() 创建一个新 NPC
- **THEN** NPC 的 HpMax、SpMax、MpMax 均不为 0，且五行亲和不全为 0