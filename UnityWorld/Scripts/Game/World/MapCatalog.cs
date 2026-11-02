using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data;

/// <summary>地图严格数据入口。错误直接失败，不沿用通用加载器的吞错行为。</summary>
public sealed class MapCatalog
{
    /// <summary>按稳定 ID 索引的同级地块类型。</summary>
    public Dictionary<string, TileDefine> Tiles { get; } = new(StringComparer.Ordinal);
    /// <summary>作者固定地区拼图。</summary>
    public Dictionary<string, RegionDefine> Regions { get; } = new(StringComparer.Ordinal);
    /// <summary>自然与人工地标定义。</summary>
    public Dictionary<string, LandMarkDefine> LandMarks { get; } = new(StringComparer.Ordinal);
    /// <summary>地标未来每秒速率修正定义。</summary>
    public Dictionary<string, TileModifierDefine> Modifiers { get; } = new(StringComparer.Ordinal);
    /// <summary>代码默认生成参数，不从额外的 Define 文件加载。</summary>
    public Dictionary<string, WorldGenerationOptions> Profiles { get; } = new(StringComparer.Ordinal);
    /// <summary>对解析后语义内容排序并取 SHA-256，不包含文件路径，包含有效生成参数。</summary>
    public string ConfigHash => ComputeConfigHash();
    /// <summary>严格大小写及未知字段策略；tuple 坐标由定义级转换器处理。</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };
    /// <summary>从项目根或 Data 根加载单文件及递归目录，重复和坏配置立即失败。</summary>
    public static MapCatalog Load(string baseDir)
    {
        var dataDir = File.Exists(Path.Combine(baseDir, "TileDefines.json")) || Directory.Exists(Path.Combine(baseDir, "TileDefines")) ? baseDir : Path.Combine(baseDir, "Data");
        var result = new MapCatalog();
        ReadSources(Path.Combine(dataDir, "TileDefines.json"), result.Tiles);
        ReadSources(Path.Combine(dataDir, "RegionDefines.json"), result.Regions);
        ReadSources(Path.Combine(dataDir, "LandMarkDefines.json"), result.LandMarks);
        ReadSources(Path.Combine(dataDir, "TileModifierDefines.json"), result.Modifiers);
        var defaults = WorldGenerationPresets.CreateDefault(result.Regions.Values);
        result.Profiles.Add(defaults.ID, defaults);
        result.Validate();
        return result;
    }
    private static void ReadSources<T>(string legacyFile, Dictionary<string, T> destination) where T : DefineBase
    {
        var directory = Path.Combine(Path.GetDirectoryName(legacyFile)!, Path.GetFileNameWithoutExtension(legacyFile));
        var paths = new List<string>();
        if (File.Exists(legacyFile)) paths.Add(legacyFile);
        if (Directory.Exists(directory)) paths.AddRange(Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal));
        if (paths.Count == 0) throw new InvalidDataException($"Map configuration missing: {legacyFile} or {directory}");
        foreach (var path in paths) Read(path, destination);
    }
    private static void Read<T>(string path, Dictionary<string, T> destination) where T : DefineBase
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            RejectDuplicates(doc.RootElement, Path.GetFileName(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"{path}: root must be an array.");
            foreach (var node in doc.RootElement.EnumerateArray())
            {
                if (typeof(T) != typeof(TileModifierDefine) && (!node.TryGetProperty("SchemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number || schema.GetInt32() != 2))
                    throw new InvalidDataException($"{path}: explicit SchemaVersion=2 is required; migrate old data first.");

            }
            var items = JsonSerializer.Deserialize<List<T>>(doc.RootElement.GetRawText(), JsonOptions);
            if (items == null || items.Count == 0) throw new InvalidDataException($"{path}: definitions must not be empty.");
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ID)) throw new InvalidDataException($"{path}: definition ID is required.");
                if (!destination.TryAdd(item.ID, item)) throw new InvalidDataException($"{path}: duplicate ID '{item.ID}'.");
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is IOException || ex is ArgumentException)
        { throw new InvalidDataException($"Map configuration {path}: {ex.Message}", ex); }
    }
    private static void RejectDuplicates(JsonElement node, string path)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in node.EnumerateObject())
            {
                if (!keys.Add(property.Name)) throw new InvalidDataException($"{path}: duplicate property '{property.Name}'.");
                if (property.Value.ValueKind == JsonValueKind.Null && property.Name is not ("OutDirection" or "LandMarkId"))
                    throw new InvalidDataException($"{path}.{property.Name}: null configuration is not allowed.");
                RejectDuplicates(property.Value, path + "." + property.Name);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        { int index = 0; foreach (var child in node.EnumerateArray()) RejectDuplicates(child, $"{path}[{index++}]"); }
    }
    /// <summary>完整核对引用、坐标、布局、数值及边界参数，聚合可定位错误。</summary>
    public void Validate() => Validate(null);
    public void Validate(WorldGenerationOptions options)
    {
        var errors = new List<string>();
        void Error(string path, string detail) => errors.Add(path + ": " + detail);
        void Finite(string path, double value, double min = double.NegativeInfinity, double max = double.PositiveInfinity)
        { if (!double.IsFinite(value) || value < min || value > max) Error(path, $"invalid value {value}, expected finite [{min},{max}]"); }
        void Aura(string path, TileAura aura, bool nonnegative = false)
        {
            if (aura == null) { Error(path, "null aura"); return; }
            Finite(path + ".Jin", aura.Jin, nonnegative ? 0 : double.NegativeInfinity);
            Finite(path + ".Mu", aura.Mu, nonnegative ? 0 : double.NegativeInfinity);
            Finite(path + ".Shui", aura.Shui, nonnegative ? 0 : double.NegativeInfinity);
            Finite(path + ".Huo", aura.Huo, nonnegative ? 0 : double.NegativeInfinity);
            Finite(path + ".Tu", aura.Tu, nonnegative ? 0 : double.NegativeInfinity);
        }
        void Environment(string path, EnvironmentRule rule)
        {
            if (rule == null) { Error(path, "null environment rule"); return; }
            foreach (var (name, range) in new[] { ("Elevation", rule.Elevation), ("Moisture", rule.Moisture), ("Temperature", rule.Temperature) })
            {
                if (range == null) { Error(path + "." + name, "null range"); continue; }
                Finite(path + "." + name + ".Min", range.Min, 0, 1); Finite(path + "." + name + ".Max", range.Max, 0, 1);
                if (range.Min > range.Max) Error(path + "." + name, "Min exceeds Max");
            }
            Finite(path + ".PreferredElevation", rule.PreferredElevation, 0, 1);
            Finite(path + ".PreferredMoisture", rule.PreferredMoisture, 0, 1);
            Finite(path + ".PreferredTemperature", rule.PreferredTemperature, 0, 1);
            Finite(path + ".PreferenceStrength", rule.PreferenceStrength, 0);
        }
        void References<T>(string path, IEnumerable<string> ids, Dictionary<string, T> dict)
        { foreach (var id in ids) if (string.IsNullOrWhiteSpace(id) || !dict.ContainsKey(id)) Error(path, $"unknown ID '{id}'"); }
        void Positions(string path, List<OffsetPointDefine> positions)
        {
            var seen = new HashSet<(int, int)>();
            foreach (var point in positions)
                if (point == null || point.Col < 0 || point.Col >= 200 || point.Row < 0 || point.Row >= 200 || !seen.Add((point.Col, point.Row)))
                    Error(path, "invalid or duplicate forced position");
        }
        if (Tiles.Count == 0 || Regions.Count == 0 || LandMarks.Count == 0 || Modifiers.Count == 0)
            Error("MapCatalog", "all four definition collections are required");
        foreach (var (id, tile) in Tiles)
        {
            var p = "TileDefines/" + id;
            if (tile.SchemaVersion != 2) Error(p, "SchemaVersion must be 2");
            Finite(p + ".FillWeight", tile.FillWeight, 0); Finite(p + ".MovementCost", tile.MovementCost, double.Epsilon);
            Aura(p + ".InitialAura", tile.InitialAura, true); Environment(p + ".Environment", tile.Environment);
            if (!new[] { "none", "river", "lake", "ocean" }.Contains(tile.WaterKind)) Error(p, "unknown WaterKind");
            if (tile.BlocksAllMovement && tile.MovementModes.Count != 0) Error(p, "BlocksAllMovement conflicts with MovementModes");
            References(p + ".AllowedNeighborIds", tile.AllowedNeighborIds, Tiles);
            References(p + ".ForbiddenNeighborIds", tile.ForbiddenNeighborIds, Tiles);
            References(p + ".NeighborWeights", tile.NeighborWeights.Keys, Tiles);
            foreach (var pair in tile.NeighborWeights) Finite(p + ".NeighborWeights[" + pair.Key + "]", pair.Value, 0);
            if (tile.AllowedNeighborIds.Intersect(tile.ForbiddenNeighborIds).Any()) Error(p, "same neighbor both allowed and forbidden");
        }
        foreach (var (id, modifier) in Modifiers)
        {
            Aura("TileModifierDefines/" + id + ".AuraData", modifier.AuraData);
            if (!float.IsFinite(modifier.Duration) || (modifier.Duration != -1 && modifier.Duration <= 0)) Error("TileModifierDefines/" + id, "Duration must be -1 or positive");
        }
        foreach (var (id, landmark) in LandMarks)
        {
            var p = "LandMarkDefines/" + id;
            Positions(p + ".ForcedPositions", landmark.ForcedPositions);
            if (landmark.SchemaVersion != 2) Error(p, "SchemaVersion must be 2");
            Finite(p + ".SpawnWeight", landmark.SpawnWeight, 0); Aura(p + ".AuraOffset", landmark.AuraOffset);
            References(p + ".PlacementTypeIds", landmark.PlacementTypeIds, Tiles);
            References(p + ".ModifierDefineIds", landmark.ModifierDefineIds, Modifiers);
        }
        foreach (var (id, region) in Regions)
        {
            var p = "RegionDefines/" + id;
            Positions(p + ".ForcedPositions", region.ForcedPositions);
            if (region.ForcedPositions.Count > 0)
            {
                var environment = region.FixedEnvironment;
                if (environment == null) Error(p, "FixedEnvironment is required");
                else { Finite(p + ".FixedEnvironment.Elevation", environment.Elevation, 0, 1); Finite(p + ".FixedEnvironment.Moisture", environment.Moisture, 0, 1); Finite(p + ".FixedEnvironment.Temperature", environment.Temperature, 0, 1); }
            }
            if (region.SchemaVersion != 2) Error(p, "SchemaVersion must be 2");
            if (region.MinCount < 0 || region.MaxCount < -1 || (region.MaxCount != -1 && region.MaxCount < region.MinCount)) Error(p, "invalid MinCount/MaxCount");
            if (region.MinSpacing < 0 || region.Width < 0 || region.Height < 0) Error(p, "negative spacing/display dimensions");
            Finite(p + ".SpawnWeight", region.SpawnWeight, 0); Finite(p + ".RequiredEnvironmentFraction", region.RequiredEnvironmentFraction, 0, 1);
            Environment(p + ".AnchorEnvironment", region.AnchorEnvironment);
            Environment(p + ".OverallEnvironment", region.OverallEnvironment);
            if (region.TerrainLayout.Count == 0) Error(p, "TerrainLayout footprint is empty");
            References(p + ".TerrainLayout", region.TerrainLayout.Values, Tiles);
            References(p + ".PlacementTypeIds", region.PlacementTypeIds, Tiles);
            References(p + ".LandMarkLayout", region.LandMarkLayout.Values, LandMarks);
            void Footprint(string field, int q, int r)
            { if (!region.TerrainLayout.ContainsKey((q, r))) Error(p + "." + field, $"({q},{r}) outside actual footprint"); }
            foreach (var pair in region.LandMarkLayout) Footprint("LandMarkLayout", pair.Key.dq, pair.Key.dr);
            foreach (var point in region.KeyEnvironment) { Footprint("KeyEnvironment", point.Dq, point.Dr); Environment(p + ".KeyEnvironment", point.Environment); }
            var adjustmentKeys = new HashSet<(int, int)>();
            foreach (var adjustment in region.EnvironmentAdjustments)
            {
                Footprint("EnvironmentAdjustments", adjustment.Dq, adjustment.Dr);
                if (!adjustmentKeys.Add((adjustment.Dq, adjustment.Dr))) Error(p, "duplicate adjustment coordinate");
                Finite(p + ".Adjustment.Elevation", adjustment.Elevation, -1, 1); Finite(p + ".Adjustment.Moisture", adjustment.Moisture, -1, 1); Finite(p + ".Adjustment.Temperature", adjustment.Temperature, -1, 1);
            }
            var pointIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var point in region.KeyPoints)
            { Footprint("KeyPoints", point.Dq, point.Dr); if (string.IsNullOrWhiteSpace(point.ID) || !pointIds.Add(point.ID)) Error(p, "empty/duplicate key point ID"); }
            var river = new Dictionary<(int, int), RegionRiverCell>();
            foreach (var cell in region.RiverCells)
            {
                Footprint("RiverCells", cell.Dq, cell.Dr);
                if (!river.TryAdd((cell.Dq, cell.Dr), cell)) Error(p, "duplicate river coordinate");
                if (cell.InDirections.Any(d => d < 0 || d > 5) || cell.InDirections.Distinct().Count() != cell.InDirections.Count || cell.OutDirection is < 0 or > 5) Error(p, "invalid/duplicate river direction");
                if (cell.OutDirection.HasValue && cell.InDirections.Contains(cell.OutDirection.Value)) Error(p, "same inlet and outlet direction");
                if (cell.IsSource && cell.InDirections.Count > 0 || cell.IsSink && cell.OutDirection.HasValue) Error(p, "source/sink directions conflict");
                if (region.TerrainLayout.TryGetValue((cell.Dq, cell.Dr), out var typeId) && Tiles.TryGetValue(typeId, out var type) && type.WaterKind == "none") Error(p, "river cell type has no water capability");
            }
            var portKeys = new HashSet<(int, int, int, bool)>();
            foreach (var port in region.RiverPorts)
            {
                Footprint("RiverPorts", port.Dq, port.Dr);
                if (port.Direction < 0 || port.Direction > 5) { Error(p, "invalid port direction"); continue; }
                if (!portKeys.Add((port.Dq, port.Dr, port.Direction, port.IsInlet))) Error(p, "duplicate river port");
                var neighbor = new TileId(port.Dq, port.Dr).Neighbor(port.Direction);
                if (region.TerrainLayout.ContainsKey((neighbor.Q, neighbor.R))) Error(p, "port must face outside actual footprint");
                if (!river.TryGetValue((port.Dq, port.Dr), out var cell) || (port.IsInlet ? !cell.InDirections.Contains(port.Direction) : cell.OutDirection != port.Direction)) Error(p, "port does not match river cell");
            }
            foreach (var cell in region.RiverCells)
            {
                var at = new TileId(cell.Dq, cell.Dr);
                foreach (var (direction, inlet) in cell.InDirections.Select(d => (d, true)).Concat(cell.OutDirection.HasValue ? new[] { (cell.OutDirection.Value, false) } : Array.Empty<(int, bool)>()))
                {
                    var next = at.Neighbor(direction);
                    if (region.TerrainLayout.ContainsKey((next.Q, next.R)))
                    {
                        if (!river.TryGetValue((next.Q, next.R), out var adjacent) || (inlet ? adjacent.OutDirection != (direction + 3) % 6 : !adjacent.InDirections.Contains((direction + 3) % 6))) Error(p, $"asymmetric internal river at {at}, direction {direction}");
                    }
                    else if (!portKeys.Contains((cell.Dq, cell.Dr, direction, inlet))) Error(p, $"missing river port at {at}, direction {direction}");
                }
            }
        }
        foreach (var profile in options == null ? Profiles.Values.AsEnumerable() : new[] { options })
        // Pure parameter validation, independent of a Define registry.
        {
            var p = "WorldGenerationOptions/" + profile.ID;
            if (profile.Width != 200 || profile.Height != 200) Error(p, "mainland requires 200x200");
            Finite(p + ".RegionCoverageTarget", profile.RegionCoverageTarget, 0, 1); Finite(p + ".MaxEnvironmentAdjustment", profile.MaxEnvironmentAdjustment, 0, 1); Finite(p + ".LandmarkDensity", profile.LandmarkDensity, 0, 1);
            if (profile.WholeMapAttempts <= 0 || profile.RequiredRegionAttempts <= 0 || profile.OptionalRegionAttempts < 0 || profile.RegionBacktrackBudget < 0 || profile.FillBacktrackBudget < 0 || profile.FillRepairBudget < 0 || profile.WaterRepairBudget < 0) Error(p, "invalid generation budget");
            References(p + ".RegionIds", profile.RegionIds, Regions);
            if (profile.RegionIds.Distinct().Count() != profile.RegionIds.Count) Error(p, "duplicate RegionIds");
            var env = profile.Environment;
            Finite(p + ".Environment.NoiseScale", env.NoiseScale, double.Epsilon); Finite(p + ".Environment.DetailScale", env.DetailScale, double.Epsilon);
            if (env.BoundaryBlendDistance < 0) Error(p, "negative boundary blend distance");
            Finite(p + ".Environment.NorthTemperature", env.NorthTemperature, 0, 1); Finite(p + ".Environment.SouthTemperature", env.SouthTemperature, 0, 1);
            Finite(p + ".Environment.WestMoisture", env.WestMoisture, 0, 1); Finite(p + ".Environment.EastMoisture", env.EastMoisture, 0, 1);
            var hydro = profile.Hydrology;
            if (hydro.RiverCount < 0 || hydro.MinSourceColumn < 1 || hydro.MaxSourceColumn < hydro.MinSourceColumn || hydro.MaxSourceColumn >= 199) Error(p, "invalid hydrology source settings");
            References(p + ".Hydrology.RiverTypeIds", hydro.RiverTypeIds, Tiles); References(p + ".Hydrology.LakeTypeIds", hydro.LakeTypeIds, Tiles); References(p + ".Hydrology.OceanTypeIds", hydro.OceanTypeIds, Tiles);
            foreach (var (field, ids, kind) in new[] { ("RiverTypeIds", hydro.RiverTypeIds, "river"), ("LakeTypeIds", hydro.LakeTypeIds, "lake"), ("OceanTypeIds", hydro.OceanTypeIds, "ocean") })
            {
                if (ids.Count == 0) Error(p + ".Hydrology." + field, "at least one type ID is required");
                if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Count) Error(p + ".Hydrology." + field, "duplicate type ID");
                foreach (var typeId in ids)
                    if (Tiles.TryGetValue(typeId, out var type) && type.WaterKind != kind)
                        Error(p + ".Hydrology." + field, $"type '{typeId}' has WaterKind '{type.WaterKind}', expected '{kind}'");
            }
            foreach (var point in hydro.LakeSinks) if (point.Col < 1 || point.Col > 198 || point.Row < 1 || point.Row > 198) Error(p, "lake sink outside interior");
            foreach (var group in profile.ConnectivityGroups)
            {
                if (string.IsNullOrWhiteSpace(group.ID) || string.IsNullOrWhiteSpace(group.MovementMode)) Error(p, "connectivity group identity/movement is required");
                foreach (var point in group.FixedPoints) if (point.Col < 0 || point.Col > 199 || point.Row < 0 || point.Row > 199) Error(p, "connectivity point outside map");
                foreach (var point in group.RegionPoints)
                {
                    var pieces = point.Split(':');
                    if (pieces.Length != 2 || !Regions.TryGetValue(pieces[0], out var region) || !region.KeyPoints.Any(k => k.ID == pieces[1])) Error(p, "unknown region key point '" + point + "'");
                }
            }
        }
        if (errors.Count > 0) throw new InvalidDataException("Map validation failed:\n" + string.Join("\n", errors));
    }
    /// <summary>每次按当前配置重新计算规范化摘要，供诊断与修改后的夹具使用。</summary>
    public string ComputeConfigHash(WorldGenerationOptions options = null)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            SchemaVersion = 2,
            Tiles = Tiles.Values.OrderBy(x => x.ID, StringComparer.Ordinal), Regions = Regions.Values.OrderBy(x => x.ID, StringComparer.Ordinal),
            LandMarks = LandMarks.Values.OrderBy(x => x.ID, StringComparer.Ordinal), Modifiers = Modifiers.Values.OrderBy(x => x.ID, StringComparer.Ordinal),
            Profiles = (options == null ? Profiles.Values.AsEnumerable() : new[] { options }).OrderBy(x => x.ID, StringComparer.Ordinal)
        }, JsonOptions));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) CanonicalWrite(writer, doc.RootElement);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }
    private static void CanonicalWrite(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var p in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)) { writer.WritePropertyName(p.Name); CanonicalWrite(writer, p.Value); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        { writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) CanonicalWrite(writer, item); writer.WriteEndArray(); }
        else value.WriteTo(writer);
    }
}
