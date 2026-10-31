using Godot;
using System;
using System.Linq;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>保持原观察器构图，组合面板并把显示态路由给具体控件。</summary>
internal sealed class ObserverBattleLayout
{
    public Control Canvas { get; } = new() { Size = new Vector2(1600, 900) };
    public ObserverPreviewSkin Skin { get; } = new();
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
    public Label Status { get; }
    public string? PresentationCaption { get; set; }
    private readonly Label _info, _recent;
    private readonly Label[] _scans = new Label[2], _scanStates = new Label[2];
    private readonly OptionButton _cases;

    public ObserverBattleLayout(Control host, Action play, Action pause, Action step, Action restart)
    {
        host.Theme = Skin.Theme;
        host.AddChild(Canvas);
        Image(Canvas, Skin.Arena, new Rect2(0, 0, 1600, 900));
        Canvas.AddChild(new ColorRect { Color = new Color(0.06f, 0.11f, 0.13f, 0.16f), Size = new Vector2(1600, 900), MouseFilter = Control.MouseFilterEnum.Ignore });
        Panel(Canvas, new Rect2(425, 20, 750, 72), Skin.Header);
        Text(Canvas, "青岚山 · 论剑台", new Rect2(445, 22, 710, 42), 29, PaperInk, HorizontalAlignment.Center);
        _info = Text(Canvas, "正在装配案例…", new Rect2(437, 62, 726, 26), 15, PaperMuted, HorizontalAlignment.Center);
        Text(Canvas, "战斗观察 · 真实模板", new Rect2(1224, 20, 350, 30), 16, Ink, HorizontalAlignment.Right);
        _cases = new OptionButton { Position = new Vector2(24, 22), Size = new Vector2(326, 38) };
        _cases.AddThemeFontSizeOverride("font_size", 17);
        _cases.AddThemeStyleboxOverride("normal", Skin.Dark);
        _cases.AddThemeColorOverride("font_color", Ink);
        _cases.AddItem("正式模板 A / B");
        foreach (string title in new[] { "布局/模板断言：无完整战斗", "背包战斗：手工布置待适配", "时钟/效果/事件：注入待适配" })
        {
            _cases.AddItem(title);
            _cases.SetItemDisabled(_cases.ItemCount - 1, true);
        }
        _cases.TooltipText = "只显示已适配的真实案例；未适配测试保留原控制台断言。";
        Canvas.AddChild(_cases);
        Panel(Canvas, new Rect2(505, 104, 590, 52), Skin.Dark);
        Play = Button(Canvas, Skin, "播放", new Rect2(516, 113, 76, 34), play);
        Pause = Button(Canvas, Skin, "暂停", new Rect2(602, 113, 76, 34), pause);
        Step = Button(Canvas, Skin, "单步 +1", new Rect2(688, 113, 100, 34), step);
        Restart = Button(Canvas, Skin, "重开当前案例 ↻", new Rect2(798, 113, 286, 34), restart);
        Restart.TooltipText = "保留当前案例完整输入，重启观察器游戏窗口。";
        Status = Text(Canvas, "正在初始化", new Rect2(390, 163, 820, 27), 16, Ink, HorizontalAlignment.Center);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? 24 : 1228;
            Image(Canvas, Skin.Backpack, new Rect2(x, 166, 348, 522));
            Text(Canvas, i == 0 ? "我方 · 储物阵" : "敌方 · 储物阵", new Rect2(x + 25, 195, 298, 44), 24, PaperInk, HorizontalAlignment.Center);
            _scans[i] = Text(Canvas, "等待快照", new Rect2(x + 24, 260, 300, 25), 15, PaperMuted, HorizontalAlignment.Center);
            Packs[i] = new ObserverBackpackView { Position = new Vector2(x + 24, 296), Size = new Vector2(300, 360) };
            Packs[i].Configure(Art, Skin.Cell);
            Canvas.AddChild(Packs[i]);
            Panel(Canvas, new Rect2(x + 4, 694, 340, 40), Skin.Dark);
            _scanStates[i] = Text(Canvas, "", new Rect2(x + 12, 698, 324, 32), 14, Ink, HorizontalAlignment.Center);
            Actors[i] = new ObserverActorPanel(Canvas, Skin, i);
        }
        Pending = new ObserverPendingPanel(Canvas, Skin, Art);
        Panel(Canvas, new Rect2(530, 387, 540, 49), Skin.Dark);
        _recent = Text(Canvas, "开场记录", new Rect2(543, 392, 514, 39), 14, Ink, HorizontalAlignment.Center);
        Effects = new ObserverPresentationLayer();
        Canvas.AddChild(Effects);
        Effects.Configure(this);
        Log = new ObserverLogPanel(Canvas, Skin);
    }

    public void Fit(Vector2 size)
    {
        float scale = Mathf.Min(size.X / 1600, size.Y / 900);
        Canvas.Scale = new Vector2(scale, scale);
        Canvas.Position = (size - new Vector2(1600, 900) * scale) / 2;
    }

    public void Submit(BattleObserverSession session, BattlePresentationPlayer player, string directory)
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
