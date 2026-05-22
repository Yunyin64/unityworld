## ADDED Requirements

### Requirement: CardItemData bridges Card to Item
CardItemData SHALL implement `IDomainDataBase`.
CardItemData SHALL have field `CardId` (int).
CardItemData SHALL provide `GetItem()` method that retrieves Item from `ItemMgr.Instance` by CardId.

#### Scenario: GetItem returns linked item
- **WHEN** `cardItemData.GetItem()` is called and an Item with matching Id exists in ItemMgr
- **THEN** the Item instance is returned

#### Scenario: GetItem returns null for non-item card
- **WHEN** `cardItemData.GetItem()` is called and no Item with matching Id exists
- **THEN** null is returned

### Requirement: CardSystemItem sub-system
CardSystemItem SHALL inherit `CardSystemBase<CardItemData>`.
CardSystemItem SHALL be registered in `CardMgr` as a public property `ItemSystem`.

#### Scenario: CardMgr registers ItemSystem
- **WHEN** a Card is instantiated from CardDefine
- **THEN** CardItemData is registered in CardMgr.ItemSystem

### Requirement: Card partial class exposes Item access
Card SHALL have a partial class extension with:
- `ItemData` property accessing CardMgr.Instance.ItemSystem
- `IsItemCard` boolean property checking if ItemMgr has an entity for this Card.Id

#### Scenario: IsItemCard check
- **WHEN** `card.IsItemCard` is accessed on a card that has an associated Item
- **THEN** returns true
