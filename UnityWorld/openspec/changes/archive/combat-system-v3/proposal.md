## Why

战斗系统需要从旧的回合制出招表模型，重构为全新的"Tick驱动 + 独立计时器 + 待发槽对拼"模型。旧模型无法表达CD节奏博弈、伤势累积、灵元资源管理等核心玩法，需要彻底重构战斗框架、适配卡牌数据、设计基础卡牌、跑通流程并接入大世界。计划5天完成。

## What Changes

- **BREAKING** 重构 `CombatScene`：从回合制顺序出招改为 Tick 驱动，每张卡独立计时器并行推进
- **BREAKING** 重构 `CombatNpc`：移除 DeckSequence/CurrentDeckIndex 出招表机制，改为卡组空间(SP)、待发槽、独立CD追踪
- **BREAKING** 重构 `CombatResult`：新增伤势卡列表、SP溢出判负等结算数据
- 扩展 `CardData` / `CardDefine`：新增 ManaCost、CardType 字段，Effect 中新增攻击/盾/防数值字段
- 新增 `ActionDefine` 战斗数值字段：AtkValue、ShieldValue、DefendValue、Element、PhysicalType
- 新增待发槽（PendingSlot）机制：上限1张，溢出直击、双方有卡立即对拼
- 新增伤势系统：HP清零产生伤势卡塞入卡组，SP溢出判负，伤势持续到战后
- 新增 Mana（灵元）系统：蓝条定期转化为带元素属性灵元，喂给高级卡
- 手配30张基础卡牌（招式/法术/法宝等）用于验证战斗流程
- 战斗Log系统：完整的战斗过程日志输出
- 战斗与NPC接通：从大世界NPC读取属性，结算后回写伤势

## Capabilities

### New Capabilities
- `combat-tick-engine`: Tick驱动的战斗引擎核心，包括独立计时器、待发槽机制、对拼结算、SP溢出判负
- `combat-card-data`: 卡牌战斗数据适配，CardType/ManaCost/攻防数值等字段扩展
- `combat-injury`: 伤势系统，HP清零产生伤势卡，塞入卡组占空间，持续到战后
- `combat-mana`: 灵元系统，蓝条转化为带元素属性的Mana资源
- `combat-log`: 战斗日志系统，输出完整可读的战斗过程记录

### Modified Capabilities

## Impact

- `Scripts/Game/Domain/Combat/` — CombatScene、CombatNpc、CombatResult 全面重构
- `Scripts/Game/Domain/Object/Card/` — CardData、EffectData 扩展战斗字段
- `Scripts/Game/Data/Defines/` — ActionDefine、CardDefine 新增数值字段
- `Scripts/Game/Data/Enum/EnumTypes.cs` — 新增 CardType、PhysicalType 等枚举
- `Data/*.json` — CardDefines、ActionDefines 数据文件重写，新增30张手配卡
- `Scripts/Game/Domain/Object/Npc/` — NPC属性读取（体魄→HP、神识→SP）
- `Scripts/Game/World/WorldMgr.cs` — 战斗结算回写接口