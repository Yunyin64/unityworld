## Context

当前战斗系统中，所有卡牌（包括招式）在 Field 中并行 Tick，各自独立走 CD。ZhaoShi Keyword 的 Tick hook 控制 Phase 流转（Waiting→InCD→CDFull→Ready→Apply→Finished）。招式的"出牌"由 CombatNpc.UseCard() 统一触发——遍历所有 IsReady() 的卡调用 Use()。

C# 侧提供基础机制（Phase 状态机、Tick 驱动、Field 管理），Lua Keyword 控制具体流转逻辑。本次改动延续这一分工：C# 只新增一个字段和几个辅助方法，核心轮转逻辑在 Lua 中实现。

## Goals / Non-Goals

**Goals:**
- 招式卡串行轮转：同一时间只有一张招式走 CD，其余停在 Waiting
- 轮转顺序 = Field 物理顺序中 ZhaoShi 卡的子序列
- 位移操作实时影响轮转（移到当前卡之前 = 跳过，之后 = 插队）
- 当前卡被移除时自动 fallback
- 冻结当前招式 = 锁死整条链（有意设计）

**Non-Goals:**
- 不改动法术/功法/法宝等其他 Keyword 的 CD 逻辑
- 不引入"每装备独立轮转"——全局一条链
- 不做 UI/前端展示适配（后续独立处理）

## Decisions

### 1. 只存 currentZhaoShiCardId，不存 index

**选择**: CombatNpc 只维护 `int CurrentZhaoShiCardId`，index 通过实时过滤 Field 派生。

**理由**: Field 的增删/位移频繁发生，如果同时维护 index 需要监听所有变更事件同步更新。只存 CardId 则天然免疫 Field 变动——任何时候需要 index 就实时算。

**替代方案**: 存 index + cardId 双字段，FieldChange 时同步。被否决：多一份状态多一份 bug 风险，且没有性能收益（招式列表通常 3~5 张，过滤开销可忽略）。

### 2. 辅助方法暴露给 Lua

C# 侧新增方法供 Lua 调用：
- `GetCurrentZhaoShiCardId()` → int
- `SetCurrentZhaoShiCardId(int id)` → void
- `GetZhaoShiList()` → List<CombatCard>（按 Field 顺序过滤 ZhaoShi keyword）
- `AdvanceZhaoShi()` → void（封装 advance 逻辑：找下一张，更新 cardId）

Advance 逻辑也可纯 Lua 实现，但放 C# 更安全（处理边界情况：列表为空、当前卡不在列表中等）。

### 3. 初始化时机

PreStart 阶段，InitDeck 完成后，扫描 Field 中第一张 ZhaoShi 卡设为 currentZhaoShiCardId。若无招式卡则 id = -1（不影响其他卡运转）。

### 4. Fallback 策略

当 currentZhaoShiCardId 对应的卡在 Field 中找不到时：
- 重建招式列表
- 若列表非空 → currentId = list[0]（从头开始）
- 若列表为空 → currentId = -1

## Risks / Trade-offs

- **[冻结即锁链]** 冻结当前招式会卡住所有后续招式输出 → 这是有意设计（水克武修），不做缓解
- **[位移复杂度]** 位移实时影响轮转，可能导致某些边界场景下玩家困惑（比如一张卡反复被位移导致永远轮不到）→ 后续可通过 UI 显示当前活跃招式来缓解
- **[招式为 0 的情况]** 所有招式被移除后 currentId = -1，此时 ZhaoShi.Tick 对所有卡不生效 → 无招式 = 无输出，符合预期
