using UnityWorld.Game.Data;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain;

/// <summary>位面自己的地块系统：管理地块，并提供通行与空间查询。</summary>
public sealed class PlaneSystemTile
{
    private readonly Plane _plane;
    internal PlaneSystemTile(Plane plane) { _plane = plane; }

    public Tile GetTile(TileId id) => _plane.TileData.Entries.GetValueOrDefault(id);
    public void SetTile(Tile tile) { WorldMgr.EnsureMutable(); _plane.TileData.Entries[tile.Id] = tile; }
    public bool RemoveTile(TileId id) { WorldMgr.EnsureMutable(); return _plane.TileData.Entries.Remove(id); }

    public IReadOnlyList<string> GetMovementModes(Tile tile)
    {
        var define = tile == null ? null : GameDataMgr.Instance?.MapCatalog?.Tiles.GetValueOrDefault(tile.TypeId);
        var modes = tile?.AllowedMovement ?? (IReadOnlyList<string>)define?.MovementModes ?? Array.Empty<string>();
        return Array.AsReadOnly(modes.ToArray());
    }

    public bool CanWalk(TileId id)
    {
        if (_plane.State != PlaneTypes.PlaneState.Active || !_plane.IsInBounds(id)) return false;
        var tile = GetTile(id);
        var define = tile == null ? null : GameDataMgr.Instance?.MapCatalog?.Tiles.GetValueOrDefault(tile.TypeId);
        if (define == null || tile.BlockAllMovement || define.BlocksAllMovement || !GetMovementModes(tile).Contains("walk")) return false;
        var water = tile.Water;
        return define.WaterKind == "none" && water.Kind == "none" && !water.Active;
    }

    /// <summary>只筛选实际地块；大半径不创建坐标网格，返回独立Tile副本。</summary>
    public IReadOnlyList<Tile> GetTilesInRange(TileId center, double radius)
    {
        if (!double.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        radius = Math.Floor(radius);
        return Array.AsReadOnly(_plane.Tiles.Values.Where(tile => _plane.IsInBounds(tile.Id) && Distance(center, tile.Id) <= radius)
            .OrderBy(tile => Plane.AxialToOffset(tile.Id).row).ThenBy(tile => Plane.AxialToOffset(tile.Id).col)
            .Select(tile => tile.CopyForObservation()).ToArray());
    }

    private static long Distance(TileId a, TileId b)
    {
        long q = (long)a.Q - b.Q, r = (long)a.R - b.R;
        return (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;
    }
}
