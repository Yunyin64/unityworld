using Godot;

namespace Xianxia.Client.Exploration;

public partial class ExplorationTileThumbnail : Control
{
    private ExplorationArt? _art;
    private ExplorationTile? _tile;
    private int _seed, _plane;

    public void SetTile(ExplorationArt art, ExplorationTile tile, int seed, int planeId)
    {
        _art = art;
        _tile = tile;
        _seed = seed;
        _plane = planeId;
        MouseFilter = MouseFilterEnum.Ignore;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_tile != null) _art?.DrawThumbnail(this, new Rect2(Vector2.Zero, Size), _tile, _seed, _plane);
    }
}
