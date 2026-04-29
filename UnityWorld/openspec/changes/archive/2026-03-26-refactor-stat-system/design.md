## Context

### 当前状态

现有 Stat 系统由以下组件构成：
- `StatId.cs`：硬编码常量类，定义 `age_accumulated`、`lifespan_max` 等 7 个属性 ID
- `StatBlock`：属性集合，嵌入在 `Npc` 实体中（`npc.Stats`）
- `StatEntry`：单条属性，包含 `_baseValue` + `_modifiers` 列表
- `StatModifier`：修正项，支持 Flat/Percent/Override/ClampMin/ClampMax 五种类型

### 问题痛点
1. **扩展性差**：新增属性需修改 `StatId.cs`，策划无法自行配置
2. **无类型归属**：属性不区分 Object 类型，无法为 Npc/Tile/LandMark 定义专属属性集
3. **热重载不支持**：`_baseValue` 在 StatEntry 中存储副本，Define 变更无法同步
4. **与 Flag 边界模糊**：Stat 和 Flag 都是 KV 存储，但语义和用途未明确区分

### 约束条件
- 必须遵循现有命名规范（PascalCase 类名、`_` + camelCase 私有字段）
- 必须实现 `IDomainMgrBase` / `IDataMgrBase<T>` 接口
- 不能删除 .cs 文件（例外：`StatId.cs` 常量类可删除，替换为数据驱动）
- 热路径属性（TileAura、年龄、修为）已明确**不是 Stat**，由各 System 管理

## Goals / Non-Goals

**Goals:**
1. 建立 `StatDefine` 数据驱动体系，策划可通过 JSON 定义新属性
2. 建立 `StatMgr` 集中管理所有 StatBlock，支持按 Object 类型分组
3. 重构 `StatEntry` 计算模型，支持三层值来源（Define.Default + Modifier + AddValue）
4. 实现惰性创建模式，内存高效、热重载友好
5. 明确 Stat 与 Flag 的边界：Stat 是预定义数值属性，Flag 是任意命名的状态标记

**Non-Goals:**
1. **不**迁移 TileAura/NpcBio 等底层机制属性到 Stat 系统（它们属于 System 管理的热路径）
2. **不**实现 `Formula` 字段的 Lua 解析（占位，后续扩展）
3. **不**实现 `OnStatChanged` 事件广播的具体逻辑（仅预留接口）
4. **不**为 Plane/Region/LandMark 创建实际的 StatBlock（仅预留数据结构和接口）

## Decisions

### D1: StatBlock 所有权 — 集中式 vs 嵌入式

**选择：集中式（StatMgr 持有）+ 引用共享**

```
方案 A：嵌入式（现状）
  Npc.Stats = new StatBlock() ← 实体自己持有
  问题：无法集中管理、无法统一查询

方案 B：集中式（StatMgr 持有）
  StatMgr._npcBlocks[id] = statBlock
  统一生命周期、统一查询

方案 C：集中 + 引用共享（选择）
  StatMgr 持有 Dict 作为 owner
  Npc.Stats 属性返回引用（快捷访问）
  两边指向同一对象
```

**理由：** 集中管理便于 Save/Load、跨实体查询、统一事件广播；引用共享降低迁移成本，现有 `npc.Stats.Get(...)` 代码几乎不用改。

### D2: BaseValue 存储策略 — 存副本 vs 实时读 Define

**选择：实时读 Define**

```
方案 I：StatEntry 存 _baseValue
  问题：Define 重载后不同步，需遍历更新

方案 II：Recalculate 时从 Define.DefaultValue 读取
  优点：热重载零成本，天然同步
  约束：StatEntry 需知其 statId
```

**理由：** 热重载友好，避免副本不一致问题。StatEntry 构造时传入 statId 即可。

### D3: 累加型属性的值来源 — AddValue 字段

**选择：新增 AddValue 作为第三层值来源**

```
Final = ((Define.Default + FlatSum) × (1 + PctSum) + AddValue) → Override → Clamp

AddValue 适用场景：
  财富：+100（任务奖励）、+200（卖出物品）、-50（购买）
  声望：+10（完成任务）、+50（击败Boss）

serModifier 也可实现，但 AddValue 语义更清晰：
  财富 Modifier 列表会无限增长（每笔交易一条记录）
  AddValue 是累计账本，Modifer 是临时修正（Trait/Buff）
```

**理由：** 财富、声望等累加型属性存在"中间态"——既需要数据驱动，又不是纯 Modifier 驱动。AddValue 提供了清晰的语义边界。

### D4: 惰性创建 Entry

**选择：无 Entry 时 Get 返回 Define.DefaultValue**

```
传统方案：
  CreateBlock 时遍历 Define 列表，预填充所有 StatEntry
  问题：新增 Define 需遍历补上

惰性方案：
  Get(statId) → 无 Entry → 返回 Define.DefaultValue
  AddModifier 时才创建 Entry
  优点：内存高效、新增 Define 零成本
```

**理由：** 无任何 Trait/Buff 的 NPC 不需要任何 StatEntry，内存高效；新增 Define 后自动对已存在实体生效。

### D5: Min/Max 夹紧层次

**选择：Modifier Clamp → Define Min/Max（两层）**

```
计算顺序：
  ① Define.DefaultValue 作为 base
  ② (base + FlatSum) × (1 + PctSum)
  ③ + AddValue
  ④ Override（如果存在）
  ⑤ Modifier 的 ClampMin/ClampMax（运行时动态）
  ⑥ Define 的 MinValue/MaxValue（硬限制，最终夹紧）
```

**理由：** Modifier 可以临时扩大范围（如 Buff 提升"声望上限"），但 Define 的 Min/Max 是绝对边界，策划定义的硬限制。

## Risks / Trade-offs

### R1: StatEntry 需存储 statId
- **风险**：每个 Entry 多一个 string 引用，内存略增
- **缓解**：对比热重载收益，可接受；另 string 是引用类型，开销很小

### R2: Get() 增加 Define 查询
- **风险**：每次 Get 多一次 Dictionary lookup
- **缓解**：Stat 系统已排除热路径属性（TileAura、年龄），Get 频率可控；Dict lookup 极快

### R3: 删除 SetBase() 破坏兼容性
- **风险**：现有代码中 `npc.Stats.SetBase(...)` 调用需全部迁移
- **缓解**：经排查，SetBase 调用集中在 NpcMgr.Create 中初始化属性，这些是"底层机制属性"（年龄、修为），本就不应是 Stat——迁移逻辑为删除这些调用

### R4: StatId.cs 常量删除
- **风险**：`StatId.AgeAccumulated` 等常量引用需全量替换为字符串
- **缓解**：编译错误易于发现；可批量 sed 替换；常量类作为 alias 保留一段过渡期（可选）

## Migration Plan

### Phase 1: 基础设施（无破坏性）
1. 新增 `StatDefine.cs` + `StatDefineMgr.cs`
2. 新增 `stat_defines.json`
3. 新增 `StatMgr.cs`（基本框架，Dict 持有）

### Phase 2: 重构核心（破坏性）
4. 重构 `StatEntry`：删除 `_baseValue`，新增 `_statId`、`_addValue`
5. 重构 `StatBlock`：惰性创建 + Define 夹紧
6. 删除 `StatId.cs`

### Phase 3: 调用方迁移
7. NpcMgr.Create：改用 `StatMgr.CreateBlock(id, "Npc")`
8. Npc.Stats：改为查询 StatMgr 的属性
9. 批量替换 `StatId.xxx` 为字符串字面量

### 回滚策略
- StatMgr 独立于现有逻辑，可直接删除
- 保留原 `StatId.cs` 副本（注释），如需回滚可恢复
- Git 分支隔离：在 feature 分支完成全部迁移后再合并

## Open Questions

1. **Planet/Region/LandMark 何时需要 StatBlock？**
   - 当前仅预留 Dict 结构，不实际创建
   - 待具体玩法需求明确后再扩展

2. **Stat 变化事件广播的具体需求？**
   - 触发 Narrative 系统检查？触发 UI 刷新？
   - 待叙事系统设计明确后再实现

3. **DisplayFormat 字段的枚举值？**
   - Integer / Float1 / Float2 / Percent / ChineseNumber ...
   - 待 UI 层接入时确定
