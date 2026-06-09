## ADDED Requirements

### Requirement: PhysicalAffinity struct
PhysicalAffinity SHALL be a struct wrapping `Dictionary<string, int>`.
PhysicalAffinity SHALL provide:
- `Get(string key)` → returns value or 0 if key absent
- `Set(string key, int value)` → sets value
- `Has(string key)` → returns true if key exists with value > 0
- `Keys` property → returns all keys
- `Clone()` → returns deep copy
- `ToString()` → human-readable output

#### Scenario: Get existing key
- **WHEN** `affinity.Get("hardness")` is called and "hardness" was set to 5
- **THEN** returns 5

#### Scenario: Get missing key
- **WHEN** `affinity.Get("nonexistent")` is called
- **THEN** returns 0

#### Scenario: Clone independence
- **WHEN** a PhysicalAffinity is cloned and the clone is modified
- **THEN** the original is unchanged

### Requirement: PhysicalAffinity JSON serialization
PhysicalAffinity SHALL serialize to/from JSON as a flat `{ "key": value }` object (same as Dictionary<string, int>).

#### Scenario: Round-trip serialization
- **WHEN** a PhysicalAffinity with {"hardness": 5, "toughness": 3} is serialized and deserialized
- **THEN** the result equals the original
