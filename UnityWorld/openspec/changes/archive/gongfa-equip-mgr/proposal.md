## Why

GongFa 和 Equip 的运行时实例目前嵌套在 NpcCultivationData / NpcGongFaData 中，与 Npc 数据强耦合。这导致：
1. 无法独立查询/遍历全局所有 GongFa/Equip 实例（如"全世界谁在修炼某功法"）
2. 未来 Equip 可能挂在非 Npc 实体上（如地块、商店），现有结构无法支持
3. 与 CardMgr、TraitMgr 等已独立管理的系统风格不一致

**关键设计决策**：GongFa/Equip 与 Card 是 1:0..1 关系，生命周期完全绑定 Card。因此用 Card.Id 作为全局索引 key，GongFa/Equip 不需要独立 ID。

## What Changes

- **新增** `GongFaMgr`（`IDomainMgrBase`）：扁平全局表 `Dict<int, GongFa>`，key = cardId，API 为 Add/Remove/Get/GetAll
- **新增** `EquipMgr`（`IDomainMgrBase`）：扁平全局表 `Dict<int, Equip>`，key = cardId，API 为 Add/Remove/Get/GetAll
- **新增** `CardGongFaData`（`IDomainDataBase`）：Card 侧功法数据类，提供便捷方法壳子，实际数据问 GongFaMgr
- **新增** `CardEquipData`（`IDomainDataBase`）：Card 侧装备数据类，提供便捷方法壳子，实际数据问 EquipMgr
- **修改** `NpcGongFaData`：`List<GongFa>` → `List<int>` cardId 索引，保留结构作为 Npc 侧快速查询索引，增加便捷查询方法
- **修改** `NpcPraticeData.NowGongFaData` → `NowGongFaCardId: int`，增加便捷查询方法
- **修改** `CultivationMgr`：AddCultivation 改为通过 GongFaMgr 操作 + 给 Card 挂 CardGongFaData + 同步 NpcGongFaData 索引
- **修改** `Npc` partial 上的便捷方法改为从 Mgr 查询
- **修改** `Card` partial：移除原 GongFaData 字段，新增 CardGongFaData/CardEquipData 属性，IsGongFaCard/IsEquipCard 改为查 Mgr

## Capabilities

### New Capabilities
- `gongfa-mgr`: GongFa 运行时管理器，扁平全局表（cardId → GongFa），负责 Add/Remove/Get/全局遍历
- `equip-mgr`: Equip 运行时管理器，扁平全局表（cardId → Equip），负责 Add/Remove/Get/全局遍历

### Modified Capabilities
- `equip-define`: Equip 运行时实例不再只有 FromDefine 静态工厂，改由 EquipMgr 统一创建和管理

## Impact

- `Scripts/Game/Domain/Object/GongFa/` — GongFa.cs 保留，移除 Card partial；新增 GongFaMgr.cs
- `Scripts/Game/Domain/Object/Equip/` — Equip.cs 保留，新增 EquipMgr.cs
- `Scripts/Game/Domain/Object/Card/Data/` — 新增 CardGongFaData.cs、CardEquipData.cs
- `Scripts/Game/Domain/Object/Card/Card.cs` — Card partial 新增 GongFaData/EquipData 属性，IsGongFaCard/IsEquipCard 查 Mgr
- `Scripts/Game/Domain/Object/Npc/Data/NpcGongFaData.cs` — 保留，`List<GongFa>` → `List<int>` + 便捷查询
- `Scripts/Game/Domain/Object/Npc/Data/NpcPraticeData.cs` — `NowGongFaData` → `NowGongFaCardId` + 便捷查询
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCultivation.cs` — 适配新 API
- `Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs` — 通过 GongFaMgr 操作 + 挂 CardGongFaData + 同步索引
- `Scripts/Game/World/WorldMgr.cs` — 注册 GongFaMgr、EquipMgr（CardMgr 之后）
