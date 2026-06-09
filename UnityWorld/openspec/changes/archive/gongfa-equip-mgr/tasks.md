## 1. 新建 GongFaMgr / EquipMgr 空壳

- [x] 1.1 创建 `Scripts/Game/Domain/Object/GongFa/GongFaMgr.cs`：实现 IDomainMgrBase，内部 `Dictionary<int, GongFa> _table`，提供 Add/Remove/Get/GetAll/GetAllWithId，构造函数设 Instance，End 清空并置 null
- [x] 1.2 创建 `Scripts/Game/Domain/Object/Equip/EquipMgr.cs`：实现 IDomainMgrBase，内部 `Dictionary<int, Equip> _table`，Add(cardId, EquipDefine) 内部调 Equip.FromDefine 并注册，Remove/Get/GetAll，构造函数设 Instance，End 清空并置 null

## 2. GongFa / Equip 增加 Id 字段

- [x] 2.1 `GongFa.cs`：新增 `int Id` 属性（= 所属 Card.Id），移除 Card partial 中的 `GongFaData` 属性和 `IsGongFaCard`（原第 59-63 行）
- [x] 2.2 `Equip.cs`：新增 `int Id` 属性（= 所属 Card.Id）

## 3. 新建 CardGongFaData / CardEquipData

- [x] 3.1 创建 `Scripts/Game/Domain/Object/Card/Data/CardGongFaData.cs`：实现 IDomainDataBase，持有 CardId 字段，提供无参的 GetGongFa()、GetUnlockedPoints()、GetNextPoint()、IsComplete() 便捷方法，Clone 和 Log
- [x] 3.2 创建 `Scripts/Game/Domain/Object/Card/Data/CardEquipData.cs`：实现 IDomainDataBase，持有 CardId 字段，提供无参的 GetEquip() 便捷方法，Clone 和 Log
- [x] 3.3 添加 Card partial：新增 `CardGongFaData GongFaData` 属性、`CardEquipData EquipData` 属性、`IsGongFaCard => GongFaMgr.Instance?.Get(Id) != null`、`IsEquipCard => EquipMgr.Instance?.Get(Id) != null`

## 4. WorldMgr 注册

- [x] 4.1 在 `WorldMgr.Initialize()` 中 CardMgr 之后添加 `_domains.Add(new GongFaMgr())` 和 `_domains.Add(new EquipMgr())`

## 5. NpcGongFaData 改为 CardId 索引

- [x] 5.1 `NpcGongFaData.cs`：将 `List<GongFa> AllSlots` 改为 `List<int> AllSlotCardIds`，`List<GongFa> ActiveSlots` 改为 `List<int> ActiveSlotCardIds`
- [x] 5.2 `NpcGongFaData.cs`：添加便捷方法 `GetAllGongFa()` 和 `GetActiveGongFa()`，从 GongFaMgr 查询实例
- [x] 5.3 `NpcGongFaData.cs`：更新 Clone() 方法适配新字段类型
- [x] 5.4 `NpcGongFaData.cs`：更新 Log() 方法

## 6. NpcPraticeData 改存 CardId

- [x] 6.1 `NpcPraticeData.cs`：将 `GongFa NowGongFaData` 改为 `int NowGongFaCardId = -1`
- [x] 6.2 `NpcPraticeData.cs`：添加便捷方法 `GetNowGongFa()` 从 GongFaMgr 查询
- [x] 6.3 `NpcPraticeData.cs`：更新 Log() 方法适配

## 7. NpcSystemCultivation 适配

- [x] 7.1 `NpcSystemCultivation.cs`：修改 `AddGongFa` 方法，仍接收 GongFa 实例，内部改为 `data.GongFaData.AllSlotCardIds.Add(gongFa.Id)` 和 `ActiveSlotCardIds.Add(gongFa.Id)`
- [x] 7.2 `NpcSystemCultivation.cs`：修改 `RemoveGongFa` 方法，仍接收 GongFa 实例，内部从 AllSlotCardIds/ActiveSlotCardIds 中移除 gongFa.Id
- [x] 7.3 `NpcSystemCultivation.cs`：修改 `SetNowGongFa` 方法，仍接收 GongFa 实例，校验 gongFa.Id 在 ActiveSlotCardIds 中后设置 `data.PracticeData.NowGongFaCardId = gongFa.Id`

## 8. CultivationMgr 适配

- [x] 8.1 `CultivationMgr.cs`：修改 `AddCultivation`，创建 GongFa 时设 Id = card.Id，调用 `GongFaMgr.Add(gongFa.Id, gongFa)`，给 Card 挂 CardGongFaData，通过 npc.AddGongFa(gongFa) 同步索引
- [x] 8.2 `CultivationMgr.cs`：修改 `RemoveCultivation`，通过 npc.RemoveGongFa(gongFa) 同步索引，再 `GongFaMgr.Remove(gongFa.Id)`
- [x] 8.3 `CultivationMgr.cs`：修改 `SetNowCultivation` 等相关方法适配

## 9. Npc partial 便捷方法更新

- [x] 9.1 `NpcGongFaData.cs` Npc partial：`GetAllSlots()` 改为返回 `GongFa.GetAllGongFa()`，`GetActiveSlots()` 改为返回 `GongFa.GetActiveGongFa()`
- [x] 9.2 `NpcPraticeData.cs` Npc partial：`GetNowGongFaData()` 改为返回 `PracticeData.GetNowGongFa()`
- [x] 9.3 `NpcCultivationData.cs` Npc partial：`AddGongFa/RemoveGongFa/SetNowGongFa` 签名保持接收 GongFa 实例
