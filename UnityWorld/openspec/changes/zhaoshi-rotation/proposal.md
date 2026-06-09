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
