## Architecture Decision: 扁平全局表 + CardId 索引 + Card Data 层

GongFa/Equip 与 Card 是 1:0..1 关系，生命周期完全跟随 Card。
不引入独立 ID，直接用 Card.Id 做 key。

**三层职责分离**：
- **GongFaMgr / EquipMgr**：独立顶级 Mgr，持有实例的扁平全局表
- **CardGongFaData / CardEquipData**：Card 侧的数据类，提供字段空间和便捷方法壳子
- **GongFa / Equip**：纯运行时实例对象

```
┌─────────────────────────────┐     ┌───────────────────────────────┐
│       GongFaMgr             │     │        EquipMgr               │
│  Dict<int, GongFa> _table   │     │  Dict<int, Equip> _table      │
│  Add / Remove / Get / GetAll│     │  Add / Remove / Get / GetAll  │
└──────────────┬──────────────┘     └──────────────┬────────────────┘
               │ cardId                             │ cardId
               ▼                                    ▼
┌──────────────────────────────────────────────────────────────┐
│                          Card                                 │
│  CardBaseData    (已有，直接持有)                               │
│  CardGongFaData  (新增，便捷方法壳子，实际数据问 GongFaMgr)     │
│  CardEquipData   (新增，便捷方法壳子，实际数据问 EquipMgr)      │
│                                                               │
│  IsGongFaCard => GongFaMgr.Get(Id) != null                   │
│  IsEquipCard  => EquipMgr.Get(Id) != null                    │
└──────────────────────────────────────────────────────────────┘
```

## CardGongFaData / CardEquipData 的定位

这两个类 **不持有 GongFa/Equip 实例本身**，实例归 Mgr 管。
它们的作用是：
1. 给卡级别附属字段提供存放空间（未来扩展用）
2. 给便捷方法提供编写位置（如 GetUnlockedPoints、IsComplete 等）
3. 遵循 Card/Data/ 下的 IDomainDataBase 规范（Clone、Log）

```csharp
// Card/Data/CardGongFaData.cs
public class CardGongFaData : IDomainDataBase
{
    /// <summary>所属 Card 的 Id（创建时传入）</summary>
    public int CardId { get; set; }

    /// <summary>从 GongFaMgr 获取功法实例</summary>
    public GongFa GetGongFa()
        => GongFaMgr.Instance?.Get(CardId);

    // ── 便捷方法（委托到 GongFa 实例） ──
    public List<CultivationPointDefine> GetUnlockedPoints()
        => GetGongFa()?.GetUnlockedPoints() ?? [];

    public CultivationPointDefine GetNextPoint()
        => GetGongFa()?.GetNextPoint();

    public bool IsComplete()
        => GetGongFa()?.IsComplete() ?? false;
}
```

```csharp
// Card/Data/CardEquipData.cs
public class CardEquipData : IDomainDataBase
{
    /// <summary>所属 Card 的 Id（创建时传入）</summary>
    public int CardId { get; set; }

    /// <summary>从 EquipMgr 获取装备实例</summary>
    public Equip GetEquip()
        => EquipMgr.Instance?.Get(CardId);
}
```

## Card.cs 扩展

```csharp
public partial class Card
{
    // 已有：CardBaseData BaseData
    // 新增：
    public CardGongFaData GongFaData { get; set; }   // null = 非功法卡
    public CardEquipData EquipData { get; set; }      // null = 非装备卡

    public bool IsGongFaCard => GongFaMgr.Instance?.Get(Id) != null;
    public bool IsEquipCard => EquipMgr.Instance?.Get(Id) != null;
}
```

## 查询路径

### "某 NPC 的所有功法"

```
NpcGongFaData.AllSlotCardIds
    → 逐个 GongFaMgr.Get(cardId) → GongFa 实例
```

### "某张卡的功法详情"

```
card.GongFaData.GetUnlockedPoints()
// 或直接
GongFaMgr.Get(card.Id)
```

### "当前修炼的功法"

```
NpcPraticeData.NowGongFaCardId (int)
    → GongFaMgr.Get(cardId)
```

### "全世界谁在修炼某 DefineId 的功法"

```
GongFaMgr.GetAll()
    .Where(g => g.DefineId == targetId)
    → 对应 cardId → CardMgr.GetById(cardId) → Card 的 owner
```

## 数据结构变更

### GongFa.cs（保留，增加 Id）

```csharp
public class GongFa : IFormDefine<CultivationDefine>
{
    /// <summary>实例 ID（= 所属 Card.Id）</summary>
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string DefineId { get; set; } = "";
    public int CurrentPoint { get; set; } = 0;
    // 方法保留不变：GetUnlockedPoints / GetNextPoint / IsComplete
}
```

Id 在创建时由 CultivationMgr 赋值为 card.Id，GongFaMgr 也用 gongFa.Id 做 key。

### Equip.cs（保留，增加 Id）

已有字段全部保留，新增 `int Id` 字段（= 所属 Card.Id）。

### NpcGongFaData.cs — 保留，微调

`List<GongFa>` → `List<int>` (cardId 索引)，作为 Npc 侧的快速查询索引。
GongFa 实例的真正持有者是 GongFaMgr，NpcGongFaData 只存引用 ID。

```csharp
public class NpcGongFaData : IDomainDataBase
{
    /// <summary>持有的功法卡 ID 列表</summary>
    public List<int> AllSlotCardIds { get; set; } = [];

    /// <summary>激活的功法卡 ID 列表</summary>
    public List<int> ActiveSlotCardIds { get; set; } = [];

    // ── 便捷查询（从 GongFaMgr 动态获取实例） ──
    public List<GongFa> GetAllGongFa()
        => AllSlotCardIds.Select(id => GongFaMgr.Instance?.Get(id))
                         .Where(g => g != null).ToList();

    public List<GongFa> GetActiveGongFa()
        => ActiveSlotCardIds.Select(id => GongFaMgr.Instance?.Get(id))
                            .Where(g => g != null).ToList();
}
```

### NpcPraticeData.cs — 微调

```csharp
// 旧：public GongFa NowGongFaData { get; set; }
// 新：
public int NowGongFaCardId { get; set; } = -1;  // -1 表示无

// 便捷查询
public GongFa GetNowGongFa() => NowGongFaCardId >= 0
    ? GongFaMgr.Instance?.Get(NowGongFaCardId) : null;
```

## GongFaMgr 设计

```csharp
public class GongFaMgr : IDomainMgrBase
{
    public static GongFaMgr Instance { get; private set; }
    private readonly Dictionary<int, GongFa> _table = new();

    // ── 核心 API ──
    public void Add(int cardId, GongFa gongFa) => _table[cardId] = gongFa;
    public void Remove(int cardId) => _table.Remove(cardId);
    public GongFa Get(int cardId) => _table.GetValueOrDefault(cardId);
    public IEnumerable<GongFa> GetAll() => _table.Values;
    public IEnumerable<KeyValuePair<int,GongFa>> GetAllWithId() => _table;

    // ── 生命周期 ──
    // Init/Begin/Tick/Update/Render/End/Save/Load
}
```

## EquipMgr 设计

结构与 GongFaMgr 完全对称：

```csharp
public class EquipMgr : IDomainMgrBase
{
    public static EquipMgr Instance { get; private set; }
    private readonly Dictionary<int, Equip> _table = new();

    public Equip Add(int cardId, EquipDefine define)
    {
        var equip = Equip.FromDefine(define);
        _table[cardId] = equip;
        return equip;
    }
    public void Remove(int cardId) => _table.Remove(cardId);
    public Equip Get(int cardId) => _table.GetValueOrDefault(cardId);
    public IEnumerable<Equip> GetAll() => _table.Values;
}
```

## WorldMgr 注册顺序

```csharp
_domains.Add(new GongFaMgr());   // CardMgr 之后
_domains.Add(new EquipMgr());    // CardMgr 之后
```

须在 CardMgr 之后注册，因为依赖 Card 实例已存在。

## CultivationMgr 适配

`AddCultivation` 流程变为：
1. 通过 CardMgr 创建一张功法卡（Card）
2. 创建 GongFa 实例（Id = card.Id）
3. `GongFaMgr.Add(gongFa.Id, gongFa)`
4. 给 Card 挂上 `CardGongFaData`
5. `npc.AddGongFa(gongFa)` — 内部同步 NpcGongFaData.AllSlotCardIds.Add(gongFa.Id)
6. 将 card 加入 Npc 卡组

`RemoveCultivation` 流程变为：
1. `npc.RemoveGongFa(gongFa)` — 内部同步 NpcGongFaData 索引移除
2. `GongFaMgr.Remove(gongFa.Id)` — 全局表移除
3. CardMgr 销毁对应 Card
4. 从 Npc 卡组移除

## 迁移策略

1. 先建 GongFaMgr / EquipMgr 空壳并注册到 WorldMgr
2. 新建 CardGongFaData.cs / CardEquipData.cs 在 Card/Data/ 下
3. 修改 Card partial：新增 GongFaData/EquipData 属性 + IsGongFaCard/IsEquipCard
4. 移除 GongFa.cs 中原有的 Card partial 扩展
5. 修改 NpcGongFaData：`List<GongFa>` → `List<int>` + 便捷查询方法
6. 修改 NpcPraticeData：`NowGongFaData` → `NowGongFaCardId` + 便捷查询方法
7. 修改 CultivationMgr：使用 GongFaMgr.Add/Remove + 挂 CardGongFaData + 同步 NpcGongFaData 索引
8. 清理 NpcSystemCultivation 中的 AddGongFa/RemoveGongFa/SetNowGongFa
9. 更新 Npc partial 便捷方法
