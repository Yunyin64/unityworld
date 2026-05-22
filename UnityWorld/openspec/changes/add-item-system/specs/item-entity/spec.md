## ADDED Requirements

### Requirement: Item entity definition
Item SHALL be a class inheriting `GameEntityBase` and implementing `IFormDefine<ItemDefine>`.
Item SHALL have fields: `Id` (int), `DefineId` (string), `DisplayName` (string), `Value` (int), `ElementalAffinity` (Dictionary<ElementType, int>), `PhysicalAffinity` (PhysicalAffinity struct), `Entries` (List<string>).

#### Scenario: Item instantiated from ItemDefine
- **WHEN** `Item.FromDefine(ItemDefine)` is called
- **THEN** a new Item instance is created with all fields populated from the define's base values

### Requirement: ItemMgr manages all Item instances
ItemMgr SHALL inherit `DomainMgrBase<Item>` and implement `ISoulBase`.
ItemMgr SHALL provide a static `Instance` singleton.
ItemMgr SHALL be registered in `WorldMgr.Initialize()`.

#### Scenario: ItemMgr lifecycle
- **WHEN** WorldMgr initializes
- **THEN** ItemMgr is created and Instance is set

#### Scenario: ItemMgr cleanup
- **WHEN** ItemMgr.End() is called
- **THEN** all entities are cleared and Instance is set to null

### Requirement: Item LogAllInfo
Item SHALL override `LogAllInfo()` to output Id, DefineId, DisplayName, Value, affinities, and entries.

#### Scenario: Logging an item
- **WHEN** `item.LogAllInfo()` is called
- **THEN** structured log output includes all item fields
