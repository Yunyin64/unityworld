using Godot;
using System;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.UI;

/// <summary>独立拼点卡面。场景定义样式，舞台提供位置与透明度，卡牌事实提供内容。</summary>
public partial class ObserverContestCard : Control
{
    /// <summary>确认胜招后使用的卡面描边颜色。</summary>
    [Export] public Color WinningBorder { get; set; } = new("e1c082", .89f);
    /// <summary>确认胜招后使用的点数颜色。</summary>
    [Export] public Color WinningPoints { get; set; } = new("ffe4aa");

    private Panel _face = null!;
    private StyleBoxFlat _normalFace = null!, _winningFace = null!, _artFrame = null!;
    private Control _leftSpine = null!, _rightSpine = null!, _typeIcon = null!;
    private TextureRect _picture = null!;
    private Label _title = null!, _description = null!, _points = null!;
    private int _titleSize, _descriptionSize, _pointsSize;
    private Color _normalPoints;
    private bool _won;
    private string _contestType = "Zhan";
    private Color _element = ObserverArtCatalog.ElementColor("Jin");
    private ContestSidePresentation? _action;
    private ObserverArtCatalog? _art;

    internal Vector2 PreferredSize { get; private set; }

    /// <summary>缓存场景中的尺寸与样式，每张实例保有自己的胜招描边和属性色。</summary>
    public override void _Ready()
    {
        PreferredSize = Size;
        _face = GetNode<Panel>("Face");
        _normalFace = (StyleBoxFlat)_face.GetThemeStylebox("panel");
        _winningFace = (StyleBoxFlat)_normalFace.Duplicate();
        _winningFace.BorderColor = WinningBorder;
        _winningFace.BorderWidthLeft++;
        _winningFace.BorderWidthTop++;
        _winningFace.BorderWidthRight++;
        _winningFace.BorderWidthBottom++;
        var illustration = GetNode<Panel>("Illustration");
        _artFrame = (StyleBoxFlat)illustration.GetThemeStylebox("panel").Duplicate();
        illustration.AddThemeStyleboxOverride("panel", _artFrame);
        _picture = illustration.GetNode<TextureRect>("Picture");
        _picture.Draw += DrawPicture;
        _picture.Resized += _picture.QueueRedraw;
        _leftSpine = GetNode<Control>("LeftSpine");
        _rightSpine = GetNode<Control>("RightSpine");
        _title = GetNode<Label>("Readout/Title");
        _description = GetNode<Label>("Readout/Description");
        _points = GetNode<Label>("Readout/Points");
        _typeIcon = GetNode<Control>("Readout/TypeIcon");
        _typeIcon.Draw += DrawTypeIcon;
        _titleSize = _title.GetThemeFontSize("font_size");
        _descriptionSize = _description.GetThemeFontSize("font_size");
        _pointsSize = _points.GetThemeFontSize("font_size");
        _normalPoints = _points.GetThemeColor("font_color");
    }

    internal void Submit(ObserverArtCatalog art, ContestSidePresentation action, Rect2 bounds, float opacity, bool left, bool won)
    {
        bool resized = Size != bounds.Size;
        Position = bounds.Position;
        Size = bounds.Size;
        Modulate = new Color(Colors.White, opacity);
        _leftSpine.Visible = left;
        _rightSpine.Visible = !left;
        if (_won != won)
        {
            _won = won;
            _face.AddThemeStyleboxOverride("panel", won ? _winningFace : _normalFace);
            _points.AddThemeColorOverride("font_color", won ? WinningPoints : _normalPoints);
        }
        if (!ReferenceEquals(_action, action))
        {
            _art = art;
            _action = action;
            _picture.Texture = null;
            _contestType = action.ContestType;
            _element = ObserverArtCatalog.ElementColor(action.Element);
            _artFrame.BorderColor = new Color(_element, _artFrame.BorderColor.A);
            _title.Text = ObserverArtCatalog.CardName(action.Card.Name, action.Card.DefineId);
            _description.Text = string.IsNullOrWhiteSpace(action.Card.Description)
                ? $"发起{ObserverArtCatalog.ContestName(_contestType)}拼点" : action.Card.Description;
            _points.Text = action.Value.ToString();
            _picture.QueueRedraw();
            _typeIcon.QueueRedraw();
            resized = true;
        }
        if (resized) AdjustFonts();
        Show();
    }

    private void AdjustFonts()
    {
        float ratio = Mathf.Clamp(Size.X / PreferredSize.X, .70f, 1);
        SetFont(_title, Mathf.RoundToInt(_titleSize * ratio));
        SetFont(_description, Mathf.RoundToInt(_descriptionSize * ratio));
        int size = Mathf.RoundToInt(_pointsSize * ratio);
        float width = _points.GetThemeFont("font").GetStringSize(_points.Text, fontSize: size).X;
        if (width > _points.Size.X) size = Math.Max(9, (int)(size * _points.Size.X / width));
        SetFont(_points, size);
    }

    private static void SetFont(Label label, int size)
    {
        if (label.GetThemeFontSize("font_size") != size) label.AddThemeFontSizeOverride("font_size", size);
    }

    private void DrawPicture()
    {
        if (_art != null && _action != null)
            ObserverDuelStage.DrawCardArt(_picture, _art, _action.Card.DefineId, _contestType,
                new Rect2(Vector2.Zero, _picture.Size), _element, 1);
    }

    private void DrawTypeIcon()
    {
        ObserverDuelStage.DrawContestGlyph(_typeIcon, _contestType, _typeIcon.Size / 2,
            Math.Min(_typeIcon.Size.X, _typeIcon.Size.Y) / 2, _element);
    }
}
