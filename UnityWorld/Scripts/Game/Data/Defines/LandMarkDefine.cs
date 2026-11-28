using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data;

/// <summary>自然／人工只决定创世快照阶段；运行后新增的两类地标均动态影响元气。</summary>
public class LandMarkDefine : DefineBase
{
    /// <summary>可选的 2D 探索图片，使用 src 下的逻辑 PNG 地址。</summary>
    public string ImagePath { get; set; } = "";
    /// <summary>可选的创世选址与数量规则。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InitialPlacement? InitialPlacement { get; set; }
    /// <summary>地图画法。支持 Icon、TerrainMask、Shader、Atmosphere、None。</summary>
    public string DrawType { get; set; } = "Icon";
    /// <summary>从玩家和 NPC 的地标观察中隐藏。实际占位与效果继续生效。</summary>
    public bool IsHide { get; set; }
    /// <summary>创世自然地标先于快照生成；人工地标后于快照生成，名称沿用自然／人工。</summary>
    public bool IsNatural { get; set; } = true;
    /// <summary>全局最多一个有效地标实例。</summary>
    public bool IsSingleton { get; set; }
    /// <summary>未来每秒速率修正定义引用。</summary>
    public List<string> ModifierDefineIds { get; set; } = new();
    /// <summary>
    /// 唯一元气偏移：创世自然地标一次性计入原初浓度；其余放置按每秒变化量累积，移除即停止。
    /// 即使 IsNatural=true，世界运行后新增也不瞬间改浓度、不重拍快照。
    /// </summary>
    public TileAura AuraOffset { get; set; } = new() { Jin = 0, Mu = 0, Shui = 0, Huo = 0, Tu = 0 };
}


/// <summary>内容定义可复用的创世选址配置。各内容的生成阶段负责解释候选与已有实例。</summary>
public sealed class InitialPlacement
{
    /// <summary>创世前预留的绝对 Offset 坐标。</summary>
    public List<OffsetPointDefine> ForcedPositions { get; set; } = new();
    /// <summary>普通散布的逐格入选概率。普通散布开启后，留空或填 0 时，合法格全部通过概率筛选。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PerTileChance { get; set; }
    /// <summary>超过上限时抽取数量的下界，也用于记录普通候选不足。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinCount { get; set; }
    /// <summary>总量上限，包含作者布局已有的实例。留空时不限量。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCount { get; set; }
    /// <summary>实际总量必须达到的数量。单独配置时只补缺口，必要时放宽普通选址条件。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NeedCount { get; set; }
    /// <summary>新落点距离同定义已有落点的最小六边形格距。</summary>
    public int MinSpacing { get; set; }
    /// <summary>允许或排除的最终 Tile 类型。多个正向类型满足任一即可。</summary>
    public List<string> TerrainConditions { get; set; } = new();
    /// <summary>逐条检查已规划地标的同格、近邻或顺序关系。</summary>
    public List<string> LandMarkConditions { get; set; } = new();
    /// <summary>最终 Tile 定义必须同时拥有或排除的标签。</summary>
    public List<string> TagConditions { get; set; } = new();
    /// <summary>预留内联 Lua 条件文本，当前只读写配置。</summary>
    // TODO: 接入只读创世上下文后，再实现 Lua 编译、布尔判断和错误定位。
    public string ExtraCondition { get; set; } = "";

    /// <summary>概率或正常数量区间中的任意一项开启普通散布。</summary>
    [JsonIgnore]
    internal bool EnablesOrdinaryPlacement => PerTileChance.HasValue || MinCount.HasValue || MaxCount.HasValue;

    /// <summary>需要规划普通散布或保底位置。</summary>
    [JsonIgnore]
    internal bool IsEnabled => EnablesOrdinaryPlacement || NeedCount.HasValue;
}
