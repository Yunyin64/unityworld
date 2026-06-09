## 1. 枚举与数据结构

- [x] 1.1 在 `EnumTypes.cs` 的 ContestType 枚举中新增 `Dodge`
- [x] 1.2 在 `ContestData.cs` 中新增 `IsDefenseType` 属性（Shield/Block/Dodge 返回 true）
- [x] 1.3 拼点结果通过 ContextBase.Set() 字典传递（不新增 APIContext 强类型字段），key 包括 Winner/Loser/Overflow/WinnerType/LoserType/WinnerCard/LoserCard

## 2. API 层统一

- [x] 2.1 新增 `Defend` APIFunc（参数 DefendType:String, DefendValue:Int），调用 TryPushToPendingSlot，与 Attack 对称
- [x] 2.2 删除独立的 `Shield` APIFunc 和 `Block` APIFunc
- [x] 2.3 更新现有卡牌 JSON 数据中 ActionId 为 Shield/Block 的条目，改为 Defend + DefendType 参数

## 3. 核心拼点逻辑简化

- [x] 3.1 重写 `CombatNpcFunc.cs` 中的 `ResolveContest` 方法：统一为比大小→攻击赢则差值伤害→广播 OnContestOverflow
- [x] 3.2 简化 `ContestWin` 方法体：攻击赢→差值伤害，防御赢→无事（移除 Shield叠甲/Block消失 分支）
- [x] 3.3 简化 `ContestLose` 方法体：基础层无额外惩罚（清空原有分支，保留方法壳）
- [x] 3.4 重写 `Straight` 方法：攻击直击=全额伤害，防御直击=无事发生

## 4. 验证与清理

- [x] 4.1 排查 `Data/` 目录下 Lua/JSON 文件中 ActionId 为 Shield/Block 的条目，迁移为 Defend
- [x] 4.2 排查 Lua 中是否有依赖通吃规则的代码，标注需适配项
- [x] 4.3 运行 CombatTestRunner 确认编译通过且基础拼点流程正常
