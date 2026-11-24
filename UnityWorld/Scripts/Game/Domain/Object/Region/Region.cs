using System.Collections.Frozen;

namespace UnityWorld.Game.Domain;

/// <summary>作者拼图的运行实例；实际格集合不包含外接框内未配置的孔洞。</summary>
public sealed class Region
{
    /// <summary>地区实例编号</summary>
    public int Id { get; }
    /// <summary>所属位面编号</summary>
    public int PlaneId { get; }
    /// <summary>地区模板定义编号</summary>
    public string DefineId { get; }
    /// <summary>本次生成中的地区顺序号</summary>
    public int GenerationIndex { get; }
    /// <summary>地区在位面的锚点</summary>
    public TileId Anchor { get; }
    /// <summary>地区是否按强制位置放置</summary>
    public bool IsFixed { get; }
    /// <summary>地区实际覆盖的地块集合</summary>
    public IReadOnlySet<TileId> OwnedTiles { get; }
    /// <summary>地区已关联的地标编号</summary>
    private readonly List<int> _landMarkIds = new();
    /// <summary>地区内已关联的地标实例，生成时与固定地标布局逐项对应。</summary>
    public IReadOnlyList<int> LandMarkIds => _landMarkIds.AsReadOnly();

    /// <summary>向地区登记尚未关联的地标</summary>
    internal void AddLandMark(int id)
    {
        if (!_landMarkIds.Contains(id)) _landMarkIds.Add(id);
    }
    /// <summary>解除指定地标与地区的关联</summary>
    internal void RemoveLandMark(int id) => _landMarkIds.Remove(id);

    /// <summary>创建地区并冻结其实际拥有的格子集合</summary>
    internal Region(int id, int planeId, string defineId, int generationIndex, TileId anchor, IEnumerable<TileId> tiles, bool isFixed = false)
    {
        Id = id; PlaneId = planeId; DefineId = defineId; GenerationIndex = generationIndex;
        Anchor = anchor; IsFixed = isFixed; OwnedTiles = tiles.ToFrozenSet();
    }
}
