## ADDED Requirements

### Requirement: CardBaseData ConsumeStack field
CardBaseData SHALL have a `ConsumeStack` field (int, default 1).
ConsumeStack represents how many times/units this card can be consumed before removal.

#### Scenario: Default value
- **WHEN** a CardBaseData is created without specifying ConsumeStack
- **THEN** ConsumeStack equals 1

#### Scenario: Item card with stack
- **WHEN** a Card is instantiated from a CardDefine with consumeStack = 3
- **THEN** CardBaseData.ConsumeStack equals 3

### Requirement: CardDefine consumeStack JSON field
CardDefine SHALL have a `consumeStack` JSON field (int, default 1) that maps to CardBaseData.ConsumeStack at instantiation.

#### Scenario: CardDefine deserialization
- **WHEN** a CardDefine JSON contains `"consumeStack": 5`
- **THEN** the loaded CardDefine.ConsumeStack equals 5

#### Scenario: CardDefine missing field defaults
- **WHEN** a CardDefine JSON omits `consumeStack`
- **THEN** CardDefine.ConsumeStack defaults to 1
