## Why

Day 1 完成了 Tick 驱动战斗引擎的基础框架（CombatScene、CombatNpc、待发槽、对拼、伤势），但所有战斗数值（攻击值、盾值、防值、元素、物理类型）都是 CardData 上的**临时占位字段**，没有与 TCA（Trigger+Condition+Action）体系打通。现在需要让战斗数值从 ActionDefine → ActionData（运行时实例）→ 拼点数据 这条完整数据流跑通，使卡牌行为完全由数据驱动，且 buff/debuff 可在运行时修改 ActionData 参数。

## What Changes

- **ActionDefine 新增 `funcName` + `params` 字段**：JSON 格式从纯标签描述变为 `{funcName:"Attack", params:["Huo","SheJi",3]}`，成为真正的函数调用描述
- **新增 ActionData 运行时类**：持有 `FuncName` + `ContextBase` 参数包，从 ActionDefine 拷贝初始化，支持运行时修改
- **EffectData 从 `List<string> ActionIds` 改为 `List<ActionData>`**：战斗中直接操作实例而非 ID 引用
- **CardDefine 新增 `cardType` + `manaCost` 字段**：CardType 决定 Mana 消耗模式，ManaCost 为 Dict 格式 `{"Huo":1}`
- **CardData 同步新增 CardType、ManaCost**，移除临时占位字段（ContestValue/ContestType/PhysicalType）
- **CombatCardState 拼点方法重写**：从 ActionData 实例中提取拼点数值，构造临时 ContestData
- **新增 ContestData 临时结构**：封装一次拼点的数据快照（数值、类型、元素、物理类型、来源卡引用）
- **CardSystemGenerate 适配新 ActionDefine 格式**：生成 EffectData 时实例化 ActionData
- **APIMgr 补全 Shield/Block 签名**：当前 Defend 签名不匹配设计文档中的 Shield/Block 分离
- **Mana 系统基础框架**：ManaPool 数据结构、MP→灵元转化逻辑、消耗检查
- **更新现有 JSON 数据文件**：ActionDefines.json 补充 funcName/params 字段

## Capabilities

### New Capabilities
- `action-data-runtime`: ActionData 运行时实例体系——从 ActionDefine 解析到 ContextBase，支持运行时数值修改
- `contest-data`: 拼点数据临时结构——CD 满时从 ActionData 构造，拼完即丢弃
- `mana-framework`: 基础灵元系统框架——ManaPool 结构、MP 转化、灵元消耗检查

### Modified Capabilities
- `combat-card-data`: CardDefine/CardData 新增 cardType + manaCost 字段，EffectData 改持 ActionData 实例，移除临时占位字段
- `combat-tick-engine`: CombatCardState 拼点方法改为从 ActionData 提取，待发槽放 ContestData 而非 CombatCardState

## Impact

- **Data 层**：`ActionDefine.cs`、`CardDefine.cs`、`EffectDefine.cs` 字段变更
- **Domain 层**：`CardData.cs`、`EffectData.cs`（结构性变更）、`CombatCardState.cs`、`CombatNpc.cs`、`CombatScene.cs`、`CombatContestHandler.cs`、`CombatCardFlowHandler.cs`、`CombatSlotHandler.cs`
- **新文件**：`ActionData.cs`、`ContestData.cs`、`ManaHandler.cs`（或类似）
- **API 层**：`APIMgr.cs` 签名修正（Shield/Block 分离）
- **生成系统**：`CardSystemGenerate.cs` 适配新格式
- **JSON 数据**：`ActionDefines.json`、`CardDefines.json` 格式更新