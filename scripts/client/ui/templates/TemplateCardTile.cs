using Godot;
using System;
using System.Linq;
using UnityWorld.Game.Data;

namespace Xianxia.Client.UI.Templates;

/// <summary>Library tile. Missing artwork never changes whether a real card may be selected or dragged.</summary>
public partial class TemplateCardTile : Control
{
    private CardDefine? _card;
    private Texture2D? _texture;
    private string _description = "";
    private bool _transpose, _canDrag, _hovered, _unavailable;
    private readonly StyleBoxFlat _normal = TemplateEditorSkin.Flat(new Color("D5C59E"), new Color("9D8D65"), 1, 5);
    private readonly StyleBoxFlat _hover = TemplateEditorSkin.Flat(new Color("E2D4AE"), new Color("668A68"), 2, 5);
    public event Action<string>? Selected;

    public TemplateCardTile()
    {
        CustomMinimumSize = new Vector2(108, 112);
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += () =>
        {
            _hovered = true;
            if (_card != null && !GetViewport().GuiIsDragging())
                DetailTips.ShowCard(this, _card.ID, NameOf(_card), _description, _texture);
            QueueRedraw();
        };
        MouseExited += () => { _hovered = false; DetailTips.Hide(this); QueueRedraw(); };
    }

    public void Configure(CardDefine card, Texture2D? texture, bool transpose, bool canDrag, string? unavailableReason = null)
    {
        _card = card; _texture = texture; _transpose = transpose; _canDrag = canDrag;
        _unavailable = !string.IsNullOrWhiteSpace(unavailableReason);
        string requirements = string.Join("、", card.ManaCost.Select(p => $"{ObserverArtCatalog.ElementName(p.Key)} × {p.Value}"));
        _description = $"[{card.ID}]\n占格 {card.Size} · {string.Join(" / ", card.Keywords)}\n灵元需求：{(requirements.Length == 0 ? "无" : requirements)}\n{card.Desc}"
            + (string.IsNullOrWhiteSpace(unavailableReason) ? "" : "\n不可添加：" + unavailableReason);
        MouseDefaultCursorShape = canDrag ? CursorShape.PointingHand : CursorShape.Arrow;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_card != null && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        { Selected?.Invoke(_card.ID); AcceptEvent(); }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!_canDrag || _card == null) return default;
        DetailTips.Hide(this);
        SetDragPreview(DragPreview(NameOf(_card)));
        return new Godot.Collections.Dictionary { ["kind"] = "card", ["id"] = _card.ID };
    }

    public override void _ExitTree() => DetailTips.Hide(this);

    internal static Control DragPreview(string title)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(120, 38), Modulate = new Color(1, 1, 1, 0.9f) };
        panel.AddThemeStyleboxOverride("panel", TemplateEditorSkin.Flat(new Color("294C3B"), new Color("DBCB8A"), 2));
        var label = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeColorOverride("font_color", new Color("FFF4D0"));
        label.AddThemeFontSizeOverride("font_size", 17);
        panel.AddChild(label);
        return panel;
    }

    public override void _Draw()
    {
        if (_card == null) return;
        DrawStyleBox(_hovered ? _hover : _normal, new Rect2(Vector2.Zero, Size));
        var content = new Rect2(5, 5, Math.Max(1, Size.X - 10), Math.Max(1, Size.Y - 29));
        Color tileColor = _card.ManaCost.Keys.FirstOrDefault() is { } element
            ? ObserverArtCatalog.ElementColor(element).Lerp(new Color("54655B"), 0.65f)
            : new Color("6F7563");
        DrawRect(content, tileColor);
        var font = GetThemeDefaultFont();
        if (_texture != null)
        {
            TemplateEditorSkin.DrawFit(this, _texture, new Rect2(content.Position + new Vector2(5, 22), content.Size - new Vector2(10, 25)), _transpose, _canDrag ? 1 : 0.55f);
            DrawString(font, content.Position + new Vector2(4, 17), ShortName(NameOf(_card), 8), HorizontalAlignment.Center, content.Size.X - 8, 15, new Color("FFF2D0"));
        }
        else
        {
            string title = NameOf(_card);
            int count = Math.Max(3, (int)(content.Size.X / 17));
            var lines = Enumerable.Range(0, Math.Min(3, (title.Length + count - 1) / count)).Select(i =>
                title.Substring(i * count, Math.Min(count, title.Length - i * count))).ToArray();
            for (int i = 0; i < lines.Length; i++)
                DrawString(font, new Vector2(content.Position.X + 3, content.GetCenter().Y - (lines.Length - 1) * 10 + i * 21 + 5),
                    lines[i], HorizontalAlignment.Center, content.Size.X - 6, 17, new Color("FFF2D0"));
        }
        DrawString(font, new Vector2(5, Size.Y - 7), $"{_card.Size} 格" + (_unavailable ? " · 不可用" : ""),
            HorizontalAlignment.Center, Size.X - 10, 13, new Color("4B4C35"));
        if (!_canDrag) DrawRect(new Rect2(Vector2.Zero, Size), new Color("6F716B28"));
    }

    internal static string NameOf(CardDefine card) => string.IsNullOrWhiteSpace(card.DisplayName) ? card.ID : card.DisplayName;
    internal static string ShortName(string value, int count) => value.Length > count ? value[..Math.Max(1, count - 1)] + "…" : value;
}
