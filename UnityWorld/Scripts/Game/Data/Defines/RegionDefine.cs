using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 区域静态配置模板。
    /// 描述一种区域的生成规则、占用形状、内部地形/地标布局及五行倾向。
    /// 运行时由 <see cref="PlaneGenerator"/> 在世界生成 Step3 中实例化为 <see cref="Region"/>。
    ///
    /// <para><b>布局坐标说明</b>：TerrainLayout 和 LandMarkLayout 的键使用
    /// 相对中心点的 Axial 偏移 (dq, dr)，序列化格式为字符串 "dq,dr"（如 "0,0"、"1,-1"）。</para>
    /// </summary>
    public class RegionDefine : DefineBase
    {
        // ── 生成规则 ──────────────────────────────────────

        /// <summary>必然生成（世界宿命地点，优先于随机区域放置）</summary>
        [JsonPropertyName("IsGuaranteed")]
        public bool IsGuaranteed { get; set; } = false;

        /// <summary>全图最多实例数（-1 = 不限）</summary>
        [JsonPropertyName("MaxCount")]
        public int MaxCount { get; set; } = -1;

        /// <summary>随机放置权重（IsGuaranteed=true 时忽略）</summary>
        [JsonPropertyName("SpawnWeight")]
        public float SpawnWeight { get; set; } = 1f;

        /// <summary>
        /// 中心点允许落在的地形类型（空列表 = 不限）。
        /// 生成时检查中心点地块是否在列表内。
        /// </summary>
        [JsonPropertyName("PlacementTerrains")]
        public List<TerrainType> PlacementTerrains { get; set; } = new();

        // ── 占用形状 ──────────────────────────────────────

        /// <summary>区域占用长方形的宽（Offset 列数）</summary>
        [JsonPropertyName("Width")]
        public int Width { get; set; } = 5;

        /// <summary>区域占用长方形的高（Offset 行数）</summary>
        [JsonPropertyName("Height")]
        public int Height { get; set; } = 5;

        // ── 内容布局（相对中心点 Axial 偏移） ─────────────

        /// <summary>
        /// 地形覆盖布局。
        /// Key 格式：JSON 中为 "dq,dr" 字符串，运行时解析为 (int dq, int dr)。
        /// Value：覆盖后的地形类型。
        /// </summary>
        [JsonPropertyName("TerrainLayout")]
        [JsonConverter(typeof(AxialOffsetTerrainDictConverter))]
        public Dictionary<(int dq, int dr), TerrainType> TerrainLayout { get; set; } = new();

        /// <summary>
        /// 固定地标布局。
        /// Key 格式：JSON 中为 "dq,dr" 字符串，运行时解析为 (int dq, int dr)。
        /// Value：LandMarkDefine ID（区域落地时必然在该偏移处生成）。
        /// </summary>
        [JsonPropertyName("LandMarkLayout")]
        [JsonConverter(typeof(AxialOffsetStringDictConverter))]
        public Dictionary<(int dq, int dr), string> LandMarkLayout { get; set; } = new();

        // ── 区域属性 ──────────────────────────────────────

        /// <summary>区域持有的拓展地形语义标签 ID 列表（如 "volcanic_zone"、"leyline_dense"）</summary>
        [JsonPropertyName("ExtraTerrainIds")]
        public List<string> ExtraTerrainIds { get; set; } = new();

        /// <summary>区域整体五行浓度偏向（叠加到区域内所有地块的 CurrentAura）</summary>
        [JsonPropertyName("AuraProfile")]
        public TileAura AuraProfile { get; set; } = new TileAura { Jin=0,Mu=0,Shui=0,Huo=0,Tu=0 };

        /// <summary>叙事标签</summary>
        [JsonPropertyName("Tags")]
        public List<string> Tags { get; set; } = new();
    }

    // ── JSON 自定义转换器 ──────────────────────────────────

    /// <summary>将 "dq,dr" 字符串 key 转换为 (int,int) ValueTuple 的字典（Value 为 TerrainType）</summary>
    public class AxialOffsetTerrainDictConverter : JsonConverter<Dictionary<(int, int), TerrainType>>
    {
        public override Dictionary<(int, int), TerrainType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<(int, int), TerrainType>();
            using var doc = JsonDocument.ParseValue(ref reader);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var parts = prop.Name.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out var dq) && int.TryParse(parts[1], out var dr))
                    result[(dq, dr)] = (TerrainType)prop.Value.GetInt32();
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), TerrainType> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kv in value)
                writer.WriteNumber($"{kv.Key.Item1},{kv.Key.Item2}", (int)kv.Value);
            writer.WriteEndObject();
        }
    }

    /// <summary>将 "dq,dr" 字符串 key 转换为 (int,int) ValueTuple 的字典（Value 为 string）</summary>
    public class AxialOffsetStringDictConverter : JsonConverter<Dictionary<(int, int), string>>
    {
        public override Dictionary<(int, int), string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<(int, int), string>();
            using var doc = JsonDocument.ParseValue(ref reader);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var parts = prop.Name.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out var dq) && int.TryParse(parts[1], out var dr))
                    result[(dq, dr)] = prop.Value.GetString() ?? string.Empty;
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), string> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kv in value)
                writer.WriteString($"{kv.Key.Item1},{kv.Key.Item2}", kv.Value);
            writer.WriteEndObject();
        }
    }
}
