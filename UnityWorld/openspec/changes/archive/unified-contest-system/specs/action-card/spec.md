## ADDED Requirements

### Requirement: ContestType Dodge enumeration
The system SHALL include `Dodge` as a valid ContestType enum value, classified as defense-type alongside Shield and Block.

#### Scenario: Dodge card enters contest
- **WHEN** a card with ContestType.Dodge enters a contest
- **THEN** it SHALL be treated identically to Shield/Block at the base layer (defense-type, no built-in special behavior)

### Requirement: IsDefenseType helper
The system SHALL provide an `IsDefenseType` property on ContestData that returns true for Shield, Block, and Dodge.

#### Scenario: Defense type classification
- **WHEN** ContestData.ContestType is Shield, Block, or Dodge
- **THEN** ContestData.IsDefenseType SHALL return true

## REMOVED Requirements

### Requirement: Same-type attack eat-all rule
**Reason**: Replaced by unified difference-based damage. Eat-all behavior can be reimplemented via GongFa card Lua hooks if desired.
**Migration**: Any Lua scripts checking for eat-all behavior should use OnContestOverflow hook with a condition checking WinnerType == LoserType to replicate the effect.
