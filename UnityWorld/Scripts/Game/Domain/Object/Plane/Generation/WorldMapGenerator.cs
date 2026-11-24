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
                c.Stage("sea", () => ClassifySea(c));
                c.Stage("lakes", () => PlaceLakes(c));
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
        /// <summary>用于生成的地图定义目录</summary>
        public readonly MapCatalog Catalog;
        /// <summary>本轮有效的生成参数</summary>
        public readonly WorldGenerationOptions Profile;
        /// <summary>当前尝试的地图草稿</summary>
        public readonly GeneratedWorld World;
        /// <summary>按格和方向索引的邻格</summary>
        public readonly int[][] Neighbors;
        /// <summary>按编号索引的启用地区模板</summary>
        public readonly Dictionary<string, RegionDefine> Regions;
        /// <summary>已经预留的单例地标定义</summary>
        public readonly HashSet<string> Singletons;
        /// <summary>地区、填充、水系、地标随机源</summary>
        public readonly MapRandom RegionRandom, FillRandom, WaterRandom, LandmarkRandom;
        /// <summary>本次尝试序号</summary>
        public readonly int Attempt;
        /// <summary>按格子索引记录的排水顺位</summary>
        public readonly int[] DrainOrder;
        /// <summary>按地区模板缓存的候选锚点</summary>
        public readonly Dictionary<string, List<int>> AnchorPools = new(StringComparer.Ordinal);
        /// <summary>按模板索引的已放置地区</summary>
        public readonly Dictionary<string, List<GeneratedRegion>> PlacedByType = new(StringComparer.Ordinal);
        /// <summary>可用的填充地形定义</summary>
        public readonly TileDefine[] FillTypes;
        /// <summary>计入覆盖率的地区格数</summary>
        public int RegionCells;
        /// <summary>各格受地区间距约束的计数</summary>
        public readonly int[] RegionBuffer;
        /// <summary>建立单次地图生成的草稿、邻格和随机流</summary>
        public Context(MapCatalog catalog, WorldGenerationOptions profile, int seed, int attempt, IReadOnlySet<string> singleton)
        {
            Catalog = catalog; Profile = profile; Attempt = attempt;
            World = new() { GeneratorVersion = profile.GeneratorVersion, ProfileId = profile.ID, ConfigHash = catalog.ComputeConfigHash(profile), Seed = seed,
                Parameters=System.Text.Json.JsonSerializer.Deserialize<WorldGenerationOptions>(System.Text.Json.JsonSerializer.Serialize(profile)),
                Width = profile.Width, Height = profile.Height, Cells = new GeneratedCell[profile.Width * profile.Height] };
            World.Diagnostics.Attempt = attempt + 1;
            Neighbors = new int[World.Cells.Length][];
            RegionBuffer = new int[World.Cells.Length];
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
        /// <summary>为指定生成阶段派生独立随机流</summary>
        public MapRandom Random(string phase) => new(World.Seed, Profile.GeneratorVersion, phase, Attempt);
        /// <summary>将有效轴坐标转为行序索引，越界返回负值</summary>
        public int Index(TileId id)
        {
            var (col, row) = Plane.AxialToOffset(id);
            return col < 0 || col >= Profile.Width || row < 0 || row >= Profile.Height ? -1 : row * Profile.Width + col;
        }
        /// <summary>执行指定阶段并记录其耗时</summary>
        public void Stage(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            try { action(); }
            finally { World.Diagnostics.StageMilliseconds[name] = sw.Elapsed.TotalMilliseconds; }
        }
        /// <summary>抛出带失败阶段和当前诊断的地图生成异常</summary>
        public void Fail(string stage, string reason) => throw new MapGenerationException(stage, reason, World.Diagnostics);
    }

    /// <summary>检查环境的海拔、湿度和温度是否符合硬条件</summary>
    private static bool EnvironmentMatches(EnvironmentRule rule, MapEnvironment e) => rule.Matches(e.Elevation, e.Moisture, e.Temperature);
    /// <summary>按海拔、湿度和温度计算环境适应权重</summary>
    private static double EnvironmentScore(EnvironmentRule rule, MapEnvironment e) => rule.Score(e.Elevation, e.Moisture, e.Temperature);
    /// <summary>检查三项环境数值是否有限且都在零到一之间</summary>
    private static bool ValidEnvironment(MapEnvironment e) => double.IsFinite(e.Elevation) && double.IsFinite(e.Moisture)
        && double.IsFinite(e.Temperature) && e.Elevation >= 0 && e.Elevation <= 1 && e.Moisture >= 0 && e.Moisture <= 1 && e.Temperature >= 0 && e.Temperature <= 1;
    /// <summary>判断两种相邻地形是否满足双方的允许和禁止规则</summary>
    private static bool AdjacentAllowed(TileDefine a, TileDefine b)
        => (a.AllowedNeighborIds.Count == 0 || a.AllowedNeighborIds.Contains(b.ID))
        && !a.ForbiddenNeighborIds.Contains(b.ID)
        && (b.AllowedNeighborIds.Count == 0 || b.AllowedNeighborIds.Contains(a.ID))
        && !b.ForbiddenNeighborIds.Contains(a.ID);

    /// <summary>判定候选类型是否匹配该格规划的水域与河宽</summary>
    private static bool WaterCompatible(Context c, int i, TileDefine type)
    {
        var w = c.World.Cells[i].Water;
        string kind=w.Kind;
        // Detached legacy diagnostic fixtures can still describe directed water without a footprint.
        if(kind=="none" && w.Active) kind=w.IsSink ? (type.WaterKind=="ocean"?"ocean":"lake") : "river";
        return kind switch
        {
            "none" => type.WaterKind=="none",
            "ocean" => type.WaterKind=="ocean" && c.Profile.Hydrology.OceanTypeIds.Contains(type.ID),
            "lake" => type.WaterKind=="lake" && c.Profile.Hydrology.LakeTypeIds.Contains(type.ID),
            "river" => type.WaterKind=="river" && c.Profile.Hydrology.RiverTypeIds.Contains(type.ID),
            _ => false
        };
    }

    /// <summary>按有效权重随机选中一个列表下标</summary>
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
