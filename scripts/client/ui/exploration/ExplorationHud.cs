using System;
using Godot;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Exploration;

/// <summary>浮在地图上方的身份、日期和地点信息。预留位置直接透出下层地图。</summary>
internal sealed class ExplorationHud
{
    private static readonly Color Ink = new("302f27"), Muted = new("605e4f");
    private readonly SystemFont _titles = new() { FontNames = new[] { "KaiTi", "STKaiti", "SimSun", "Noto Serif CJK SC" } };
    private readonly ExplorationArt _art;
    private readonly Label _name, _identity, _date, _where, _title, _coords, _terrain, _distance, _context, _journey, _feedback, _zoom;
    private readonly Control _page;
    private readonly Panel _identityPlate, _portraitRim, _place, _controls;
    private readonly Button _autoMode, _manualMode, _continueTrip;
    private readonly TextureRect _portrait;
    private readonly ExplorationCalendar _calendar;
    private readonly ExplorationTileThumbnail _preview;
    private ExplorationSnapshot? _displayed;

    public ExplorationHud(Control page, ExplorationArt art, ExplorationMap map, Action<bool> setAutomatic, Action continueTrip)
    {
        _page = page;
        _art = art;
        page.Theme = new Theme
        {
            DefaultFont = new SystemFont { FontNames = new[] { "Microsoft YaHei", "Noto Sans CJK SC", "sans-serif" } },
            DefaultFontSize = 16
        };

        _identityPlate = Paper(page, new(49, 39, 245, 77));
        _name = Text(_identityPlate, new(72, 9, 163, 34), 26, Ink, _titles);
        _identity = Text(_identityPlate, new(73, 45, 160, 23), 15, Muted);
        _portraitRim = Plate(page, new(20, 31, 89, 89), Flat("443e31", "b69a64", 2, 45));
        var mask = Plate(_portraitRim, new(5, 5, 79, 79), Flat("63726a", "dbc992", 1, 40));
        mask.ClipChildren = CanvasItem.ClipChildrenMode.AndDraw;
        _portrait = new TextureRect
        {
            Position = new(0, 1), Size = new(79, 82), Texture = art.Portrait,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        mask.AddChild(_portrait);

        _calendar = new ExplorationCalendar
        {
            Size = new(410, 52),
            MouseFilter = Control.MouseFilterEnum.Stop, MouseForcePassScrollEvents = false
        };
        page.AddChild(_calendar);
        _date = Text(_calendar, new(15, 2, 380, 47), 24, new("f6e9c5"), _titles, HorizontalAlignment.Center);
        _date.Name = "WorldDate";

        _place = Paper(page, new(24, 0, 259, 326));
        _place.Name = "Place";
        _where = Text(_place, new(18, 13, 110, 22), 14, Muted);
        _coords = Text(_place, new(114, 13, 126, 22), 15, Muted, align: HorizontalAlignment.Right);
        _title = Text(_place, new(17, 35, 227, 35), 28, Ink, _titles);
        var pictureBorder = Plate(_place, new(16, 77, 227, 105), Flat("6f7054", "a19167", 1));
        _preview = new ExplorationTileThumbnail { Position = new(2, 2), Size = new(223, 101), MouseFilter = Control.MouseFilterEnum.Ignore };
        pictureBorder.AddChild(_preview);
        _terrain = Text(_place, new(18, 190, 224, 24), 16, Ink);
        _distance = Text(_place, new(18, 218, 224, 24), 16, Ink);
        _context = Text(_place, new(18, 246, 224, 24), 15, Muted);
        _journey = Text(_place, new(18, 276, 223, 58), 15, new("364e43"));
        _journey.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _feedback = Text(_place, new(18, 276, 223, 44), 15, Muted);
        _feedback.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        // 模式签和缩放始终贴在右下角。“继续”单独浮在上一行，显示时不会推开常驻控件。
        _controls = Plate(page, new(0, 0, 300, 42), Flat("283731", "a99161", 1, 5));
        var minus = HudButton(_controls, "ZoomOut", "−", new(3, 3, 36, 36), () => map.ChangeZoom(-0.25f));
        minus.TooltipText = "缩小地图";
        _zoom = Text(_controls, new(39, 3, 65, 36), 18, new("f4e5bd"), align: HorizontalAlignment.Center);
        _zoom.Name = "Zoom";
        var plus = HudButton(_controls, "ZoomIn", "+", new(104, 3, 36, 36), () => map.ChangeZoom(0.25f));
        plus.TooltipText = "放大地图";
        var group = new ButtonGroup();
        _autoMode = ModeButton(_controls, "AutoMode", "自动", 152, group, () => setAutomatic(true));
        _autoMode.TooltipText = "在已确认的行程中自动续行";
        _manualMode = ModeButton(_controls, "ManualMode", "手动", 225, group, () => setAutomatic(false));
        _manualMode.TooltipText = "每按一次继续，行走一周";
        _continueTrip = HudButton(page, "ContinueTrip", "继续", new(0, 0, 112, 36), continueTrip);
        _continueTrip.AddThemeStyleboxOverride("disabled", Flat("373e36", "736f5c", 1, 4));
        _continueTrip.AddThemeColorOverride("font_disabled_color", new Color("a6a48f"));
        _continueTrip.TooltipText = "继续已确认的行程一周";
        _continueTrip.Hide();
        page.Resized += Layout;
        Layout();
        ShowZoom(map.Zoom);
    }

    public void ShowZoom(float zoom) => _zoom.Text = $"{zoom:P0}".Replace(" ", "");

    public void Show(ExplorationSnapshot state, TileId selected, bool automatic, bool autoPaused)
    {
        // 本周结果只在结算刷新时提示。玩家查看下一格后，地点栏重新给出该格的操作；
        // 会话已停止接收命令时保留故障原因，方便继续查看地图时辨认状态。
        bool showResult = !ReferenceEquals(_displayed, state) || !state.CommandsEnabled;
        _displayed = state;
        _name.Text = state.Name;
        _name.TooltipText = state.Name;
        _autoMode.SetPressedNoSignal(automatic);
        _manualMode.SetPressedNoSignal(!automatic);
        _continueTrip.Visible = !automatic;
        _continueTrip.Disabled = !state.CanContinueJourney;
        _identity.Text = state.Identity;
        _portrait.Visible = state.CanDrawActor;
        _date.Text = state.Date.ToString();
        _calendar.Moon = state.Date.MoonPhase;
        _calendar.QueueRedraw();
        if (!state.Tiles.TryGetValue(selected, out var tile))
        {
            _where.Text = "地点信息";
            _title.Text = "地点不可用";
            _coords.Text = _terrain.Text = _distance.Text = _context.Text = _journey.Text = "";
            _preview.Hide();
            _journey.Hide();
            _feedback.Text = state.Message;
            _feedback.TooltipText = state.Message;
            _feedback.Position = new(18, 245);
            _feedback.Size = new(223, 60);
            _feedback.Show();
            _place.Size = new(259, 321);
            Layout();
            return;
        }

        _preview.Show();
        _preview.SetTile(_art, tile, state.Seed, state.PlaneId);
        bool here = state.CanDrawActor && selected == state.Position;
        bool moving = state.CanDrawActor && state.Action.State == "move" && state.Action.Target.HasValue;
        bool canStart = state.CommandsEnabled && state.Candidates.Contains(selected);
        _where.Text = here ? "当前位置" : "所选地点";
        _coords.Text = Coordinates(selected);
        _title.Text = string.IsNullOrWhiteSpace(tile.LandmarkName) ? tile.Name : tile.LandmarkName;
        string passage = tile.CanWalk ? "可步行" : "不可步行";
        _terrain.Text = string.IsNullOrWhiteSpace(tile.LandmarkName) ? $"通行　{passage}" : $"{tile.Name}  ·  {passage}";
        int steps = selected.DistanceTo(state.Position);
        bool canEstimate = canStart && state.Speed > 0;
        _distance.Text = here ? "距离　脚下"
            : steps == 1 ? $"邻格　{MoveBehavior.AdjacentDistance:0} 路程"
            : $"距离　{steps} 格";
        string region = string.IsNullOrWhiteSpace(tile.RegionName) ? "" : $"地区　{tile.RegionName}";
        string estimateLine = canEstimate ? $"按当前速度预计 {Weeks(MoveBehavior.AdjacentDistance, state.Speed)} 周抵达" : "";
        bool twoLines = region.Length > 0 && canEstimate;
        _context.Text = twoLines ? region + "\n" + estimateLine : region + estimateLine;
        _context.Size = new(224, twoLines ? 50 : 24);
        _terrain.TooltipText = _terrain.Text;
        _context.TooltipText = _context.Text;

        float y = twoLines ? 301 : _context.Text.Length > 0 ? 274 : 245;
        _journey.Visible = moving;
        if (moving && state.Action.Target is TileId target)
        {
            string targetName = state.Tiles.TryGetValue(target, out var destination) ? destination.Name : "";
            string estimate = state.Speed > 0
                ? $"预计剩 {Weeks(state.Action.TotalDistance - state.Action.Progress, state.Speed)} 周" : "当前无法前进";
            _journey.Text = $"前往 {targetName} {Coordinates(target)}\n行路中  {state.Action.Progress:0.##}/{state.Action.TotalDistance:0.##}  ·  {estimate}";
            _journey.Position = new(18, y);
            y += 57;
        }
        string hint = "";
        if (state.CommandsEnabled)
        {
            if (moving && automatic && autoPaused)
                hint = "自动续行已暂停，可切到手动";
            else if (moving && !automatic)
                hint = "点击右下角「继续」行走一周";
            else if (canStart)
                hint = "再点此格，启程并推进一周";
            else if (here && state.Candidates.Count > 0)
                hint = "点选金边邻格查看行程";
        }
        string notice = showResult ? state.Message : "";
        _feedback.Text = string.IsNullOrWhiteSpace(notice) ? hint
            : string.IsNullOrWhiteSpace(hint) ? notice : $"{notice}\n{hint}";
        _feedback.Visible = !string.IsNullOrWhiteSpace(_feedback.Text);
        if (_feedback.Visible)
        {
            _feedback.TooltipText = _feedback.Text;
            _feedback.Position = new(18, y);
            float height = _feedback.Text.Contains('\n') || _feedback.Text.Length > 14 ? 48 : 25;
            _feedback.Size = new(223, height);
            _feedback.MouseFilter = Control.MouseFilterEnum.Pass;
            y += height + 2;
        }
        _place.Size = new(259, y + 16);
        Layout();
    }

    private void Layout()
    {
        var viewport = _page.Size;
        float gap = _calendar.Moon.HasValue ? 35 : 0;
        float dateWidth = _titles.GetStringSize(_date.Text, fontSize: 24).X;
        _calendar.Size = new(Math.Max(410, dateWidth + gap + 42), 52);
        _calendar.Position = new(Mathf.Round((viewport.X - _calendar.Size.X) / 2), 24);
        _date.Position = new((_calendar.Size.X - dateWidth + gap) / 2 - 3, 2);
        _date.Size = new(dateWidth + 6, 47);
        _calendar.MoonX = _date.Position.X - 16;
        _calendar.QueueRedraw();

        // 历牌始终贴顶居中。窄窗先收名牌尾部空边，空间不足时让名牌在左上区域错行。
        float nameSpace = _calendar.Position.X - 49 - 10;
        bool stackName = nameSpace < 180;
        _portraitRim.Position = new(20, _calendar.Position.X < 113 ? 82 : 31);
        _identityPlate.Position = new(49, stackName ? Math.Max(82, _portraitRim.Position.Y + 8) : 39);
        _identityPlate.Size = new(stackName ? 245 : Math.Min(245, nameSpace), 77);
        _name.Size = new(_identityPlate.Size.X - 83, 34);
        _identity.Size = new(_identityPlate.Size.X - 85, 23);
        float nameWidth = Math.Max(1, _titles.GetStringSize(_name.Text, fontSize: 26).X);
        _name.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(26 * _name.Size.X / nameWidth, 15, 26));

        float headBottom = Math.Max(_identityPlate.GetRect().End.Y, _portraitRim.GetRect().End.Y) + 8;
        float space = viewport.Y - _place.Size.Y;
        _place.Position = new(24, Mathf.Round(Math.Max(6, Math.Min(Math.Max(headBottom, space / 2), space - 6))));
        _controls.Position = (viewport - _controls.Size - new Vector2(20, 18)).Round();
        _continueTrip.Position = _controls.Position + new Vector2(_controls.Size.X - _continueTrip.Size.X, -_continueTrip.Size.Y - 6);
    }

    private static string Coordinates(TileId id) => $"({id.Q}, {id.R})";
    private static string Weeks(double distance, float speed) => Math.Ceiling(distance / speed).ToString("0");
    private Panel Paper(Control parent, Rect2 area) => Plate(parent, area, _art.Panel);

    private static Panel Plate(Control parent, Rect2 area, StyleBox style)
    {
        // HUD 浮在可拖动的全屏地图上，仅实有面板拦下点击、拖动和滚轮。
        var panel = new Panel
        {
            Position = area.Position, Size = area.Size,
            MouseFilter = Control.MouseFilterEnum.Stop, MouseForcePassScrollEvents = false
        };
        panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel);
        return panel;
    }

    private static Label Text(Control parent, Rect2 area, int size, Color color, Font? font = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Position = area.Position, Size = area.Size, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center,
            ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (font != null) label.AddThemeFontOverride("font", font);
        parent.AddChild(label);
        return label;
    }

    private static Button ModeButton(Control parent, string name, string text, int x, ButtonGroup group, Action choose)
    {
        var button = HudButton(parent, name, text, new(x, 3, 72, 36), choose);
        button.ToggleMode = true;
        button.ButtonGroup = group;
        var selected = Flat("526649", "e2c987", 2, 4);
        button.AddThemeStyleboxOverride("pressed", selected);
        button.AddThemeStyleboxOverride("hover_pressed", selected);
        return button;
    }

    private static Button HudButton(Control parent, string name, string text, Rect2 area, Action action)
    {
        var button = new Button { Name = name, Text = text, Position = area.Position, Size = area.Size };
        StyleButton(button);
        parent.AddChild(button);
        button.Pressed += action;
        return button;
    }

    private static void StyleButton(Button button)
    {
        button.AddThemeStyleboxOverride("normal", Flat("31483c", "a69163", 1, 4));
        button.AddThemeStyleboxOverride("hover", Flat("45614f", "d6bc7c", 1, 4));
        button.AddThemeStyleboxOverride("pressed", Flat("20372e", "e3cb8b", 1, 4));
        var focus = Flat("00000000", "e1c982", 2, 4);
        focus.DrawCenter = false;
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeColorOverride("font_color", new Color("f6e9cb"));
        button.AddThemeFontSizeOverride("font_size", 17);
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
    }

    private static StyleBoxFlat Flat(string fill, string edge, int width, int radius = 0)
    {
        var box = new StyleBoxFlat { BgColor = new(fill), BorderColor = new(edge) };
        box.SetBorderWidthAll(width);
        box.SetCornerRadiusAll(radius);
        return box;
    }
}
