## ADDED Requirements

### Requirement: ItemDefine static data class
ItemDefine SHALL inherit `DefineBase`.
ItemDefine SHALL have JSON-serializable fields: `Desc` (string), `Value` (int), `UseEffect` (string), `Tags` (List<string>), `ElementalAffinity` (Dictionary<string, int>), `PhysicalAffinity` (Dictionary<string, int>), `Entries` (List<string>).

#### Scenario: ItemDefine deserialized from JSON
- **WHEN** a JSON file in `Data/Item/` is loaded
- **THEN** ItemDefine fields are populated including affinities and entries

### Requirement: ItemDefineMgr loader
ItemDefineMgr SHALL implement `IDataMgrBase` and load all ItemDefine from `Data/Item/` directory.
ItemDefineMgr SHALL be registered in `GameDataMgr`.
ItemDefineMgr SHALL provide `Get(string id)` to retrieve a define by ID.

#### Scenario: GameDataMgr loads ItemDefineMgr
- **WHEN** GameDataMgr initializes
- **THEN** ItemDefineMgr loads all JSON files from Data/Item/ and registers defines

### Requirement: Empty JSON data template
A `Data/Item/` directory SHALL exist with at least one example JSON file demonstrating the schema.

#### Scenario: Data directory exists
- **WHEN** project is checked
- **THEN** `Data/Item/` directory exists with valid example JSON
