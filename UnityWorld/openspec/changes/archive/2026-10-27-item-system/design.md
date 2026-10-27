## Context

Card 系统已有两种身份数据面：Equip（装备，运行时可变 Atk/Def/Spd）和 GongFa（功法，运行时可变 CurrentPoint）。两者均遵循相同模式：

```
XxxDefine (静态JSON) → Xxx 运行时实例 → XxxMgr 全局表 → CardXxxData 壳子
```

战斗侧 CombatCardData 已有 ConsumeStack 机制（消耗品堆叠，归0移除），但世界侧缺少对应的 Stack 字段。NpcInventoryData 当前是空壳（`List<string> ItemIds`，Clone 抛异常）。

## Goals / Non-Goals

**Goals:**
- Item 成为 Card 的第三种身份数据面，与 Equip/GongFa 完全同构
- 通用消耗堆叠机制（Stack）作为 CardBaseData 级别属性，与 Amount（弹药）平级
- NpcInventoryData 改造为查询接口，为未来背包玩法预留扩展点
- 所有骨架到位，具体 Item 字段可后续迭代填充

**Non-Goals:**
- 不定义 Item 的完整字段集（Value/Element/ChargeProgress 等留后续）
- 不实现背包 UI 或交易系统
- 不实现战斗侧 Stack 与世界侧 Stack 的联动扣除逻辑（已有 ConsumeStack，联动后做）
- 不新增 ItemDefine 的 JSON 数据文件（骨架先行，数据后填）

## Decisions

### 1. Item 走完整三层（Define → Instance → Mgr），不做纯静态壳

**选择**: 与 GongFa/Equip 同构，有 ItemMgr 管理运行时实例

**理由**: Item 未来会有可变状态（价值浮动、词条附加、充能进度）。如果做成纯静态查询（只读 Define），后续加可变状态需要大改。一步到位。

**否决方案**: 无 Mgr 纯壳子（像 CardGongFaData 直接查 Define）—— 不满足可变需求。

### 2. Stack/StackMax 放 CardBaseData，不放 CardItemData

**选择**: Stack 是 Card 通用机制，有 Consume keyword 时生效

**理由**: 
- 与 Amount（弹药）平级，是卡牌通用属性
- 非 Item 也可能 Consume（比如一次性符文卡不一定有 Item 身份）
- 战斗侧 ConsumeStack 对应世界侧 Stack，保持对称

### 3. NpcInventoryData 保留但改为查询视图

**选择**: 不废弃，改为从 NpcCardData 中筛选 Item 卡的便捷接口

**理由**: 背包系统未来会有独立玩法（排列、容量限制、分类），需要一个扩展点。直接废弃后续加回来更麻烦。

### 4. ItemDefine 与 CardDefine 通过 ID 关联

**选择**: ItemDefine.ID 即为 CardDefine.ID，约定同名（与 CultivationDefine 同模式）

**理由**: GongFa 就是这样做的——CultivationDefine.ID 与 CardDefine 引用的 defineId 相同。不需要额外关联字段。

## Risks / Trade-offs

- [骨架空跑] Item 字段暂不定义，ItemDefine 只有 ID 和 DisplayName → 后续第一个真实 Item 接入时再丰满字段，不影响骨架正确性
- [Stack 与 ConsumeStack 同步] 世界侧 Stack 扣除与战斗侧 ConsumeStack 的联动需要明确时机（开战分配 / Apply 同步扣）→ 标记为 Open Question，本次不实现
- [NpcInventoryData 空查询] 改造后如果没有 Item 卡数据，查询永远返回空 → 无副作用，骨架正常
