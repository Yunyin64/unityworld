using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>导出同一核心生成结果；结构哈希排除运行时实体 ID 和计时信息。</summary>
public static class WorldMapExport
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>规范化地图结构摘要；环境以百万分之一精度表达。</summary>
    public static string StructureHash(GeneratedWorld world)
        => Hash(JsonSerializer.Serialize(new
        {
            world.GeneratorVersion, world.Width, world.Height,
            cells = world.Cells.OrderBy(c => c.Row).ThenBy(c => c.Col).Select(c => new
            {
                c.Col, c.Row, c.TileDefineId, source = c.Source.ToString(), c.RegionIndex, c.IsFixed, c.HasForcedLandmark,
                c.LandMarkDefineId, elevation = Math.Round(c.Environment.Elevation, 6),
                moisture = Math.Round(c.Environment.Moisture, 6), temperature = Math.Round(c.Environment.Temperature, 6),
                c.Water.IncomingMask, c.Water.OutDirection, c.Water.IsSource, c.Water.IsSink,
                c.BlockAllMovement, movement = c.AllowedMovement?.OrderBy(x => x, StringComparer.Ordinal).ToArray()
            }),
            regions = world.Regions.OrderBy(r => r.Index).Select(r => new
            {
                r.Index, r.DefineId, r.IsFixed, q = r.Anchor.Q, r = r.Anchor.R,
                cells = r.CellIndices.OrderBy(x => x).ToArray()
            })
        }, Json));

    /// <summary>固定边界摘要不包含种子或可随机的腹地内容。</summary>
    public static string BoundaryHash(GeneratedWorld world)
        => Hash(JsonSerializer.Serialize(world.Cells.Where(c => c.IsFixed || c.HasForcedLandmark)
            .OrderBy(c => c.Row).ThenBy(c => c.Col).Select(c => new
            {
                c.Col, c.Row, typeId = c.IsFixed ? c.TileDefineId : null, c.LandMarkDefineId,
                blocked = c.IsFixed && c.BlockAllMovement,
                movement = c.IsFixed ? c.AllowedMovement?.OrderBy(x => x, StringComparer.Ordinal).ToArray() : null
            }), Json));

    /// <summary>每次写入独立目录；最后写成功清单，防止失败被旧文件冒充。</summary>
    public static string Export(GeneratedWorld world, MapCatalog catalog, string outputRoot)
    {
        string directory = RunDirectory(outputRoot, world.Seed);
        string structureHash = StructureHash(world), boundaryHash = BoundaryHash(world);
        var data = new
        {
            version = world.GeneratorVersion, profile = world.ProfileId, world.Seed, world.Width, world.Height,
            configHash = world.ConfigHash ?? catalog.ConfigHash, structureHash, boundaryHash,
            coverage = world.Diagnostics.RegionCoverage, boundaryCount = world.Diagnostics.BoundaryCount,
            riverCount = world.Diagnostics.RiverCellCount, elapsedMs = world.Diagnostics.StageMilliseconds.Values.Sum(),
            warnings = world.Diagnostics.Warnings,
            types = catalog.Tiles.Values.OrderBy(t => t.ID, StringComparer.Ordinal).Select(t => new
            { id = t.ID, name = t.DisplayName, color = t.Color, tags = t.Tags, movement = t.MovementModes, t.Description }),
            regions = world.Regions.OrderBy(r => r.Index).Select(r => new
            {
                r.Index, r.DefineId, r.IsFixed, name = catalog.Regions[r.DefineId].DisplayName,
                q = r.Anchor.Q, r = r.Anchor.R, cellCount = r.CellIndices.Count, score = r.AdaptationScore
            }),
            cells = world.Cells.OrderBy(c => c.Row).ThenBy(c => c.Col).Select(c => new
            {
                c.Col, c.Row, q = c.Id.Q, r = c.Id.R, typeId = c.TileDefineId, source = c.Source.ToString(),
                c.RegionIndex, c.IsFixed, c.HasForcedLandmark, landmarkId = c.LandMarkDefineId,
                landmark = c.LandMarkDefineId == null ? null : catalog.LandMarks[c.LandMarkDefineId].DisplayName,
                elevation = Math.Round(c.Environment.Elevation, 6), moisture = Math.Round(c.Environment.Moisture, 6),
                temperature = Math.Round(c.Environment.Temperature, 6), c.Water.IncomingMask, c.Water.OutDirection,
                c.Water.IsSource, c.Water.IsSink, blockAll = c.BlockAllMovement, movement = c.AllowedMovement,
                candidateCount = c.FillCandidateCount, c.SelectedWeight, c.FillCandidates
            })
        };
        string json = JsonSerializer.Serialize(data, Json);
        File.WriteAllText(Path.Combine(directory, "map.json"), json, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "diagnostics.json"), JsonSerializer.Serialize(world.Diagnostics, Json), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "map.html"), WorldMapPreview.Render(json), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(new
        {
            status = "success", world.Seed, world.ProfileId, world.GeneratorVersion, ConfigHash = world.ConfigHash ?? catalog.ConfigHash,
            world.Width, world.Height, tileCount = world.Cells.Length, regionCount = world.Regions.Count,
            structureHash, boundaryHash, world.Diagnostics.RegionCoverage,
            stageMilliseconds = world.Diagnostics.StageMilliseconds,
            world.Diagnostics.Attempt, world.Diagnostics.RequiredAttempts, world.Diagnostics.OptionalAttempts,
            world.Diagnostics.RequiredBacktracks, world.Diagnostics.FillBacktracks,
            files = new[] { "map.json", "diagnostics.json", "map.html" }
        }, Json), new UTF8Encoding(false));
        return directory;
    }

    /// <summary>失败拥有独立报告和非成功状态，绝不写成功清单。</summary>
    public static string ExportFailure(Exception error, int seed, string profile, string outputRoot, string configHash = null)
    {
        string directory = RunDirectory(outputRoot, seed);
        var generation = error as MapGenerationException;
        File.WriteAllText(Path.Combine(directory, "failure.json"), JsonSerializer.Serialize(new
        {
            status = "failed", seed, profile, configHash,
            stage = generation?.Stage ?? "configuration-or-export", reasonCode = error.GetType().Name,
            message = error.Message, diagnostics = generation?.Diagnostics
        }, Json), new UTF8Encoding(false));
        return directory;
    }

    private static string RunDirectory(string root, int seed)
    {
        string directory = Path.Combine(Path.GetFullPath(root), $"seed-{seed}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
