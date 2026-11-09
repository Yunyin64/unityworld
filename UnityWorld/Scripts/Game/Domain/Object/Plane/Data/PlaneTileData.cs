namespace UnityWorld.Game.Domain;

/// <summary>位面的唯一地块数据；生成器与运行时查询使用同一份集合。</summary>
public sealed class PlaneTileData
{
    internal Dictionary<TileId, Tile> Entries { get; } = new();
    public IReadOnlyDictionary<TileId, Tile> Tiles => Entries;
}
