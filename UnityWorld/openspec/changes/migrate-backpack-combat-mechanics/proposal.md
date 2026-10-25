# 背包战斗旧机制迁移与模板装配

## Why

背包扫描战斗核心已经归档，但旧控制效果、法宝触发和资源操作仍依赖独立 CD、资源池或线性卡序，现有 NPC 模板也不能直接生成完整背包。本变更把已确认的新玩法接回现有 Card、功法和 NPC 数据体系，使真实旧内容能够按明确规则迁移和验收。

## What Changes

- **BREAKING** 冻结改为单卡下次完整扫描时跳过主动发动并解冻，不叠加，优先选择未冻结卡；不影响被动、蓄灵和已经入槽的结算。
- **BREAKING** 加速/减速改为本方背包扫描速度乘 2/0.5，分别按真实战斗时间计时，同类累加时间；充能只增加正常速度折算的扫描进度。
- **BREAKING** 多 Keyword 提供可组合的发动机会：法术扫描、法宝条件事件，共用一次 Use、点亮进度、冻结和单待发槽约束；事件发动不占扫描行机会。
- **BREAKING** AboveAll 改为二维空间内完全位于来源卡上方的所有卡，不限列、按实例去重，效果另行过滤候选。
- 灵元转换回 MP 和减灵只作用于格上未分配灵元；按实际数量处理，转换受 MP 空缺限制，随机选择具体格子。
- Min/Max/ManaAdj 保留代码和数据、标记废弃并禁用；战斗部署/召回/线性位移保留、标记待定并禁用，留给后续神修玩法。
- **BREAKING** 在既有功法定义增加背包尺寸，现阶段正式内容统一 5×5；旧 Size 0 卡改为 1，形状由 Size 自动生成横条。位置属于 Card 实例，公开坐标从 (1,1) 起，主功法固定 (3,3)。
- **BREAKING** 模板 NPC 显式指定主功法及卡牌初始位置，配置为 key=卡牌定义 ID、value=坐标的键值对列表，重复 key 生成不同实例。模板不自动获得功法解锁子卡和装备附带招式；玩家与生成 NPC 的自动发牌路径保留。
- 扩展现有旧数据和测试入口，不以新增 BackpackTest 内容替代正式内容迁移；明确迁移完成、废弃、待定、未迁移的边界。

## Capabilities

### New Capabilities

- `combat-status-effects`: 单卡冻结、背包加速/减速、充能的时间与扫描语义。
- `combat-mana-manipulation`: 格上灵元回收与减灵、数量限制、空间即时变化。
- `npc-template-backpack`: 模板 NPC 的显式实例装配、初始化坐标及生成路径隔离。

### Modified Capabilities

- `combat-backpack`: 默认尺寸、横条形状、实例位置、主动资源统一、AboveAll 及旧机制禁用。
- `combat-backpack-scan`: 可变扫描进度、条件触发例外、冻结跳过与扫描行额度边界。
- `combat-contest-scheduling`: 扫描与条件触发共用单槽，保留行边界和独立轮末调度。
- `keyword-system`: 多 Keyword 提供独立发动机会，统一资格检查、事件上下文及去重。
- `passive-card`: 被动效果与主动机会可共存，不因冻结或 Passive Keyword 阻断其他合法机会。

## Impact

- 核心：CombatBackpack、CombatScene、CombatCard、CombatNpc，扫描/事件/拼点顺序及世界与战斗快照。
- 接口与脚本：CombatCDAction、CombatManaAction、CombatReserveAction、APIDomainFunc、FaShu/FaBao/ZhaoShi/Passive 和引用旧接口的卡牌、五行效果。
- 数据与生成：CardDefine/CardBaseData、CultivationDefine、NpcDefine、NpcDefineMgr、NpcSystemCard、NpcSystemCultivation、CultivationMgr、NpcMgr、NpcBackpackLayout；既有 Data/Card、Data/Practice/GongFa、Data/Npc。
- 验证：现有 BackpackModelTests、BackpackCombatTests、CombatTestRunner；新增真实模板装配和多机会链路验收。不增加外部依赖。
- 规格协调：更新已归档背包规格的扫描专属假设；旧 zhaoshi-rotation 仍待重设计。keyword-system 主规格仍有历史 delta 结构问题，实施完成后的同步/归档需做局部格式修整。

## Non-goals

- 不实现玩家手动排包界面、生成 NPC 的组牌算法、战斗中背包操作及神修流派。
- 不定案距离函数、旋转、复杂新形状、所有未来特殊触发；不以本次定义替代未来功法等级成长设计。
- 不修 ApplyHeal 超上限、全库 nullable/依赖告警，不制作 Godot 展示或美术。
- 不删除旧代码/数据，不恢复资源池或独立 CD，不一并归档其他活动变更。

详细决策、实施默认值、风险与验收映射见 design.md；尚未执行的任务全部保留未勾选。
