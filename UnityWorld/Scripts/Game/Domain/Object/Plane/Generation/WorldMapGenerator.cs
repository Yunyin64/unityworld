using System.Diagnostics;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>Generates an isolated, validated map plan without touching managers, IDs or world time.</summary>
public static partial class WorldMapGenerator
{
    /// <summary>Builds the complete isolated continent; exhaustion throws a structured failure.</summary>
    public static GeneratedWorld Generate(MapCatalog catalog, WorldGenerationOptions profile, int seed,
        IReadOnlySet<string> reservedSingletons = null)
    {
        if (catalog == null || profile == null) throw new ArgumentNullException("catalog/profile");
        try { catalog.Validate(profile); }
        catch (InvalidDataException error) { throw new MapGenerationException("configuration", error.Message, new()); }
        var failed = new List<string>();
        MapGenerationException last = null;
        for (int attempt = 0; attempt < profile.WholeMapAttempts; attempt++)
        {
            var c = new Context(catalog, profile, seed, attempt, reservedSingletons);
            try
            {
                c.Stage("fixed-regions", () => InstallFixedRegions(c));
                c.Stage("fixed-landmarks", () => InstallFixedLandmarks(c));
                c.Stage("environment", () => GenerateEnvironment(c));
                c.Stage("hydrology", () => PlanHydrology(c));
                c.Stage("required-regions", () => PlaceRequiredRegions(c));
                c.Stage("optional-regions", () => PlaceOptionalRegions(c));
                c.Stage("fill", () => Fill(c));
                c.Stage("water-repair", () => RepairWater(c));
                c.Stage("landmarks", () => ScatterLandmarks(c));
                c.Stage("audit", () => Audit(c));
                c.World.Diagnostics.Warnings.InsertRange(0, failed);
                return c.World;
            }
            catch (MapGenerationException ex)
            {
                last = ex;
                failed.Add($"Attempt {attempt + 1} failed at {ex.Stage}: {ex.Message}");
            }
        }
        last ??= new MapGenerationException("configuration", "WholeMapAttempts must be positive.", new());
        last.Diagnostics.Warnings.InsertRange(0, failed);
        throw last;
    }

    private sealed class Context
    {
        public readonly MapCatalog Catalog;
        public readonly WorldGenerationOptions Profile;
        public readonly GeneratedWorld World;
        public readonly int[][] Neighbors;
        public readonly Dictionary<string, RegionDefine> Regions;
        public readonly HashSet<string> Singletons;
        public readonly MapRandom RegionRandom, FillRandom, WaterRandom, LandmarkRandom;
        public readonly int Attempt;
        public readonly int[] DrainOrder;
        public readonly Dictionary<string, List<int>> AnchorPools = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<GeneratedRegion>> PlacedByType = new(StringComparer.Ordinal);
        public readonly TileDefine[] FillTypes;
        public int RegionCells;
        public Context(MapCatalog catalog, WorldGenerationOptions profile, int seed, int attempt, IReadOnlySet<string> singleton)
        {
            Catalog = catalog; Profile = profile; Attempt = attempt;
            World = new() { GeneratorVersion = profile.GeneratorVersion, ProfileId = profile.ID, ConfigHash = catalog.ComputeConfigHash(profile), Seed = seed,
                Width = profile.Width, Height = profile.Height, Cells = new GeneratedCell[profile.Width * profile.Height] };
            World.Diagnostics.Attempt = attempt + 1;
            Neighbors = new int[World.Cells.Length][];
            DrainOrder = Enumerable.Repeat(-1, World.Cells.Length).ToArray();
            for (int i = 0; i < World.Cells.Length; i++)
            {
                World.Cells[i] = new() { Col = i % profile.Width, Row = i / profile.Width };
                Neighbors[i] = Enumerable.Range(0, 6).Select(d => Index(World.Cells[i].Id.Neighbor(d))).ToArray();
            }
            Regions = profile.RegionIds.Concat(catalog.Regions.Values.Where(d => d.ForcedPositions.Count > 0).Select(d => d.ID)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToDictionary(x => x, x => catalog.Regions[x], StringComparer.Ordinal);
            foreach (string id in Regions.Keys) PlacedByType[id] = new();
            FillTypes = catalog.Tiles.Values.Where(x => x.AllowFill).OrderBy(x => x.ID, StringComparer.Ordinal).ToArray();
            Singletons = singleton == null ? new(StringComparer.Ordinal) : new(singleton, StringComparer.Ordinal);
            RegionRandom = Random("regions"); FillRandom = Random("fill"); WaterRandom = Random("water"); LandmarkRandom = Random("landmarks");
        }
        public MapRandom Random(string phase) => new(World.Seed, Profile.GeneratorVersion, phase, Attempt);
        public int Index(TileId id)
        {
            var (col, row) = Plane.AxialToOffset(id);
            return col < 0 || col >= Profile.Width || row < 0 || row >= Profile.Height ? -1 : row * Profile.Width + col;
        }
        public void Stage(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            try { action(); }
            finally { World.Diagnostics.StageMilliseconds[name] = sw.Elapsed.TotalMilliseconds; }
        }
        public void Fail(string stage, string reason) => throw new MapGenerationException(stage, reason, World.Diagnostics);
    }

    private static bool EnvironmentMatches(EnvironmentRule rule, MapEnvironment e) => rule.Matches(e.Elevation, e.Moisture, e.Temperature);
    private static double EnvironmentScore(EnvironmentRule rule, MapEnvironment e) => rule.Score(e.Elevation, e.Moisture, e.Temperature);
    private static bool ValidEnvironment(MapEnvironment e) => double.IsFinite(e.Elevation) && double.IsFinite(e.Moisture)
        && double.IsFinite(e.Temperature) && e.Elevation >= 0 && e.Elevation <= 1 && e.Moisture >= 0 && e.Moisture <= 1 && e.Temperature >= 0 && e.Temperature <= 1;
    private static bool AdjacentAllowed(TileDefine a, TileDefine b)
        => (a.AllowedNeighborIds.Count == 0 || a.AllowedNeighborIds.Contains(b.ID))
        && !a.ForbiddenNeighborIds.Contains(b.ID)
        && (b.AllowedNeighborIds.Count == 0 || b.AllowedNeighborIds.Contains(a.ID))
        && !b.ForbiddenNeighborIds.Contains(a.ID);

    private static bool WaterCompatible(Context c, int i, TileDefine type)
    {
        var w = c.World.Cells[i].Water;
        if (!w.Active) return type.WaterKind == "none";
        if (w.IsSink) return type.WaterKind == "ocean" && c.Profile.Hydrology.OceanTypeIds.Contains(type.ID)
            || type.WaterKind == "lake" && c.Profile.Hydrology.LakeTypeIds.Contains(type.ID);
        return type.WaterKind == "river" && c.Profile.Hydrology.RiverTypeIds.Contains(type.ID);
    }

    private static int WeightedIndex<T>(IReadOnlyList<T> values, Func<T, double> weight, MapRandom random)
    {
        double sum = 0;
        foreach (var v in values) sum += weight(v);
        if (!(sum > 0) || !double.IsFinite(sum)) return -1;
        double pick = random.NextDouble() * sum;
        for (int i = 0; i < values.Count; i++) { pick -= weight(values[i]); if (pick <= 0) return i; }
        return values.Count - 1;
    }
}
