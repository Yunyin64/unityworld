namespace UnityWorld.Game.Data;

/// <summary>普通生成参数；默认值来自代码，不是 JSON Define，也不负责固定地区内容。</summary>
public sealed class WorldGenerationOptions
{
    public string ID { get; set; } = "china-inspired-v1";
    public string DisplayName { get; set; } = "大陆生成默认参数";
    /// <summary>生成器语义版本，参与种子派生与复现摘要。</summary>
    public string GeneratorVersion { get; set; } = "region-puzzle-v2";
    /// <summary>标明开发样例；不代表已确认的正式世界设定。</summary>
    public bool IsDevelopmentSample { get; set; } = true;
    /// <summary>生成配置说明与未定案内容边界。</summary>
    public string Notes { get; set; } = "";
    /// <summary>大陆宽度，固定为 200。</summary>
    public int Width { get; set; } = 200;
    /// <summary>大陆高度，固定为 200。</summary>
    public int Height { get; set; } = 200;
    /// <summary>参与当前大陆生成的地区定义 ID。</summary>
    public List<string> RegionIds { get; set; } = new();
    /// <summary>地区实际占地占非边界面积的软目标。</summary>
    public double RegionCoverageTarget { get; set; } = 0.42;
    /// <summary>整图环境重新生成的最大尝试数。</summary>
    public int WholeMapAttempts { get; set; } = 3;
    /// <summary>必需地区候选搜索预算。</summary>
    public int RequiredRegionAttempts { get; set; } = 4000;
    /// <summary>可选地区候选搜索预算。</summary>
    public int OptionalRegionAttempts { get; set; } = 18000;
    /// <summary>必需地区阶段可撤销重排的次数上限。</summary>
    public int RegionBacktrackBudget { get; set; } = 32;
    /// <summary>基础填充阶段的局部回退预算。</summary>
    public int FillBacktrackBudget { get; set; } = 256;
    /// <summary>填充完成后的合法局部修整预算。</summary>
    public int FillRepairBudget { get; set; } = 64;
    /// <summary>仅对 Fill 格执行的水系修补预算。</summary>
    public int WaterRepairBudget { get; set; } = 64;
    /// <summary>每格每环境轴允许的最大绝对调整量。</summary>
    public double MaxEnvironmentAdjustment { get; set; } = 0.12;
    /// <summary>仅在空闲 Fill 格进行独立地标散布的密度。</summary>
    public double LandmarkDensity { get; set; } = 0.003;
    /// <summary>环境参数或硬限制与软偏好，按所属配置应用。</summary>
    public EnvironmentGenerationSettings Environment { get; set; } = new();
    /// <summary>水源、合法终点及可用水文类型配置。</summary>
    public HydrologyGenerationSettings Hydrology { get; set; } = new();
    /// <summary>需要以指定移动能力互通的地点组。</summary>
    public List<ConnectivityGroupDefine> ConnectivityGroups { get; set; } = new();
}

/// <summary>连续环境场的尺度与宏观气候趋势。</summary>
public sealed class EnvironmentGenerationSettings
{
    /// <summary>大尺度连续环境噪声的空间尺度。</summary>
    public double NoiseScale { get; set; } = 34;
    /// <summary>局部连续环境噪声的空间尺度。</summary>
    public double DetailScale { get; set; } = 12;
    /// <summary>固定边界向内平滑过渡的距离。</summary>
    public int BoundaryBlendDistance { get; set; } = 12;
    /// <summary>北方总体温度趋势。</summary>
    public double NorthTemperature { get; set; } = 0.2;
    /// <summary>南方总体温度趋势。</summary>
    public double SouthTemperature { get; set; } = 0.85;
    /// <summary>西方总体湿润度趋势。</summary>
    public double WestMoisture { get; set; } = 0.25;
    /// <summary>东方总体湿润度趋势。</summary>
    public double EastMoisture { get; set; } = 0.75;
}

/// <summary>跨格水系的源头、终点和类型约束。</summary>
public sealed class HydrologyGenerationSettings
{
    /// <summary>计划生成的河流水源数量。</summary>
    public int RiverCount { get; set; } = 3;
    /// <summary>随机水源允许的最小列。</summary>
    public int MinSourceColumn { get; set; } = 35;
    /// <summary>随机水源允许的最大列。</summary>
    public int MaxSourceColumn { get; set; } = 70;
    /// <summary>是否允许明确湖泊作为水系终点。</summary>
    public bool AllowInlandLakes { get; set; } = true;
    /// <summary>是否要求所有主河流抵达海域。</summary>
    public bool RequireOceanExit { get; set; } = true;
    /// <summary>明确配置的内陆湖泊终点坐标。</summary>
    public List<OffsetPointDefine> LakeSinks { get; set; } = new();
    /// <summary>允许承载跨格河流的类型。</summary>
    public List<string> RiverTypeIds { get; set; } = new() { "river_lake" };
    /// <summary>允许承载湖泊终点的类型。</summary>
    public List<string> LakeTypeIds { get; set; } = new() { "lake" };
    /// <summary>允许承载海洋终点的类型。</summary>
    public List<string> OceanTypeIds { get; set; } = new() { "ocean" };
}

/// <summary>共享移动能力下必须互通的一组地点。</summary>
public sealed class ConnectivityGroupDefine
{
    /// <summary>在所属配置范围内稳定且唯一的标识。</summary>
    public string ID { get; set; } = "";
    /// <summary>此连通组检查使用的移动能力。</summary>
    public string MovementMode { get; set; } = "walk";
    /// <summary>此连通组是否允许跨越河流。</summary>
    public bool CanCrossRivers { get; set; }
    /// <summary>格式 regionId:keyPointId，选中已落地的该类全部实例。</summary>
    public List<string> RegionPoints { get; set; } = new();
    /// <summary>明确 Offset 坐标的互通地点。</summary>
    public List<OffsetPointDefine> FixedPoints { get; set; } = new();
}

/// <summary>明确的 Offset 世界地图坐标。</summary>
public sealed class OffsetPointDefine
{
    /// <summary>Offset 列坐标。</summary>
    public int Col { get; set; }
    /// <summary>Offset 行坐标。</summary>
    public int Row { get; set; }
}
