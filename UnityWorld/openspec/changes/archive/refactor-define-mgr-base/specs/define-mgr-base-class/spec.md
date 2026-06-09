## ADDED Requirements

### Requirement: DefineMgrBase provides unified JSON loading
`DefineMgrBase<TDefine>` SHALL accept a path string in its constructor and auto-detect whether it is a file or directory at load time using `File.Exists` / `Directory.Exists`.

#### Scenario: Path is an existing .json file
- **WHEN** `Load()` is called and the stored path satisfies `File.Exists(path) == true`
- **THEN** the base class SHALL deserialize that single file as `List<TDefine>` and populate the internal dictionary keyed by `DefineBase.ID`

#### Scenario: Path is an existing directory
- **WHEN** `Load()` is called and the stored path satisfies `Directory.Exists(path) == true`
- **THEN** the base class SHALL enumerate all `*.json` files in that directory (TopDirectoryOnly), deserialize each as `List<TDefine>`, and merge into the internal dictionary

#### Scenario: Path does not exist
- **WHEN** `Load()` is called and the path is neither an existing file nor directory
- **THEN** the base class SHALL log a warning via `LogMgr.Instance.Warn` and leave the dictionary empty (no exception thrown)

### Requirement: Duplicate ID handling
When loading multiple entries, `DefineMgrBase` SHALL skip entries whose `ID` already exists in the dictionary and log a warning for each duplicate.

#### Scenario: Duplicate ID across files in folder mode
- **WHEN** two JSON files in the same folder both contain a define with `ID = "abc"`
- **THEN** the first loaded entry wins, the second is skipped, and a warning is logged

### Requirement: Subclass can customize JsonSerializerOptions
`DefineMgrBase<TDefine>` SHALL expose a `protected virtual JsonSerializerOptions CreateJsonOptions()` method. The default implementation returns options with `PropertyNameCaseInsensitive = true` and `ReadCommentHandling = JsonCommentHandling.Skip`.

#### Scenario: Subclass needs EnumConverter
- **WHEN** a subclass overrides `CreateJsonOptions()` to add `JsonStringEnumConverter`
- **THEN** deserialization SHALL use the overridden options

### Requirement: Standard query interface
`DefineMgrBase<TDefine>` SHALL implement `IDataMgrBase<TDefine>` providing `Get(id)`, `GetAll()`, `Contains(id)`, `Query(predicate)`.

#### Scenario: Get existing ID
- **WHEN** `Get("warrior")` is called and "warrior" exists
- **THEN** the corresponding `TDefine` instance is returned

#### Scenario: Get non-existing ID
- **WHEN** `Get("nonexistent")` is called
- **THEN** `null` is returned

### Requirement: IDataMgrBase.Load(string path) parameter rename
The `IDataMgrBase` interface SHALL rename `Load(string filePath)` to `Load(string path)` with documentation stating it accepts either a file path or directory path.

#### Scenario: Interface signature updated
- **WHEN** a consumer calls `mgr.Load(somePath)` where `somePath` is a directory
- **THEN** the call compiles and functions correctly (no breaking change at binary level since parameter names don't affect IL)
