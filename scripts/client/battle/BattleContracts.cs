using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Xianxia.Client.Battle;

// Value-only contracts. No core entities, Lua contexts, or live collections cross this boundary.
/// <summary>Zero-based core backpack coordinates.</summary>
public sealed record CellSnapshot(int X, int Y);
/// <summary>A copied unassigned mana entity at its actual cell.</summary>
public sealed record ManaSnapshot(int X, int Y, string Element);
/// <summary>A copied card resource requirement.</summary>
public sealed record ElementAmount(string Element, int Amount);
/// <summary>The currently occupied pending slot; absent means currently empty, not no prior action.</summary>
public sealed record PendingSnapshot(int CardId, string DefineId, string Name, string ContestType, int Value, string Element);
/// <summary>Already-existing NPC modifier values, without dynamic stat evaluation.</summary>
public sealed record BuffSnapshot(string Id, string DefineId, string Name, int Stacks, float Duration, float RemainingTime, string ExpirePolicy);
/// <summary>A single card instance with copied occupancy, resources, and actual activation counters.</summary>
public sealed record CardSnapshot(int Id, string DefineId, string Name, int Size, string Phase, bool IsFrozen,
    int UseCount, int ApplyCount, bool IsMain, bool IsWound, IReadOnlyList<CellSnapshot> Cells,
    IReadOnlyList<string> AssignedElements, IReadOnlyList<ElementAmount> RequiredElements);
/// <summary>One participant's spatial state and independent scan clock. Row is zero-based.</summary>
public sealed record BackpackSnapshot(int Width, int Height, int Capacity, int MainCardId, int Row, int Round,
    decimal ScanProgress, int HasteTicks, int SlowTicks, IReadOnlyList<CellSnapshot> ValidCells, IReadOnlyList<ManaSnapshot> Mana);
/// <summary>Copied current numbers. Nullable maxima are only initial existing-cache references, never final dynamic limits.</summary>
public sealed record NpcSnapshot(int Id, string Name, string Team, string Status, int Hp, int Mp, int Sp, int Shield,
    float? HpMaxReference, float? MpMaxReference, float? SpMaxReference, BackpackSnapshot Backpack,
    IReadOnlyList<CardSnapshot> Cards, IReadOnlyList<BuffSnapshot> Buffs, PendingSnapshot? Pending);
/// <summary>One participant's explicit core result.</summary>
public sealed record CombatantResultSnapshot(int NpcId, string Team, string Status);
/// <summary>A normal core result; runtime failure is represented separately.</summary>
public sealed record BattleResultSnapshot(string Reason, string WinnerTeam, int TotalTicks, IReadOnlyList<CombatantResultSnapshot> Combatants);
/// <summary>Complete immutable value projection committed after initialization or one whole logical Tick.</summary>
public sealed record BattleSnapshot(int Tick, int WorldTick, int TicksPerRow, bool IsFinished,
    IReadOnlyList<NpcSnapshot> Participants, BattleResultSnapshot? Result, string? Error)
{
    /// <summary>Value state before successful initialization.</summary>
    public static BattleSnapshot Empty { get; } = new(0, 0, 10, false, Array.Empty<NpcSnapshot>(), null, null);
}
/// <summary>Received original trace or known event fields, with actual battle Tick and local receipt order.</summary>
public sealed record BattleRecord(long Sequence, int Tick, string Source, string Name, string Text,
    long? EventId, long? ChainId, int? CasterId, int? SourceCardId, IReadOnlyDictionary<string, string> Fields);
/// <summary>Canonical comparison data shared by the Godot host and standalone verification process.</summary>
public sealed record BattleVerificationFrame(BattleSnapshot Snapshot, IReadOnlyList<string> Trace);

/// <summary>Shared wire format for case replay and per-Tick verification.</summary>
public static class BattleJson
{
    /// <summary>Stable camel-case JSON serialization options.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    /// <summary>Serialize a value snapshot without referencing core runtime objects.</summary>
    public static string SerializeSnapshot(BattleSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Options);
    /// <summary>Serialize one canonical JSONL verification frame.</summary>
    public static string SerializeFrame(BattleVerificationFrame frame) => JsonSerializer.Serialize(frame, Options);
}
