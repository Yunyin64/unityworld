using System.Collections.Frozen;

namespace UnityWorld.Game.Domain;

/// <summary>作者拼图的运行实例；实际格集合不包含外接框内未配置的孔洞。</summary>
public sealed class Region
{
    public int Id { get; }
    public int PlaneId { get; }
    public string DefineId { get; }
    public int GenerationIndex { get; }
    public TileId Anchor { get; }
    public bool IsFixed { get; }
    public IReadOnlySet<TileId> OwnedTiles { get; }
    private readonly List<int> _landMarkIds = new();
    /// <summary>地区内已关联的地标实例，生成时与固定地标布局逐项对应。</summary>
    public IReadOnlyList<int> LandMarkIds => _landMarkIds.AsReadOnly();

    internal void AddLandMark(int id)
    {
        if (!_landMarkIds.Contains(id)) _landMarkIds.Add(id);
    }
    internal void RemoveLandMark(int id) => _landMarkIds.Remove(id);

    internal Region(int id, int planeId, string defineId, int generationIndex, TileId anchor, IEnumerable<TileId> tiles, bool isFixed = false)
    {
        Id = id; PlaneId = planeId; DefineId = defineId; GenerationIndex = generationIndex;
        Anchor = anchor; IsFixed = isFixed; OwnedTiles = tiles.ToFrozenSet();
    }
}
