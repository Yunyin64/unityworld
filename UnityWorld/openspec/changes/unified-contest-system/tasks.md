## 1. 枚举与数据结构

- [ ] 1.1 在 `EnumTypes.cs` 的 ContestType 枚举中新增 `Dodge`
- [ ] 1.2 在 `ContestData.cs` 中新增 `IsDefenseType` 属性（Shield/Block/Dodge 返回 true）
- [ ] 1.3 在 `APIContext.cs` 中新增拼点结果字段：Winner(CombatNpc)、Loser(CombatNpc)、Overflow(float)、WinnerType(ContestType)、LoserType(ContestType)、WinnerCard(CombatCard)、LoserCard(CombatCard)

## 2. 核心拼点逻辑简化

- [ ] 2.1 重写 `CombatNpcFunc.cs` 中的 `ResolveContest` 方法：统一为比大小→攻击赢则差值伤害→广播 OnContestOverflow
- [ ] 2.2 删除 `ContestWin` 和 `ContestLose` 私有方法（逻辑已内联到 ResolveContest）
- [ ] 2.3 重写 `Straight` 方法：攻击直击=全额伤害+广播，防御直击=仅广播 OnContestOverflow

## 3. 事件广播

- [ ] 3.1 在 `CombatScene.cs` 或 `CombatNpcFunc.cs` 中新增 `BroadcastContestOverflow` 辅助方法，构建 APIContext 并调用 TriggerCombatEvent("OnContestOverflow", ctx, winner)
- [ ] 3.2 确保 ResolveContest 和 Straight 在结算后都调用 BroadcastContestOverflow

## 4. 验证与清理

- [ ] 4.1 排查 `Data/` 目录下 Lua 文件中是否有依赖通吃规则或 Shield/Block 特殊行为的代码，标注需适配项
- [ ] 4.2 运行 CombatTestRunner 确认编译通过且基础拼点流程正常
