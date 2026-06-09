## Context

战斗系统 V3 经过 Day1-Day4 已完成核心推演引擎：Tick 循环、卡牌 CD、待发槽/对拼/溢出、伤势生成、Action/Condition 执行器。但整个战斗都跑在"虚空"中——CombatNpc 的 HP/SP/MP 是硬编码，卡牌是从全卡池随机抽取，与大世界 NPC 的属性、功法、卡组完全无关。

当前 NPC 创建后的状态：
- `BaseProperty` 全为 0（八大属性未赋值）
- `HpMax=100, SpMax=50, MpMax=50`（硬编码默认值）
- `ElementalAffinity` 全为 0（五行亲和未从 Soul 映射）
- `NpcCardData.CardIds` 为空列表（无卡牌）
- `NpcMgr` 未注册 `CardDeckSystem`
- `CultivationMgr.AddCultivation` 不处理节点奖励（不发牌）

## Goals / Non-Goals

**Goals:**
- NPC 创建后拥有由公式计算的战斗三维（HpMax/SpMax/MpMax）
- NPC 创建后拥有由 SoulData 驱动的五行亲和
- NPC 通过功法系统获得卡牌，支持多本功法叠加卡组
- 战斗场景从真实 NPC 读取所有战斗数据，不再硬编码
- 战斗结束后伤势卡回写到大世界 NPC 卡组
- 提供 WorldMgr.RunCombat 一键发起战斗的便捷方法
- 战斗日志结构化、可读

**Non-Goals:**
- 不实现境界突破/修炼进度推进逻辑（已有其他系统负责）
- 不新增 CardDefine 卡牌数据（使用 Day3 已有的 53 张卡）
- 不实现功法核心效果（CoreEffect）的运行时触发
- 不实现 Modifier 类型节点奖励的运行时应用
- 不修改 NpcDefine（已废弃，不走此路线）
- 不处理八大属性的动态成长/修正（后续 Trait/境界系统负责）

## Decisions

### D1：战斗三维公式

```
HpMax = Properties.QiXue
MpMax = Properties.QiGan × 3
SpMax = Properties.ShenShi
```

**依据**：用户明确指定。简洁直接，后续可通过 StatModifier 或 Trait 修正八大属性间接影响战斗三维。

**替代方案**：StatBlock 系统计算（过重，且战斗三维目前不需要运行时修正层）。

### D2：BaseProperty 默认值

所有八大属性默认值 = **10**。凡人基准线：HpMax=10, MpMax=30, SpMax=10。

**依据**：用户明确指定。

### D3：五行亲和映射规则

```
Shui(水) = Soul.FI + Soul.FE
Huo(火) = Soul.NI + Soul.NE
Jin(金) = Soul.TI + Soul.TE
Mu(木)  = Soul.SI + Soul.SE
Tu(土)  = Soul.MI + Soul.ME
```

每项范围 0~198。ManaPool 初始化时按此权重比例随机分配每点 MP。

**依据**：用户明确指定 Soul 认知功能与五行的对应关系。

### D4：卡组来源 = 功法节点奖励

NPC 的卡组由其持有的功法决定。`CultivationMgr.AddCultivation()` 添加功法时，扫描所有已解锁节点（`currentPoint >= threshold`），若 `Type == Card`，则调用 `CardMgr.InstantiateFromDefine(RefId)` 并将 `card.Id` 加入 `NpcCardData.CardIds`。

**多功法支持**：`AddCultivation` 可多次调用，每次追加新卡到同一个 CardIds 列表。

**依据**：CultivationPointDefine 已有 Type=Card + RefId 设计；CultivationDefines.json 已配好 realmLevel=1 的 9 本功法，每本 3 张现有卡。

### D5：ManaPool 初始化 = 五行亲和加权随机

战斗 PreStart 时，ManaPool 的每个灵元点按五行亲和权重随机抽取元素类型。总点数 = MpMax（即 QiGan×3）。

```
概率(元素X) = Affinity.X / (Jin + Mu + Shui + Huo + Tu)
```

### D6：伤势卡回写 = 战斗新增伤势卡 → 大世界卡组

战斗结束后，比较战前卡组与战后卡组，识别新增的伤势卡（CardType == Wound），将其实例 ID 追加到 `npc.CardData.CardIds`。下次战斗时 NPC 会携带伤势卡入场。

### D7：CombatTestRunner 改造

保留 CombatTestRunner，但改为：传入两个真实 NPC → 调用 WorldMgr.RunCombat。不再自行随机抽卡。

## Risks / Trade-offs

- **[卡组为空]** 若 NPC 未添加任何功法，CardIds 为空，战斗无卡可用 → 缓解：RunCombat 入口做前置检查，CardIds 为空时报错/跳过
- **[功法引用不存在的卡]** CultivationDefines.json 中 realmLevel≥2 的功法仍引用不存在的 CardDefine → 缓解：InstantiateFromDefine 返回 null 时跳过并打印警告，不影响流程
- **[性能]** 每次 AddCultivation 都遍历所有节点 → 缓解：功法节点数量少（3-5个），无需优化
- **[ManaPool 随机性]** 五行亲和差异大时，ManaPool 可能极端偏向某元素 → 可接受，这就是设计意图