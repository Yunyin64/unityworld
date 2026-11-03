using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>Pure generation plans. Runtime identities are assigned only by the publisher.</summary>
public enum TileGenerationSource { Region = 1, Fill = 2 }

/// <summary>Finite normalized generation conditions, independent of final authored tile types.</summary>
public sealed class MapEnvironment
{
    /// <summary>Normalized elevation in the closed interval [0, 1].</summary>
    public double Elevation { get; set; }
    /// <summary>Normalized temperature in the closed interval [0, 1].</summary>
    public double Temperature { get; set; }
    /// <summary>Normalized moisture in the closed interval [0, 1].</summary>
    public double Moisture { get; set; }
    /// <summary>Returns an independent value copy for adjustment and publication isolation.</summary>
    public MapEnvironment Copy() => new() { Elevation = Elevation, Temperature = Temperature, Moisture = Moisture };
}

/// <summary>Independent water footprint, drainage data and optional directed centerline.</summary>
public sealed class MapWater
{
    /// <summary>Water footprint, independent of directed flow: none/ocean/lake/river.</summary>
    public string Kind { get; set; } = "none";
    public string BodyId { get; set; }
    public double? SurfaceLevel { get; set; }
    public double DrainageElevation { get; set; }
    public int DrainageRank { get; set; } = -1;
    public double LocalRunoff { get; set; }
    public int DrainageTarget { get; set; } = -1;
    public double Runoff { get; set; }
    public int Width { get; set; }
    /// <summary>Row-major owning centerline index; -1 outside river bands.</summary>
    public int CenterlineIndex { get; set; } = -1;
    public bool IsCenterline { get; set; }
    public string Reason { get; set; }
    /// <summary>Bit d denotes inflow from neighbour direction d, using TileId.Directions.</summary>
    public int IncomingMask { get; set; }
    /// <summary>Single downstream direction, or -1 for no downstream.</summary>
    public int OutDirection { get; set; } = -1;
    /// <summary>Explicit river source allowed to have no upstream.</summary>
    public bool IsSource { get; set; }
    /// <summary>Explicit legal ocean or inland-lake outlet.</summary>
    public bool IsSink { get; set; }
    /// <summary>Whether this cell participates in a realized cross-tile watercourse.</summary>
    public bool Active => IncomingMask != 0 || OutDirection >= 0 || IsSource;
    /// <summary>Returns an independent connection copy.</summary>
    public MapWater Copy() => (MapWater)MemberwiseClone();
}

/// <summary>One unpublished tile plan, including provenance and generation decision evidence.</summary>
public sealed class GeneratedCell
{
    /// <summary>来自强制锚点地区的锁定地块；普通拼图、填充和修补不能改写。</summary>
    public bool IsFixed { get; set; }
    /// <summary>地标定义通过绝对位置预留，独立于地块类型的生成来源。</summary>
    public bool HasForcedLandmark { get; set; }
    /// <summary>Zero-based odd-r offset column.</summary>
    public int Col { get; set; }
    /// <summary>Zero-based odd-r offset row.</summary>
    public int Row { get; set; }
    /// <summary>Axial coordinate derived from the offset position.</summary>
    public TileId Id => Plane.OffsetToAxial(Col, Row);
    /// <summary>Stable content type ID; null only while the isolated draft is incomplete.</summary>
    public string TileDefineId { get; set; }
    /// <summary>Whether the cell came from an author puzzle or constrained fill; IsFixed marks forced puzzle copies.</summary>
    public TileGenerationSource Source { get; set; }
    /// <summary>Stable generated region sequence, absent outside a puzzle footprint.</summary>
    public int? RegionIndex { get; set; }
    /// <summary>Optional landmark definition planned at this tile.</summary>
    public string LandMarkDefineId { get; set; }
    /// <summary>Final local environment after explicitly configured puzzle adjustments.</summary>
    public MapEnvironment Environment { get; set; } = new();
    /// <summary>Water footprint, drainage and directed connections.</summary>
    public MapWater Water { get; set; } = new();
    /// <summary>Absolute boundary prohibition, including flight.</summary>
    public bool BlockAllMovement { get; set; }
    /// <summary>Boundary-specific movement abilities; null or empty inherits type abilities.</summary>
    public List<string> AllowedMovement { get; set; }
    /// <summary>Product weight of the selected fill type at the latest decision.</summary>
    public double SelectedWeight { get; set; }
    /// <summary>Number of hard-compatible positive-weight choices at that fill decision.</summary>
    public int FillCandidateCount { get; set; }
    /// <summary>All choices and factorized weights at the latest fill decision.</summary>
    public List<FillCandidateRecord> FillCandidates { get; set; } = new();
}

/// <summary>Compact per-cell decision evidence; hard-rejected types remain in aggregated diagnostics.</summary>
public sealed class FillCandidateRecord
{
    /// <summary>Stable type ID of this candidate.</summary>
    public string TypeId { get; set; }
    /// <summary>Configured basic fill weight.</summary>
    public double BaseWeight { get; set; }
    /// <summary>Soft environmental adaptation multiplier.</summary>
    public double EnvironmentWeight { get; set; }
    /// <summary>Product of preference multipliers for already assigned neighbours.</summary>
    public double AdjacencyWeight { get; set; }
    /// <summary>Base, environment and adjacency weight product.</summary>
    public double FinalWeight { get; set; }
    /// <summary>True when one-step propagation proved this choice would strand a neighbour.</summary>
    public bool RejectedByLookahead { get; set; }
}

/// <summary>A complete fixed-orientation region placement, with runtime identity deliberately absent.</summary>
public sealed class GeneratedRegion
{
    public bool IsFixed { get; set; }
    /// <summary>Zero-based placement sequence used by cell associations and stable hashes.</summary>
    public int Index { get; set; }
    /// <summary>Stable author template ID.</summary>
    public string DefineId { get; set; }
    /// <summary>World axial origin added to each template offset.</summary>
    public TileId Anchor { get; set; }
    /// <summary>Exactly the template footprint as row-major world indices.</summary>
    public List<int> CellIndices { get; set; } = new();
    /// <summary>Footprint indices with author-fixed landmarks.</summary>
    public List<int> LandMarkCellIndices { get; set; } = new();
    /// <summary>Environmental selection score recorded at acceptance.</summary>
    public double AdaptationScore { get; set; }
}

/// <summary>Reversible evidence for an explicit author-configured local environmental change.</summary>
public sealed class EnvironmentAdjustmentRecord
{
    /// <summary>Row-major cell index changed by the puzzle.</summary>
    public int CellIndex { get; set; }
    /// <summary>Responsible author template.</summary>
    public string RegionDefineId { get; set; }
    /// <summary>Independent copy before adjustment.</summary>
    public MapEnvironment Before { get; set; }
    /// <summary>Independent copy after adjustment.</summary>
    public MapEnvironment After { get; set; }
}

/// <summary>Whole lake node; shoreline/islands remain ordinary authored tiles in its region.</summary>
public sealed class GeneratedLake
{
    public string Id { get; set; }
    public int RegionIndex { get; set; }
    public bool Closed { get; set; }
    public double SurfaceLevel { get; set; }
    public List<int> CellIndices { get; set; } = new();
    public int OutletCell { get; set; } = -1;
    public int OutletTarget { get; set; } = -1;
    public List<int> InletCells { get; set; } = new();
    public double LocalRunoff { get; set; }
    public int DrainageTarget { get; set; } = -1;
    public double Runoff { get; set; }
}

/// <summary>Bounded diagnostics; rejected candidates are counted rather than exhaustively logged.</summary>
public sealed class MapGenerationDiagnostics
{
    /// <summary>Rejected-candidate counts grouped by stable reason code.</summary>
    public Dictionary<string, long> Rejections { get; set; } = new(StringComparer.Ordinal);
    /// <summary>At most 32 illustrative rejection messages.</summary>
    public List<string> Samples { get; set; } = new();
    /// <summary>Nonfatal coverage shortfalls and previous whole-map retry summaries.</summary>
    public List<string> Warnings { get; set; } = new();
    /// <summary>Measured phase durations, excluded from structural hashes.</summary>
    public Dictionary<string, double> StageMilliseconds { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Final instance count for every enabled template.</summary>
    public Dictionary<string, int> RegionCounts { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Accepted explicit environment edits only; rolled-back edits are removed.</summary>
    public List<EnvironmentAdjustmentRecord> Adjustments { get; set; } = new();
    /// <summary>Successful local fill or water repairs with before/after evidence.</summary>
    public List<string> Repairs { get; set; } = new();
    /// <summary>One-based whole-map attempt.</summary>
    public int Attempt { get; set; }
    /// <summary>Required placement candidate evaluations consumed.</summary>
    public int RequiredAttempts { get; set; }
    /// <summary>Required placement rollback operations consumed.</summary>
    public int RequiredBacktracks { get; set; }
    /// <summary>Optional placement attempts consumed.</summary>
    public int OptionalAttempts { get; set; }
    /// <summary>Fill decisions undone after a constraint conflict.</summary>
    public int FillBacktracks { get; set; }
    /// <summary>Protected cells in forced region copies; legacy diagnostic field name.</summary>
    public int BoundaryCount { get; set; }
    /// <summary>Actual river footprint cells, including wide side cells; ocean/lake surfaces counted separately.</summary>
    public int RiverCellCount { get; set; }
    /// <summary>Ordinary puzzle land divided by eligible land after fixed, sea, river and whole lake-region exclusion.</summary>
    public double RegionCoverage { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Adds one rejection and, while capacity remains, an illustrative sample.</summary>
    public void Reject(string reason, string sample = null)
    {
        Rejections[reason] = Rejections.GetValueOrDefault(reason) + 1;
        if (sample != null && Samples.Count < 32) Samples.Add(reason + ": " + sample);
    }
}

/// <summary>Validated unpublished world plan produced by the shared map generation service.</summary>
public sealed class GeneratedWorld
{
    /// <summary>Catalog and effective ordinary options, including caller overrides.</summary>
    public string ConfigHash { get; set; }
    /// <summary>Detached effective ordinary generation options for exported reproduction.</summary>
    public WorldGenerationOptions Parameters { get; set; }
    /// <summary>Semantic generation algorithm version included in seed derivation.</summary>
    public string GeneratorVersion { get; set; } = "region-puzzle-v3";
    /// <summary>Stable generation profile ID.</summary>
    public string ProfileId { get; set; }
    /// <summary>User-selected world seed, independent of runtime identities.</summary>
    public int Seed { get; set; }
    /// <summary>Offset-grid width, fixed to 200 for this continent.</summary>
    public int Width { get; set; }
    /// <summary>Offset-grid height, fixed to 200 for this continent.</summary>
    public int Height { get; set; }
    /// <summary>All world cells ordered by row then column.</summary>
    public GeneratedCell[] Cells { get; set; }
    /// <summary>Author puzzles in stable acceptance sequence.</summary>
    public List<GeneratedRegion> Regions { get; set; } = new();
    public List<GeneratedLake> Lakes { get; set; } = new();
    /// <summary>Bounded evidence and phase measurements for this generation.</summary>
    public MapGenerationDiagnostics Diagnostics { get; set; } = new();
    /// <summary>Retrieves a cell by its valid odd-r offset position.</summary>
    public GeneratedCell At(int col, int row) => Cells[row * Width + col];
}

/// <summary>Structured failure of an unpublished candidate, retaining diagnostic evidence.</summary>
public sealed class MapGenerationException : Exception
{
    /// <summary>Stable phase name that rejected the candidate.</summary>
    public string Stage { get; }
    /// <summary>Attempt budgets and rejection evidence at the point of failure.</summary>
    public MapGenerationDiagnostics Diagnostics { get; }
    /// <summary>Creates an explicit generation failure without publishing candidate state.</summary>
    public MapGenerationException(string stage, string message, MapGenerationDiagnostics diagnostics) : base(message)
    { Stage = stage; Diagnostics = diagnostics; }
}

/// <summary>FNV-1a derived independent project Rng streams; never calls entity NewId.</summary>
internal sealed class MapRandom
{
    private readonly UnityWorld.Core.Rng _rng;
    /// <summary>Derives an independent stream from seed, version, phase and attempt.</summary>
    public MapRandom(int seed, string version, string stage, int attempt)
    {
        ulong h = 14695981039346656037UL;
        foreach (byte b in System.Text.Encoding.UTF8.GetBytes($"{seed.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{version}|{stage}|{attempt.ToString(System.Globalization.CultureInfo.InvariantCulture)}"))
        { h ^= b; h *= 1099511628211UL; }
        _rng = new UnityWorld.Core.Rng(unchecked((int)(h ^ (h >> 32))));
    }
    /// <summary>Produces a reproducible 32-bit salt using the project's RNG.</summary>
    public uint NextUInt()
    {
        return ((uint)_rng.Range(0, 65536) << 16) | (uint)_rng.Range(0, 65536);
    }
    /// <summary>Produces a unit selection value using the project's RNG.</summary>
    public double NextDouble() => _rng.Range(0f, 1f);
    /// <summary>Produces an integer in [0, max).</summary>
    public int Next(int max) => _rng.Range(0, max);
}
