#nullable enable
namespace UnityWorld.Game.Domain.Combat;

/// <summary>纯值表现事实；坐标均为核心零基坐标，所有集合在创建时复制。</summary>
public enum CombatPresentationKind { ManaAssigned, CardUsed, PendingChanged, ContestResolved, DamageResolved, CardAddedToBackpack, CardApplied }
public sealed record PresentationCell(int X, int Y);
public sealed record PresentationManaCost(string Element, int Count);
public sealed record PresentationManaCell(PresentationCell Cell, string Element);
public sealed record CombatPresentationCard(int Id, string DefineId, string Name, int Size,
    IReadOnlyList<string> Keywords, IReadOnlyList<PresentationManaCost> ManaCost,
    IReadOnlyList<string> AssignedElements, IReadOnlyList<PresentationCell> Cells, IReadOnlyList<PresentationCell> Shape);
public sealed record ManaAssignedPresentation(PresentationCell SourceCell, string Element,
    IReadOnlyList<string> Before, IReadOnlyList<string> After);
public sealed record CardUsedPresentation(IReadOnlyList<string> Before, IReadOnlyList<string> After);
public sealed record PendingPresentation(bool Entered, string Reason, string ContestType, string Element, int Value);
public sealed record ContestSidePresentation(int NpcId, CombatPresentationCard Card, long? ActionId,
    string ContestType, string Element, int Value);
public sealed record ContestPresentation(ContestSidePresentation Left, ContestSidePresentation Right,
    int? WinnerNpcId, int Difference);
public sealed record DamagePresentation(int? SourceNpcId, CombatPresentationCard? SourceCard, int TargetNpcId,
    int HpBefore, int HpAfter, int HpDamage, int ShieldAbsorbed, string DamageType, string Element);
public sealed record CardAddedPresentation(IReadOnlyList<PresentationManaCell> ReplacedMana);
public sealed record CardAppliedPresentation(string Origin, bool IsPassive);

/// <summary>序号与关联 ID 仅属于观察通道，不复用玩法事件计数或随机数。</summary>
public sealed record CombatPresentationEvent(
    int Tick, long Sequence, bool IsInitialization, CombatPresentationKind Kind, int NpcId,
    CombatPresentationCard? Card, long? ActionId, long? ParentActionId, long? ContestId, long? EffectId,
    ManaAssignedPresentation? ManaAssigned = null, CardUsedPresentation? CardUsed = null,
    PendingPresentation? Pending = null, ContestPresentation? Contest = null, DamagePresentation? Damage = null,
    CardAddedPresentation? CardAdded = null, CardAppliedPresentation? Applied = null);

/// <summary>延迟请求只携带值关联；不改原有伤害来源或玩法上下文。</summary>
internal sealed record PresentationOrigin(long? ActionId, long? ParentActionId, long? ContestId, long? EffectId);
