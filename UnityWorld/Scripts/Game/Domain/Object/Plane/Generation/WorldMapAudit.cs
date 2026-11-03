using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    /// <summary>Rechecks a detached plan; useful for import validation and failure-injection tests.</summary>
    public static void ValidatePlan(MapCatalog catalog, WorldGenerationOptions profile, GeneratedWorld world)
    {
        var c = ContextForPlan(catalog, profile, world);
        Audit(c);
    }

    /// <summary>Repairs reciprocal inlet metadata only on unprotected fill cells, then checks all water contracts.</summary>
    public static void RepairAndValidateWater(MapCatalog catalog, WorldGenerationOptions profile, GeneratedWorld world)
    {
        var c = ContextForPlan(catalog, profile, world);
        RepairWater(c);
        AuditWater(c);
    }

    private static Context ContextForPlan(MapCatalog catalog, WorldGenerationOptions profile, GeneratedWorld world)
    {
        var c = new Context(catalog, profile, world.Seed, 0, null);
        if (world.Width != profile.Width || world.Height != profile.Height || world.Cells.Length != profile.Width * profile.Height)
            c.Fail("audit", "Map dimensions do not match the profile.");
        c.World.Cells = world.Cells; c.World.Regions = world.Regions; c.World.Lakes=world.Lakes; c.World.Diagnostics = world.Diagnostics;
        return c;
    }

    private static bool FlowValidAt(Context c, int index, Func<int, MapEnvironment> environment)
    {
        var w = c.World.Cells[index].Water;
        if (w.OutDirection >= 0)
        {
            int n = c.Neighbors[index][w.OutDirection];
            if(n>=0 && w.DrainageRank>=0 && c.World.Cells[n].Water.DrainageRank>=0 && w.DrainageRank<=c.World.Cells[n].Water.DrainageRank) return false;
            if (n < 0 || (w.DrainageRank>=0 ? w.DrainageElevation : environment(index).Elevation) + 1e-10 < (c.World.Cells[n].Water.DrainageRank>=0 ? c.World.Cells[n].Water.DrainageElevation : environment(n).Elevation)) return false;
        }
        for (int d = 0; d < 6; d++) if ((w.IncomingMask & (1 << d)) != 0)
        {
            int n = c.Neighbors[index][d];
            if(n>=0 && w.DrainageRank>=0 && c.World.Cells[n].Water.DrainageRank>=0 && w.DrainageRank>=c.World.Cells[n].Water.DrainageRank) return false;
            if (n < 0 || (c.World.Cells[n].Water.DrainageRank>=0 ? c.World.Cells[n].Water.DrainageElevation : environment(n).Elevation) + 1e-10 < (w.DrainageRank>=0 ? w.DrainageElevation : environment(index).Elevation)) return false;
        }
        return true;
    }

    private static void RepairWater(Context c)
    {
        var expected = new int[c.World.Cells.Length];
        for (int i = 0; i < c.World.Cells.Length; i++)
        {
            var water = c.World.Cells[i].Water;
            if (water.OutDirection < 0) continue;
            if (water.OutDirection > 5) c.Fail("water-repair", $"Invalid out direction at {c.World.Cells[i].Id}.");
            int n = c.Neighbors[i][water.OutDirection];
            if (n < 0) c.Fail("water-repair", $"Outlet leaves the world at {c.World.Cells[i].Id}; protected endpoints are not rerouted.");
            expected[n] |= 1 << ((water.OutDirection + 3) % 6);
        }
        var changes = new List<(int index, int before, int after)>();
        for (int i = 0; i < expected.Length; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.Water.IncomingMask == expected[i]) continue;
            if (cell.Source != TileGenerationSource.Fill || cell.LandMarkDefineId != null)
                c.Fail("water-repair", $"Repair would change protected water at ({cell.Col},{cell.Row}).");
            if (changes.Count >= c.Profile.WaterRepairBudget)
                c.Fail("water-repair", $"Water repair budget exhausted at ({cell.Col},{cell.Row}).");
            changes.Add((i, cell.Water.IncomingMask, expected[i]));
        }
        // Preflight every change before mutation; restore all candidates if a downstream check rejects it.
        foreach (var change in changes) c.World.Cells[change.index].Water.IncomingMask = change.after;
        try { AuditWater(c); }
        catch
        {
            foreach (var change in changes) c.World.Cells[change.index].Water.IncomingMask = change.before;
            throw;
        }
        foreach (var change in changes)
            c.World.Diagnostics.Repairs.Add($"water {c.World.Cells[change.index].Id}: incoming mask {change.before} -> {change.after}");
    }

    private static void AuditWater(Context c)
    {
        var colors = new byte[c.World.Cells.Length];
        var lakes=c.World.Lakes.ToDictionary(x=>x.Id,StringComparer.Ordinal);
        foreach(var lake in c.World.Lakes)
        {
            if(lake.CellIndices.Count==0 || lake.CellIndices.Distinct().Count()!=lake.CellIndices.Count) c.Fail("water", "Invalid lake footprint.");
            foreach(int i in lake.CellIndices) if(c.World.Cells[i].Water.Kind!="lake" || c.World.Cells[i].Water.BodyId!=lake.Id || c.World.Cells[i].Water.SurfaceLevel!=lake.SurfaceLevel) c.Fail("water", "Lake ownership/water level changed.");
            var region=c.World.Regions[lake.RegionIndex];var def=c.Catalog.Regions[region.DefineId];
            foreach(int i in lake.CellIndices)
            {
                var w=c.World.Cells[i].Water;
                int inletMask=0;int outlet=-1;
                foreach(var p in def.Lake.Ports.Where(p=>c.Index(region.Anchor+new TileId(p.Dq,p.Dr))==i))
                    if(p.IsInlet) inletMask|=1<<p.Direction;else outlet=p.Direction;
                if((w.IncomingMask & ~inletMask)!=0 || w.OutDirection>=0 && w.OutDirection!=outlet || w.IsSink) c.Fail("water", "Lake direction violates authored port.");
                if(i==lake.OutletCell && outlet>=0 && c.Neighbors[i][outlet]!=lake.OutletTarget) c.Fail("water", "Lake outlet target changed.");
            }
            if(!lake.Closed && (lake.OutletCell<0 || lake.OutletTarget<0 || c.World.Cells[lake.OutletCell].Water.OutDirection<0) && c.Profile.Hydrology.RiverCount>0) c.Fail("water", "Open lake missing outlet.");
        }
        for (int i = 0; i < c.World.Cells.Length; i++)
        {
            var cell = c.World.Cells[i]; var water = cell.Water;
            if (!water.Active) continue;
            if (water.IncomingMask < 0 || water.IncomingMask > 63 || water.OutDirection < -1 || water.OutDirection > 5)
                c.Fail("water", $"Invalid water directions at {cell.Id}.");
            bool lakeCell=water.Kind=="lake" && water.BodyId!=null && lakes.ContainsKey(water.BodyId);
            if (!lakeCell && !water.IsSource && water.IncomingMask == 0) c.Fail("water", $"Undeclared river source at {cell.Id}.");
            if (water.IsSink)
            {
                bool ocean = water.Kind=="ocean" || cell.TileDefineId != null && c.Profile.Hydrology.OceanTypeIds.Contains(cell.TileDefineId);
                bool lake = c.Profile.Hydrology.AllowInlandLakes && !c.Profile.Hydrology.RequireOceanExit
                    && c.Profile.Hydrology.LakeSinks.Any(p => p.Col == cell.Col && p.Row == cell.Row);
                if (!ocean && !lake) c.Fail("water", $"Undeclared inland outlet at {cell.Id}.");
                if (water.OutDirection != -1) c.Fail("water", $"A sink cannot have downstream flow at {cell.Id}.");
            }
            else if (!lakeCell && water.OutDirection < 0) c.Fail("water", $"Suspended river with no outlet at {cell.Id}.");
            if (!FlowValidAt(c, i, n => c.World.Cells[n].Environment)) c.Fail("water", $"River runs uphill at {cell.Id}.");
            for (int d = 0; d < 6; d++) if ((water.IncomingMask & (1 << d)) != 0)
            {
                int n = c.Neighbors[i][d];
                if (n < 0 || c.World.Cells[n].Water.OutDirection != (d + 3) % 6)
                    c.Fail("water", $"Non-reciprocal inlet at {cell.Id}, direction {d}.");
            }
            if (water.OutDirection >= 0)
            {
                int n = c.Neighbors[i][water.OutDirection];
                if (n < 0 || (c.World.Cells[n].Water.IncomingMask & (1 << ((water.OutDirection + 3) % 6))) == 0)
                    c.Fail("water", $"Non-reciprocal outlet at {cell.Id}, direction {water.OutDirection}.");
            }
            if (colors[i] != 0) continue;
            int at = i; var path = new List<int>();
            while (true)
            {
                if (colors[at] == 1) c.Fail("water", $"River cycle at {c.World.Cells[at].Id}.");
                if (colors[at] == 2) break;
                colors[at] = 1; path.Add(at);
                var w = c.World.Cells[at].Water;
                if (w.IsSink) break;
                if(w.Kind=="lake" && w.BodyId!=null && lakes.TryGetValue(w.BodyId,out var body))
                {
                    if(body.Closed) break;
                    int target=body.OutletTarget;
                    if(target<0) c.Fail("water", "Suspended lake outlet.");
                    at=target;continue;
                }
                if (w.OutDirection < 0 || w.OutDirection > 5) c.Fail("water", $"River ends without a legal sink at {c.World.Cells[at].Id}.");
                int next = c.Neighbors[at][w.OutDirection];
                if (next < 0) c.Fail("water", $"River exits world at {c.World.Cells[at].Id}.");
                at = next;
            }
            foreach (int n in path) colors[n] = 2;
        }
    }

    private static void Audit(Context c)
    {
        if (c.World.Cells.Length != 40000) c.Fail("audit", "Main continent must contain 40000 cells.");
        var fixedExpected=new Context(c.Catalog,c.Profile,c.World.Seed,0,null);
        InstallFixedRegions(fixedExpected);GenerateEnvironment(fixedExpected);
        int fixedCount = c.Regions.Values.Sum(def => def.ForcedPositions.Count * def.TerrainLayout.Count);
        if (c.World.Cells.Count(cell => cell.IsFixed) != fixedCount) c.Fail("audit", "Forced region cell count changed.");
        for (int i = 0; i < c.World.Cells.Length; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.Col != i % c.World.Width || cell.Row != i / c.World.Width) c.Fail("audit", $"Cell coordinate/index mismatch at {i}.");
            if (cell.TileDefineId == null || !c.Catalog.Tiles.TryGetValue(cell.TileDefineId, out var type)) c.Fail("audit", $"Unknown/unfilled tile at {cell.Id}.");
            else
            {
                if (!ValidEnvironment(cell.Environment)) c.Fail("audit", $"Invalid environment at {cell.Id}.");
                if (cell.Source == TileGenerationSource.Fill && (!type.AllowFill || type.FillWeight <= 0 || !EnvironmentMatches(type.Environment, cell.Environment)))
                    c.Fail("audit", $"Illegal fill type {type.ID} at {cell.Id}.");
                if (!cell.IsFixed && !WaterCompatible(c, i, type)) c.Fail("audit", $"Type {type.ID} cannot carry water at {cell.Id}.");
                foreach (int n in c.Neighbors[i]) if (n >= 0 && !(cell.IsFixed && c.World.Cells[n].IsFixed) && c.World.Cells[n].TileDefineId != null && !AdjacentAllowed(type, c.Catalog.Tiles[c.World.Cells[n].TileDefineId]))
                    c.Fail("audit", $"Forbidden adjacency {type.ID} / {c.World.Cells[n].TileDefineId} at {cell.Id}.");
            }
            if (cell.LandMarkDefineId != null && !c.Catalog.LandMarks.ContainsKey(cell.LandMarkDefineId)) c.Fail("audit", $"Unknown landmark at {cell.Id}.");
            if (cell.Source == TileGenerationSource.Region && (cell.RegionIndex == null || cell.RegionIndex < 0 || cell.RegionIndex >= c.World.Regions.Count))
                c.Fail("audit", $"Missing region at {cell.Id}.");
            if (cell.Source != TileGenerationSource.Region && cell.RegionIndex != null) c.Fail("audit", $"Unexpected region at {cell.Id}.");
        }
        foreach (var region in c.World.Regions)
        {
            if (region.Index < 0 || region.Index >= c.World.Regions.Count || !ReferenceEquals(c.World.Regions[region.Index], region))
                c.Fail("audit", $"Invalid stable region sequence: {region.Index}.");
            var def = c.Catalog.Regions[region.DefineId];
            var expected = def.TerrainLayout.Keys.Select(k => c.Index(region.Anchor + new TileId(k.dq, k.dr))).ToHashSet();
            if (expected.Contains(-1) || !expected.SetEquals(region.CellIndices) || region.CellIndices.Count != expected.Count)
                c.Fail("audit", $"Region footprint mismatch: {region.DefineId} #{region.Index}.");
            foreach (var kv in def.TerrainLayout)
            {
                var cell = c.World.Cells[c.Index(region.Anchor + new TileId(kv.Key.dq, kv.Key.dr))];
                if (cell.TileDefineId != kv.Value || cell.RegionIndex != region.Index || cell.Source != TileGenerationSource.Region || cell.IsFixed != region.IsFixed)
                    c.Fail("audit", $"Author puzzle changed: {region.DefineId} at {cell.Id}.");
                if (region.IsFixed)
                {
                    var env = fixedExpected.World.At(cell.Col,cell.Row).Environment;
                    var type = c.Catalog.Tiles[kv.Value];
                    if (cell.Environment.Elevation != env.Elevation || cell.Environment.Moisture != env.Moisture || cell.Environment.Temperature != env.Temperature
                        || cell.BlockAllMovement != type.BlocksAllMovement || !cell.AllowedMovement.SequenceEqual(type.MovementModes))
                        c.Fail("audit", $"Forced region attributes changed: {def.ID} at {cell.Id}.");
                }
                string expectedLandmark = def.LandMarkLayout.GetValueOrDefault(kv.Key)
                    ?? c.Catalog.LandMarks.Values.FirstOrDefault(lm => lm.ForcedPositions.Any(p => p.Col == cell.Col && p.Row == cell.Row))?.ID;
                if (cell.LandMarkDefineId != expectedLandmark) c.Fail("audit", $"Author landmark changed: {region.DefineId} at {cell.Id}.");
            }
        }
        foreach (var def in c.Regions.Values)
        {
            var instances = c.World.Regions.Where(r => r.DefineId == def.ID).ToArray();
            if (def.ForcedPositions.Count > 0)
            {
                var anchors = def.ForcedPositions.Select(p => Plane.OffsetToAxial(p.Col, p.Row)).ToHashSet();
                if (instances.Length != anchors.Count || instances.Any(r => !r.IsFixed) || !anchors.SetEquals(instances.Select(r => r.Anchor)))
                    c.Fail("audit", $"Forced placements changed: {def.ID}.");
                continue;
            }
            if (instances.Any(r => r.IsFixed)) c.Fail("audit", $"Unexpected fixed placement: {def.ID}.");
            if (instances.Length < def.MinCount || (def.MaxCount >= 0 && instances.Length > def.MaxCount)) c.Fail("audit", $"Region count violation: {def.ID}.");
            for (int i = 0; i < instances.Length; i++) for (int j = i + 1; j < instances.Length; j++)
                if (instances[i].Anchor.DistanceTo(instances[j].Anchor) < def.MinSpacing) c.Fail("audit", $"Region spacing violation: {def.ID}.");
        }
        foreach (var def in c.Catalog.LandMarks.Values)
        foreach (var point in def.ForcedPositions)
        {
            var cell = c.World.At(point.Col, point.Row);
            if (cell.LandMarkDefineId != def.ID || !cell.HasForcedLandmark) c.Fail("audit", $"Forced landmark changed: {def.ID} at {point.Col},{point.Row}.");
        }
        foreach (var cell in c.World.Cells.Where(cell => cell.HasForcedLandmark))
            if (cell.LandMarkDefineId == null || !c.Catalog.LandMarks[cell.LandMarkDefineId].ForcedPositions.Any(p => p.Col == cell.Col && p.Row == cell.Row))
                c.Fail("audit", $"Unexpected fixed landmark at {cell.Id}.");
        foreach (var group in c.World.Cells.Where(x => x.LandMarkDefineId != null).GroupBy(x => x.LandMarkDefineId))
            if (c.Catalog.LandMarks[group.Key].IsSingleton && group.Count() > 1) c.Fail("audit", $"Duplicate singleton {group.Key}.");
        AuditWater(c); AuditConnectivity(c); AuditHydrologyMetrics(c);
    }

    private static void AuditConnectivity(Context c)
    {
        foreach (var group in c.Profile.ConnectivityGroups)
        {
            var points = group.FixedPoints.Select(p => p.Row * c.World.Width + p.Col).ToList();
            foreach (string reference in group.RegionPoints)
            {
                var parts = reference.Split(':');
                foreach (var region in c.World.Regions.Where(x => x.DefineId == parts[0]))
                {
                    var point = c.Catalog.Regions[region.DefineId].KeyPoints.Single(x => x.ID == parts[1]);
                    points.Add(c.Index(region.Anchor + new TileId(point.Dq, point.Dr)));
                }
            }
            if (points.Count == 0) continue;
            bool Allowed(int i)
            {
                if (i < 0 || i >= c.World.Cells.Length) return false;
                var cell = c.World.Cells[i]; var type = c.Catalog.Tiles[cell.TileDefineId];
                if (cell.BlockAllMovement || type.BlocksAllMovement) return false;
                var modes = cell.AllowedMovement is { Count: > 0 } ? cell.AllowedMovement : type.MovementModes;
                return modes.Contains(group.MovementMode) && (group.CanCrossRivers || cell.Water.Kind!="river" && !cell.Water.Active);
            }
            if (points.Any(p => !Allowed(p))) c.Fail("connectivity", $"Group {group.ID}: key point blocks '{group.MovementMode}' or river crossing.");
            var seen = new bool[c.World.Cells.Length]; var queue = new Queue<int>();
            seen[points[0]] = true; queue.Enqueue(points[0]);
            while (queue.TryDequeue(out int i)) foreach (int n in c.Neighbors[i])
                if (n >= 0 && !seen[n] && Allowed(n)) { seen[n] = true; queue.Enqueue(n); }
            var missing = points.Where(p => !seen[p]).ToArray();
            if (missing.Length > 0) c.Fail("connectivity", $"Group {group.ID}: {missing.Length} points unreachable using '{group.MovementMode}' (river crossing={group.CanCrossRivers}); first {c.World.Cells[missing[0]].Id}.");
        }
    }
}
