using System;
using Godot;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>Displays one character's visible tiles. Camera and selection never issue world commands.</summary>
[Tool]
public partial class ExplorationMap : Control
{
    private static readonly Color GridInk = new("373F3BCC");
    private static readonly Color CandidateInk = new("514828");
    private static readonly Color CandidateGold = new("EAD18B");
    private static readonly Color SelectionInk = new("4C3724");
    private static readonly Color SelectionGold = new("FFEDB3");
    private static readonly Color LockedGold = new("FEB94D");
    private const float WalkSeconds = 0.46f;
    private const float FollowSeconds = 0.28f;
    private const float ClickSlop = 5f;

    private Control _surface = null!, _outgoingRim = null!, _incomingRim = null!;
    private ExplorationArt? _art;
    private ExplorationSnapshot? _state;
    private ExplorationSnapshot? _previous;
    private float _travelTime;
    private Vector2 _startingPan;
    private TileId _selected;
    private TileId? _hovered;
    private TileId? _pressedTile;
    private Vector2 _pressPoint;
    private Vector2 _pan;
    private bool _dragging;
    private bool _facesLeft;
    public float Zoom { get; private set; } = 1f;
    public bool IsTravelAnimating => _previous != null;
    public event Action<TileId>? TileSelected;
    public event Action<float>? ZoomChanged;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            Resized += QueueRedraw;
            return;
        }
        _surface = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _outgoingRim = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _incomingRim = new Control { MouseFilter = MouseFilterEnum.Ignore };
        // The map covers the window behind the HUD. All three terrain layers share the
        // same camera and clip at the true window bounds.
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        // 图集裁片会从数百像素缩到一个小格；mipmap 保留山形并滤掉远景碎点。
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        foreach (var layer in new[] { _outgoingRim, _incomingRim, _surface })
        {
            AddChild(layer);
            layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }
        _outgoingRim.Draw += () => PaintRim(_outgoingRim, _previous, _state);
        _incomingRim.Draw += () => PaintRim(_incomingRim, _state, _previous);
        _surface.Draw += Paint;
        Resized += OnResize;
        MouseExited += ClearHover;
        SetProcess(false);
        RedrawMap();
    }

    // 编辑器用实际地图的投影展示中性网格，不加载世界、NPC 或伪造地形数据。
    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        float edge = GetMapEdgeLength();
        for (int q = -4; q <= 4; q++)
        {
            for (int r = Math.Max(-4, -q - 4); r <= Math.Min(4, -q + 4); r++)
            {
                var center = Size / 2 + ExplorationHexProjection.Project(new TileId(q, r), edge);
                var points = ExplorationHexProjection.Outline(center, edge * 0.97f);
                DrawColoredPolygon(points, new Color(0.21f, 0.32f, 0.27f, 0.18f));
                for (int i = 0; i < points.Length; i++)
                    DrawLine(points[i], points[(i + 1) % points.Length], new Color(0.22f, 0.3f, 0.24f, 0.45f), 1, true);
            }
        }
    }

    public void Configure(ExplorationArt art)
    {
        _art = art;
        RedrawMap();
    }

    public void SetState(ExplorationSnapshot state, TileId selected)
    {
        var old = _state;
        bool sameActor = old != null && old.NpcId == state.NpcId && old.PlaneId == state.PlaneId && old.Seed == state.Seed;
        if (sameActor && old!.Position != state.Position && old.CanDrawActor && state.CanDrawActor
            && old.Position.DistanceTo(state.Position) == 1)
        {
            // The core has already committed the arrival. Start from the *displayed*
            // position and pan of the previous view; no render frame advances the world.
            _startingPan = GetReachedTileCenter() - Size * 0.5f;
            int horizontalStep = state.Position.Q - old.Position.Q;
            if (horizontalStep != 0) _facesLeft = horizontalStep < 0;
            _previous = old;
            _travelTime = 0;
            _pan = Vector2.Zero;
            _hovered = null;
            _pressedTile = null;
            SetProcess(true);
        }
        else if (!sameActor || old!.Position != state.Position || !state.CanDrawActor)
        {
            _pan = Vector2.Zero;
            if (!sameActor) _facesLeft = false;
            _previous = null;
            _hovered = null;
            _pressedTile = null;
            SetProcess(false);
        }
        _state = state;
        _selected = selected;
        RedrawMap();
    }

    public override void _Process(double delta)
    {
        if (_previous == null) return;
        _travelTime += (float)delta;
        if (_travelTime >= WalkSeconds + FollowSeconds)
        {
            _previous = null;
            SetProcess(false);
        }
        RedrawMap();
    }

    public float GetMapEdgeLength() => Math.Max(1f, 45f * Math.Min(Size.X / 1600f, Size.Y / 900f)) * Zoom;
    private float WalkFraction => Mathf.SmoothStep(0f, 1f, Mathf.Clamp(_travelTime / WalkSeconds, 0f, 1f));
    private float FollowFraction => Mathf.SmoothStep(0f, 1f, Mathf.Clamp((_travelTime - WalkSeconds) / FollowSeconds, 0f, 1f));
    private Vector2 TravelStep => _previous != null && _state != null
        ? ExplorationHexProjection.Project(_state.Position - _previous.Position, GetMapEdgeLength()) : Vector2.Zero;
    private Vector2 ActorRelativeToTarget => _previous != null ? -TravelStep * (1f - WalkFraction) : Vector2.Zero;
    private Vector2 CameraTravelOffset => _previous != null ? (_startingPan + TravelStep) * (1f - FollowFraction) : Vector2.Zero;
    private Vector2 GroundToFoot => Vector2.Down * (GetMapEdgeLength() * 0.24f);
    private float StepLift => _previous != null && _travelTime < WalkSeconds
        ? MathF.Pow(MathF.Sin(MathF.Tau * _travelTime / WalkSeconds), 2f) * GetMapEdgeLength() * 0.11f : 0f;
    // Clamp the actor's displayed feet, including the stride and camera follow. User pan is
    // a separate offset, so a wheel zoom or resize cannot accumulate motion beyond this box.
    private Vector2 FootOffset => CameraTravelOffset + ActorRelativeToTarget + GroundToFoot + Vector2.Up * StepLift;
    private Vector2 ClampPan(Vector2 desired)
    {
        var limit = Size * new Vector2(0.18f, 0.16f);
        var offset = FootOffset;
        return (desired + offset).Clamp(-limit, limit) - offset;
    }
    public Vector2 GetReachedTileCenter() => Size * 0.5f + ClampPan(_pan) + CameraTravelOffset;
    public Vector2 GetActorFootPoint() => GetReachedTileCenter() + ActorRelativeToTarget + GroundToFoot + Vector2.Up * StepLift;

    private void OnResize()
    {
        _pan = ClampPan(_pan);
        _pressedTile = null;
        RedrawMap();
    }

    private void RedrawMap()
    {
        _outgoingRim.Visible = _previous != null;
        _incomingRim.Visible = _previous != null;
        if (_previous != null)
        {
            float fade = WalkFraction;
            _outgoingRim.SelfModulate = new Color(1f, 1f, 1f, 1f - fade);
            _incomingRim.SelfModulate = new Color(1f, 1f, 1f, fade);
            _outgoingRim.QueueRedraw();
            _incomingRim.QueueRedraw();
        }
        _surface.QueueRedraw();
    }

    public Vector2 TileCenter(TileId id) => _state == null ? GetReachedTileCenter() :
        GetReachedTileCenter() + ExplorationHexProjection.Project(id - _state.Position, GetMapEdgeLength());

    public TileId? PickTile(Vector2 localPoint)
    {
        if (_state == null || !new Rect2(Vector2.Zero, Size).HasPoint(localPoint)) return null;
        var cell = _state.Position + ExplorationHexProjection.Unproject(localPoint - GetReachedTileCenter(), GetMapEdgeLength());
        if (!_state.Tiles.ContainsKey(cell)) return null;
        return _previous == null || _previous.Tiles.ContainsKey(cell) || WalkFraction > 0.02f ? cell : null;
    }

    public void ChangeZoom(float delta) => ZoomAround(delta, Size * 0.5f);

    private void ZoomAround(float delta, Vector2 anchor)
    {
        float next = Mathf.Clamp(Zoom + delta, 0.5f, 2f);
        if (Mathf.IsEqualApprox(Zoom, next)) return;
        Vector2 worldAnchor = (anchor - GetReachedTileCenter()) / GetMapEdgeLength();
        Zoom = next;
        _pan = ClampPan(anchor - worldAnchor * GetMapEdgeLength() - Size * 0.5f - CameraTravelOffset);
        _hovered = null;
        _pressedTile = null;
        ZoomChanged?.Invoke(next);
        RedrawMap();
    }

    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            if (_pressedTile.HasValue && _pressPoint.DistanceTo(motion.Position) > ClickSlop) _pressedTile = null;
            if (_dragging)
            {
                _pan = ClampPan(ClampPan(_pan) + motion.Relative);
                RedrawMap();
                AcceptEvent();
            }
            else
            {
                var hovered = PickTile(motion.Position);
                if (hovered != _hovered) { _hovered = hovered; _surface.QueueRedraw(); }
            }
            return;
        }
        if (input is not InputEventMouseButton mouse) return;
        switch (mouse.ButtonIndex)
        {
            case MouseButton.Left:
                if (mouse.Pressed)
                {
                    _pressedTile = _dragging ? null : PickTile(mouse.Position);
                    _pressPoint = mouse.Position;
                }
                else
                {
                    // Godot marks the second press as DoubleClick. Both complete press/release
                    // pairs reach the view; it decides whether the second click confirms travel.
                    var pressed = _pressedTile;
                    _pressedTile = null;
                    if (!_dragging && pressed is { } tile && _pressPoint.DistanceTo(mouse.Position) <= ClickSlop
                        && PickTile(mouse.Position) == tile) TileSelected?.Invoke(tile);
                }
                break;
            case MouseButton.Middle:
                _dragging = mouse.Pressed;
                _pressedTile = null;
                _hovered = null;
                MouseDefaultCursorShape = _dragging ? CursorShape.Drag : CursorShape.Arrow;
                _surface.QueueRedraw();
                break;
            case MouseButton.WheelUp when mouse.Pressed: ZoomAround(0.25f, mouse.Position); break;
            case MouseButton.WheelDown when mouse.Pressed: ZoomAround(-0.25f, mouse.Position); break;
            default: return;
        }
        AcceptEvent();
    }

    private void ClearHover()
    {
        if (!_hovered.HasValue) return;
        _hovered = null;
        _surface.QueueRedraw();
    }

    private void Paint()
    {
        var state = _state;
        if (state == null || _art == null) return;
        float edge = GetMapEdgeLength();
        PaintTiles(_surface, state, _previous, common: true);
        foreach (var candidate in state.Candidates)
            DrawRing(candidate, CandidateInk, CandidateGold, 2.5f, 1.35f);
        if (_hovered.HasValue && _hovered != _selected)
            DrawRing(_hovered.Value, GridInk, new Color("EDEDD499"), 2.2f, 1.35f);
        if (state.Tiles.ContainsKey(_selected))
            DrawRing(_selected, SelectionInk, SelectionGold, 4.2f, 2.5f);
        if (state.Action.State == "move" && state.Action.Target is { } target && state.Tiles.ContainsKey(target))
            DrawDestination(state.Position, target, edge);
        if (state.CanDrawActor) DrawActor(GetActorFootPoint(), edge, state.Name);
    }

    private void PaintRim(Control canvas, ExplorationSnapshot? source, ExplorationSnapshot? other)
    {
        if (source != null && other != null) PaintTiles(canvas, source, other, common: false);
    }

    private void PaintTiles(Control canvas, ExplorationSnapshot source, ExplorationSnapshot? other, bool common)
    {
        if (_state == null || _art == null) return;
        float edge = GetMapEdgeLength();
        var origin = GetReachedTileCenter();
        var bounds = new Rect2(Vector2.Zero, Size).Grow(edge * 2f);
        foreach (var item in source.Tiles.Values)
        {
            var tile = item.Tile;
            if (other != null && other.Tiles.ContainsKey(tile.Id) != common) continue;
            var center = origin + ExplorationHexProjection.Project(tile.Id - _state.Position, edge);
            if (!bounds.HasPoint(center)) continue;
            var outline = ExplorationHexProjection.Outline(center, edge);
            _art.DrawGround(canvas, outline, item, source.Seed, source.PlaneId);
            DrawWater(canvas, item, center, edge, outline);
            Stroke(canvas, outline, GridInk, Math.Max(0.8f, Zoom));
            DrawRegionEdges(canvas, source, item, outline);
            if (tile.LandMarkId.HasValue) DrawLandmark(canvas, center, edge);
        }
    }

    private void DrawRing(TileId id, Color outer, Color inner, float outerWidth, float innerWidth)
    {
        var points = ExplorationHexProjection.Outline(TileCenter(id), GetMapEdgeLength() * 0.99f);
        Stroke(_surface, points, outer, outerWidth * Zoom);
        Stroke(_surface, points, inner, innerWidth * Zoom);
    }

    private static void Stroke(Control canvas, Vector2[] points, Color color, float width)
    {
        canvas.DrawPolyline(points, color, width, true);
        canvas.DrawLine(points[^1], points[0], color, width, true);
    }

    private void DrawWater(Control canvas, ExplorationTile place, Vector2 center, float edge, Vector2[] outline)
    {
        var water = place.Tile.Water;
        // A broad river side cell or lake interior can have no centerline. Its surface
        // belongs to the water body regardless of whether this tile carries a flow edge.
        if (water.Kind != place.Tile.TypeId)
        {
            var tint = water.Kind switch
            {
                "river" => new Color("579CAEC9"), "lake" => new Color("5AA5A9D9"),
                "ocean" => new Color("39728BCF"), _ => Colors.Transparent
            };
            if (tint.A > 0) canvas.DrawColoredPolygon(outline, tint);
        }
        for (int direction = 0; direction < TileId.Directions.Length; direction++)
        {
            bool outgoing = water.OutDirection == direction;
            if (!outgoing && (water.IncomingMask & (1 << direction)) == 0) continue;
            // Each side draws only its half of the actual connection, including at the
            // edge of the visible area. Direction numbers stay in the core's axial basis.
            Vector2 halfStep = ExplorationHexProjection.Project(TileId.Directions[direction], edge) * 0.5f;
            canvas.DrawLine(center, center + halfStep, new Color("285C70CE"), 3.2f * Zoom, true);
            canvas.DrawLine(center, center + halfStep, new Color("A2D4CE"), 1.45f * Zoom, true);
            if (outgoing && Zoom >= 1)
            {
                var tip = center + halfStep * 0.72f;
                var tangent = halfStep.Normalized();
                var wing = new Vector2(-tangent.Y, tangent.X) * (2f * Zoom);
                canvas.DrawPolyline(new[] { tip - tangent * (4f * Zoom) + wing, tip, tip - tangent * (4f * Zoom) - wing },
                    new Color("DBEAD2"), 1f * Zoom, true);
            }
        }
    }

    private void DrawRegionEdges(Control canvas, ExplorationSnapshot state, ExplorationTile place, Vector2[] outline)
    {
        if (place.Tile.RegionId is not int region) return;
        for (int direction = 0; direction < 6; direction++)
        {
            if (!state.Tiles.TryGetValue(place.Tile.Id.Neighbor(direction), out var neighbor) || neighbor.Tile.RegionId == region) continue;
            canvas.DrawLine(outline[direction], outline[(direction + 1) % 6], new Color("786E48C0"), 1.6f * Zoom, true);
        }
    }

    private void DrawLandmark(Control canvas, Vector2 center, float edge)
    {
        Vector2 location = center + new Vector2(edge * 0.27f, -edge * 0.16f);
        float size = Math.Max(2f, edge * 0.095f);
        var points = new[] { location + Vector2.Up * size, location + Vector2.Right * size * 0.8f,
            location + Vector2.Down * size, location + Vector2.Left * size * 0.8f };
        canvas.DrawColoredPolygon(points, new Color("F5DC88"));
        Stroke(canvas, points, new Color("51452B"), Math.Max(0.75f, Zoom));
    }

    private void DrawDestination(TileId from, TileId target, float edge)
    {
        Vector2 origin = TileCenter(from), destination = TileCenter(target);
        DrawRing(target, SelectionInk, LockedGold, 4.3f, 2.2f);
        foreach (float fraction in new[] { 0.53f, 0.67f, 0.81f })
            _surface.DrawCircle(origin.Lerp(destination, fraction), Math.Max(1.4f, edge * 0.036f), LockedGold);
        float radius = edge * 0.115f;
        var pin = destination + Vector2.Up * radius;
        _surface.DrawColoredPolygon(new[] { pin + Vector2.Left * radius, pin + Vector2.Right * radius, destination + Vector2.Down * radius }, LockedGold);
        _surface.DrawCircle(pin, radius, LockedGold);
        _surface.DrawCircle(pin, radius * 0.42f, SelectionInk);
    }

    private void DrawActor(Vector2 foot, float edge, string name)
    {
        var ground = foot + Vector2.Down * StepLift;
        _surface.DrawEllipse(ground, edge * 0.47f, edge * 0.105f, new Color("16292275"));
        var texture = _art!.Actor;
        if (texture != null)
        {
            float height = edge * 1.44f;
            float width = height * texture.GetWidth() / texture.GetHeight();
            // The original art faces right. A purely vertical step keeps the last facing.
            _surface.DrawSetTransform(foot, 0f, new Vector2(_facesLeft ? -1f : 1f, 1f));
            _surface.DrawTextureRect(texture, new Rect2(-width * 0.5f, -height, width, height), false);
            _surface.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
            return;
        }
        var pos = foot - GroundToFoot + new Vector2(-edge, 4f);
        var font = _surface.GetThemeDefaultFont();
        _surface.DrawStringOutline(font, pos, name, HorizontalAlignment.Center, edge * 2f, 14, 3, Colors.Black);
        _surface.DrawString(font, pos, name, HorizontalAlignment.Center, edge * 2f, 14, Colors.White);
    }
}
