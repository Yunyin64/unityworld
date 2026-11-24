namespace UnityWorld.Game.Domain;

/// <summary>位面的唯一地块数据；生成器与运行时查询使用同一份集合。</summary>
public sealed class PlaneTileData
{
    /// <summary>按六边形坐标索引的地块</summary>
    internal Dictionary<TileId, Tile> Entries { get; } = new();
    /// <summary>位面地块的只读视图</summary>
    public IReadOnlyDictionary<TileId, Tile> Tiles => Entries;
}
