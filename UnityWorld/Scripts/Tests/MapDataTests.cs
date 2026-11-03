using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Tests;

/// <summary>Strict map configuration contracts, author footprint semantics and repeatable hashes.</summary>
public static class MapDataTests
{
    private static int _assertions;
    private static void Check(bool condition, string message)
    { _assertions++; if (!condition) throw new InvalidOperationException("Map data assertion failed: " + message); }
    private static void Reject(Action action, string text)
    {
        try { action(); }
        catch (InvalidDataException ex) { Check(ex.Message.Contains(text, StringComparison.OrdinalIgnoreCase), "diagnostic contains " + text + "; got " + ex.Message); return; }
        throw new InvalidOperationException("Expected map-data rejection: " + text);
    }
    public static void Run(string baseDir)
    {
        _assertions = 0;
        var catalog = MapCatalog.Load(baseDir);
        Check(catalog.Tiles.Count >= 20 && catalog.Regions.Count >= 12, "representative authored content");
        foreach (var id in new[] { "mountain", "forest_mountain", "barren_mountain", "volcano", "snow_mountain" })
            Check(catalog.Tiles.ContainsKey(id) && catalog.Tiles[id].Tags.Contains("山类"), "independent mountain type " + id);
        var mountain = catalog.Tiles["mountain"].InitialAura;
        Check(mountain.Jin == 2 && mountain.Mu == .5f && mountain.Shui == .8f && mountain.Huo == .8f && mountain.Tu == 1.2f, "legacy aura preserved");
        Check(TerrainAuraProfile.Get(TerrainType.Lake).Shui==catalog.Tiles["lake"].InitialAura.Shui, "independent lake enum aura projection");
        var legacy = catalog.Regions["region_volcanic_belt"];
        Check(legacy.Width == 8 && legacy.Height == 8 && legacy.TerrainLayout.Count == 5, "legacy rectangle does not fill implicit cells");
        Check(!catalog.Regions["region_qinling"].TerrainLayout.ContainsKey((1,-1)), "authored hole stays empty");
        Check(catalog.LandMarks["natural_volcanic_pillar"].AuraOffset.Total == 0, "old rate not migrated into initial offset");
        Check(catalog.Modifiers["aura_volcanic_heat"].AuraData.Huo == 15, "old modifier rate unchanged");
        var profile = catalog.Profiles["china-inspired-v1"];
        Check(profile.IsDevelopmentSample && profile.Width == 200 && profile.Height == 200, "explicit development profile");
        Check(catalog.Regions.Values.Count(r => r.ForcedPositions.Count > 0) == 6, "six compact fixed templates");
        var originalHash = catalog.ConfigHash;
        var reverse = catalog.Tiles.Reverse().ToArray(); catalog.Tiles.Clear(); foreach (var pair in reverse) catalog.Tiles.Add(pair.Key, pair.Value);
        Check(originalHash == catalog.ConfigHash, "hash ignores definition load order");
        var fixedSea = catalog.Regions["fixed_east_sea"];
        Check(fixedSea.TerrainLayout.Count == 100 && fixedSea.ForcedPositions.Count == 20, "one 10x10 template reused at 20 anchors");
        Check(!File.Exists(Path.Combine(baseDir, "Data", "WorldGenerationDefines.json")), "no separate world-generation Define file");
        catalog.Validate();
        var oldWeight = catalog.Tiles["plain"].FillWeight;
        catalog.Tiles["plain"].FillWeight = double.NaN; Reject(catalog.Validate, "FillWeight");
        catalog.Tiles["plain"].FillWeight = -1; Reject(catalog.Validate, "FillWeight"); catalog.Tiles["plain"].FillWeight = oldWeight;
        var region = catalog.Regions["region_taihang"];
        var oldMax = region.MaxCount; region.MaxCount = 0; Reject(catalog.Validate, "MinCount/MaxCount"); region.MaxCount = oldMax;
        region.LandMarkLayout.Add((199,199), "natural_leyline_eye"); Reject(catalog.Validate, "outside actual footprint"); region.LandMarkLayout.Remove((199,199));
        region.TerrainLayout[(0,0)] = "does_not_exist"; Reject(catalog.Validate, "unknown ID"); region.TerrainLayout[(0,0)] = "mountain";
        var range = catalog.Tiles["mountain"].Environment.Elevation; var oldMin = range.Min; range.Min = 1; range.Max = .5; Reject(catalog.Validate, "Min exceeds Max"); range.Min = oldMin; range.Max = 1;
        fixedSea.ForcedPositions.Add(fixedSea.ForcedPositions[0]); Reject(catalog.Validate, "duplicate forced position"); fixedSea.ForcedPositions.RemoveAt(fixedSea.ForcedPositions.Count - 1);
        var point = fixedSea.ForcedPositions[0]; int oldCol = point.Col; point.Col = 200; Reject(catalog.Validate, "forced position"); point.Col = oldCol;
        foreach (var (field, ids, wrong) in new[] { ("RiverTypeIds", profile.Hydrology.RiverTypeIds, "plain"), ("LakeTypeIds", profile.Hydrology.LakeTypeIds, "river"), ("OceanTypeIds", profile.Hydrology.OceanTypeIds, "lake") })
        {
            var old = ids[0]; ids[0] = wrong; Reject(catalog.Validate, "Hydrology." + field); ids[0] = old;
        }
        catalog.Validate();
        Check(originalHash == catalog.ConfigHash, "rejected mutations restored all content");
        RawFileFailures(baseDir);
        Console.WriteLine($"MAP_DATA_OK assertions={_assertions} tiles={catalog.Tiles.Count} regions={catalog.Regions.Count} config={originalHash}");
    }
    private static void RawFileFailures(string baseDir)
    {
        var dataDir = Directory.Exists(Path.Combine(baseDir,"TileDefines")) ? baseDir : Path.Combine(baseDir,"Data");
        var fixture = Path.Combine(Path.GetTempPath(), "unityworld-map-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        try
        {
            foreach (var directory in new[] { "TileDefines", "RegionDefines", "LandMarkDefines" })
            foreach (var file in Directory.GetFiles(Path.Combine(dataDir,directory), "*.json", SearchOption.AllDirectories))
            {
                var target = Path.Combine(fixture, directory, Path.GetRelativePath(Path.Combine(dataDir,directory),file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file,target);
            }
            foreach (var file in new[] { "TileModifierDefines.json" }) File.Copy(Path.Combine(dataDir,file),Path.Combine(fixture,file));
            Check(MapCatalog.Load(fixture).ConfigHash == MapCatalog.Load(baseDir).ConfigHash, "hash independent of absolute source directory");
            var plain = Path.Combine(fixture,"TileDefines","plain.json"); var original = File.ReadAllText(plain);
            File.WriteAllText(plain, original.Replace("\"ID\": \"plain\"", "\"ID\": \"plain\", \"ID\": \"plain\""));
            Reject(() => MapCatalog.Load(fixture), "duplicate property"); File.WriteAllText(plain, original);
            File.Copy(plain,Path.Combine(fixture,"TileDefines.json")); Reject(() => MapCatalog.Load(fixture), "duplicate ID"); File.Delete(Path.Combine(fixture,"TileDefines.json"));
            File.WriteAllText(plain, "[{ broken JSON]"); Reject(() => MapCatalog.Load(fixture), "Map configuration"); File.WriteAllText(plain, original);
            File.WriteAllText(plain, original.Replace("\"SchemaVersion\": 2,", "")); Reject(() => MapCatalog.Load(fixture), "SchemaVersion"); File.WriteAllText(plain, original);
            File.WriteAllText(plain, original.Replace("\"Tags\": [\"平野\"]", "\"Tags\": null")); Reject(() => MapCatalog.Load(fixture), "null configuration"); File.WriteAllText(plain, original);
            File.Move(Path.Combine(fixture,"TileModifierDefines.json"),Path.Combine(fixture,"modifiers-disabled.txt")); Reject(() => MapCatalog.Load(fixture), "missing");
            File.Move(Path.Combine(fixture,"modifiers-disabled.txt"),Path.Combine(fixture,"TileModifierDefines.json"));
            var regionFile = Path.Combine(fixture,"RegionDefines","region_volcanic_belt.json"); var text = File.ReadAllText(regionFile);
            File.WriteAllText(regionFile,text.Replace("\"0,0\": \"desert\"", "\"0,0\": \"desert\", \"+0,0\": \"mountain\""));
            Reject(() => MapCatalog.Load(fixture), "Duplicate axial coordinate");
        }
        finally
        {
            var full = Path.GetFullPath(fixture); var temp = Path.GetFullPath(Path.GetTempPath());
            if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("unityworld-map-data-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture cleanup path");
            Directory.Delete(full,recursive:true);
        }
    }
}
