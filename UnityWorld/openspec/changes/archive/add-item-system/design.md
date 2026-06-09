## Context

现有体系：Card 是通用句柄，Equip 是独立实体通过 CardEquipData 桥接。
需要新增 Item 实体，完全平行于 Equip 的架构模式。
材料类物品需要属性亲和力系统支撑未来炼丹/炼器玩法。
堆叠机制（ConsumeStack）应作为 Card 通用能力，不限于物品。

## Goals / Non-Goals

**Goals:**
- Item 作为独立 GameEntityBase 实体，与 Equip 架构完全平行
- PhysicalAffinity 作为独立 struct，开放 key（string→int），Mod 友好
- ConsumeStack 放入 CardBaseData，所有 Card 类型共享
- 整体设计 AI 友好、Mod 友好、最小化硬编码

**Non-Goals:**
- 不实现 UseEffect 具体逻辑（string 占坑）
- 不实现炼丹/炼器玩法（只提供数据基础）
- 不实现物品 UI
- 不新增 ItemType enum

## Decisions

### 1. Item 与 Equip 完全平行

**选择**：Item 有独立的 `ItemMgr`，Id 复用 Card.Id，通过 `CardItemData` 桥接。
**理由**：与 Equip 一致，已验证的模式。生命周期独立，不会污染 Card 通用逻辑。

### 2. PhysicalAffinity 为独立 struct

**选择**：`PhysicalAffinity` 包装 `Dictionary<string, int>`，提供便捷方法。
**理由**：
- 与 ElementalAffinity（ElementType enum → int）不同，物理属性是开放维度
- Modder 可自定义 key（hardness/toughness/sharpness/conductivity/...）
- struct 语义：值类型、轻量、可 Clone

**替代方案**：直接用 `Dictionary<string, int>` → 缺少类型安全和语义表达，便捷方法无处放。

### 3. ConsumeStack 放 CardBaseData

**选择**：在 `CardBaseData` 新增 `ConsumeStack` 字段（int, 默认 1）。
**理由**：
- 物品堆叠 = 药品×3
- 装备耐久 = Amount（已有，保留兼容）
- 战斗卡消耗次数 = 用完 N 次消失
- 统一机制减少分叉

### 4. 物品分类靠 Tags，无 ItemType enum

**选择**：ItemDefine.Tags 承担分类职责（consumable/material/treasure/quest/...）。
**理由**：
- Mod 友好，新物品类型不改代码
- 与 Card 体系 Keywords/Tags 一致
- UI Tab 筛选通过 Tag 匹配

### 5. ElementalAffinity 复用现有 ElementType

**选择**：Item 上的五行亲和用 `Dictionary<ElementType, int>`，与 CardBaseData.ManaCost 一致。
**理由**：五行是封闭集，已有 enum，直接复用。

## Risks / Trade-offs

- [所有 Card 注册空 CardItemData] → 开销极小（只是一个 int 字段的对象），与 Equip 做法一致
- [PhysicalAffinity key 无校验] → Mod 友好的代价，未来可加可选的 key 白名单
- [ConsumeStack 语义与 Equip.Amount 重叠] → Equip.Amount 保留为"耐久"独立概念，ConsumeStack 是卡牌层堆叠次数，两者独立
