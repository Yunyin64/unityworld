using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using Xianxia.Client.Templates;

namespace Xianxia.Client.UI.Templates;

/// <summary>Definition-backed backpack editor. Drag previews run operations on a detached draft; commits are events.</summary>
public partial class TemplateBackpackView : Control
{
    private TemplateCatalog? _catalog;
    private TemplateDraft? _draft;
    private BackpackLevelDefine? _layout;
    private HashSet<BackpackCell> _valid = new();
    private HashSet<BackpackCell> _problems = new();
    private readonly Dictionary<string, CardDefine?> _cards = new(StringComparer.Ordinal);
    private ObserverArtCatalog? _art;
    private bool _readOnly;
    private string? _selected;
    private List<BackpackCell> _previewCells = new();
    private bool _previewValid;
    private string _layoutError = "请先选择主功法";
    private string? _previewKey;
    private string _previewReason = "";

    public event Action<string>? Selected;
    public event Action<string>? Removed;
    public event Action<string, int, int>? MoveRequested;
    public event Action<string, int, int>? AddRequested;

    public override void _Ready() => MouseExited += () => DetailTips.Hide(this);
    public override void _ExitTree() => DetailTips.Hide(this);

    public TemplateBackpackView()
    {
        CustomMinimumSize = new Vector2(250, 240);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        MouseExited += () => { _previewKey = null; _previewCells.Clear(); QueueRedraw(); };
    }

    public void Configure(TemplateCatalog catalog, TemplateDraft? draft, bool readOnly,
        string? selectedEntryId, IReadOnlyList<BackpackPosition>? problemCells = null)
    {
        _catalog = catalog; _draft = draft; _readOnly = readOnly; _selected = selectedEntryId;
        _art ??= new ObserverArtCatalog();
        _layout = null; _valid.Clear(); _cards.Clear(); _previewKey = null; _previewCells.Clear();
        _layoutError = "请先选择主功法";
        _problems = (problemCells ?? Array.Empty<BackpackPosition>()).Where(p => p.X > 0 && p.Y > 0)
            .Select(BackpackCoordinates.ToCell).ToHashSet();
        if (draft != null)
        {
            foreach (var entry in draft.Entries) _cards.TryAdd(entry.CardDefineId, catalog.GetCard(entry.CardDefineId));
            try
            {
                _layout = draft.GetLayout(catalog);
                if (_layout != null) _valid = _layout.GetValidCells();
            }
            catch (Exception exception) { _layout = null; _layoutError = exception.Message; }
        }
        // 编辑状态可能刷新卡牌实例；下次悬停按当前草稿解析，而非复用上一个实例。
        DetailTips.Hide(this);
        TooltipText = EmptyCellHint();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_previewCells.Count > 0 && !GetViewport().GuiIsDragging())
        { _previewKey = null; _previewCells.Clear(); QueueRedraw(); }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_draft == null) return;
        if (@event is InputEventMouseMotion motion)
        {
            var entry = EntryAt(motion.Position);
            if (entry == null || GetViewport().GuiIsDragging())
            {
                DetailTips.Hide(this);
                if (!GetViewport().GuiIsDragging()) TooltipText = EmptyCellHint();
                return;
            }
            _cards.TryGetValue(entry.CardDefineId, out var card);
            string name = card == null ? entry.CardDefineId : TemplateCardTile.NameOf(card);
            TooltipText = "";
            string description = $"[{entry.CardDefineId}]\n位置 {entry.Position} · 实例 {entry.EntryId}"
                + (entry.EntryId == _draft.MainEntryId ? "\n主功法位置固定" : _readOnly ? "\n项目模板只读" : "\n拖动移动 · 右键移除")
                + (card == null ? "\n卡牌定义缺失" : $"\n占格 {card.Size} · {card.Desc}");
            DetailTips.ShowCard(this, entry.EntryId, name, description, _art?.Card(entry.CardDefineId));
        }
        if (@event is not InputEventMouseButton { Pressed: true } mouse) return;
        var hit = EntryAt(mouse.Position);
        if (hit == null) return;
        if (mouse.ButtonIndex == MouseButton.Left)
        { _selected = hit.EntryId; Selected?.Invoke(hit.EntryId); QueueRedraw(); AcceptEvent(); }
        else if (mouse.ButtonIndex == MouseButton.Right && !_readOnly && hit.EntryId != _draft.MainEntryId)
        { Removed?.Invoke(hit.EntryId); AcceptEvent(); }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_readOnly || _draft == null) return default;
        var entry = EntryAt(atPosition);
        if (entry == null || entry.EntryId == _draft.MainEntryId) return default;
        DetailTips.Hide(this);
        _cards.TryGetValue(entry.CardDefineId, out var card);
        SetDragPreview(TemplateCardTile.DragPreview(card == null ? entry.CardDefineId : TemplateCardTile.NameOf(card)));
        return new Godot.Collections.Dictionary { ["kind"] = "entry", ["id"] = entry.EntryId };
    }

    private string EmptyCellHint() => _readOnly
        ? "项目模板只读；复制到我的模板后可拖放编辑。"
        : "拖入卡牌以添加 · 拖动调整位置 · 右键移除";

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!TryPayload(data, out string kind, out string id) || _readOnly || _catalog == null || _draft == null || _layout == null)
            return false;
        var position = PublicPosition(atPosition);
        string key = $"{kind}/{id}/{position.X}/{position.Y}";
        if (_previewKey == key) return _previewValid;
        _previewKey = key; _previewCells.Clear(); _previewValid = false;
        string? cardId = kind == "card" ? id : _draft.Entries.FirstOrDefault(e => e.EntryId == id)?.CardDefineId;
        var card = cardId == null ? null : _catalog.GetCard(cardId);
        if (card != null)
        {
            try { _previewCells = card.GetShape().Select(c => new BackpackCell(position.X - 1 + c.X, position.Y - 1 + c.Y)).ToList(); }
            catch (Exception exception) { _previewReason = exception.Message; }
        }
        if (position.X < 1 || position.Y < 1 || position.X > _layout.Width || position.Y > _layout.Height)
            _previewReason = "落点超出背包范围。";
        else
        {
            // Reuse the document's actual geometry/capacity rules without mutating the live draft.
            var candidate = TemplateJson.Clone(_draft);
            _previewValid = kind == "card"
                ? candidate.TryAddCard(_catalog, id, position, out _previewReason)
                : candidate.TryMoveCard(_catalog, id, position, out _previewReason);
        }
        TooltipText = _previewValid ? "可以放置" : _previewReason;
        QueueRedraw();
        return _previewValid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        _previewKey = null;
        if (!_CanDropData(atPosition, data) || !TryPayload(data, out string kind, out string id)) return;
        var position = PublicPosition(atPosition);
        _previewCells.Clear(); _previewKey = null;
        if (kind == "card") AddRequested?.Invoke(id, position.X, position.Y);
        else MoveRequested?.Invoke(id, position.X, position.Y);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = GetThemeDefaultFont();
        if (_layout == null || _draft == null)
        {
            DrawString(font, new Vector2(10, Size.Y / 2), TemplateCardTile.ShortName(_layoutError, 28), HorizontalAlignment.Center,
                Math.Max(1, Size.X - 20), 18, new Color("696348"));
            return;
        }
        var grid = GridRect(); float cellSize = grid.Size.X / _layout.Width;
        for (int x = 0; x < _layout.Width; x++)
            DrawString(font, new Vector2(grid.Position.X + x * cellSize, grid.Position.Y - 7), (x + 1).ToString(), HorizontalAlignment.Center, cellSize, 14, new Color("5C543A"));
        for (int y = 0; y < _layout.Height; y++)
            DrawString(font, new Vector2(grid.Position.X - 22, grid.Position.Y + y * cellSize + cellSize / 2 + 5), (y + 1).ToString(), HorizontalAlignment.Center, 20, 14, new Color("5C543A"));
        for (int y = 0; y < _layout.Height; y++)
        for (int x = 0; x < _layout.Width; x++)
        {
            var cell = new BackpackCell(x, y); var rect = CellRect(cell).Grow(-2);
            DrawRect(rect, _valid.Contains(cell) ? new Color("D1C7A6D0") : new Color("6D706180"));
            DrawRect(rect, new Color("8A7F5D"), false, 1);
            if (!_valid.Contains(cell)) DrawLine(rect.Position, rect.End, new Color("66664F"), 1);
        }
        foreach (var entry in _draft.Entries.Where(e => e.Position != null)) DrawEntry(entry);
        foreach (var cell in _problems.Where(c => c.X >= 0 && c.Y >= 0 && c.X < _layout.Width && c.Y < _layout.Height))
        {
            var rect = CellRect(cell).Grow(-2);
            DrawRect(rect, new Color("C45D4340")); DrawRect(rect, new Color("A64136"), false, 2);
        }
        foreach (var cell in _previewCells)
        {
            var rect = CellRect(cell).Grow(-1);
            DrawRect(rect, new Color(_previewValid ? "75B58B70" : "CB756470"));
            DrawRect(rect, new Color(_previewValid ? "3F8B5A" : "AB473E"), false, 2);
        }
        if (_previewCells.Count > 0)
        {
            var rect = CellRect(_previewCells[0]);
            DrawString(font, rect.Position + new Vector2(3, rect.Size.Y - 5), _previewValid ? "可放置" : "不可放置", HorizontalAlignment.Center,
                Math.Max(1, rect.Size.X - 6), 12, new Color("FFF5DE"));
        }
    }

    private void DrawEntry(TemplateCardEntry entry)
    {
        if (entry.Position is not { X: >= 1, Y: >= 1 } position || _layout == null || _draft == null) return;
        _cards.TryGetValue(entry.CardDefineId, out var card);
        var cells = EntryCells(entry).Where(c => c.X >= 0 && c.Y >= 0 && c.X < _layout.Width && c.Y < _layout.Height).ToArray();
        if (cells.Length == 0) return;
        var regions = cells.Select(c => CellRect(c).Grow(-2)).ToArray();
        var bounds = regions.Aggregate((a, b) => a.Merge(b));
        string? element = card?.ManaCost.Keys.FirstOrDefault();
        Color color = element == null ? new Color("85816B") : ObserverArtCatalog.ElementColor(element).Lerp(new Color("4F5E5C"), 0.68f);
        foreach (var rect in regions) DrawRect(rect, color);
        // Formal cards are contiguous horizontal bars: cover internal grid seams with one surface.
        bool rectangle = cells.Length == (cells.Max(c => c.X) - cells.Min(c => c.X) + 1) * (cells.Max(c => c.Y) - cells.Min(c => c.Y) + 1);
        if (rectangle) DrawRect(bounds, color);
        var texture = card == null ? null : _art?.Card(card.ID);
        string name = card == null ? entry.CardDefineId : TemplateCardTile.NameOf(card);
        if (texture != null)
        {
            var artArea = new Rect2(bounds.Position + new Vector2(5, 18), new Vector2(Math.Max(1, bounds.Size.X - 10), Math.Max(1, bounds.Size.Y - 23)));
            if (rectangle) TemplateEditorSkin.DrawFit(this, texture, artArea, _art!.CardTranspose(card!.ID));
            else
            {
                // Never paint over holes in custom test shapes; use the largest occupied cell for its icon.
                var rect = regions[0];
                TemplateEditorSkin.DrawFit(this, texture, new Rect2(rect.Position + new Vector2(3, 17), new Vector2(Math.Max(1, rect.Size.X - 6), Math.Max(1, rect.Size.Y - 20))), _art!.CardTranspose(card!.ID));
            }
        }
        bool main = entry.EntryId == _draft.MainEntryId;
        var labelArea = rectangle ? bounds : regions[0];
        int fontSize = texture == null ? 16 : 13;
        int maxChars = Math.Max(2, (int)(labelArea.Size.X / fontSize));
        var font = GetThemeDefaultFont();
        var at = labelArea.Position + new Vector2(3, texture == null ? labelArea.Size.Y / 2 + 5 : 15);
        DrawString(font, at, TemplateCardTile.ShortName(name, maxChars), HorizontalAlignment.Center, Math.Max(1, labelArea.Size.X - 6), fontSize, new Color("FFF2D3"));
        Color outline = entry.EntryId == _selected ? new Color("F0CB6B") : main ? new Color("D0B364") : new Color("A89A70");
        if (rectangle) DrawRect(bounds, outline, false, entry.EntryId == _selected || main ? 2 : 1);
        else foreach (var rect in regions) DrawRect(rect, outline, false, entry.EntryId == _selected || main ? 2 : 1);
        if (main) DrawString(font, labelArea.Position + new Vector2(2, labelArea.Size.Y - 3), "主", HorizontalAlignment.Right, labelArea.Size.X - 5, 12, new Color("FFE5A3"));
    }

    private IEnumerable<BackpackCell> EntryCells(TemplateCardEntry entry)
    {
        if (entry.Position is not { X: >= 1, Y: >= 1 } position) return Array.Empty<BackpackCell>();
        _cards.TryGetValue(entry.CardDefineId, out var card);
        try
        {
            var shape = card?.GetShape() ?? new List<BackpackCell> { new(0, 0) };
            return shape.Select(c => new BackpackCell(position.X - 1 + c.X, position.Y - 1 + c.Y)).ToArray();
        }
        catch { return new[] { new BackpackCell(position.X - 1, position.Y - 1) }; }
    }
    private TemplateCardEntry? EntryAt(Vector2 point)
    {
        if (_draft == null || _layout == null || !GridRect().HasPoint(point)) return null;
        var position = PublicPosition(point);
        var cell = new BackpackCell(position.X - 1, position.Y - 1);
        return _draft.Entries.LastOrDefault(e => EntryCells(e).Contains(cell));
    }
    private Rect2 GridRect()
    {
        if (_layout == null) return new();
        float step = Math.Max(1, Math.Min((Size.X - 30) / _layout.Width, (Size.Y - 27) / _layout.Height));
        var extent = new Vector2(step * _layout.Width, step * _layout.Height);
        return new(new Vector2(26 + Math.Max(0, (Size.X - 30 - extent.X) / 2), 23 + Math.Max(0, (Size.Y - 27 - extent.Y) / 2)), extent);
    }
    private Rect2 CellRect(BackpackCell cell)
    {
        var grid = GridRect(); float step = _layout == null ? 1 : grid.Size.X / _layout.Width;
        return new(grid.Position + new Vector2(cell.X * step, cell.Y * step), new Vector2(step, step));
    }
    private BackpackPosition PublicPosition(Vector2 point)
    {
        var grid = GridRect(); float step = _layout == null ? 1 : grid.Size.X / _layout.Width;
        return new((int)Math.Floor((point.X - grid.Position.X) / step) + 1, (int)Math.Floor((point.Y - grid.Position.Y) / step) + 1);
    }
    private static bool TryPayload(Variant data, out string kind, out string id)
    {
        kind = id = "";
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.TryGetValue("kind", out var k) || !dictionary.TryGetValue("id", out var v)) return false;
        kind = k.AsString(); id = v.AsString();
        return (kind is "card" or "entry") && !string.IsNullOrWhiteSpace(id);
    }
}
