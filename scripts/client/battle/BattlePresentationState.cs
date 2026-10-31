using System;
using System.Linq;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>只归并已记录的值；不推导玩法。作用于副本，绝不修改核心。</summary>
internal static class BattlePresentationState
{
    /// <summary>以最新完整状态为底，只保留尚未结束的局部资源/槽位过渡；旧文字不拥有属性。</summary>
    public static BattleSnapshot Reconcile(BattleSnapshot latest, BattleSnapshot display, PresentationCue[] pendingCues)
    {
        var facts = pendingCues.Select(c => c.Event).ToArray();
        var cards = facts.Where(e => e.Card != null && (e.ManaAssigned != null || e.CardUsed != null || e.CardAdded != null))
            .Select(e => e.Card!.Id).ToHashSet();
        var pending = facts.Where(e => e.Pending != null).Select(e => e.NpcId)
            .Concat(facts.Where(e => e.Contest != null).SelectMany(e =>
                new[] { e.Contest!.Left.NpcId, e.Contest.Right.NpcId })).ToHashSet();
        return latest with
        {
            IsFinished = latest.IsFinished && pendingCues.Length == 0,
            Result = pendingCues.Length == 0 ? latest.Result : display.Result,
            Participants = Array.AsReadOnly(latest.Participants.Select(next =>
            {
                var old = display.Participants.FirstOrDefault(n => n.Id == next.Id);
                if (old == null) return next;
                // 新 Tick 的同格新灵元以最新状态为准，旧飞行动画不能将其再次清除。
                var manaCells = facts.Where(e => e.Tick == latest.Tick && e.NpcId == next.Id && e.ManaAssigned != null)
                    .Select(e => (e.ManaAssigned!.SourceCell.X, e.ManaAssigned.SourceCell.Y))
                    .Concat(facts.Where(e => e.Tick == latest.Tick && e.NpcId == next.Id && e.CardAdded != null && e.Card != null)
                        .SelectMany(e => e.Card!.Cells.Select(c => (c.X, c.Y)))).ToHashSet();
                var stable = next.Cards.Where(c => !cards.Contains(c.Id));
                var transitioning = old.Cards.Where(c => cards.Contains(c.Id)).Select(c =>
                {
                    var current = next.Cards.FirstOrDefault(n => n.Id == c.Id);
                    return current == null ? c : current with { AssignedElements = c.AssignedElements, UseCount = c.UseCount };
                });
                return next with
                {
                    Pending = pending.Contains(next.Id) ? old.Pending : next.Pending,
                    Cards = Array.AsReadOnly(stable.Concat(transitioning).ToArray()),
                    Backpack = next.Backpack with
                    {
                        Mana = Array.AsReadOnly(next.Backpack.Mana.Where(m => !manaCells.Contains((m.X, m.Y)))
                            .Concat(old.Backpack.Mana.Where(m => manaCells.Contains((m.X, m.Y)))).ToArray())
                    }
                };
            }).ToArray())
        };
    }
    public static BattleSnapshot Apply(BattleSnapshot snapshot, PresentationCue cue, bool finish)
    {
        var fact = cue.Event;
        if (fact.Contest is { } contest)
        {
            snapshot = Change(snapshot, contest.Left.NpcId, n => n with { Pending = finish ? null : Pending(contest.Left) });
            return Change(snapshot, contest.Right.NpcId, n => n with { Pending = finish ? null : Pending(contest.Right) });
        }
        int target = fact.Damage?.TargetNpcId ?? fact.NpcId;
        return Change(snapshot, target, npc =>
        {
            if (fact.ManaAssigned is { } mana)
            {
                if (!finish) return npc with { Backpack = npc.Backpack with
                    { Mana = Array.AsReadOnly(npc.Backpack.Mana.Where(m => m.X != mana.SourceCell.X || m.Y != mana.SourceCell.Y).ToArray()) } };
                return Card(npc, fact.Card!, c => c with { AssignedElements = mana.After });
            }
            if (fact.CardUsed is { } used && fact.Card != null)
                return Card(npc, fact.Card, c => c with { AssignedElements = finish ? used.After : used.Before,
                    UseCount = c.UseCount + (finish ? 1 : 0) });
            if (fact.Pending is { } pending && !finish)
                return npc with { Pending = pending.Entered && fact.Card != null
                    ? new PendingSnapshot(fact.Card.Id, fact.Card.DefineId, fact.Card.Name, pending.ContestType, pending.Value, pending.Element) : null };

            if (fact.CardAdded != null && fact.Card != null && !finish)
            {
                var card = Copy(fact.Card, npc.Backpack.MainCardId);
                return npc with
                {
                    Cards = Array.AsReadOnly(npc.Cards.Where(c => c.Id != card.Id).Append(card).ToArray()),
                    Backpack = npc.Backpack with { Mana = Array.AsReadOnly(npc.Backpack.Mana.Where(m =>
                        !card.Cells.Any(c => c.X == m.X && c.Y == m.Y)).ToArray()) }
                };
            }
            return npc;
        });
    }
    private static BattleSnapshot Change(BattleSnapshot snapshot, int id, Func<NpcSnapshot, NpcSnapshot> change) =>
        snapshot with { Participants = Array.AsReadOnly(snapshot.Participants.Select(n => n.Id == id ? change(n) : n).ToArray()) };
    private static NpcSnapshot Card(NpcSnapshot npc, CombatPresentationCard source, Func<CardSnapshot, CardSnapshot> change) =>
        npc with { Cards = Array.AsReadOnly(npc.Cards.Select(c => c.Id == source.Id ? change(c) : c).ToArray()) };
    private static PendingSnapshot Pending(ContestSidePresentation side) => new(side.Card.Id, side.Card.DefineId,
        side.Card.Name, side.ContestType, side.Value, side.Element);
    internal static CardSnapshot Copy(CombatPresentationCard card, int mainId) => new(card.Id, card.DefineId, card.Name,
        card.Size, "Waiting", false, 0, 0, card.Id == mainId, card.Keywords.Contains("Wound"),
        Array.AsReadOnly(card.Cells.Select(c => new CellSnapshot(c.X, c.Y)).ToArray()), card.AssignedElements,
        Array.AsReadOnly(card.ManaCost.Select(m => new ElementAmount(m.Element, m.Count)).ToArray()));
}
