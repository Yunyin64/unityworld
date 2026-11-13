using System;
using Godot;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Exploration;

/// <summary>浮在地图上方的身份、日期和地点信息。预留位置直接透出下层地图。</summary>
internal sealed class ExplorationHud
{
    private readonly ExplorationArt _art;
    private readonly Label _name, _identity, _date, _where, _title, _coords, _terrain, _distance, _context, _journey, _feedback, _zoom;
    private readonly Button _autoMode, _manualMode, _continueTrip;
    private readonly TextureRect _portrait;
    private readonly ExplorationCalendar _calendar;
    private readonly ExplorationTileThumbnail _preview;
    private ExplorationSnapshot? _displayed;

    public ExplorationHud(Control page, ExplorationArt art, ExplorationMap map, Action<bool> setAutomatic, Action continueTrip)
    {
        _art = art;
        // 固定分区、字体和锚点来自 tscn，代码只写入真实状态并连接交互。
        Label LabelAt(string name) => page.GetNode<Label>("%" + name);
        _name = LabelAt("ActorName");
        _identity = LabelAt("Identity");
        _date = LabelAt("WorldDate");
        _where = LabelAt("Where");
        _title = LabelAt("PlaceTitle");
        _coords = LabelAt("Coordinates");
        _terrain = LabelAt("Terrain");
        _distance = LabelAt("Distance");
        _context = LabelAt("Region");
        _journey = LabelAt("Journey");
        _feedback = LabelAt("Feedback");
        _zoom = LabelAt("Zoom");
        _autoMode = page.GetNode<Button>("%AutoMode");
        _manualMode = page.GetNode<Button>("%ManualMode");
        _continueTrip = page.GetNode<Button>("%ContinueTrip");
        _portrait = page.GetNode<TextureRect>("%Portrait");
        _portrait.Texture = art.Portrait;
        _calendar = page.GetNode<ExplorationCalendar>("%Calendar");
        _preview = page.GetNode<ExplorationTileThumbnail>("%Thumbnail");
        page.GetNode<Button>("%ZoomOut").Pressed += () => map.ChangeZoom(-0.25f);
        page.GetNode<Button>("%ZoomIn").Pressed += () => map.ChangeZoom(0.25f);
        _autoMode.Pressed += () => setAutomatic(true);
        _manualMode.Pressed += () => setAutomatic(false);
        _continueTrip.Pressed += continueTrip;
        BindResponsiveLayout(page);
        ShowZoom(map.Zoom);
    }

    private void BindResponsiveLayout(Control page)
    {
        var hud = page.GetNode<Control>("Hud");
        var plate = page.GetNode<Control>("Hud/IdentityPlate");
        var portrait = page.GetNode<Control>("Hud/PortraitFrame");
        var home = page.GetNode<Button>("%ReturnHome");
        var place = page.GetNode<PanelContainer>("Hud/Place");
        var scroll = page.GetNode<ScrollContainer>("%PlaceScroll");
        var fields = page.GetNode<VBoxContainer>("%Fields");
        float calendarY = _calendar.Position.Y;
        float placeCenterOffset = (place.OffsetTop + place.OffsetBottom) / 2;
        float previewHeight = scroll.CustomMinimumSize.Y;
        bool queued = false;

        void Arrange()
        {
            queued = false;
            if (!GodotObject.IsInstanceValid(hud) || !hud.IsInsideTree()) return;
            // 左上名牌和右上返回保留设计边距。顶栏放不下时，只把居中的历牌换到下一行。
            var top = new Rect2(new Vector2(_calendar.Position.X, calendarY), _calendar.Size);
            var identity = portrait.GetRect().Merge(plate.GetRect()).Grow(6);
            float y = calendarY;
            if (top.Intersects(identity)) y = Mathf.Max(y, identity.End.Y);
            if (top.Intersects(home.GetRect().Grow(6))) y = Mathf.Max(y, home.GetRect().End.Y + 6);
            _calendar.Position = new(_calendar.Position.X, y);

            // 地点面板居中靠左，内容过长时在面板内滚动，字号和控件尺寸保持当前倍率。
            var area = new Rect2(place.Position.X, 0, place.Size.X, hud.Size.Y);
            float firstY = area.Intersects(identity) ? identity.End.Y : 0;
            var calendar = _calendar.GetRect().Grow(6);
            if (area.Intersects(calendar)) firstY = Mathf.Max(firstY, calendar.End.Y);
            float lastY = hud.Size.Y - 6;
            var controls = hud.GetNode<Control>("Controls").GetRect();
            if (area.Intersects(controls)) lastY = controls.Position.Y - 6;
            float border = place.GetThemeStylebox("panel").GetMinimumSize().Y;
            float bodyHeight = Mathf.Max(1, Mathf.Min(Mathf.Max(previewHeight, fields.GetCombinedMinimumSize().Y), lastY - firstY - border));
            scroll.CustomMinimumSize = new Vector2(scroll.CustomMinimumSize.X, bodyHeight);
            place.Size = new(place.Size.X, bodyHeight + border);
            float center = hud.Size.Y * place.AnchorTop + placeCenterOffset;
            place.Position = new(place.Position.X, Mathf.Clamp(center - place.Size.Y / 2, firstY, Mathf.Max(firstY, lastY - place.Size.Y)));
        }
        void QueueLayout()
        {
            if (queued) return;
            queued = true;
            Callable.From(Arrange).CallDeferred();
        }
        hud.Resized += QueueLayout;
        fields.MinimumSizeChanged += QueueLayout;
        QueueLayout();
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
            _feedback.Show();
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
        _context.Visible = _context.Text.Length > 0;
        _terrain.TooltipText = _terrain.Text;
        _context.TooltipText = _context.Text;

        _journey.Visible = moving;
        if (moving && state.Action.Target is TileId target)
        {
            string targetName = state.Tiles.TryGetValue(target, out var destination) ? destination.Name : "";
            string estimate = state.Speed > 0
                ? $"预计剩 {Weeks(state.Action.TotalDistance - state.Action.Progress, state.Speed)} 周" : "当前无法前进";
            _journey.Text = $"前往 {targetName} {Coordinates(target)}\n行路中  {state.Action.Progress:0.##}/{state.Action.TotalDistance:0.##}  ·  {estimate}";
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
            _feedback.TooltipText = _feedback.Text;
    }

    private static string Coordinates(TileId id) => $"({id.Q}, {id.R})";
    private static string Weeks(double distance, float speed) => Math.Ceiling(distance / speed).ToString("0");
}
