## ADDED Requirements

### Requirement: Unified contest resolution rule
The system SHALL resolve all contests (regardless of ContestType) using a single rule: compare values, winner = higher value, overflow = absolute difference.

#### Scenario: Attack wins against any card
- **WHEN** an attack-type ContestData (Zhan/Da/Ci/SheJi) wins a contest
- **THEN** the overflow value SHALL be applied as damage to the loser's owner NPC via DamageInfo

#### Scenario: Defense wins against any card
- **WHEN** a defense-type ContestData (Shield/Block/Dodge) wins a contest
- **THEN** no damage SHALL be dealt and no built-in effect SHALL occur at the base layer

#### Scenario: Defense vs Defense contest
- **WHEN** two defense-type ContestData are resolved against each other
- **THEN** the system SHALL determine a winner by value comparison, with no base-layer effect for either party

#### Scenario: Equal values (draw)
- **WHEN** both ContestData have equal values (difference < 0.001)
- **THEN** no damage or effect SHALL occur and both cards SHALL proceed to Apply phase

### Requirement: OnContestOverflow event broadcast
The system SHALL broadcast an `OnContestOverflow` combat event after every contest resolution where a winner is determined, regardless of contest types involved.

#### Scenario: Event broadcast after contest
- **WHEN** a contest is resolved with a winner (non-draw)
- **THEN** the system SHALL broadcast `OnContestOverflow` via DispatchHookToAll with APIContext containing: Winner NPC, Loser NPC, overflow value, winner ContestType, loser ContestType, winner CombatCard, loser CombatCard

#### Scenario: Passive cards respond to overflow event
- **WHEN** `OnContestOverflow` is broadcast and a Passive-phase CombatCard has a Lua hook named `OnContestOverflow`
- **THEN** that hook SHALL be invoked with the full APIContext

#### Scenario: Attack contest also broadcasts overflow
- **WHEN** an attack-type card wins a contest (damage is dealt)
- **THEN** `OnContestOverflow` SHALL still be broadcast (after damage is queued), allowing passive cards to react to attack victories as well

### Requirement: Straight (direct hit) unified handling
The system SHALL apply the same unified rule to Straight (no opposing card in slot): attack = full value as damage, defense = full value as overflow with event broadcast.

#### Scenario: Attack straight
- **WHEN** an attack-type card overflows from PendingSlot with no opposing contest
- **THEN** the full ContestValue SHALL be dealt as damage to the target

#### Scenario: Defense straight
- **WHEN** a defense-type card overflows from PendingSlot with no opposing contest
- **THEN** no base-layer effect SHALL occur and `OnContestOverflow` SHALL be broadcast with overflow = full ContestValue

### Requirement: No eat-all rule
The system SHALL NOT have a built-in "eat-all" (通吃) rule for same-type attack contests. All attack vs attack contests SHALL use difference-based damage.

#### Scenario: Same-type attack vs attack
- **WHEN** two attack cards of the same ContestType (e.g., Zhan vs Zhan) are resolved
- **THEN** the winner SHALL deal overflow (difference) as damage, NOT the full contest value
