using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Tests;

/// <summary>Real 200x200 generation and adversarial contract checks, without global world initialization.</summary>
public static class MapGeneratorTests
{
    private static int _checks;
    private static void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException("Map generator assertion: " + message); }
    private static void Reject(Action action, string stage, string message)
    {
        try { action(); }
        catch (MapGenerationException ex) { Check(ex.Stage == stage, message + "; wrong stage " + ex.Stage); return; }
        throw new InvalidOperationException("Expected map rejection: " + message);
    }
    private static WorldGenerationOptions Profile(MapCatalog c) => c.Profiles["china-inspired-v1"];

    /// <summary>Runs deterministic success and failure fixtures using the on-disk development catalog.</summary>
    public static void Run(string baseDir)
    {
        _checks = 0;
        FixedPlacements(baseDir);
        var catalog = MapCatalog.Load(baseDir); var profile = Profile(catalog);
        Reject(() => WorldMapGenerator.Generate(catalog, new WorldGenerationOptions { ID = profile.ID, LandmarkDensity = -1 }, 1),
            "configuration", "Caller-provided ordinary options cannot bypass parameter validation.");
        string configBefore = catalog.ConfigHash;
        var a = WorldMapGenerator.Generate(catalog, profile, 1);
        Check(catalog.ConfigHash == configBefore, "Materialized rim must not alter normalized configuration hash.");
        Check(a.Cells.Length == 40000 && a.Cells.All(x => catalog.Tiles.ContainsKey(x.TileDefineId)), "Full map has 40000 known types.");
        Check(a.Cells.Count(x => x.IsFixed) == catalog.Regions.Values.Sum(r => r.ForcedPositions.Count * r.TerrainLayout.Count), "Procedural rim exact count.");
        Check(a.Cells.Where(x => x.Col == 0).All(x => x.BlockAllMovement), "West wall blocks all movement.");
        Check(a.Diagnostics.RiverCellCount > 0 && a.Cells.Any(x => x.Water.IsSink && x.Water.Active), "Water has real source-to-sink graph.");
        Check(a.Diagnostics.RequiredAttempts <= profile.RequiredRegionAttempts, "Required search stays in budget.");
        Check(a.Diagnostics.StageMilliseconds.ContainsKey("fill"), "Per-phase timing reported.");
        var decisionCell = a.Cells.First(x => x.Source == TileGenerationSource.Fill);
        Check(decisionCell.FillCandidates.Count == decisionCell.FillCandidateCount && decisionCell.FillCandidates.Any(x => x.TypeId == decisionCell.TileDefineId), "Queryable candidate set includes selected type.");
        Check(decisionCell.FillCandidates.All(x => Math.Abs(x.FinalWeight - x.BaseWeight * x.EnvironmentWeight * x.AdjacencyWeight) < 1e-8), "Candidate weight factors explain final selection weight.");
        foreach (var region in a.Regions)
        {
            var def = catalog.Regions[region.DefineId];
            Check(region.CellIndices.Count == def.TerrainLayout.Count, "Footprint exact cardinality.");
            foreach (var pair in def.TerrainLayout)
            {
                var at = region.Anchor + new TileId(pair.Key.dq, pair.Key.dr);
                var (col, row) = Plane.AxialToOffset(at); var tile = a.At(col, row);
                Check(tile.TileDefineId == pair.Value && tile.RegionIndex == region.Index, "Fixed author layout preserved.");
            }
        }
        // Odd/even anchors must translate true axial offsets, including negative q at valid offset columns.
        foreach (int row in new[] { 30, 31 })
        {
            var anchor = Plane.OffsetToAxial(6, row);
            foreach (var direction in TileId.Directions)
            {
                var moved = anchor + direction;
                var offset = Plane.AxialToOffset(moved);
                Check(Plane.OffsetToAxial(offset.col, offset.row) == moved && anchor.DistanceTo(moved) == 1, "Odd-r parity preserves axial neighbour.");
            }
        }
        string hashA = WorldMapExport.StructureHash(a);
        var b = WorldMapGenerator.Generate(catalog, profile, 2);
        Check(WorldMapExport.BoundaryHash(a) == WorldMapExport.BoundaryHash(b), "Boundary independent of world seed.");
        // Reverse registry insertion order and consume process-wide entity identities.
        foreach (var dictionary in new[] { catalog.Tiles })
        { var pairs = dictionary.Reverse().ToArray(); dictionary.Clear(); foreach (var pair in pairs) dictionary.Add(pair.Key, pair.Value); }
        var regionPairs = catalog.Regions.Reverse().ToArray(); catalog.Regions.Clear(); foreach (var pair in regionPairs) catalog.Regions.Add(pair.Key, pair.Value);
        var ids = new Rng(137); for (int i = 0; i < 30; i++) ids.NewId();
        var repeat = WorldMapGenerator.Generate(catalog, profile, 1);
        Check(WorldMapExport.StructureHash(repeat) == hashA, "Seed A stable after B, registry reversal, and NewId consumption.");

        // A legal reciprocal repair modifies only fill metadata, never the author river.
        var repairCell = a.Cells.First(x => x.Source == TileGenerationSource.Fill && x.Water.IncomingMask != 0);
        int oldMask = repairCell.Water.IncomingMask; repairCell.Water.IncomingMask = 0;
        WorldMapGenerator.RepairAndValidateWater(catalog, profile, a);
        Check(repairCell.Water.IncomingMask == oldMask, "Fill reciprocal inlet repaired.");
        var protectedCell = a.Cells.First(x => x.Source == TileGenerationSource.Region && x.Water.IncomingMask != 0);
        int protectedMask = protectedCell.Water.IncomingMask; protectedCell.Water.IncomingMask = 0;
        Reject(() => WorldMapGenerator.RepairAndValidateWater(catalog, profile, a), "water-repair", "Protected author river never repaired in place.");
        Check(protectedCell.Water.IncomingMask == 0, "Rejected repair does not silently change protected data.");
        protectedCell.Water.IncomingMask = protectedMask;
        WorldMapGenerator.ValidatePlan(catalog, profile, a);

        // Incoming symmetry and downhill flow are independent failures, not warnings.
        int directionBefore = repairCell.Water.OutDirection;
        repairCell.Water.OutDirection = -1;
        Reject(() => WorldMapGenerator.ValidatePlan(catalog, profile, a), "water", "Dangling river rejected.");
        repairCell.Water.OutDirection = directionBefore;

        // Equal-height two-cell cycles are invalid even with perfectly reciprocal metadata.
        var cycleA = a.Cells.First(x => x.Source == TileGenerationSource.Fill && !x.Water.Active
            && Enumerable.Range(0, 6).Any(d =>
            {
                var n = Plane.AxialToOffset(x.Id.Neighbor(d));
                return n.col >= 0 && n.col < 200 && n.row >= 0 && n.row < 200 && a.At(n.col, n.row).Source == TileGenerationSource.Fill && !a.At(n.col, n.row).Water.Active;
            }));
        int cycleDirection = Enumerable.Range(0, 6).First(d =>
        {
            var n = Plane.AxialToOffset(cycleA.Id.Neighbor(d));
            return n.col >= 0 && n.col < 200 && n.row >= 0 && n.row < 200 && a.At(n.col, n.row).Source == TileGenerationSource.Fill && !a.At(n.col, n.row).Water.Active;
        });
        var nextOffset = Plane.AxialToOffset(cycleA.Id.Neighbor(cycleDirection)); var cycleB = a.At(nextOffset.col, nextOffset.row);
        var waterA = cycleA.Water; var waterB = cycleB.Water; double elevationB = cycleB.Environment.Elevation;
        cycleA.Water = new() { IncomingMask = 1 << cycleDirection, OutDirection = cycleDirection };
        cycleB.Water = new() { IncomingMask = 1 << ((cycleDirection + 3) % 6), OutDirection = (cycleDirection + 3) % 6 };
        cycleB.Environment.Elevation = cycleA.Environment.Elevation;
        Reject(() => WorldMapGenerator.RepairAndValidateWater(catalog, profile, a), "water", "Equal-elevation directed cycle rejected.");
        cycleA.Water = waterA; cycleB.Water = waterB; cycleB.Environment.Elevation = elevationB;

        var impossible = MapCatalog.Load(baseDir); var impossibleProfile = Profile(impossible);
        impossibleProfile.WholeMapAttempts = 1;
        var required = impossible.Regions.Values.First(x => x.GenerationPhase=="ordinary" && x.MinCount > 0);
        required.TerrainLayout[(500, 500)] = "mountain";
        Reject(() => WorldMapGenerator.Generate(impossible, impossibleProfile, 1), "required-regions", "Out-of-bounds required template is never cut.");

        var noFill = MapCatalog.Load(baseDir); var noFillProfile = Profile(noFill); noFillProfile.WholeMapAttempts = 1;
        foreach (var tile in noFill.Tiles.Values) tile.FillWeight = 0;
        Reject(() => WorldMapGenerator.Generate(noFill, noFillProfile, 1), "fill", "Zero total fill weight has no implicit plain fallback.");

        var carrier = MapCatalog.Load(baseDir); var carrierProfile = Profile(carrier);
        carrier.Tiles.Add("unlisted-river", new() { ID = "unlisted-river", WaterKind = "river", FillWeight = 1e9 });
        var carrierWorld = WorldMapGenerator.Generate(carrier, carrierProfile, 1);
        Check(carrierWorld.Cells.All(x => x.TileDefineId != "unlisted-river"), "Profile river carrier whitelist beats arbitrarily high fill weight.");

        var noExit = MapCatalog.Load(baseDir); var noExitProfile = Profile(noExit); noExitProfile.WholeMapAttempts = 1; noExitProfile.RegionIds.RemoveAll(id=>noExit.Regions[id].GenerationPhase=="lake");
        noExit.Tiles.Add("absent-ocean-outlet", new() { ID = "absent-ocean-outlet", WaterKind = "ocean", AllowFill = false });
        noExitProfile.Hydrology.OceanTypeIds = new() { "absent-ocean-outlet" };
        Reject(() => WorldMapGenerator.Generate(noExit, noExitProfile, 1), "hydrology", "No ocean outlet is rejected.");

        var inland = MapCatalog.Load(baseDir); var inlandProfile = Profile(inland);
        inlandProfile.Hydrology.SeaLevel=0; inlandProfile.RegionIds.Clear(); inlandProfile.ConnectivityGroups.Clear(); inlandProfile.RegionCoverageTarget = 0;
        inland.Tiles.Add("absent-ocean-outlet", new() { ID = "absent-ocean-outlet", WaterKind = "ocean", AllowFill = false });
        inlandProfile.Hydrology.OceanTypeIds = new() { "absent-ocean-outlet" }; inlandProfile.Hydrology.RequireOceanExit = false;
        inlandProfile.Hydrology.LakeSinks.Add(new() { Col = 120, Row = 100 });
        inland.Tiles.Add("unlisted-lake", new() { ID = "unlisted-lake", WaterKind = "lake", FillWeight = 1e9 });
        var lakeWorld = WorldMapGenerator.Generate(inland, inlandProfile, 1);
        Check(lakeWorld.At(120, 100).Water.IsSink && lakeWorld.At(120, 100).Water.Active
            && inland.Tiles[lakeWorld.At(120, 100).TileDefineId].WaterKind == "lake", "Explicit inland lake is a valid realized terminal.");
        Check(lakeWorld.At(120, 100).TileDefineId != "unlisted-lake", "Profile lake carrier whitelist is enforced.");

        var adjustment = MapCatalog.Load(baseDir); var adjustmentProfile = Profile(adjustment); adjustmentProfile.WholeMapAttempts = 1;
        var adjustmentRegion = adjustment.Regions.Values.First(x => x.GenerationPhase=="ordinary" && x.MinCount > 0);
        var adjustmentCell = adjustmentRegion.TerrainLayout.Keys.First();
        adjustmentRegion.EnvironmentAdjustments.Add(new() { Dq = adjustmentCell.dq, Dr = adjustmentCell.dr, Elevation = .2 });
        Reject(() => WorldMapGenerator.Generate(adjustment, adjustmentProfile, 1), "required-regions", "Over-budget environment adjustment rejects whole template.");

        var singleton = MapCatalog.Load(baseDir); var singletonProfile = Profile(singleton); singletonProfile.WholeMapAttempts = 1;
        var fixedDef = singleton.Regions.Values.First(r => r.GenerationPhase=="ordinary" && r.MinCount > 0 && r.LandMarkLayout.Count > 0);
        string landmarkId = fixedDef.LandMarkLayout.Values.First(); singleton.LandMarks[landmarkId].IsSingleton = true;
        Reject(() => WorldMapGenerator.Generate(singleton, singletonProfile, 1, new HashSet<string> { landmarkId }), "required-regions", "Reserved global singleton rejects complete required puzzle.");

        var soft = MapCatalog.Load(baseDir); var softProfile = Profile(soft);
        softProfile.OptionalRegionAttempts = 0; softProfile.RegionCoverageTarget = .99;
        var sparse = WorldMapGenerator.Generate(soft, softProfile, 1);
        Check(sparse.Cells.All(x => x.TileDefineId != null) && sparse.Diagnostics.Warnings.Any(x => x.Contains("soft target")), "Soft coverage shortfall preserves complete legal map and warning.");

        // Movement contracts are profile-specific. A fly-capable group still cannot cross the absolute west wall.
        profile.ConnectivityGroups.Add(new() { ID = "west-wall-test", MovementMode = "fly", CanCrossRivers = true,
            FixedPoints = new() { new() { Col = 0, Row = 100 }, new() { Col = 100, Row = 100 } } });
        Reject(() => WorldMapGenerator.ValidatePlan(catalog, profile, a), "connectivity", "Fly cannot enter absolute west wall.");
        profile.ConnectivityGroups.RemoveAt(profile.ConnectivityGroups.Count - 1);
        var riverPoint = a.Cells.First(x => x.Source == TileGenerationSource.Fill && x.Water.Active && !x.Water.IsSink);
        profile.ConnectivityGroups.Add(new() { ID = "river-walk-test", MovementMode = "walk", CanCrossRivers = false,
            FixedPoints = new() { new() { Col = riverPoint.Col, Row = riverPoint.Row } } });
        Reject(() => WorldMapGenerator.ValidatePlan(catalog, profile, a), "connectivity", "River crossing requires explicit capability.");
        profile.ConnectivityGroups.RemoveAt(profile.ConnectivityGroups.Count - 1);

        Console.WriteLine($"MAP_GENERATOR_TESTS_OK assertions={_checks} seed1Hash={hashA} regions={a.Regions.Count} boundary={a.Diagnostics.BoundaryCount} water={a.Diagnostics.RiverCellCount}");
    }

    private static void FixedPlacements(string baseDir)
    {
        var catalog = MapCatalog.Load(baseDir);
        // Two identical 5x5 axial puzzles at odd/even Offset anchors. Normal eligibility is deliberately impossible.
        var fixedPuzzle = new RegionDefine { ID = "fixed-test", Width = 5, Height = 5, MaxCount = 0, MinSpacing = 999,
            SpawnWeight = 0, ForcedPositions = new() { new() { Col = 30, Row = 30 }, new() { Col = 60, Row = 31 } },
            FixedEnvironment = new() { Elevation = .1, Moisture = .1, Temperature = .1 },
            AnchorEnvironment = new() { Elevation = new() { Min = 1, Max = 1 } } };
        for (int row = 0; row < 5; row++) for (int col = 0; col < 5; col++)
        { var relative = Plane.OffsetToAxial(col, row); fixedPuzzle.TerrainLayout[(relative.Q, relative.R)] = "volcano"; }
        catalog.Regions.Add(fixedPuzzle.ID, fixedPuzzle);
        catalog.LandMarks.Add("fixed-nature", new() { ID = "fixed-nature", SpawnWeight = 0, IsSingleton = true,
            PlacementTypeIds = new() { "ocean" }, ForcedPositions = new() { new() { Col = 31, Row = 30 } } });
        catalog.LandMarks.Add("fixed-artificial", new() { ID = "fixed-artificial", IsNatural = false, SpawnWeight = 0,
            PlacementTypeIds = new() { "ocean" }, ForcedPositions = new() { new() { Col = 90, Row = 80 } } });
        var options = new WorldGenerationOptions { WholeMapAttempts = 1, RegionCoverageTarget = 0, OptionalRegionAttempts = 0,
            LandmarkDensity = 0, Hydrology = new() { RiverCount = 0 } };
        var world = WorldMapGenerator.Generate(catalog, options, 41);
        Check(world.Regions.Count(r => r.DefineId == fixedPuzzle.ID) == 2, "Fixed positions bypass max count, spacing, weights and environment.");
        foreach (var point in fixedPuzzle.ForcedPositions)
        foreach (var offset in fixedPuzzle.TerrainLayout.Keys)
        {
            var at = Plane.OffsetToAxial(point.Col, point.Row) + new TileId(offset.dq, offset.dr);
            var (col, row) = Plane.AxialToOffset(at); var cell = world.At(col, row);
            Check(cell.IsFixed && cell.Source == TileGenerationSource.Region && cell.TileDefineId == "volcano"
                && cell.Environment.Elevation == .1, "Small fixed template copied whole at each odd/even anchor.");
        }
        Check(world.At(31, 30).HasForcedLandmark && world.At(31, 30).LandMarkDefineId == "fixed-nature"
            && world.At(90, 80).HasForcedLandmark && world.At(90, 80).LandMarkDefineId == "fixed-artificial",
            "Absolute landmark reservations ignore scatter filters and coexist with fixed regions or fill.");
        var second = WorldMapGenerator.Generate(catalog, options, 42);
        Check(WorldMapExport.BoundaryHash(world) == WorldMapExport.BoundaryHash(second), "Fixed hash excludes random tile type beneath standalone fixed landmarks.");
        Check(world.ConfigHash == catalog.ComputeConfigHash(options) && world.ConfigHash != catalog.ConfigHash,
            "Effective caller options have their own configuration digest.");
        var changed = world.At(30, 30); changed.TileDefineId = "plain";
        Reject(() => WorldMapGenerator.ValidatePlan(catalog, options, world), "audit", "Fixed puzzle cannot be changed after placement.");
        changed.TileDefineId = "volcano";
        fixedPuzzle.ForcedPositions.Add(new() { Col = 30, Row = 32 });
        Reject(() => WorldMapGenerator.Generate(catalog, options, 41), "fixed-regions", "Overlapping fixed copies fail instead of overriding.");
        fixedPuzzle.ForcedPositions.RemoveAt(2);
        fixedPuzzle.ForcedPositions[1].Col = 199;
        Reject(() => WorldMapGenerator.Generate(catalog, options, 41), "fixed-regions", "Out of bounds fixed puzzle is never cropped.");
        fixedPuzzle.ForcedPositions[1].Col = 60;
        Reject(() => WorldMapGenerator.Generate(catalog, options, 41, new HashSet<string> { "fixed-nature" }), "fixed-landmarks", "Fixed singleton respects existing instances.");
        catalog.LandMarks["fixed-artificial"].ForcedPositions[0] = new() { Col = 31, Row = 30 };
        Reject(() => WorldMapGenerator.Generate(catalog, options, 41), "fixed-landmarks", "Two forced landmarks cannot occupy one tile.");
    }
}
