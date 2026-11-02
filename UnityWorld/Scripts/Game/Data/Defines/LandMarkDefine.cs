using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data;

/// <summary>自然／人工只决定创世快照阶段；运行后新增的两类地标均动态影响元气。</summary>
public class LandMarkDefine : DefineBase
{
    /// <summary>地图配置格式版本；当前为 2。</summary>
    public int SchemaVersion { get; set; } = 2;
    /// <summary>绝对 Offset 固定位置；创世最前预留，实际实例仍按自然→快照→人工阶段创建。</summary>
    public List<OffsetPointDefine> ForcedPositions { get; set; } = new();
    /// <summary>创世自然地标先于快照生成；人工地标后于快照生成，名称沿用自然／人工。</summary>
    public bool IsNatural { get; set; } = true;
    /// <summary>全局最多一个有效地标实例。</summary>
    public bool IsSingleton { get; set; }
    /// <summary>合法可选候选的相对生成权重。</summary>
    public double SpawnWeight { get; set; } = 1;
    /// <summary>独立散布允许的类型 ID；空列表不限。</summary>
    public List<string> PlacementTypeIds { get; set; } = new();
    /// <summary>独立散布要求类型同时包含的标签。</summary>
    public List<string> PlacementTags { get; set; } = new();
    /// <summary>保留的地区拓展标签限制；不静默丢弃。</summary>
    public List<string> PlacementExtraTerrains { get; set; } = new();
    /// <summary>未来每秒速率修正定义引用。</summary>
    public List<string> ModifierDefineIds { get; set; } = new();
    /// <summary>
    /// 唯一元气偏移：创世自然地标一次性计入原初浓度；其余放置按每秒变化量累积，移除即停止。
    /// 即使 IsNatural=true，世界运行后新增也不瞬间改浓度、不重拍快照。
    /// </summary>
    public TileAura AuraOffset { get; set; } = new() { Jin = 0, Mu = 0, Shui = 0, Huo = 0, Tu = 0 };
}
