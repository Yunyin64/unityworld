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
    public ObserverPendingPanel Pending { get; } = new();
    public ObserverLogPanel Log { get; }
    public ObserverPresentationLayer Effects { get; }
    internal ObserverDuelStage Stage { get; }
    public Button Play { get; }
    public Button Pause { get; }
    public Button Step { get; }
    public Button Restart { get; }
    public Button Return { get; }
    public Label Status { get; }
    public string? PresentationCaption { get; set; }
    private readonly Label _info;
    private readonly Label[] _scans = new Label[2], _scanStates = new Label[2];
    private readonly OptionButton _cases;
    private readonly Control _packsArea, _duel, _logView, _footerLog, _fieldLog, _header, _historyPaper;
    private readonly Control _history;
    private readonly Vector2 _headerSize, _paperSize;

    public ObserverBattleLayout(Control host, Action play, Action pause, Action step, Action restart)
    {
        Canvas = host.GetNode<Control>("%Canvas");
        FieldScroll = Canvas.GetNode<ScrollContainer>("FieldScroll");
        Field = FieldScroll.GetNode<Control>("Field");
        _packsArea = Field.GetNode<Control>("Packs");
        _duel = Field.GetNode<Control>("Duel");
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
            Packs[i].Configure(Art, Packs[i].GetThemeStylebox("panel", "CellPanel") as StyleBoxFlat);
            Actors[i] = new ObserverActorPanel(_duel.GetNode<ObserverDuelActor>(side + "Actor"), Canvas.GetNode<Control>(side + "Stats"));
        }
        Stage = Field.GetNode<ObserverDuelStage>("Stage");
        Stage.Configure(this);
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
        // 角色按实际落脚点布局。旧的图片占位框比实际远景角色大，不能拿它判断两人是否站得下。
        var leftPack = Packs[0].GetParent<Control>();
        var rightPack = Packs[1].GetParent<Control>();
        float center = Field.Size.X / 2;
        float innerLeft = BoundsIn(Field, leftPack).End.X;
        float innerRight = BoundsIn(Field, rightPack).Position.X;
        float gap = innerRight - innerLeft;
        bool stagger = gap < 596;
        float footerGap = BoundsIn(Canvas, Canvas.GetNode<Control>("RightStats")).Position.X
            - BoundsIn(Canvas, Canvas.GetNode<Control>("LeftStats")).End.X;
        bool logInField = footerGap < _logView.Size.X * _logView.Scale.X + 16;
        float logHeight = logInField ? _logView.Size.Y * _logView.Scale.Y + 19 : 0;
        Field.CustomMinimumSize = new Vector2(0, 584 + logHeight);
        _packsArea.OffsetBottom = _duel.OffsetBottom = -logHeight;

        float distance = Mathf.Clamp(gap * .28f, 115, 342);
        float left = Math.Max(-distance, innerLeft - center + 58) - Actors[0].ActorSize.X / 2;
        float right = Math.Min(distance, innerRight - center - 58) - Actors[1].ActorSize.X / 2;
        Actors[0].Arrange(left, false);
        Actors[1].Arrange(right, stagger);
        var logParent = logInField ? _fieldLog : _footerLog;
        if (_logView.GetParent() != logParent) _logView.Reparent(logParent, false);
        Log.CompactShortcut.Visible = logInField;

        float headerWidth = Mathf.Clamp((Canvas.Size.X - 2 * (BoundsIn(Canvas, _cases).End.X + 12)) / _header.Scale.X, 80, _headerSize.X);
        _header.OffsetLeft = -headerWidth / 2;
        _header.OffsetRight = headerWidth / 2;
        _header.PivotOffset = new Vector2(headerWidth / 2, 0);
        // 展开战报浮在可见窗口上方；外层滚动过后，收起按钮仍在屏内。
        var paperSize = _paperSize.Min(_history.GetParent<Control>().Size - new Vector2(40, 40));
        _historyPaper.OffsetLeft = -paperSize.X / 2;
        _historyPaper.OffsetRight = paperSize.X / 2;
        _historyPaper.OffsetTop = -paperSize.Y / 2;
        _historyPaper.OffsetBottom = paperSize.Y / 2;
        Stage.RefreshGeometry();
    }

    private static Rect2 BoundsIn(Control parent, Control control) =>
        (parent.GetGlobalTransform().AffineInverse() * control.GetGlobalTransform()) * new Rect2(Vector2.Zero, control.Size);

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
            // 败退演出占用同一舞台区域，常驻状态在终镜结束后恢复显示。
            bool playingDefeat = player.ActiveCues.Any(c => c.Exchange?.FinishingNpcId == npc.Id);
            Actors[i].Submit(npc, playingDefeat);
            _scans[i].Text = $"{scan.Width}×{scan.Height} · 第 {scan.Round} 轮 · 第 {scan.Row + 1} 行";
            _scanStates[i].Text = $"行进度 {scan.ScanProgress:0.##}/{latest.TicksPerRow}   加速 {scan.HasteTicks} · 减速 {scan.SlowTicks}";
            Packs[i].Submit(npc, latest.TicksPerRow, session.IsPlaying, scan);
        }
        Pending.Submit(participants, player.ActiveCues);
        Stage.Submit(participants, player);
        foreach (var actor in Actors) actor.SetStageLabelOpacity(Stage.BackpackOpacity);
        Effects.Submit(participants, player.ActiveCues);
        Log.Submit(session.Records);
        Status.Text = session.Diagnostic != null ? "观察停止：" + session.Diagnostic.Split('\n')[0]
            : snapshot.Result is { } result ? $"战斗结束 · {result.WinnerTeam} · {result.Reason} · {result.TotalTicks} Tick  |  可查看战报或重开"
            : latest.IsFinished && !player.IsIdle ? "终招结算中"
            : $"{(session.IsPlaying ? "播放中" : "已暂停")} · 单步推进完整 Tick" + (!session.IsPlaying && !player.IsIdle ? " · 表现收尾中" : "");
        if (!string.IsNullOrEmpty(player.Diagnostic)) Status.Text += " · " + player.Diagnostic.Split('\n')[0];
        Status.TooltipText = session.Diagnostic ?? $"本会话日志：{directory}\n数据指纹：{session.DataFingerprint}\n{player.Diagnostic}";
        Play.Disabled = !session.CanAdvance || session.IsPlaying;
        Pause.Disabled = !session.CanAdvance || !session.IsPlaying;
        Step.Disabled = !session.CanAdvance || session.IsPlaying;
    }
}
