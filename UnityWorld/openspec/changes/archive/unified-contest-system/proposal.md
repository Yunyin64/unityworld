## Why

当前拼点系统中防御类型（Shield/Block）有硬编码的差异行为——Shield 赢了叠甲、Block 赢了差值消失，导致 Block 永远是下位替代，且底层代码分支复杂。同时缺少"闪避"防御类型。

新设计将拼点底层统一为"比大小出差值"一条规则，攻击赢=差值伤害，防御赢=差值默认浪费；所有额外效果（溢出转甲、反伤、充能等）全部由功法卡（Passive Card）的 Lua hook 响应，实现数据驱动的无限扩展性。

## What Changes

- **BREAKING** 简化 `ResolveContest` / `ContestWin` / `ContestLose`：移除所有 ContestType 分支判定，统一为"攻击赢→差值伤害，否则→广播溢出事件"
- **BREAKING** 移除 `Straight` 中 Shield/Block 的特殊处理，统一为"攻击直击→全额伤害，防御直击→广播溢出事件"
- 新增 `ContestType.Dodge`（闪避），与 Shield/Block 基础行为一致
- 新增战斗事件 `OnContestOverflow`，携带溢出值、ContestType、赢家/输家信息
- 移除通吃规则（同类型攻击对拼也改为差值制），如需通吃由功法 hook 实现
- 防御 vs 防御对撞正常拼点出差值，赢家获得溢出事件（默认无事）

## Capabilities

### New Capabilities
- `contest-overflow-event`: 拼点溢出事件广播机制——当防御赢或防御对撞时，广播 `OnContestOverflow` 事件，供功法卡 Lua hook 响应

### Modified Capabilities
- `action-card`: ContestType 新增 Dodge；移除通吃规则；简化拼点结算逻辑为统一差值制

## Impact

- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs` — ResolveContest / ContestWin / ContestLose / Straight 大幅简化
- `Scripts/Game/Data/Enum/EnumTypes.cs` — ContestType 枚举新增 Dodge
- `Scripts/Game/Domain/Combat/CombatScene.cs` — TriggerCombatEvent 新增 OnContestOverflow 调用
- `Data/` 层 — 功法卡 JSON/Lua 需要新增 OnContestOverflow hook 示例
- 现有卡牌 Lua 中如有依赖通吃规则的逻辑需适配
