using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data;

/// <summary>同级地域类型。山地及其变体均用独立 ID；标签不构成继承。</summary>
public sealed class TileDefine : DefineBase
{
    /// <summary>地图配置格式版本；当前为 2。</summary>
    public int SchemaVersion { get; set; } = 2;
    /// <summary>地域内容和作者说明。</summary>
    public string Description { get; set; } = "";
    /// <summary>诊断地图使用的显示颜色。</summary>
    public string Color { get; set; } = "#888888";
    /// <summary>诊断图例的短标识。</summary>
    public string Symbol { get; set; } = "";
    /// <summary>预留美术资源引用，可为空。</summary>
    public string AssetRef { get; set; } = "";
    /// <summary>该类型地块的一次性自然初始元气。</summary>
    public TileAura InitialAura { get; set; } = new();
    /// <summary>是否允许作为空隙填充候选。</summary>
    public bool AllowFill { get; set; } = true;
    /// <summary>基础填充权重；零表示不参加抽取。</summary>
    public double FillWeight { get; set; } = 1;
    /// <summary>环境硬限制与软偏好。</summary>
    public EnvironmentRule Environment { get; set; } = new();
    /// <summary>允许相邻的类型 ID；空列表表示不限。</summary>
    public List<string> AllowedNeighborIds { get; set; } = new();
    /// <summary>始终禁止相邻的类型 ID。</summary>
    public List<string> ForbiddenNeighborIds { get; set; } = new();
    /// <summary>相邻类型对候选权重的乘数。</summary>
    public Dictionary<string, double> NeighborWeights { get; set; } = new();
    /// <summary>允许进入的移动能力 ID。</summary>
    public List<string> MovementModes { get; set; } = new() { "walk", "fly" };
    /// <summary>静态移动消耗倍率，必须为正。</summary>
    public double MovementCost { get; set; } = 1;
    /// <summary>none / river / lake / ocean。格内景物不使用此跨格水系字段。</summary>
    public string WaterKind { get; set; } = "none";
    /// <summary>绝对阻断所有移动能力，适用于西界长城。</summary>
    public bool BlocksAllMovement { get; set; }
    /// <summary>预留资源内容引用；本期不执行采集。</summary>
    public List<string> ResourceRefs { get; set; } = new();
    /// <summary>预留遭遇内容引用；本期不执行事件。</summary>
    public List<string> EncounterRefs { get; set; } = new();
}

/// <summary>归一化环境轴的包含端点区间。</summary>
public sealed class FloatRange
{
    /// <summary>包含端点的下限。</summary>
    public double Min { get; set; } = 0;
    /// <summary>包含端点的上限。</summary>
    public double Max { get; set; } = 1;
    /// <summary>有限值位于包含端点的区间内时返回 true。</summary>
    public bool Contains(double value) => double.IsFinite(value) && value >= Min && value <= Max;
}

/// <summary>硬区间 + 非零软偏好；PreferenceStrength 为零时不偏好。</summary>
public sealed class EnvironmentRule
{
    /// <summary>归一化海拔范围或显式调整量。</summary>
    public FloatRange Elevation { get; set; } = new();
    /// <summary>湿润度的归一化环境数据。</summary>
    public FloatRange Moisture { get; set; } = new();
    /// <summary>归一化温度范围或显式调整量。</summary>
    public FloatRange Temperature { get; set; } = new();
    /// <summary>软偏好的海拔中心，范围 0..1。</summary>
    public double PreferredElevation { get; set; } = 0.5;
    /// <summary>软偏好的湿润度中心，范围 0..1。</summary>
    public double PreferredMoisture { get; set; } = 0.5;
    /// <summary>软偏好的温度中心，范围 0..1。</summary>
    public double PreferredTemperature { get; set; } = 0.5;
    /// <summary>偏离中心的权重衰减强度；零表示不偏好。</summary>
    public double PreferenceStrength { get; set; } = 2;
    /// <summary>三个环境轴的硬条件必须同时满足。</summary>
    public bool Matches(double elevation, double moisture, double temperature)
        => Elevation.Contains(elevation) && Moisture.Contains(moisture) && Temperature.Contains(temperature);
    /// <summary>返回随偏离偏好中心连续下降的正软分数。</summary>
    public double Score(double elevation, double moisture, double temperature)
        => Math.Exp(-PreferenceStrength * (Math.Abs(elevation - PreferredElevation)
            + Math.Abs(moisture - PreferredMoisture) + Math.Abs(temperature - PreferredTemperature)));
}

/// <summary>单个地区实际占地格的显式环境调整。</summary>
public sealed class EnvironmentAdjustment
{
    /// <summary>相对于地区锚点的 Axial q 偏移。</summary>
    public int Dq { get; set; }
    /// <summary>相对于地区锚点的 Axial r 偏移。</summary>
    public int Dr { get; set; }
    /// <summary>归一化海拔范围或显式调整量。</summary>
    public double Elevation { get; set; }
    /// <summary>湿润度的归一化环境数据。</summary>
    public double Moisture { get; set; }
    /// <summary>归一化温度范围或显式调整量。</summary>
    public double Temperature { get; set; }
}

/// <summary>一个关键格的环境约束。</summary>
public sealed class RegionEnvironmentPoint
{
    /// <summary>相对于地区锚点的 Axial q 偏移。</summary>
    public int Dq { get; set; }
    /// <summary>相对于地区锚点的 Axial r 偏移。</summary>
    public int Dr { get; set; }
    /// <summary>环境硬限制与软偏好。</summary>
    public EnvironmentRule Environment { get; set; } = new();
}

/// <summary>每格最多一个下游，输入方向可多个；边缘跨模板连接另声明端口。</summary>
public sealed class RegionRiverCell
{
    /// <summary>相对于地区锚点的 Axial q 偏移。</summary>
    public int Dq { get; set; }
    /// <summary>相对于地区锚点的 Axial r 偏移。</summary>
    public int Dr { get; set; }
    /// <summary>上游相邻方向，0..5，可多个。</summary>
    public List<int> InDirections { get; set; } = new();
    /// <summary>唯一的下游方向；无下游时为 null。</summary>
    public int? OutDirection { get; set; }
    /// <summary>该河格是否为显式源头。</summary>
    public bool IsSource { get; set; }
    /// <summary>该河格是否为合法终点。</summary>
    public bool IsSink { get; set; }
}

/// <summary>固定河段跨越地区边缘的连接口。</summary>
public sealed class RegionRiverPort
{
    /// <summary>相对于地区锚点的 Axial q 偏移。</summary>
    public int Dq { get; set; }
    /// <summary>相对于地区锚点的 Axial r 偏移。</summary>
    public int Dr { get; set; }
    /// <summary>连接口的六边形方向，0..5。</summary>
    public int Direction { get; set; }
    /// <summary>true 为进入地区，false 为流出地区。</summary>
    public bool IsInlet { get; set; }
}

/// <summary>地区中可由连通组引用的位置。</summary>
public sealed class RegionKeyPoint
{
    /// <summary>在所属配置范围内稳定且唯一的标识。</summary>
    public string ID { get; set; } = "";
    /// <summary>相对于地区锚点的 Axial q 偏移。</summary>
    public int Dq { get; set; }
    /// <summary>相对于地区锚点的 Axial r 偏移。</summary>
    public int Dr { get; set; }
}
