using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Regions;

/// <summary>区域模板的相对坐标画布，绘制与命中测试采用同一个六边形投影。</summary>
public partial class RegionEditorCanvas : Control
{
    private readonly record struct Edit(TileId Cell, string? BeforeTerrain, string[]? BeforeLandMarks = null);

    private const float SmallestEdge = 8f;
    private static readonly Color Background = new("141e1b");
    private static readonly Color Grid = new("293b33");
    private static readonly Color Accent = new("ebcf85");
    private readonly Stack<Edit> history = new();
    private readonly HashSet<string> missingLandMarkImages = new(StringComparer.Ordinal);
    private Vector2 pan;
    private float edge = 36;
    private TileId? hover;
    private TileId? selected;
    private Vector2? pointerInCanvas;
    private bool panning;
    private bool dragHint;

    internal Dictionary<(int dq, int dr), string> Layout { get; private set; } = new();
    internal Dictionary<TileId, string> Markers { get; set; } = new();
    internal Dictionary<TileId, string[]> LandMarks { get; private set; } = new();
    internal IReadOnlyDictionary<string, LandMarkDefine> LandMarkDefinitions { get; set; } = new Dictionary<string, LandMarkDefine>();
    internal IReadOnlyDictionary<string, TileDefine> Tiles { get; set; } = new Dictionary<string, TileDefine>();
    internal string? Brush { get; set; }
    internal string? LandMarkBrush { get; set; }
    internal bool RightClickErasesTerrain { get; set; } = true;
    internal bool Eraser { get; set; }
    internal bool ShowTerrainArt { get; set; } = true;
    internal bool CanUndo => history.Count > 0;
    internal event Action? Edited;
    internal event Action<string>? Inspected;
    internal event Action? SelectionChanged;
    internal TileId? Selected => selected;

    internal void Load(Dictionary<(int, int), string> layout, Dictionary<TileId, string> markers, Dictionary<TileId, string[]> landMarks)
    {
        Layout = layout;
        Markers = markers;
        LandMarks = new(landMarks);
        selected = hover = null;
        history.Clear();
        Fit();
    }

    internal void Fit()
    {
        var min = Vector2.Zero;
        var max = Vector2.Zero;
        foreach (var cell in Layout.Keys)
        {
            var center = ExplorationHexProjection.Project(new TileId(cell.dq, cell.dr), 1);
            min = min.Min(center);
            max = max.Max(center);
        }
        Vector2 units = max - min + new Vector2(3.5f, 3);
        edge = Mathf.Clamp(Mathf.Min(Size.X / units.X, Size.Y / units.Y), SmallestEdge, 57);
        pan = -(min + max) * (edge / 2);
        QueueRedraw();
    }

    internal void Zoom(float factor) => ZoomAt(factor, Size / 2);

    private Vector2 Origin => Size / 2 + pan;
    private TileId CellAt(Vector2 point) => ExplorationHexProjection.Unproject(point - Origin, edge);

    /// <summary>从视口输入跟踪拖放指针，兼容界面缩放和视口转发的鼠标输入。</summary>
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouse mouse) pointerInCanvas = GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
    }

    private void ZoomAt(float factor, Vector2 point)
    {
        float next = Mathf.Clamp(edge * factor, SmallestEdge, 120);
        pan += (point - Origin) * (1 - next / edge);
        edge = next;
        QueueRedraw();
    }

    internal void Undo()
    {
        if (!history.TryPop(out var edit)) return;
        Select(edit.Cell);
        if (edit.BeforeLandMarks is { } landMarks) ApplyLandMarks(edit.Cell, landMarks);
        else Apply(edit.Cell, edit.BeforeTerrain);
    }

    private void Paint(TileId cell, string? tileId)
    {
        Layout.TryGetValue((cell.Q, cell.R), out var previous);
        Select(cell);
        if (previous != tileId)
        {
            history.Push(new(cell, previous));
            Apply(cell, tileId);
        }
        else QueueRedraw();
        Report(cell);
    }

    private void Apply(TileId cell, string? value)
    {
        if (value == null) Layout.Remove((cell.Q, cell.R));
        else Layout[(cell.Q, cell.R)] = value;
        QueueRedraw();
        Edited?.Invoke();
        Report(cell);
    }

    private void Report(TileId cell)
    {
        Layout.TryGetValue((cell.Q, cell.R), out var id);
        string name = id != null && Tiles.TryGetValue(id, out var tile) ? $"{tile.DisplayName}  ·  {id}" : id ?? "空格";
        string suffix = LandMarks.TryGetValue(cell, out var items) ? "  ·  地标 " + string.Join("、", items.Select(LandMarkName)) : "";
        if (Markers.TryGetValue(cell, out var marker)) suffix += $"  ·  {marker}";
        Inspected?.Invoke($"({cell.Q}, {cell.R})  ·  {name}{suffix}");
    }

    private string LandMarkName(string id) => LandMarkDefinitions.TryGetValue(id, out var definition) ? definition.DisplayName : id;

    private void Select(TileId cell)
    {
        if (selected == cell) return;
        selected = cell;
        QueueRedraw();
        SelectionChanged?.Invoke();
    }

    private string? LandMarkRejection(TileId cell, string id)
    {
        if (!Layout.ContainsKey((cell.Q, cell.R))) return $"({cell.Q}, {cell.R}) 是空格，先放一个地块。";
        if (LandMarks.GetValueOrDefault(cell)?.Contains(id) == true) return $"本格已有「{LandMarkName(id)}」。";
        if (LandMarkDefinitions[id].IsSingleton && LandMarks.Values.Any(items => items.Contains(id)))
            return $"单例地标「{LandMarkName(id)}」已放在本模板的其它格。";
        return null;
    }

    private void PlaceLandMark(TileId cell, string id)
    {
        Select(cell);
        if (LandMarkRejection(cell, id) is { } reason) { Inspected?.Invoke(reason); return; }
        string[] previous = LandMarks.GetValueOrDefault(cell) ?? [];
        history.Push(new(cell, null, previous));
        ApplyLandMarks(cell, [.. previous, id]);
    }

    internal void RemoveLandMark(TileId cell, string id)
    {
        if (!LandMarks.TryGetValue(cell, out var previous) || !previous.Contains(id)) return;
        history.Push(new(cell, null, previous));
        ApplyLandMarks(cell, previous.Where(item => item != id).ToArray());
    }

    private void ApplyLandMarks(TileId cell, string[] items)
    {
        if (items.Length == 0) LandMarks.Remove(cell);
        else LandMarks[cell] = items;
        QueueRedraw();
        Edited?.Invoke();
        Report(cell);
    }

    /// <summary>处理画笔、选择、平移、缩放和擦除。</summary>
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            if (panning && (motion.ButtonMask & MouseButtonMask.Middle) != 0) pan += motion.Relative;
            hover = CellAt(motion.Position);
            Report(hover.Value);
            QueueRedraw();
            return;
        }
        if (input is not InputEventMouseButton mouse) return;
        if (mouse.ButtonIndex == MouseButton.Middle) panning = mouse.Pressed;
        if (!mouse.Pressed) return;
        var cell = CellAt(mouse.Position);
        switch (mouse.ButtonIndex)
        {
            case MouseButton.WheelUp: ZoomAt(1.16f, mouse.Position); break;
            case MouseButton.WheelDown: ZoomAt(1 / 1.16f, mouse.Position); break;
            case MouseButton.Right:
                if (RightClickErasesTerrain) Paint(cell, null);
                else { Select(cell); Report(cell); }
                break;
            case MouseButton.Left:
                if (LandMarkBrush is { } landMark) PlaceLandMark(cell, landMark);
                else if (Eraser || Brush != null) Paint(cell, Eraser ? null : Brush);
                else { Select(cell); Report(cell); }
                break;
            case MouseButton.Middle: break;
            default: return;
        }
        AcceptEvent();
    }

    private static string? IdFromDrag(Variant data, string key)
    {
        if (data.VariantType != Variant.Type.Dictionary || !data.AsGodotDictionary().TryGetValue(key, out var value))
            return null;
        return value.AsString();
    }

    /// <summary>接收地形或合法的地标落点，并突出显示目标。</summary>
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var cell = CellAt(pointerInCanvas ?? atPosition);
        string? reason = null;
        if (IdFromDrag(data, "region_landmark_id") is { } landmark && LandMarkDefinitions.ContainsKey(landmark))
            reason = LandMarkRejection(cell, landmark);
        else if (IdFromDrag(data, "region_tile_id") is not { } tile || !Tiles.ContainsKey(tile)) return false;
        dragHint = reason == null;
        hover = cell;
        if (reason == null) Report(cell);
        else Inspected?.Invoke(reason);
        QueueRedraw();
        return dragHint;
    }

    /// <summary>按落点编辑地形或追加地标。</summary>
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var cell = CellAt(pointerInCanvas ?? atPosition);
        if (IdFromDrag(data, "region_tile_id") is { } id && Tiles.ContainsKey(id)) Paint(cell, id);
        else if (IdFromDrag(data, "region_landmark_id") is { } landmark && LandMarkDefinitions.ContainsKey(landmark))
            PlaceLandMark(cell, landmark);
    }

    /// <summary>离开画布或完成拖动后清除悬停提示。</summary>
    public override void _Notification(int what)
    {
        if (what != NotificationMouseExit && what != NotificationDragEnd) return;
        hover = null;
        dragHint = false;
        QueueRedraw();
    }

    /// <summary>绘制可见网格、地形、坐标原点和引用标记。</summary>
    public override void _Draw()
    {
        ExplorationArt.ClearLandmarkShaders(this);
        DrawRect(new Rect2(Vector2.Zero, Size), Background);
        Vector2 origin = Origin;
        var corners = new[] { CellAt(Vector2.Zero), CellAt(new Vector2(Size.X, 0)), CellAt(new Vector2(0, Size.Y)), CellAt(Size) };
        var visible = new Rect2(-edge, -edge, Size.X + edge * 2, Size.Y + edge * 2);
        int minQ = corners.Min(cell => cell.Q) - 1, maxQ = corners.Max(cell => cell.Q) + 1;
        int minR = corners.Min(cell => cell.R) - 1, maxR = corners.Max(cell => cell.R) + 1;
        var outline = ExplorationHexProjection.Outline(Vector2.Zero, edge);
        var gridLines = new List<Vector2>();
        var font = GetThemeDefaultFont();
        var terrainArt = ShowTerrainArt ? GameBootstrap.Assets.Terrain : null;
        for (int q = minQ; q <= maxQ; q++)
        for (int r = minR; r <= maxR; r++)
        {
            var cell = new TileId(q, r);
            Vector2 center = origin + ExplorationHexProjection.Project(cell, edge);
            if (!visible.HasPoint(center)) continue;
            if (Layout.TryGetValue((q, r), out var id) && Tiles.TryGetValue(id, out var tile))
            {
                var points = ExplorationHexProjection.Outline(center, edge * 0.94f);
                Color terrain = RegionTileCard.TileColor(tile);
                var material = terrainArt?.Resolve(tile.ID, tile.AssetRef, cell);
                if (material is { } artwork) artwork.DrawHex(this, points);
                else DrawColoredPolygon(points, terrain);
                DrawPolyline([.. points, points[0]], terrain.Lightened(0.17f), 1, true);
                if (material == null && edge >= 15 && tile.Symbol.Length > 0)
                {
                    int textSize = (int)Mathf.Clamp(edge * 0.45f, 11, 21);
                    float width = font.GetStringSize(tile.Symbol, fontSize: textSize).X;
                    DrawString(font, center + new Vector2(-width / 2, textSize / 3f), tile.Symbol,
                        fontSize: textSize, modulate: terrain.Luminance > 0.43f ? new Color("16251f") : new Color("f4efdc"));
                }
            }
            else
            {
                // 三条边足够组成共享网格，避免每条内部边重复绘制。
                for (int side = 0; side < 3; side++)
                {
                    gridLines.Add(center + outline[side]);
                    gridLines.Add(center + outline[side + 1]);
                }
            }
            if (Markers.ContainsKey(cell))
                DrawCircle(center + new Vector2(edge * 0.48f, -edge * 0.39f), Mathf.Clamp(edge * 0.12f, 2, 5), Accent);
            if (cell == TileId.Zero)
            {
                DrawArc(center, Mathf.Clamp(edge * 0.28f, 3, 10), 0, Mathf.Tau, 24, Accent, 1, true);
                DrawCircle(center, 2, Accent);
            }
            if (LandMarks.TryGetValue(cell, out var landMarks)) DrawLandMarks(center, landMarks);
        }
        if (gridLines.Count > 0) DrawMultiline(gridLines.ToArray(), Grid, 1);
        DrawHighlight(selected, Accent);
        DrawHighlight(hover, dragHint ? new Color("e4f5ba") : new Color("aec2ae"));
    }

    internal Texture2D? LandMarkTexture(string id)
    {
        if (!LandMarkDefinitions.TryGetValue(id, out var landmark) || string.IsNullOrWhiteSpace(landmark.ImagePath)) return null;
        var image = GameBootstrap.Assets.Texture(landmark.ImagePath);
        if (image == null && missingLandMarkImages.Add(landmark.ImagePath))
            GD.PushWarning($"[RegionEditor] 地标 {id} 的图片不可用：{landmark.ImagePath}");
        return image;
    }

    private void DrawLandMarks(Vector2 center, string[] landMarks)
    {
        if (ShowTerrainArt)
        {
            ExplorationArt.DrawMapLandmarks(this, center, edge, landMarks.Select(id =>
            {
                string type = LandMarkDefinitions.TryGetValue(id, out var define) ? define.DrawType : "Icon";
                return (type, type is "Icon" or "TerrainMask" ? LandMarkTexture(id) : null);
            }).ToArray());
            return;
        }
        Vector2 pin = center + new Vector2(-edge * 0.44f, edge * 0.27f);
        if (edge < 17)
        {
            DrawCircle(pin, 3, Accent);
            return;
        }
        float radius = Mathf.Clamp(edge * 0.26f, 7, 14);
        DrawLandMarkSymbol(pin, radius);
        if (landMarks.Length > 1)
            DrawLandMarkCount(pin + new Vector2(radius * 0.75f, radius * 0.55f), landMarks.Length - 1);
    }

    private void DrawLandMarkSymbol(Vector2 pin, float radius)
    {
        DrawCircle(pin, radius, Background);
        DrawArc(pin, radius, 0, Mathf.Tau, 24, Accent, 1, true);
        DrawString(GetThemeDefaultFont(), pin + new Vector2(-5, 4), "地", fontSize: 11, modulate: Accent);
    }

    private void DrawLandMarkCount(Vector2 position, int number)
    {
        string count = "+" + number;
        var font = GetThemeDefaultFont();
        DrawStringOutline(font, position, count, fontSize: 13, size: 4, modulate: Background);
        DrawString(font, position, count, fontSize: 13, modulate: Accent);
    }

    private void DrawHighlight(TileId? cell, Color color)
    {
        if (cell == null) return;
        var points = ExplorationHexProjection.Outline(Origin + ExplorationHexProjection.Project(cell.Value, edge), edge * 0.97f);
        DrawPolyline([.. points, points[0]], color, 2, true);
    }
}
