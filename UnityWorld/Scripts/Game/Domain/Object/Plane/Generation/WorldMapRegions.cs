using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private sealed class RegionPlan
    {
        public RegionDefine Define;
        public bool IsFixed;
        public int Anchor;
        public List<(int index, string type, MapEnvironment environment)> Cells = new();
        public List<(int index, string define)> Landmarks = new();
        public List<EnvironmentAdjustmentRecord> Adjustments = new();
        public double Score;
    }

    private static List<int> AnchorPool(Context c, RegionDefine define)
    {
        if (c.AnchorPools.TryGetValue(define.ID, out var pool)) return pool;
        pool = new();
        if (define.RiverCells.Count > 0)
        {
            var river = define.RiverCells.OrderBy(x => x.Dr).ThenBy(x => x.Dq).First();
            int mask = river.InDirections.Aggregate(0, (a, d) => a | 1 << d);
            foreach (var cell in c.World.Cells)
            {
                var w = cell.Water;
                if (w.IncomingMask != mask || w.OutDirection != (river.OutDirection ?? -1) || w.IsSource != river.IsSource || w.IsSink != river.IsSink) continue;
                int anchor = c.Index(cell.Id - new TileId(river.Dq, river.Dr));
                if (anchor >= 0) pool.Add(anchor);
            }
        }
        else for (int i = 0; i < c.World.Cells.Length; i++) if (c.World.Cells[i].TileDefineId == null) pool.Add(i);
        c.AnchorPools[define.ID] = pool;
        return pool;
    }

    private static bool TryRegion(Context c, RegionDefine def, int anchor, out RegionPlan plan, bool report = true)
    {
        plan = null;
        bool Reject(string reason) { if (report) c.World.Diagnostics.Reject("region." + reason, def.ID + " @ " + c.World.Cells[anchor].Id); return false; }
        var placed = c.PlacedByType[def.ID];
        if (def.MaxCount >= 0 && placed.Count >= def.MaxCount) return Reject("max-count");
        TileId origin = c.World.Cells[anchor].Id;
        if (placed.Any(x => x.Anchor.DistanceTo(origin) < def.MinSpacing)) return Reject("spacing");
        var trial = new RegionPlan { Define = def, Anchor = anchor, Score = 1 };
        var indexTypes = new Dictionary<int, string>();
        var environment = new Dictionary<int, MapEnvironment>();
        foreach (var kv in def.TerrainLayout.OrderBy(x => x.Key.dr).ThenBy(x => x.Key.dq))
        {
            int i = c.Index(origin + new TileId(kv.Key.dq, kv.Key.dr));
            if (i < 0) return Reject("bounds");
            if (c.World.Cells[i].TileDefineId != null || c.World.Cells[i].HasForcedLandmark) return Reject("occupied");
            if(c.RegionBuffer[i]>0) return Reject("footprint-gap");
            if (def.GenerationPhase=="lake" ? c.World.Cells[i].Water.Kind!="none" || c.Catalog.Tiles[kv.Value].WaterKind is not ("none" or "lake") : !WaterCompatible(c, i, c.Catalog.Tiles[kv.Value])) return Reject("water-kind");
            indexTypes[i] = kv.Value; environment[i] = c.World.Cells[i].Environment;
        }
        foreach (var adjustment in def.EnvironmentAdjustments)
        {
            if (Math.Max(Math.Max(Math.Abs(adjustment.Elevation), Math.Abs(adjustment.Moisture)), Math.Abs(adjustment.Temperature)) > c.Profile.MaxEnvironmentAdjustment)
                return Reject("adjustment-budget");
            int i = c.Index(origin + new TileId(adjustment.Dq, adjustment.Dr));
            if (!environment.ContainsKey(i)) return Reject("adjustment-footprint");
            MapEnvironment before = environment[i], after = before.Copy();
            after.Elevation += adjustment.Elevation; after.Moisture += adjustment.Moisture;
            after.Temperature += adjustment.Temperature-.18*adjustment.Elevation;
            if ((before.Elevation<c.Profile.Hydrology.SeaLevel)!=(after.Elevation<c.Profile.Hydrology.SeaLevel)) return Reject("adjustment-sea-mask");
            var water=c.World.Cells[i].Water;
            if(water.SurfaceLevel.HasValue && after.Elevation>water.SurfaceLevel || water.DrainageRank>=0 && after.Elevation>water.DrainageElevation+1e-10) return Reject("adjustment-water-level");
            if(Math.Abs(after.Temperature-before.Temperature)>c.Profile.MaxEnvironmentAdjustment+1e-12) return Reject("adjustment-budget-temperature");
            if (!ValidEnvironment(after)) return Reject("adjustment-range");
            environment[i] = after;
            trial.Adjustments.Add(new() { CellIndex = i, RegionDefineId = def.ID, Before = before.Copy(), After = after.Copy() });
        }
        MapEnvironment anchorEnv = environment.GetValueOrDefault(anchor) ?? c.World.Cells[anchor].Environment;
        if (!EnvironmentMatches(def.AnchorEnvironment, anchorEnv)) return Reject("anchor-environment");
        if (def.PlacementTypeIds.Count > 0 && !def.PlacementTypeIds.Any(x => EnvironmentMatches(c.Catalog.Tiles[x].Environment, anchorEnv)))
            return Reject("legacy-placement-environment");
        foreach (var key in def.KeyEnvironment)
        {
            int i = c.Index(origin + new TileId(key.Dq, key.Dr));
            if (i < 0 || !EnvironmentMatches(key.Environment, environment.GetValueOrDefault(i) ?? c.World.Cells[i].Environment))
                return Reject("key-environment");
        }
        if (environment.Values.Count(e => EnvironmentMatches(def.OverallEnvironment, e)) / (double)environment.Count + 1e-12 < def.RequiredEnvironmentFraction)
            return Reject("overall-environment");
        trial.Score = EnvironmentScore(def.AnchorEnvironment, anchorEnv) * environment.Values.Average(e => EnvironmentScore(def.OverallEnvironment, e));
        foreach (var pair in indexTypes)
        {
            int i = pair.Key; var type = c.Catalog.Tiles[pair.Value]; var w = c.World.Cells[i].Water;
            foreach (int n in c.Neighbors[i])
            {
                if (n < 0) continue;
                string other = indexTypes.GetValueOrDefault(n) ?? c.World.Cells[n].TileDefineId;
                if (other != null && !AdjacentAllowed(type, c.Catalog.Tiles[other])) return Reject("adjacency");
            }
            // Author-fixed river cells must describe every segment the template covers, including all ports.
            var offset = c.World.Cells[i].Id - origin;
            var river = def.RiverCells.FirstOrDefault(x => x.Dq == offset.Q && x.Dr == offset.R);
            if (def.GenerationPhase!="lake" && w.Active)
            {
                if (river == null || river.InDirections.Aggregate(0, (mask, d) => mask | 1 << d) != w.IncomingMask
                    || (river.OutDirection ?? -1) != w.OutDirection || river.IsSource != w.IsSource || river.IsSink != w.IsSink)
                    return Reject("water-ports");
            }
            else if (def.GenerationPhase!="lake" && river != null) return Reject("water-missing-skeleton");
            if (!FlowValidAt(c, i, n => environment.GetValueOrDefault(n) ?? c.World.Cells[n].Environment)) return Reject("adjustment-flow");
            trial.Cells.Add((i, pair.Value, environment[i]));
        }
        foreach (var port in def.RiverPorts)
        {
            int i = c.Index(origin + new TileId(port.Dq, port.Dr));
            var w = c.World.Cells[i].Water;
            if (port.IsInlet ? (w.IncomingMask & (1 << port.Direction)) == 0 : w.OutDirection != port.Direction)
                return Reject("external-port");
        }
        var localSingleton = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in def.LandMarkLayout.OrderBy(x => x.Key.dr).ThenBy(x => x.Key.dq))
        {
            int i = c.Index(origin + new TileId(pair.Key.dq, pair.Key.dr));
            if (!indexTypes.ContainsKey(i) || !c.Catalog.LandMarks.TryGetValue(pair.Value, out var landmark)) return Reject("landmark-reference");
            if (landmark.IsSingleton && (c.Singletons.Contains(pair.Value) || !localSingleton.Add(pair.Value))) return Reject("landmark-singleton");
            trial.Landmarks.Add((i, pair.Value));
        }
        plan = trial; return true;
    }

    private static GeneratedRegion CommitRegion(Context c, RegionPlan plan)
    {
        var region = new GeneratedRegion { Index = c.World.Regions.Count, DefineId = plan.Define.ID,
            Anchor = c.World.Cells[plan.Anchor].Id, IsFixed = plan.IsFixed, AdaptationScore = plan.Score };
        foreach (var item in plan.Cells)
        {
            var cell = c.World.Cells[item.index];
            cell.TileDefineId = item.type; cell.Source = TileGenerationSource.Region; cell.RegionIndex = region.Index;
            cell.IsFixed = plan.IsFixed;
            if (plan.IsFixed)
            {
                var type = c.Catalog.Tiles[item.type];
                cell.BlockAllMovement = type.BlocksAllMovement;
                cell.AllowedMovement = type.MovementModes.ToList();
            }
            cell.Environment = item.environment;
            region.CellIndices.Add(item.index);
        }
        foreach (var item in plan.Landmarks)
        {
            c.World.Cells[item.index].LandMarkDefineId = item.define;
            region.LandMarkCellIndices.Add(item.index);
            if (c.Catalog.LandMarks[item.define].IsSingleton) c.Singletons.Add(item.define);
        }
        c.World.Diagnostics.Adjustments.AddRange(plan.Adjustments);
        c.World.Regions.Add(region); c.PlacedByType[plan.Define.ID].Add(region); if (!plan.IsFixed && plan.Define.GenerationPhase=="ordinary") c.RegionCells += region.CellIndices.Count(i=>EligibleLand(c,i));
        if(!plan.IsFixed) ChangeRegionBuffer(c,region.CellIndices,1);
        return region;
    }

    private static void UndoRegion(Context c, RegionPlan plan, GeneratedRegion region)
    {
        foreach (int i in region.CellIndices)
        { var cell = c.World.Cells[i]; cell.TileDefineId = null; cell.RegionIndex = null; cell.LandMarkDefineId = null; cell.Source=default; cell.IsFixed=false; }
        foreach (var item in plan.Landmarks) if (c.Catalog.LandMarks[item.define].IsSingleton) c.Singletons.Remove(item.define);
        foreach (var a in plan.Adjustments) c.World.Cells[a.CellIndex].Environment = a.Before.Copy();
        if (plan.Adjustments.Count > 0) c.World.Diagnostics.Adjustments.RemoveRange(c.World.Diagnostics.Adjustments.Count - plan.Adjustments.Count, plan.Adjustments.Count);
        c.World.Regions.RemoveAt(c.World.Regions.Count - 1); c.PlacedByType[plan.Define.ID].RemoveAt(c.PlacedByType[plan.Define.ID].Count - 1);
        if(plan.Define.GenerationPhase=="ordinary") c.RegionCells -= region.CellIndices.Count(i=>EligibleLand(c,i));
        ChangeRegionBuffer(c,region.CellIndices,-1);
    }

    private static bool EligibleLand(Context c,int i) => !c.World.Cells[i].IsFixed && c.World.Cells[i].Water.Kind=="none"
        && !(c.World.Cells[i].RegionIndex is int r && c.Catalog.Regions[c.World.Regions[r].DefineId].GenerationPhase=="lake");

    private static void ChangeRegionBuffer(Context c,IEnumerable<int> cells,int delta)
    {
        var seen=new HashSet<int>(cells);var frontier=seen.ToArray();
        for(int step=0;step<c.Profile.RegionGap;step++) { var next=new List<int>(); foreach(int i in frontier) foreach(int n in c.Neighbors[i]) if(n>=0 && seen.Add(n)) next.Add(n); frontier=next.ToArray(); }
        foreach(int i in seen) c.RegionBuffer[i]+=delta;
    }

    private static void PlaceRequiredRegions(Context c)
    {
        var required = c.Regions.Values.Where(x => x.ForcedPositions.Count == 0 && x.GenerationPhase=="ordinary" && x.MinCount > 0).ToArray();
        if (required.Length == 0) return;
        var choices = new Dictionary<string, List<(int anchor, double key)>>(StringComparer.Ordinal);
        int quota = Math.Max(1, c.Profile.RequiredRegionAttempts / (required.Length * 2));
        foreach (var def in required)
        {
            var pool = AnchorPool(c, def).ToArray();
            // Fisher-Yates gives a bounded, uniformly sampled search order, independent of registry insertion order.
            for (int n = pool.Length - 1; n > 0; n--) { int j = c.RegionRandom.Next(n + 1); (pool[j], pool[n]) = (pool[n], pool[j]); }
            var candidates = new List<(int anchor, double key)>();
            foreach (int anchor in pool.Take(quota))
            {
                if (c.World.Diagnostics.RequiredAttempts >= c.Profile.RequiredRegionAttempts) break;
                c.World.Diagnostics.RequiredAttempts++;
                if (TryRegion(c, def, anchor, out var plan))
                    candidates.Add((anchor, -Math.Log(Math.Max(1e-12, c.RegionRandom.NextDouble())) / Math.Max(1e-12, plan.Score)));
            }
            choices[def.ID] = candidates.OrderBy(x => x.key).ThenBy(x => x.anchor).ToList();
        }
        var slots = required.OrderBy(x => choices[x.ID].Count).ThenByDescending(x => x.TerrainLayout.Count).ThenBy(x => x.ID, StringComparer.Ordinal)
            .SelectMany(x => Enumerable.Repeat(x, x.MinCount)).ToArray();
        bool Search(int depth)
        {
            if (depth == slots.Length) return true;
            var def = slots[depth];
            foreach (var choice in choices[def.ID])
            {
                if (c.World.Diagnostics.RequiredAttempts >= c.Profile.RequiredRegionAttempts) return false;
                c.World.Diagnostics.RequiredAttempts++;
                if (!TryRegion(c, def, choice.anchor, out var plan)) continue;
                var region = CommitRegion(c, plan);
                if (Search(depth + 1)) return true;
                UndoRegion(c, plan, region);
                if (c.World.Diagnostics.RequiredBacktracks >= c.Profile.RegionBacktrackBudget) return false;
                c.World.Diagnostics.RequiredBacktracks++;
            }
            return false;
        }
        if (!Search(0)) c.Fail("required-regions", "Required regions unsatisfied within budgets: " + string.Join(", ", required.Select(x => $"{x.ID}: missing {x.MinCount - c.PlacedByType[x.ID].Count}, candidates={choices[x.ID].Count}")));
    }

    private static void PlaceOptionalRegions(Context c)
    {
        int available = Enumerable.Range(0,c.World.Cells.Length).Count(i=>EligibleLand(c,i));
        double target = available * c.Profile.RegionCoverageTarget;
        var definitions = c.Regions.Values.Where(x => x.ForcedPositions.Count == 0 && x.GenerationPhase=="ordinary" && x.SpawnWeight > 0).ToArray();
        for (int trial = 0; trial < c.Profile.OptionalRegionAttempts && c.RegionCells < target; trial++)
        {
            c.World.Diagnostics.OptionalAttempts++;
            int pick = WeightedIndex(definitions, x => x.MaxCount < 0 || c.PlacedByType[x.ID].Count < x.MaxCount ? x.SpawnWeight : 0, c.RegionRandom);
            if (pick < 0) break;
            var def = definitions[pick]; var pool = AnchorPool(c, def);
            if (pool.Count == 0) continue;
            int anchor = pool[c.RegionRandom.Next(pool.Count)];
            if (!TryRegion(c, def, anchor, out var plan)) continue;
            // Rejection sampling makes accepted optional placements proportional to spawn weight * adaptation.
            if (c.RegionRandom.NextDouble() > plan.Score) { c.World.Diagnostics.Reject("region.soft-selection"); continue; }
            CommitRegion(c, plan);
        }
        c.World.Diagnostics.RegionCoverage = available == 0 ? 0 : c.RegionCells / (double)available;
        if (c.RegionCells + 1e-9 < target) c.World.Diagnostics.Warnings.Add($"Region coverage {c.World.Diagnostics.RegionCoverage:P2} is below soft target {c.Profile.RegionCoverageTarget:P2}; legal fill continues.");
        foreach (var pair in c.PlacedByType) c.World.Diagnostics.RegionCounts[pair.Key] = pair.Value.Count;
    }
}
