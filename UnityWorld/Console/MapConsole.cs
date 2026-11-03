using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.ConsoleHost;

/// <summary>地图诊断命令。参数解析发生在任何世界初始化之前。</summary>
public static class MapConsole
{
    /// <summary>成功返回 0，配置/生成/测试失败返回 1 并写独立失败报告。</summary>
    public static int Run(string[] args)
    {
        int seed = 12345;
        string profile = "china-inspired-v1", configHash = null;
        string output = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "docs", "world-map-preview"));
        try
        {
            var flags = new HashSet<string>(StringComparer.Ordinal);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] modes = ["--map-generate", "--map-generation-tests", "--map-data-tests", "--map-generator-tests", "--map-runtime-tests", "--map-batch", "--map-runtime-hash", "--map-hydrology-tests"];
            string[] options = ["--seed", "--profile", "--output", "--data-root", "--count"];
            for (int i = 0; i < args.Length; i++)
            {
                if (modes.Contains(args[i]))
                {
                    if (!flags.Add(args[i])) throw new ArgumentException("重复模式：" + args[i]);
                }
                else if (options.Contains(args[i]))
                {
                    string key = args[i];
                    if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("参数缺少值：" + key);
                    if (!values.TryAdd(key, args[i])) throw new ArgumentException("重复参数：" + key);
                }
                else throw new ArgumentException("未知地图参数：" + args[i]);
            }
            if (flags.Count != 1) throw new ArgumentException("一次只能选择一种地图命令。");
            if (values.TryGetValue("--seed", out var seedText) && !int.TryParse(seedText, out seed)) throw new ArgumentException("seed 必须是整数。");
            if (values.TryGetValue("--profile", out var profileText)) profile = profileText;
            if (values.TryGetValue("--output", out var outputText)) output = Path.GetFullPath(outputText);
            string baseDir = values.TryGetValue("--data-root", out var root) ? Path.GetFullPath(root) : FindDataRoot();
            if (flags.Contains("--map-data-tests") || flags.Contains("--map-generation-tests")) UnityWorld.Tests.MapDataTests.Run(baseDir);
            if (flags.Contains("--map-generator-tests") || flags.Contains("--map-generation-tests")) UnityWorld.Tests.MapGeneratorTests.Run(baseDir);
            if (flags.Contains("--map-runtime-tests") || flags.Contains("--map-generation-tests")) UnityWorld.Tests.MapRuntimeTests.Run(baseDir);
            if (flags.Contains("--map-generation-tests") || flags.Contains("--map-hydrology-tests")) UnityWorld.Tests.MapHydrologyTests.Run(baseDir);
            if (flags.Contains("--map-generation-tests")) UnityWorld.Tests.MapDiagnosticsTests.Run(baseDir);
            if (flags.Any(x => x.EndsWith("tests", StringComparison.Ordinal)))
            {
                if (flags.Contains("--map-generation-tests")) Console.WriteLine("MAP_GENERATION_TESTS_OK");
                return 0;
            }
            var catalog = MapCatalog.Load(baseDir);
            configHash = catalog.ConfigHash;
            if (!catalog.Profiles.TryGetValue(profile, out var definition)) throw new ArgumentException("不存在的地图配置：" + profile);
            if (flags.Contains("--map-runtime-hash"))
            {
                try
                {
                    WorldMgr.Initialize(seed, baseDir, profile);
                    var world = PlaneMgr.Instance.MainPlane.GenerationResult;
                    Console.WriteLine($"MAP_RUNTIME_OK seed={seed} hash={WorldMapExport.StructureHash(world)} tiles={world.Cells.Length} ticks={WorldTime.CurrentTick}");
                }
                finally { WorldMgr.End(); }
                return 0;
            }
            if (flags.Contains("--map-batch"))
            {
                int count = 20;
                if (values.TryGetValue("--count", out var countText) && (!int.TryParse(countText, out count) || count < 1 || count > 1000)) throw new ArgumentException("count 必须在 1..1000。");
                var reports = new List<object>();
                string boundary = null;
                for (int n = 0; n < count; n++)
                {
                    var result = WorldMapGenerator.Generate(catalog, definition, checked(seed + n));
                    string current = WorldMapExport.BoundaryHash(result);
                    boundary ??= current;
                    if (boundary != current) throw new InvalidOperationException("同模板不同种子的固定边界发生变化。");
                    string hash = WorldMapExport.StructureHash(result);
                    reports.Add(new { seed = seed + n, hash, boundaryHash = current, tiles = result.Cells.Length, regions = result.Regions.Count,
                        result.Diagnostics.RegionCoverage, result.Diagnostics.RiverCellCount, result.Diagnostics.StageMilliseconds,
                        result.Diagnostics.Metrics, lakes=result.Lakes.Select(l=>new{l.Id,l.Closed,area=l.CellIndices.Count,l.SurfaceLevel,l.Runoff}),
                        result.Diagnostics.Attempt, result.Diagnostics.RequiredBacktracks, result.Diagnostics.FillBacktracks, result.Diagnostics.Warnings });
                    Console.WriteLine($"MAP_BATCH_SEED_OK seed={seed + n} tiles={result.Cells.Length} regions={result.Regions.Count} coverage={result.Diagnostics.RegionCoverage:P1} hash={hash}");
                }
                Directory.CreateDirectory(output);
                string path = Path.Combine(output, $"batch-{seed}-{count}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
                File.WriteAllText(path, JsonSerializer.Serialize(new { status = "success", profile, configHash, reports }, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("MAP_BATCH_OK " + path);
                return 0;
            }
            var generated = WorldMapGenerator.Generate(catalog, definition, seed);
            string directory = WorldMapExport.Export(generated, catalog, output);
            Console.WriteLine($"MAP_GENERATED_OK seed={seed} tiles={generated.Cells.Length} regions={generated.Regions.Count} hash={WorldMapExport.StructureHash(generated)}");
            Console.WriteLine(Path.Combine(directory, "map.html"));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("MAP_FAILED " + ex.Message);
            try { Console.Error.WriteLine(WorldMapExport.ExportFailure(ex, seed, profile, output, configHash)); }
            catch (Exception reportError) { Console.Error.WriteLine("失败报告无法写入：" + reportError.Message); }
            return 1;
        }
    }

    private static string FindDataRoot()
    {
        foreach (string candidate in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory, Path.Combine(Environment.CurrentDirectory, "UnityWorld") })
            if (Directory.Exists(Path.Combine(candidate, "Data"))) return Path.GetFullPath(candidate);
        throw new DirectoryNotFoundException("找不到 Data，请用 --data-root 指定 UnityWorld 根目录。");
    }
}
