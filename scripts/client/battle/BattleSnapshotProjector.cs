using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace Xianxia.Client.Battle;

/// <summary>每次提交只投影一次；保留历史参考，当前上限通过隔离的核心只读入口求值。</summary>
internal sealed class BattleSnapshotProjector
{
    private readonly Dictionary<int, (float? Hp, float? Mp, float? Sp)> _initialReferences = new();
    public void CaptureInitial(CombatScene scene)
    {
        foreach (var npc in scene.GetCombatants().Values)
        {
            var cache = npc.Stats.GetFinalCache();
            _initialReferences[npc.Id] = (Existing(cache, "HpMax"), Existing(cache, "MpMax"), Existing(cache, "SpMax"));
        }
    }
    public NpcSnapshot CopyNpc(CombatNpc npc)
    {
        var pack = npc.Backpack;
        var valid = new List<CellSnapshot>();
        for (var y = 0; y < pack.Height; y++)
        for (var x = 0; x < pack.Width; x++)
            if (pack.IsValid(new BackpackCell(x, y))) valid.Add(new(x, y));
        var backpack = new BackpackSnapshot(pack.Width, pack.Height, pack.Capacity, pack.MainCardId,
            pack.Row, pack.Round, pack.ScanProgress, pack.HasteTicks, pack.SlowTicks, valid.AsReadOnly(),
            ReadOnly(pack.Mana.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X)
                .Select(p => new ManaSnapshot(p.Key.X, p.Key.Y, p.Value.ToString()))));
        var cards = ReadOnly(npc.GetField().OrderBy(c => c.Id).Select(card =>
        {
            if (!pack.Placements.TryGetValue(card.Id, out var placement))
                throw new InvalidOperationException($"战斗卡实例 {card.Id}:{card.DefineId} 缺少背包布局，观察器不能补造位置。");
            var cells = ReadOnly(placement.Select(c => new CellSnapshot(c.X, c.Y)));
            var phase = Enum.GetValues<CombatCardPhase>().First(card.CheckPhase).ToString();
            return new CardSnapshot(card.Id, card.DefineId, card.DisplayName, card.BaseData.Size, phase,
                card.IsFrozen, card.UseCount, card.ApplyCount, card.Id == pack.MainCardId,
                card.BaseData.Keywords.Contains("Wound"), cells, ReadOnly(pack.AssignedTo(card.Id).Select(e => e.ToString())),
                ReadOnly(card.BaseData.ManaCost.OrderBy(p => p.Key.ToString(), StringComparer.Ordinal)
                    .Select(p => new ElementAmount(p.Key.ToString(), p.Value))));
        }));
        var buffs = ReadOnly(npc.GetAllModifiers().OrderBy(m => m.Id, StringComparer.Ordinal)
            .Select(m => new BuffSnapshot(m.Id, m.DefineId, m.DisplayName ?? m.DefineId, m.CurrentStack,
                m.Duration, m.RemainingTime, m.ExpirePolicy.ToString())));
        _initialReferences.TryGetValue(npc.Id, out var reference);
        var pending = npc.PendingSlot;
        var snapshot = new NpcSnapshot(npc.Id, npc.GetName(), npc.Team.ToString(), npc.Status.ToString(), npc.GetHp(), npc.GetMp(),
            npc.GetSp(), npc.ShieldValue, reference.Hp, reference.Mp, reference.Sp, backpack, cards, buffs,
            pending == null ? null : new(pending.SourceCard.Id, pending.SourceCard.DefineId,
                pending.SourceCard.DisplayName, pending.ContestType.ToString(), pending.ContestValue, pending.Element.ToString()));
        try
        {
            return snapshot with
            {
                HpMax = ReadMax(npc, "HpMax"),
                MpMax = ReadMax(npc, "MpMax"),
                SpMax = ReadMax(npc, "SpMax")
            };
        }
        catch (Exception ex) { return snapshot with { StatReadError = ex.Message }; }
    }

    private static int ReadMax(CombatNpc npc, string key)
    {
        var value = npc.Stats.GetReadOnly(key, float.NaN);
        if (!float.IsFinite(value)) throw new InvalidOperationException($"{key} 上限不可用。");
        return checked((int)value);
    }

    private static float? Existing(Dictionary<string, object> cache, string key) =>
        cache.TryGetValue(key, out var value) && value is float number && float.IsFinite(number) ? number : null;
    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
    public static BattleResultSnapshot CopyResult(CombatResult result) => new(
        result.GetValue<CombatEndReason>("EndReason").ToString(), result.GetValue<CombatTeam>("WinnerTeam").ToString(),
        result.GetValue<int>("TotalTicks"), ReadOnly((result.Get<List<CombatantResult>>("Combatants") ?? new())
            .OrderBy(c => c.NpcId).Select(c => new CombatantResultSnapshot(c.NpcId, c.Team.ToString(), c.FinalStatus.ToString()))));

}
