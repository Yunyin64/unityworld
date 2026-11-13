using Godot;
using System;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>绑定正式场景的控件，把观察快照与表现状态路由给各面板。</summary>
internal sealed class ObserverBattleLayout
{
    public Control Canvas { get; }
    public Control Field { get; }
    public ScrollContainer FieldScroll { get; }
    public ObserverArtCatalog Art { get; } = new();
    public ObserverBackpackView[] Packs { get; } = new ObserverBackpackView[2];
    public ObserverActorPanel[] Actors { get; } = new ObserverActorPanel[2];
    public ObserverPendingPanel Pending { get; }
    public ObserverLogPanel Log { get; }
    public ObserverPresentationLayer Effects { get; }
    public Button Play { get; }
    public Button Pause { get; }
    public Button Step { get; }
    public Button Restart { get; }
    public Button Return { get; }
    public Label Status { get; }
    public string? PresentationCaption { get; set; }
    private readonly Label _info, _recent;
    private readonly Label[] _scans = new Label[2], _scanStates = new Label[2];
    private readonly OptionButton _cases;
    private readonly Control _packsArea, _duel, _pendingSlots, _recentPanel, _logView, _footerLog, _fieldLog, _header, _historyPaper;
    private readonly Control _history;
    private readonly float _originalPendingTop, _originalRecentTop;
    private readonly Vector2 _headerSize, _paperSize;

    public ObserverBattleLayout(Control host, Action play, Action pause, Action step, Action restart)
    {
        Canvas = host.GetNode<Control>("%Canvas");
        FieldScroll = Canvas.GetNode<ScrollContainer>("FieldScroll");
        Field = FieldScroll.GetNode<Control>("Field");
        _packsArea = Field.GetNode<Control>("Packs");
        _duel = Field.GetNode<Control>("Duel");
        _pendingSlots = _duel.GetNode<Control>("Pending");
        _recentPanel = _duel.GetNode<Control>("RecentPanel");
        _originalPendingTop = _pendingSlots.OffsetTop;
        _originalRecentTop = _recentPanel.OffsetTop;
        _fieldLog = Field.GetNode<Control>("LogSlot");
        _footerLog = Canvas.GetNode<Control>("FooterLog");
        _logView = _footerLog.GetNode<Control>("Log");
        _header = Canvas.GetNode<Control>("Header");
        _history = host.GetNode<Control>("History");
        _historyPaper = _history.GetNode<Control>("Paper");
        _headerSize = _header.Size;
        _paperSize = _historyPaper.Size;
        _info = Canvas.GetNode<Label>("Header/Info");
        _info.Text = "正在装配案例…";
        _recent = _duel.GetNode<Label>("RecentPanel/Recent");
        _recent.Text = "";
        _cases = Canvas.GetNode<OptionButton>("Cases");
        Play = Canvas.GetNode<Button>("Playback/Play");
        Pause = Canvas.GetNode<Button>("Playback/Pause");
        Step = Canvas.GetNode<Button>("Playback/Step");
        Restart = Canvas.GetNode<Button>("Playback/Restart");
        Return = Canvas.GetNode<Button>("Return");
        Return.Visible = false;
        Play.Pressed += play;
        Pause.Pressed += pause;
        Step.Pressed += step;
        Restart.Pressed += restart;
        Restart.TooltipText = "保留当前案例完整输入，重启观察器游戏窗口。";
        Status = Canvas.GetNode<Label>("Status");
        Status.Text = "正在初始化";
        var skin = new ObserverPreviewSkin();
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left" : "Right";
            var pack = _packsArea.GetNode<Control>(side + "Pack");
            _scans[i] = pack.GetNode<Label>("Scan");
            _scans[i].Text = "等待快照";
            _scanStates[i] = pack.GetNode<Label>("ScanState/Progress");
            _scanStates[i].Text = "";
            Packs[i] = pack.GetNode<ObserverBackpackView>("Grid");
            Packs[i].GetNode<Control>("GridPreview").Visible = false;
            Packs[i].Configure(Art, skin.Cell);
            Actors[i] = new ObserverActorPanel(_duel.GetNode<Control>(side + "Actor"), Canvas.GetNode<Control>(side + "Stats"));
        }
        Pending = new ObserverPendingPanel(_pendingSlots, Canvas, Art);
        Effects = Field.GetNode<ObserverPresentationLayer>("Effects");
        Effects.Configure(this);
        Log = new ObserverLogPanel(Canvas, _logView, _history);
        host.Resized += Arrange;
        Canvas.Resized += Arrange;
        Field.Resized += Arrange;
        Arrange();
    }

    // 真正的字号和格子大小只由窗口倍率决定。空间不足时换行或滚动，始终使用同一组节点。
    private void Arrange()
    {
        if (Field.Size.X <= 0 || Canvas.Size.X <= 0) return;
        // 宽屏保留场景内的原构图；窄屏两侧背包仍贴边，中间先排两人，空间再不足就上下排。
        var leftPack = Packs[0].GetParent<Control>();
        var rightPack = Packs[1].GetParent<Control>();
        float center = Field.Size.X / 2;
        float innerLeft = leftPack.GetRect().End.X;
        float innerRight = rightPack.Position.X;
        float gap = innerRight - innerLeft;
        bool stackedActors = gap < Actors[0].ActorSize.X + Actors[1].ActorSize.X + 36;
        bool recentBelow = gap < _recentPanel.Size.X + 16;
        bool stagger = !stackedActors && gap < 596;
        float footerGap = Canvas.GetNode<Control>("RightStats").Position.X - Canvas.GetNode<Control>("LeftStats").GetRect().End.X;
        bool logInField = footerGap < _logView.Size.X + 16;
        float logHeight = logInField ? _logView.Size.Y + 19 : 0;
        float band = stackedActors ? 636 : 584;
        float lowerHeight = (stackedActors ? _pendingSlots.Size.Y + 12 : 0) + (recentBelow ? _recentPanel.Size.Y + 12 : 0);
        Field.CustomMinimumSize = new Vector2(0, band + lowerHeight + logHeight);
        _packsArea.OffsetBottom = _duel.OffsetBottom = -lowerHeight - logHeight;
        float pendingY = stackedActors ? band + 6 : _originalPendingTop;
        _pendingSlots.Position = new Vector2(_pendingSlots.Position.X, pendingY);
        _recentPanel.Position = new Vector2(_recentPanel.Position.X, recentBelow
            ? (stackedActors ? pendingY + _pendingSlots.Size.Y + 10 : band + 6) : _originalRecentTop);

        if (stackedActors)
        {
            Actors[0].Arrange(-Actors[0].ActorSize.X / 2, 32, false);
            Actors[1].Arrange(-Actors[1].ActorSize.X / 2, 352, false);
        }
        else
        {
            float left = Mathf.Max(Actors[0].PreferredLeft, innerLeft - center + 12);
            float right = Mathf.Min(Actors[1].PreferredLeft, innerRight - center - Actors[1].ActorSize.X - 12);
            float? top = recentBelow ? pendingY + _pendingSlots.Size.Y + 12 : null;
            Actors[0].Arrange(left, top, false);
            Actors[1].Arrange(right, top + 28, stagger);
        }
        var logParent = logInField ? _fieldLog : _footerLog;
        if (_logView.GetParent() != logParent) _logView.Reparent(logParent, false);
        Log.CompactShortcut.Visible = logInField;

        float headerWidth = Mathf.Clamp(Canvas.Size.X - 2 * (_cases.GetRect().End.X + 12), 80, _headerSize.X);
        _header.OffsetLeft = -headerWidth / 2;
        _header.OffsetRight = headerWidth / 2;
        // 展开战报浮在可见窗口上方；外层滚动过后，收起按钮仍在屏内。
        var paperSize = _paperSize.Min(_history.GetParent<Control>().Size - new Vector2(40, 40));
        _historyPaper.OffsetLeft = -paperSize.X / 2;
        _historyPaper.OffsetRight = paperSize.X / 2;
        _historyPaper.OffsetTop = -paperSize.Y / 2;
        _historyPaper.OffsetBottom = paperSize.Y / 2;
    }

    public void Submit(IBattleObserverState session, BattlePresentationPlayer player, string directory)
    {
        var snapshot = player.DisplaySnapshot;
        var latest = session.CurrentSnapshot;
        _info.Text = PresentationCaption ?? $"{session.Case.Name}   ·   Tick {latest.Tick}/{session.Case.MaxTicks}   ·   种子 {session.Case.WorldSeed} / {session.Case.CombatSeed}";
        _info.TooltipText = session.Case.ToJson();
        _cases.SetItemText(0, string.IsNullOrWhiteSpace(session.Case.Name) ? session.Case.Id : session.Case.Name);
        var participants = snapshot.Participants.OrderBy(p => p.Team, StringComparer.Ordinal).ToArray();
        for (int i = 0; i < Math.Min(2, participants.Length); i++)
        {
            var npc = participants[i];
            var scan = latest.Participants.FirstOrDefault(p => p.Id == npc.Id)?.Backpack ?? npc.Backpack;
            Actors[i].Submit(npc);
            _scans[i].Text = $"{scan.Width}×{scan.Height} · 第 {scan.Round} 轮 · 第 {scan.Row + 1} 行";
            _scanStates[i].Text = $"行进度 {scan.ScanProgress:0.##}/{latest.TicksPerRow}   加速 {scan.HasteTicks} · 减速 {scan.SlowTicks}";
            Packs[i].Submit(npc, latest.TicksPerRow, session.IsPlaying, scan);
        }
        Pending.Submit(participants, player.ActiveCues);
        Effects.Submit(participants, player.ActiveCues);
        Log.Submit(session.Records);
        _recent.Text = string.Join("\n", session.Records.Where(r => r.Source == "event").TakeLast(2).Select(r => $"T{r.Tick}  {r.Text}"));
        Status.Text = session.Diagnostic != null ? "观察停止：" + session.Diagnostic.Split('\n')[0]
            : latest.Result is { } result ? $"战斗结束 · {result.WinnerTeam} · {result.Reason} · {result.TotalTicks} Tick" + (player.IsIdle ? "  |  可查看战报或重开" : "  ·  表现收尾中")
            : $"{(session.IsPlaying ? "播放中" : "已暂停")} · 单步推进完整 Tick" + (!session.IsPlaying && !player.IsIdle ? " · 表现收尾中" : "");
        if (!string.IsNullOrEmpty(player.Diagnostic)) Status.Text += " · " + player.Diagnostic.Split('\n')[0];
        Status.TooltipText = session.Diagnostic ?? $"本会话日志：{directory}\n数据指纹：{session.DataFingerprint}\n{player.Diagnostic}";
        Play.Disabled = !session.CanAdvance || session.IsPlaying;
        Pause.Disabled = !session.CanAdvance || !session.IsPlaying;
        Step.Disabled = !session.CanAdvance || session.IsPlaying;
    }
}
