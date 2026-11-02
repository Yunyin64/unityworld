using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Tests;

/// <summary>导出、特殊文本、失败隔离和稳定摘要的真实生成验证。</summary>
public static class MapDiagnosticsTests
{
    /// <summary>运行结果放在独立临时目录，失败也保留证据。</summary>
    public static void Run(string baseDir)
    {
        int assertions = 0;
        void Check(bool ok, string message)
        {
            assertions++;
            if (!ok) throw new InvalidOperationException("地图诊断断言失败：" + message);
        }
        var catalog = MapCatalog.Load(baseDir);
        var profile = catalog.Profiles["china-inspired-v1"];
        var world = WorldMapGenerator.Generate(catalog, profile, 12345);
        string hash = WorldMapExport.StructureHash(world);
        var repeated = WorldMapGenerator.Generate(catalog, profile, 12345);
        Check(hash == WorldMapExport.StructureHash(repeated), "同输入结构一致");
        repeated.Diagnostics.StageMilliseconds["synthetic-timing"] = 100000;
        Check(hash == WorldMapExport.StructureHash(repeated), "摘要不受耗时影响");
        repeated.Cells[10000].TileDefineId = repeated.Cells[10000].TileDefineId == "plain" ? "mountain" : "plain";
        Check(hash != WorldMapExport.StructureHash(repeated), "类型变化进入结构摘要");
        string root = Path.Combine(Path.GetTempPath(), "UnityWorld-MapDiagnostics-" + Guid.NewGuid().ToString("N"));
        string firstType = world.Cells[0].TileDefineId;
        string originalName = catalog.Tiles[firstType].DisplayName;
        catalog.Tiles[firstType].DisplayName = "</script><img src=x onerror=alert(1)> 测试";
        string directory;
        try { directory = WorldMapExport.Export(world, catalog, root); }
        finally { catalog.Tiles[firstType].DisplayName = originalName; }
        using var map = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "map.json")));
        Check(map.RootElement.GetProperty("cells").GetArrayLength() == 40000, "导出40000格");
        Check(map.RootElement.GetProperty("structureHash").GetString() == hash, "导出结构摘要");
        string html = File.ReadAllText(Path.Combine(directory, "map.html"));
        Check(!html.Contains("</script><img"), "脚本终止标签安全转义");
        Check(html.Contains("application/json") && !html.Contains("__MAP_DATA__"), "自包含数据已嵌入");
        Check(File.Exists(Path.Combine(directory, "manifest.json")) && File.Exists(Path.Combine(directory, "diagnostics.json")), "成功产物完整");
        string failed = WorldMapExport.ExportFailure(new MapGenerationException("fixture", "测试失败", new()), 12345, profile.ID, root, catalog.ConfigHash);
        Check(failed != directory, "失败独立目录");
        Check(File.Exists(Path.Combine(failed, "failure.json")) && !File.Exists(Path.Combine(failed, "manifest.json")), "失败不写成功清单");
        Check(File.Exists(Path.Combine(directory, "manifest.json")), "失败不影响旧成功记录");
        using var failure = JsonDocument.Parse(File.ReadAllText(Path.Combine(failed, "failure.json")));
        Check(failure.RootElement.GetProperty("stage").GetString() == "fixture", "失败阶段保留");
        Console.WriteLine($"MAP_DIAGNOSTICS_TESTS_OK assertions={assertions} evidence={root}");
    }
}
