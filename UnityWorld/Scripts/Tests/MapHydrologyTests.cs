using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Tests;

/// <summary>Numerical fixtures exercise production stages, including failures before publication.</summary>
public static class MapHydrologyTests
{
    private static int _checks;
    private static void Check(bool value, string message)
    { _checks++; if (!value) throw new InvalidOperationException("Hydrology assertion: " + message); }
    private static void Reject(Action action, string text)
    {
        try { action(); }
        catch (Exception e) when (e is InvalidDataException or MapGenerationException)
        { Check(e.Message.Contains(text, StringComparison.OrdinalIgnoreCase), text + ": " + e.Message); return; }
        throw new InvalidOperationException("Expected hydrology rejection: " + text);
    }

    // Test-only reflection avoids publishing generation-stage mutation APIs to gameplay callers.
    private sealed class Fixture
    {
        public readonly MapCatalog Catalog;
        public readonly WorldGenerationOptions Options;
        public readonly GeneratedWorld World;
        private readonly object _context;
        public Fixture(string root, int seed = 17, bool lakes = false)
        {
            Catalog = MapCatalog.Load(root);
            Options = new() { RegionIds = Catalog.Regions.Values.Where(r => lakes ? r.GenerationPhase == "lake" : r.GenerationPhase=="ordinary" && r.ForcedPositions.Count==0).Select(r => r.ID).ToList(),
                WholeMapAttempts = 1, LandmarkDensity = 0, RegionCoverageTarget = 0 };
            var type = typeof(WorldMapGenerator).GetNestedType("Context", BindingFlags.NonPublic)!;
            _context = Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { Catalog, Options, seed, 0, null }, null)!;
            World = (GeneratedWorld)type.GetField("World")!.GetValue(_context)!;
            foreach (var cell in World.Cells) cell.Environment = new() { Elevation = .4, Moisture = .6, Temperature = .5 };
        }
        public object Call(string name, params object[] tail)
        {
            var args = new object[] { _context }.Concat(tail).ToArray();
            try { return typeof(WorldMapGenerator).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
        }
        public bool Try(RegionDefine def, int col, int row, bool commit = false)
        {
            var args = new object[] { _context, def, row * 200 + col, null, true };
            bool ok = (bool)typeof(WorldMapGenerator).GetMethod("TryRegion", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;
            if (ok && commit) Call("CommitRegion", args[3]);
            return ok;
        }
        public void OceanEdge()
        {
            for (int row = 0; row < 200; row++)
            {
                var c = World.At(199, row); c.Environment.Elevation = .01;
                c.Water.Kind = "ocean"; c.Water.IsSink = true; c.Water.BodyId = "ocean";
            }
        }
    }

    public static void Run(string root)
    {
        _checks = 0;
        Parameters(root); SeaAndFill(root); Environment(root); LakesAndDrainage(root); RegionRules(root); NarrowCanyon(root);
        Console.WriteLine($"MAP_HYDROLOGY_TESTS_OK assertions={_checks}");
    }

    private static void Parameters(string root)
    {
        var catalog = MapCatalog.Load(root); var p = catalog.Profiles["china-inspired-v1"];
        foreach (var bad in new[] { double.NaN, double.PositiveInfinity, -.1, 1.1 })
        { p.Hydrology.SeaLevel = bad; Reject(() => catalog.Validate(p), "SeaLevel"); }
        p.Hydrology.SeaLevel = .05;
        foreach (int bad in new[] { 0, -1 })
        { p.Hydrology.RoutingBudget = bad; Reject(() => catalog.Validate(p), "routing budget"); }
        p.Hydrology.RoutingBudget = 2000000;
        p.LakePlacementAttempts = 0; Reject(() => catalog.Validate(p), "LakePlacementAttempts"); p.LakePlacementAttempts = 4000;
        p.RegionGap = -1; Reject(() => catalog.Validate(p), "RegionGap"); p.RegionGap = 2;
        p.Hydrology.MainRiverWidth = 4; Reject(() => catalog.Validate(p), "width"); p.Hydrology.MainRiverWidth = 2;
        p.Hydrology.MainRiverThreshold = 1; Reject(() => catalog.Validate(p), "MainRiverThreshold"); p.Hydrology.MainRiverThreshold = 500;
        p.Environment.FixedVariationScale = 0; Reject(() => catalog.Validate(p), "FixedVariationScale"); p.Environment.FixedVariationScale = 22;
        var lake = catalog.Regions["lake_plain"];
        lake.Lake.Ports.Add(lake.Lake.Ports.Last()); Reject(() => catalog.Validate(p), "Lake.Ports"); lake.Lake.Ports.RemoveAt(2);
        lake.Lake.Ports[0].Direction = 0; Reject(() => catalog.Validate(p), "outside footprint"); lake.Lake.Ports[0].Direction = 3;
        lake.FixedVariation.Moisture = double.NaN; Reject(() => catalog.Validate(p), "FixedVariation.Moisture"); lake.FixedVariation.Moisture = 0;
        catalog.Validate(p);
        Check(!catalog.Tiles.ContainsKey("river_lake") && (int)TerrainType.River == 3 && (int)TerrainType.Lake == 7, "canonical types preserve legacy numeric ABI");
        var aura = catalog.Tiles["river"].InitialAura;
        Check(aura.Jin == .8f && aura.Mu == 1.2f && aura.Shui == 2.5f && aura.Huo == .5f && aura.Tu == .8f, "river aura unchanged");
        var water = new MapWater { Kind = "lake", BodyId = "fixture", SurfaceLevel = .7, Runoff = 12, Width = 3, DrainageElevation = .8 };
        var copy = water.Copy(); copy.Runoff = 999; copy.BodyId = "other";
        Check(water.Runoff == 12 && water.BodyId == "fixture" && copy.SurfaceLevel == .7, "all scalar projection fields copy independently");
    }

    private static void SeaAndFill(string root)
    {
        var f = new Fixture(root); f.Options.Hydrology.RiverCount = 0;
        f.World.At(60, 60).Environment.Elevation = .049;
        f.World.At(61, 60).Environment.Elevation = .05;
        var locked = f.World.At(62, 60); locked.IsFixed = true; locked.TileDefineId = "mountain"; locked.Environment.Elevation = .01;
        f.Call("ClassifySea");
        Check(f.World.At(60, 60).Water.Kind == "ocean" && !f.World.At(60, 60).Water.Active, "landlocked .049 sea without any centerline");
        Check(f.World.At(61, 60).Water.Kind == "none", ".05 boundary stays unforced");
        Check(locked.TileDefineId == "mountain" && locked.Water.Kind == "none" && f.World.Diagnostics.Rejections.ContainsKey("sea.fixed-land-exception"), "fixed land override diagnosed");
        f.Catalog.Tiles["plain"].FillWeight = 1e12; f.Call("Fill");
        Check(f.World.At(60, 60).TileDefineId == "ocean", "huge land weight cannot take sea");
        Check(f.World.At(61, 60).TileDefineId == "plain", "water cannot appear on normal land");
        var zero = new Fixture(root); zero.World.At(50, 50).Environment.Elevation = .01; zero.Call("ClassifySea");
        zero.Catalog.Tiles["ocean"].FillWeight = 0;
        Reject(() => zero.Call("Fill"), "No environment/water-compatible fill");
    }

    private static void Environment(string root)
    {
        // Bounds declared before sampling: per-edge E <= .12, M/T <= .10, excluding only explicit wall/coast cliffs.
        double[] bounds = { .12, .10, .10 };
        var a = new Fixture(root, 19); a.Call("InstallFixedRegions"); a.Call("GenerateEnvironment");
        var b = new Fixture(root, 20); b.Call("InstallFixedRegions"); b.Call("GenerateEnvironment");
        foreach (var region in a.World.Regions.Where(r => r.IsFixed))
        {
            var cells = region.CellIndices.Select(i => a.World.Cells[i]).ToArray();
            foreach (var axis in new Func<MapEnvironment, double>[] { e => e.Elevation, e => e.Moisture, e => e.Temperature })
                Check(cells.Max(c => axis(c.Environment)) - cells.Min(c => axis(c.Environment)) > 1e-6, "fixed interiors vary on every axis");
            foreach (int i in region.CellIndices)
                Check(a.World.Cells[i].Environment.Elevation == b.World.Cells[i].Environment.Elevation
                    && a.World.Cells[i].Environment.Moisture == b.World.Cells[i].Environment.Moisture
                    && a.World.Cells[i].Environment.Temperature == b.World.Cells[i].Environment.Temperature, "fixed coordinates stable across seeds");
        }
        var first = a.World.Regions.First(r => r.DefineId == "fixed_north_mountains");
        var next = a.World.Regions.First(r => r.DefineId == first.DefineId && r.Index != first.Index);
        Check(a.World.Cells[first.CellIndices[0]].Environment.Elevation != a.World.Cells[next.CellIndices[0]].Environment.Elevation, "repeated template does not reset texture");
        a.Call("ClassifySea"); a.Call("AuditHydrologyMetrics");
        string[] axes = { "elevation", "moisture", "temperature" };
        for (int k = 0; k < 3; k++)
            Check(a.World.Diagnostics.Metrics[axes[k] + "GradientMax"] <= bounds[k] && a.World.Diagnostics.Metrics[axes[k] + "SeamMax"] <= bounds[k], "continuous " + axes[k] + " including region seams");
    }

    private static void LakesAndDrainage(string root)
    {
        var f = new Fixture(root, lakes: true); f.OceanEdge(); f.Call("PlaceLakes");
        Check(f.World.Lakes.Count == 3 && f.World.Lakes.All(x => x.CellIndices.Count >= 25), "three complete large lakes");
        Check(f.World.Lakes.Count(x => x.Closed) == 1, "explicit closed role represented");
        foreach (var lake in f.World.Lakes)
        {
            var region = f.World.Regions[lake.RegionIndex]; var def = f.Catalog.Regions[region.DefineId];
            Check(region.CellIndices.Count == def.TerrainLayout.Count, "shore and islands not cropped");
            foreach (var p in def.TerrainLayout)
            {
                var (col, row) = Plane.AxialToOffset(region.Anchor + new TileId(p.Key.dq, p.Key.dr));
                Check(f.World.At(col, row).TileDefineId == p.Value, "lake exact authored type");
            }
            Check(lake.CellIndices.All(i => f.World.Cells[i].Water.SurfaceLevel == lake.SurfaceLevel && !f.World.Cells[i].Water.Active), "flat lake surface needs no direction per cell");
        }
        // A real depression remains lower in natural data after drainage crosses its spill level.
        var depression = f.World.Cells.First(c => c.RegionIndex == null && c.Col == 100 && c.Row > 50);
        depression.Environment.Elevation = .08;
        var before = f.World.Cells.Select(c => c.Environment.Copy()).ToArray();
        f.Call("PlanHydrology"); f.Call("AuditHydrologyMetrics");
        Check(depression.Water.DrainageElevation > depression.Environment.Elevation, "depression has separate spill height");
        Check(f.World.Cells.Select((c, i) => c.Environment.Elevation == before[i].Elevation && c.Environment.Temperature == before[i].Temperature).All(x => x), "hydrology preserves every natural height and temperature");
        Check(f.World.Cells.Any(c => c.Water.Kind == "river" && !c.Water.IsCenterline && !c.Water.Active), "real wide side cells, no fake direction edges");
        foreach (int width in new[] { 1, 2, 3 }) Check(f.World.Cells.Any(c => c.Water.IsCenterline && c.Water.Width == width), "river width class " + width);
        foreach (var lake in f.World.Lakes.Where(x => !x.Closed)) Check(f.World.Cells[lake.OutletCell].Water.OutDirection >= 0 && lake.DrainageTarget >= 0, "open lake continues via unique outlet");
        Check(f.World.Lakes.Where(x=>!x.Closed).All(l=>l.InletCells.Any(i=>f.World.Cells[i].Water.IncomingMask!=0)), "declared inlets feed open lakes and continue downstream");
        var impossible = new Fixture(root, lakes: true);
        foreach (var cell in impossible.World.Cells) cell.Water.Kind = "ocean";
        Reject(() => impossible.Call("PlaceLakes"), "missing");
        Check(impossible.World.Regions.Count == 0 && impossible.World.Cells.All(c => c.TileDefineId == null), "failed lake search leaves no partial region");
        var singleton = new Fixture(root, lakes: true); singleton.Catalog.LandMarks["natural_reed_marsh"].IsSingleton = true;
        Reject(() => singleton.Call("PlaceLakes"), "missing");
        Check(singleton.World.Regions.Count == 0 && singleton.World.Cells.All(c=>c.Source!=TileGenerationSource.Region && c.RegionIndex==null && c.LandMarkDefineId==null), "singleton backtracking restores earlier lakes and provenance");
        var reserved=new Fixture(root,lakes:true);foreach(var cell in reserved.World.Cells) cell.HasForcedLandmark=true;
        Reject(()=>reserved.Call("PlaceLakes"), "missing");
        Check(reserved.World.Regions.Count==0 && reserved.World.Cells.All(c=>c.HasForcedLandmark && c.TileDefineId==null), "fixed landmark reservations block whole lake atomically");
        var flow = new Fixture(root); flow.OceanEdge(); flow.Call("PlanHydrology");
        var children = flow.World.Cells.Where(c => c.Water.DrainageTarget >= 0).GroupBy(c => c.Water.DrainageTarget).ToDictionary(g => g.Key, g => g.Sum(c => c.Water.Runoff));
        var confluence = flow.World.Cells.First(c => c.Water.IsCenterline && System.Numerics.BitOperations.PopCount((uint)c.Water.IncomingMask) > 1);
        Check(Math.Abs(confluence.Water.Runoff - confluence.Water.LocalRunoff - children[confluence.Row * 200 + confluence.Col]) < 1e-7, "known upstream sums plus local contribution conserved");
        Check(flow.World.Cells.Count(c => c.Water.IsSource) > 4, "tributaries are not limited to four western paths");
        foreach (var c in flow.World.Cells.Where(c => c.Water.IsCenterline && c.Water.Width > 1))
        {
            int d = c.Water.OutDirection;
            bool Fits(int offset) => new[] { (d + offset) % 6, (d + offset + 3) % 6 }.Take(c.Water.Width - 1).All(v =>
            {
                var (col, row) = Plane.AxialToOffset(c.Id.Neighbor(v));
                return col >= 0 && col < 200 && row >= 0 && row < 200 && flow.World.At(col, row).Water.Kind is "river" or "ocean";
            });
            Check(Fits(2) || Fits(1), "actual perpendicular water cross-section matches width");
        }
    }

    private static void RegionRules(string root)
    {
        var f = new Fixture(root);
        var a = f.Catalog.Regions["region_taihang"]; var b = f.Catalog.Regions["region_qinling"];
        // Single-cell actual footprints leave surrounding bounding metadata irrelevant.
        a.TerrainLayout = new() { [(0, 0)] = "plain" }; a.LandMarkLayout.Clear(); a.AnchorEnvironment = a.OverallEnvironment = new(); a.KeyPoints.Clear();
        b.TerrainLayout = new() { [(0, 0)] = "plain" }; b.LandMarkLayout.Clear(); b.AnchorEnvironment = b.OverallEnvironment = new(); b.KeyPoints.Clear();
        Check(f.Try(a, 70, 70, true), "first region placed");
        Check(!f.Try(b, 71, 70) && !f.Try(b, 72, 70), "cross-type touching and one-cell gap rejected");
        Check(!f.Try(a,73,70), "same-type anchor spacing still applies");
        Check(f.Try(b, 73, 70), "two unowned transition cells accepted");
        var holes=new Fixture(root);
        var ring=holes.Catalog.Regions["region_taihang"];var inset=holes.Catalog.Regions["region_qinling"];
        ring.TerrainLayout=new();ring.LandMarkLayout.Clear();ring.AnchorEnvironment=ring.OverallEnvironment=new();ring.KeyPoints.Clear();
        for(int r=-4;r<=4;r++) for(int q=-4;q<=4;q++) if(new TileId(q,r).DistanceTo(TileId.Zero)==4) ring.TerrainLayout[(q,r)]="plain";
        inset.TerrainLayout=new(){[(0,0)]="plain"};inset.LandMarkLayout.Clear();inset.AnchorEnvironment=inset.OverallEnvironment=new();inset.KeyPoints.Clear();
        Check(holes.Try(ring,100,71,true) && holes.Try(inset,100,71), "actual ring hole can host another piece at odd-row anchor");
        Check(!holes.Try(inset,103,71), "hole does not bypass distance to actual ring cells");
        a.EnvironmentAdjustments = new() { new() { Elevation = -.1 } };
        f.World.At(90, 90).Environment.Elevation = .10;
        Check(!f.Try(a, 90, 90), "post-water adjustment cannot cross sea level");
        a.EnvironmentAdjustments[0].Elevation = .05;
        f.World.At(90,90).Water.DrainageRank=10;f.World.At(90,90).Water.DrainageElevation=.12;
        Check(!f.Try(a,90,90) && f.World.At(90,90).Environment.Elevation==.1, "adjustment breaking established drainage height rejected atomically");
        f.World.At(90,90).Water.DrainageRank=-1;
        Check(f.Try(a, 90, 90, true), "legal natural height adjustment accepted");
        f.World.At(0,0).IsFixed=true; f.World.At(0,0).RegionIndex=0;
        f.World.At(1,0).Water.Kind="ocean";
        f.World.At(2,0).Water.Kind="river";f.World.At(2,0).Water.IsCenterline=true;
        f.World.At(70,70).Water.Kind="river";f.World.At(70,70).Water.IsCenterline=true;
        a.EnvironmentAdjustments=new(){new(){Elevation=-.05,Temperature=.12}};
        Check(!f.Try(a,150,100), "derived temperature change also respects per-axis adjustment budget");
        var lakeRegion=new GeneratedRegion {Index=f.World.Regions.Count,DefineId="lake_plain",CellIndices=new(){3,4,5}};
        f.World.Regions.Add(lakeRegion);
        foreach(int i in lakeRegion.CellIndices) f.World.Cells[i].RegionIndex=lakeRegion.Index;
        f.World.Cells[3].Water.Kind="ocean"; // Deliberate overlap verifies set subtraction, not double counting.
        f.Call("AuditHydrologyMetrics");
        Check(f.World.Diagnostics.Metrics["eligibleLand"]==39993 && f.World.Diagnostics.Metrics["ordinaryLand"]==1, "coverage subtracts union and excludes ordinary valley water");
        var zero=new Fixture(root);foreach(var cell in zero.World.Cells) cell.Water.Kind="ocean";
        zero.Call("AuditHydrologyMetrics");Check(zero.World.Diagnostics.RegionCoverage==0 && zero.World.Diagnostics.Metrics["eligibleLand"]==0, "zero eligible area is finite");
        Check(Math.Abs(f.World.At(90, 90).Environment.Temperature - (.5 - .18 * .05)) < 1e-10, "temperature recomputed with final natural height");
    }

    private static void NarrowCanyon(string root)
    {
        var f = new Fixture(root); f.OceanEdge();
        var def = f.Catalog.Regions["region_river_valley_0"];
        def.ForcedPositions = new() { new() { Col = 80, Row = 90 } };
        def.TerrainLayout = new() { [(0, 0)] = "river", [(1, 0)] = "river", [(2, 0)] = "river" };
        def.NarrowRiverCells = Enumerable.Range(0, 3).Select(q => new RegionKeyPoint { Dq = q, Dr = 0 }).ToList();
        f.Options.Hydrology.TributaryThreshold = .1; f.Options.Hydrology.MainRiverThreshold = .1; f.Options.Hydrology.WideRiverThreshold = .1;
        f.Call("InstallFixedRegions"); f.Call("ClassifySea"); f.Call("PlanHydrology");
        Check(f.World.At(80, 90).Water.Width == 1 && f.World.At(80, 90).Water.Reason == "authored-narrow-canyon", "only authored canyon narrows");
        Check(f.World.Diagnostics.Repairs.Any(x => x.Contains("explicit narrow canyon")), "narrow exception diagnosed");
        Check(f.World.At(83, 90).Water.Width == 3, "normal downstream restores required width");
        var blocked=new Fixture(root);var canyon=blocked.Catalog.Regions["region_river_valley_0"];
        canyon.ForcedPositions=new(){new(){Col=80,Row=90}};
        canyon.TerrainLayout=new(){[(0,0)]="river",[(1,0)]="river",[(2,0)]="river",[(1,1)]="mountain",[(0,1)]="mountain",[(1,-1)]="mountain",[(2,-1)]="mountain"};
        blocked.Options.Hydrology.TributaryThreshold=.1;blocked.Options.Hydrology.MainRiverThreshold=.1;blocked.Options.Hydrology.WideRiverThreshold=.1;
        blocked.Call("InstallFixedRegions");blocked.Call("ClassifySea");
        Reject(()=>blocked.Call("PlanHydrology"), "Authored river");
        Check(blocked.World.At(81,91).TileDefineId=="mountain" && blocked.World.At(81,90).TileDefineId=="river", "blocked main channel cannot overwrite peak or silently narrow");
    }
}
