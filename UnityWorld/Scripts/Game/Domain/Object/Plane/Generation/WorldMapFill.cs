using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private readonly record struct FillChoice(TileDefine Type, double Weight);

    private static List<FillChoice> FillChoices(Context c, int i, IReadOnlyList<FillChoice> baseCandidates, HashSet<string> excluded = null)
    {
        var choices = new List<FillChoice>();
        foreach (var candidate in baseCandidates)
        {
            if (excluded != null && excluded.Contains(candidate.Type.ID)) continue;
            double weight = candidate.Weight; bool allowed = true;
            foreach (int n in c.Neighbors[i])
            {
                if (n < 0 || c.World.Cells[n].TileDefineId == null) continue;
                var other = c.Catalog.Tiles[c.World.Cells[n].TileDefineId];
                if (!AdjacentAllowed(candidate.Type, other)) { allowed = false; break; }
                weight *= candidate.Type.NeighborWeights.GetValueOrDefault(other.ID, 1);
            }
            if (allowed && weight > 0 && double.IsFinite(weight)) choices.Add(new(candidate.Type, weight));
        }
        return choices;
    }

    private static void Fill(Context c)
    {
        int count = c.World.Cells.Length;
        var bases = new List<FillChoice>[count];
        var revision = new int[count]; var tie = new double[count];
        var excluded = new Dictionary<int, HashSet<string>>();
        var frontier = new PriorityQueue<(int index, int revision), (int options, double tie, int index)>();
        var history = new List<(int index, string type)>();
        int left = 0;
        for (int i = 0; i < count; i++)
        {
            tie[i] = c.FillRandom.NextDouble();
            if (c.World.Cells[i].TileDefineId != null) continue;
            left++;
            bases[i] = c.FillTypes.Where(t => t.FillWeight > 0 && EnvironmentMatches(t.Environment, c.World.Cells[i].Environment) && WaterCompatible(c, i, t))
                .Select(t => new FillChoice(t, t.FillWeight * EnvironmentScore(t.Environment, c.World.Cells[i].Environment))).ToList();
            if (bases[i].Count == 0) c.Fail("fill", $"No environment/water-compatible fill type at ({c.World.Cells[i].Col},{c.World.Cells[i].Row}); no fallback is permitted.");
        }
        void Enqueue(int i)
        {
            if (i < 0 || c.World.Cells[i].TileDefineId != null) return;
            revision[i]++;
            int options = FillChoices(c, i, bases[i], excluded.GetValueOrDefault(i)).Count;
            frontier.Enqueue((i, revision[i]), (options, tie[i], i));
        }
        for (int i = 0; i < count; i++)
            if (bases[i] != null && c.Neighbors[i].Any(n => n >= 0 && c.World.Cells[n].TileDefineId != null)) Enqueue(i);
        int stableStart = 0;
        while (left > 0)
        {
            if (frontier.Count == 0)
            {
                while (stableStart < count && c.World.Cells[stableStart].TileDefineId != null) stableStart++;
                if (stableStart == count) c.Fail("fill", "Fill accounting mismatch.");
                Enqueue(stableStart);
            }
            var head = frontier.Dequeue(); int i = head.index;
            if (head.revision != revision[i] || c.World.Cells[i].TileDefineId != null) continue;
            var choices = FillChoices(c, i, bases[i], excluded.GetValueOrDefault(i));
            var decision = CandidateRecords(c, i, choices);
            FillChoice chosen = default; bool success = false;
            int initialChoices = choices.Count;
            while (choices.Count > 0)
            {
                int pick = WeightedIndex(choices, x => x.Weight, c.FillRandom);
                if (pick < 0) break;
                chosen = choices[pick];
                c.World.Cells[i].TileDefineId = chosen.Type.ID;
                // One-step propagation checks every unassigned neighbour still has a positive-weight legal option.
                success = c.Neighbors[i].All(n => n < 0 || c.World.Cells[n].TileDefineId != null || FillChoices(c, n, bases[n]).Count > 0);
                c.World.Cells[i].TileDefineId = null;
                if (success) break;
                decision.First(x => x.TypeId == chosen.Type.ID).RejectedByLookahead = true;
                choices.RemoveAt(pick); c.World.Diagnostics.Reject("fill.lookahead", c.World.Cells[i].Id.ToString());
            }
            if (!success)
            {
                c.World.Diagnostics.Reject("fill.no-candidates", $"({c.World.Cells[i].Col},{c.World.Cells[i].Row})");
                if (history.Count == 0 || c.World.Diagnostics.FillBacktracks >= c.Profile.FillBacktrackBudget)
                    c.Fail("fill", $"No legal positive-weight candidate at ({c.World.Cells[i].Col},{c.World.Cells[i].Row}); fill backtrack budget exhausted.");
                var previous = history[^1]; history.RemoveAt(history.Count - 1);
                c.World.Cells[previous.index].TileDefineId = null;
                c.World.Cells[previous.index].FillCandidateCount = 0;
                c.World.Cells[previous.index].SelectedWeight = 0;
                c.World.Cells[previous.index].FillCandidates.Clear();
                if (!excluded.TryGetValue(previous.index, out var banned)) excluded[previous.index] = banned = new(StringComparer.Ordinal);
                banned.Add(previous.type); left++; c.World.Diagnostics.FillBacktracks++;
                // Decisions downstream of the undone cell may be retried under the new local state.
                foreach (int n in c.Neighbors[previous.index]) if (n >= 0 && n != previous.index) { excluded.Remove(n); Enqueue(n); }
                Enqueue(previous.index); Enqueue(i); stableStart = Math.Min(stableStart, previous.index);
                continue;
            }
            var cell = c.World.Cells[i]; cell.TileDefineId = chosen.Type.ID; cell.Source = TileGenerationSource.Fill;
            cell.SelectedWeight = chosen.Weight; cell.FillCandidateCount = initialChoices;
            cell.FillCandidates = decision;
            excluded.Remove(i); history.Add((i, chosen.Type.ID)); left--;
            foreach (int n in c.Neighbors[i]) Enqueue(n);
        }
        // Optional local smoothing is restricted to fill and retains all hard constraints and water capabilities.
        int repairs = 0;
        for (int i = 0; i < count && repairs < c.Profile.FillRepairBudget; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.Source != TileGenerationSource.Fill || cell.Water.Active) continue;
            if (c.Neighbors[i].Any(n => n >= 0 && c.World.Cells[n].TileDefineId == cell.TileDefineId)) continue;
            var options = FillChoices(c, i, bases[i]);
            var preferred = options.OrderByDescending(x => c.Neighbors[i].Count(n => n >= 0 && c.World.Cells[n].TileDefineId == x.Type.ID))
                .ThenByDescending(x => x.Weight).ThenBy(x => x.Type.ID, StringComparer.Ordinal).FirstOrDefault();
            if (preferred.Type == null || preferred.Type.ID == cell.TileDefineId || !c.Neighbors[i].Any(n => n >= 0 && c.World.Cells[n].TileDefineId == preferred.Type.ID)) continue;
            c.World.Diagnostics.Repairs.Add($"fill ({cell.Col},{cell.Row}): {cell.TileDefineId} -> {preferred.Type.ID}");
            cell.TileDefineId = preferred.Type.ID; cell.SelectedWeight = preferred.Weight; repairs++;
            cell.FillCandidates = CandidateRecords(c, i, options); cell.FillCandidateCount = options.Count;
        }
    }

    private static List<FillCandidateRecord> CandidateRecords(Context c, int index, IReadOnlyList<FillChoice> choices)
        => choices.Select(x =>
        {
            double environment = EnvironmentScore(x.Type.Environment, c.World.Cells[index].Environment);
            return new FillCandidateRecord { TypeId = x.Type.ID, BaseWeight = x.Type.FillWeight, EnvironmentWeight = environment,
                AdjacencyWeight = x.Weight / (x.Type.FillWeight * environment), FinalWeight = x.Weight };
        }).ToList();

    private static void ScatterLandmarks(Context c)
    {
        var definitions = c.Catalog.LandMarks.Values.OrderBy(x => x.ID, StringComparer.Ordinal).Where(x => x.ForcedPositions.Count == 0 && x.SpawnWeight > 0).ToArray();
        foreach (var cell in c.World.Cells)
        {
            if (cell.Source != TileGenerationSource.Fill || cell.LandMarkDefineId != null || c.LandmarkRandom.NextDouble() >= c.Profile.LandmarkDensity) continue;
            var tile = c.Catalog.Tiles[cell.TileDefineId];
            var options = definitions.Where(x => (!x.IsSingleton || !c.Singletons.Contains(x.ID))
                && (x.PlacementTypeIds.Count == 0 || x.PlacementTypeIds.Contains(tile.ID))
                && (x.PlacementTags.Count == 0 || x.PlacementTags.All(t => tile.Tags.Contains(t)))
                && x.PlacementExtraTerrains.Count == 0).ToArray();
            int pick = WeightedIndex(options, x => x.SpawnWeight, c.LandmarkRandom);
            if (pick < 0) continue;
            cell.LandMarkDefineId = options[pick].ID;
            if (options[pick].IsSingleton) c.Singletons.Add(options[pick].ID);
        }
    }
}
