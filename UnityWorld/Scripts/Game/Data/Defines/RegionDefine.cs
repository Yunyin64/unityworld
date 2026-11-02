using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data;

/// <summary>固定朝向地区；TerrainLayout 的键集合是唯一占地真相。</summary>
public class RegionDefine : DefineBase
{
    /// <summary>地图配置格式版本；当前为 2。</summary>
    public int SchemaVersion { get; set; } = 2;
    /// <summary>绝对 Offset 固定锚点；每个位置完整放一份拼图，最先放置，不参加普通随机筛选。</summary>
    public List<OffsetPointDefine> ForcedPositions { get; set; } = new();
    /// <summary>固定地区为后续环境生成提供的锚定值；没有固定锚点时不使用。</summary>
    public MapEnvironment FixedEnvironment { get; set; } = new() { Elevation = .5, Moisture = .5, Temperature = .5 };
    /// <summary>每个位面必须完整放置的最少实例数。</summary>
    public int MinCount { get; set; }
    /// <summary>每个位面的最多实例数；-1 表示不限。</summary>
    public int MaxCount { get; set; } = -1;
    /// <summary>合法可选候选的相对生成权重。</summary>
    public double SpawnWeight { get; set; } = 1;
    /// <summary>同类实例锚点之间的最小六边形距离。</summary>
    public int MinSpacing { get; set; } = 8;
    /// <summary>宽度；大陆固定为 200，地区中只作展示元信息。</summary>
    public int Width { get; set; }
    /// <summary>地图高度；大陆固定为 200，地区中只作展示元信息。</summary>
    public int Height { get; set; }
    [JsonConverter(typeof(AxialOffsetStringDictConverter))]
    /// <summary>实际占地集合及逐格固定类型，键为相对 Axial 坐标。</summary>
    public Dictionary<(int dq, int dr), string> TerrainLayout { get; set; } = new();
    [JsonConverter(typeof(AxialOffsetStringDictConverter))]
    /// <summary>随地区原子生成的固定地标布局。</summary>
    public Dictionary<(int dq, int dr), string> LandMarkLayout { get; set; } = new();
    /// <summary>保留的地区拓展语义标签，不增加隐式地形。</summary>
    public List<string> ExtraTerrainIds { get; set; } = new();
    /// <summary>保留的兼容功能：对实际占地应用一次偏移；正式内容默认全零，不作为地区玩法依赖。</summary>
    public TileAura AuraProfile { get; set; } = new() { Jin = 0, Mu = 0, Shui = 0, Huo = 0, Tu = 0 };
    /// <summary>地区锚点必须满足的环境条件。</summary>
    public EnvironmentRule AnchorEnvironment { get; set; } = new();
    /// <summary>对实际 footprint 统计满足比例的环境条件。</summary>
    public EnvironmentRule OverallEnvironment { get; set; } = new();
    /// <summary>整体环境条件要求的占地满足比例。</summary>
    public double RequiredEnvironmentFraction { get; set; } = 1;
    /// <summary>指定关键格的独立环境硬条件。</summary>
    public List<RegionEnvironmentPoint> KeyEnvironment { get; set; } = new();
    /// <summary>作者显式声明的局部环境偏移，默认不调整。</summary>
    public List<EnvironmentAdjustment> EnvironmentAdjustments { get; set; } = new();
    /// <summary>地区固定的有向跨格河段。</summary>
    public List<RegionRiverCell> RiverCells { get; set; } = new();
    /// <summary>连接地区外部水系的固定边缘端口。</summary>
    public List<RegionRiverPort> RiverPorts { get; set; } = new();
    /// <summary>供通行组引用的地区内部关键位置。</summary>
    public List<RegionKeyPoint> KeyPoints { get; set; } = new();
    /// <summary>旧锚点类型限制的显式迁移；非空时按对应类型的环境条件判定。</summary>
    public List<string> PlacementTypeIds { get; set; } = new();
    /// <summary>旧调用兼容属性；实际生成以 MinCount 为准。</summary>
    [JsonIgnore] public bool IsGuaranteed { get => MinCount > 0; set { if (value) MinCount = Math.Max(1, MinCount); } }
}

/// <summary>严格解析相对 Axial 坐标；不再吞掉坏键、重复键或数字旧类型。</summary>
public class AxialOffsetStringDictConverter : JsonConverter<Dictionary<(int, int), string>>
{
    /// <summary>以文化无关格式解析 dq,dr，拒绝不完整或越界数字。</summary>
    public static (int, int) Parse(string key)
    {
        var parts = key.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var q)
            || !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var r))
            throw new JsonException($"Invalid axial coordinate '{key}'; expected dq,dr.");
        return (q, r);
    }
    /// <summary>读取固定布局并拒绝旧数字类型与归一化后重复的坐标。</summary>
    public override Dictionary<(int, int), string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<(int, int), string>();
        using var doc = JsonDocument.ParseValue(ref reader);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String) throw new JsonException($"Terrain at '{prop.Name}' must be a type ID; run map migration for legacy numbers.");
            if (!result.TryAdd(Parse(prop.Name), prop.Value.GetString() ?? "")) throw new JsonException($"Duplicate axial coordinate '{prop.Name}'.");
        }
        return result;
    }
    /// <summary>按 r、q 稳定次序保存坐标，避免字典插入顺序改变摘要。</summary>
    public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var pair in value.OrderBy(x => x.Key.Item2).ThenBy(x => x.Key.Item1))
            writer.WriteString(FormattableString.Invariant($"{pair.Key.Item1},{pair.Key.Item2}"), pair.Value);
        writer.WriteEndObject();
    }
}
