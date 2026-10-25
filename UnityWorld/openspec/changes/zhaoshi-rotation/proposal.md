> 状态说明（2026-10-24）：本草案依赖的独立/串行 CD、Field 线性轮转和冻结锁链已与 `backpack-scan-combat` 的背包扫描机制冲突。当前战斗以 `openspec/specs/combat-backpack-scan/spec.md`、`combat-backpack/spec.md`、`combat-contest-scheduling/spec.md` 为准。本草案保留为历史设计及待重设计事项，不代表已实现，不应直接执行旧任务或将旧 CD 规则同步回主规格。招式差异化与控制效果后续按新玩法讨论。

## Why

招式(ZhaoShi)当前所有卡并行走CD，多张招式同时就绪同时出牌，导致武修输出节奏与法术无差异化。需要将招式改为串行轮转——同一时间只有一张招式走CD，形成"一招一式"的有序出招链。这使招式数量成为"广度 vs 频率"的纯策略取舍，同时让冻结等控制效果能锁死整条招式链，赋予水克武修以机制层面的硬逻辑。

## What Changes

- CombatNpc 新增 `currentZhaoShiCardId` 属性，标记当前正在走CD的招式卡
- ZhaoShi Keyword Lua 逻辑重写：非当前卡停留在 Waiting，当前卡 Apply 完成后 advance 到下一张
- index 通过实时过滤 Field 中 ZhaoShi 卡的物理顺序派生，不单独存储
- 位移操作可影响当前轮转顺序（移到当前卡之前=本轮跳过，移到之后=插队）
- 当前卡被移除时 fallback 到列表首位

## Capabilities

### New Capabilities
- `zhaoshi-rotation`: 招式串行轮转机制——CombatNpc 维护 currentZhaoShiCardId，ZhaoShi Keyword 根据此状态决定是否走CD，Apply 后 advance 到 Field 中下一张招式卡

### Modified Capabilities
- `keyword-system`: ZhaoShi keyword 的 Tick/Apply hook 行为变更，新增轮转判定逻辑

## Impact

- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs` — 新增 currentZhaoShiCardId 字段
- `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs` — 新增 GetZhaoShiList() / AdvanceZhaoShi() 等辅助方法
- `Data/LuaScripts/Keywords/ZhaoShi.lua` — 重写 Tick/Apply 逻辑
- 现有招式卡 Lua 脚本无需改动（它们只在 Contest/Apply 里写效果，不涉及CD控制）
