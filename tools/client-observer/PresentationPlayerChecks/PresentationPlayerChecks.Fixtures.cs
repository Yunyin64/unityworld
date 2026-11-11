using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

public static partial class PresentationPlayerChecks
{
    // 各场景独立创建播放器，不共享播放状态。
    private static BattlePresentationPlayer Start(BattleSnapshot? initial = null, string session = "test")
    {
        var player = new BattlePresentationPlayer();
        player.Submit(Frame(initial ?? Snapshot(0), 0, session: session, initialization: true));
        return player;
    }

    private static CombatPresentationCard Card(int id) => new(id, "WaterCard", "水卡", 2, Array.Empty<string>(),
        new[] { new PresentationManaCost("Shui", 2) }, Array.Empty<string>(),
        new[] { new PresentationCell(0, 0), new PresentationCell(1, 0) },
        new[] { new PresentationCell(0, 0), new PresentationCell(1, 0) });
    private static NpcSnapshot Npc(int id, int card) => new(id, "角色" + id, id == 10 ? "left" : "right", "Alive", 100, 30, 2, 0,
        100, 30, 25, new(5, 5, 25, 0, 0, 0, 0, 0, 0,
            Array.Empty<CellSnapshot>(), new[] { new ManaSnapshot(3, 0, "Shui"), new ManaSnapshot(4, 0, "Shui") }),
        new[] { BattlePresentationState.Copy(Card(card), 0) }, Array.Empty<BuffSnapshot>(), null)
        { HpMax = 100, MpMax = 30, SpMax = 25 };
    private static BattleSnapshot Snapshot(int tick) => new(tick, 0, 10, false,
        new[] { Npc(10, 101), Npc(20, 201) }, null, null);
    private static BattleSnapshot Change(BattleSnapshot snapshot, int npcId, Func<NpcSnapshot, NpcSnapshot> change) =>
        snapshot with { Participants = snapshot.Participants.Select(n => n.Id == npcId ? change(n) : n).ToArray() };
    private static CardSnapshot Visible(BattlePresentationPlayer player, int card) =>
        player.DisplaySnapshot.Participants.SelectMany(n => n.Cards).Single(c => c.Id == card);
    private static BattlePresentationFrame Frame(BattleSnapshot snapshot, long previousSequence,
        IReadOnlyList<CombatPresentationEvent>? events = null, string session = "test", bool initialization = false)
    {
        events ??= Array.Empty<CombatPresentationEvent>();
        return new(BattlePresentationFrame.CurrentSchemaVersion, session, snapshot.Tick, initialization, snapshot,
            events, events.Count == 0 ? previousSequence : events[0].Sequence,
            events.Count == 0 ? previousSequence : events[^1].Sequence);
    }
    private static CombatPresentationEvent Fact(CombatPresentationKind kind, long sequence, int npc = 10,
        CombatPresentationCard? card = null) => new(1, sequence, false, kind, npc, card, null, null, null, null);
    private static CombatPresentationEvent Mana(long sequence, CombatPresentationCard card, int npc, int x,
        IReadOnlyList<string> before, IReadOnlyList<string> after) => Fact(CombatPresentationKind.ManaAssigned, sequence, npc, card) with
        { ManaAssigned = new(new(x, 0), "Shui", before, after) };
    private static PendingSnapshot Pending(CombatPresentationCard card) => new(card.Id, card.DefineId, card.Name, "Attack", 10, "Shui");
    private static CombatPresentationEvent PendingFact(long sequence, int npc, CombatPresentationCard card, bool entered) =>
        Fact(CombatPresentationKind.PendingChanged, sequence, npc, card) with { Pending = new(entered, "test", "Attack", "Shui", 10) };
    private static CombatPresentationEvent Contest(long sequence) => Fact(CombatPresentationKind.ContestResolved, sequence) with
    {
        ContestId = 700,
        Contest = new(new(10, Card(101), null, "Attack", "Shui", 12), new(20, Card(201), null, "Attack", "Shui", 10), 10, 2)
    };
    private static CombatPresentationEvent Damage(long sequence) => Fact(CombatPresentationKind.DamageResolved, sequence, 20) with
    { Damage = new(10, Card(101), 20, 100, 80, 20, 0, "Physical", "Shui") };
}
