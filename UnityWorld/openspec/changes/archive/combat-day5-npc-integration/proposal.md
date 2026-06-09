## Why

战斗系统（combat-system-v3）的核心推演逻辑已于 Day1-Day4 完成，但战斗中使用的 HP/SP/MP、卡组、灵元池全部是硬编码常量或测试占位数据。NPC 创建后没有真实属性公式、没有卡组、五行亲和为零——战斗系统无法与大世界对接。Day5 的目标是补全 NPC 战斗基础设施，打通"大世界 NPC → 战斗 → 伤势回写"的完整链路。

## What Changes

- NPC 战斗三维（HpMax/SpMax/MpMax）由八大属性公式驱动，不再硬编码
- NPC 五行亲和由 SoulData 认知功能映射计算，不再为零
- NPC 创建时通过功法系统自动发放卡牌到卡组（NpcCardData）
- NPC 支持多本功法，每本功法的已解锁节点奖励的 Card 会加入卡组
- CombatScene.PreStart 从真实 NPC 读取 HP/SP/MP/卡组/五行亲和
- ManaPool 初始化改为按五行亲和权重比例随机分配
- 战斗结束后伤势卡回写到大世界 NPC 卡组
- WorldMgr 新增 RunCombat 便捷方法串联完整流程
- CombatTestRunner 改造为使用真实 NPC 而非虚空构造
- 战斗 Log 增强（结果摘要、关键节点日志）
- **移除** SetupTestCombatNpc 硬编码占位

## Capabilities

### New Capabilities
- `npc-combat-stats`: NPC 战斗三维属性公式（BaseProperty → HpMax/SpMax/MpMax）及五行亲和从 Soul 映射
- `npc-card-deck`: NPC 卡组基础设施——CardDeckSystem 注册、功法节点发牌、多功法支持
- `combat-npc-bridge`: 战斗场景与大世界 NPC 的桥接——PreStart 读取、伤势回写、RunCombat 入口

### Modified Capabilities
（无已有 spec 需要修改）

## Impact

- `Scripts/Game/Domain/Object/Npc/Data/NpcCultivationData.cs` — 属性公式、五行亲和计算
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCardDeck.cs` — Register 实装
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCultivation.cs` — OnEntityBorn 调用公式
- `Scripts/Game/Domain/Object/Npc/NpcMgr.cs` — 注册 CardDeckSystem
- `Scripts/Game/Domain/Object/Npc/Npc.cs` — 添加 CardData 访问器
- `Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs` — 功法节点奖励发牌逻辑
- `Scripts/Game/Domain/Combat/CombatScene.cs` — PreStart 改造、移除 SetupTestCombatNpc
- `Scripts/Game/Domain/Combat/CombatResult.cs` — 伤势卡回写接口
- `Scripts/Game/Domain/Combat/CombatLogger.cs` — 日志增强
- `Scripts/Game/Domain/Combat/CombatTestRunner.cs` — 改用真实 NPC
- `Scripts/Game/World/WorldMgr.cs` — RunCombat 便捷方法
- `Scripts/Core/Base/ISoulBase.cs` — SoulData 已有，无需修改