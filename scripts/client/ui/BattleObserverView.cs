using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityWorld.Core;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>Godot 战斗观察宿主；生命周期和时钟由独立会话管理，控件只读快照。</summary>
public partial class BattleObserverView : Control
{
    private readonly BattleObserverSession _session = new();
    private readonly Control _canvas = new();
    private readonly Label[] _names = new Label[2];
    private readonly Label[] _scans = new Label[2];
    private readonly Label[,] _values = new Label[2, 4];
    private readonly Label[] _actorNames = new Label[2];
    private readonly Label[] _scanStates = new Label[2];
    private readonly Label[] _pendingNames = new Label[2];
    private readonly Label[] _pendingDetails = new Label[2];
    private readonly Label[] _pendingEmpty = new Label[2];
    private readonly TextureRect[] _pendingImages = new TextureRect[2];
    private readonly Label[] _buffs = new Label[2];
    private readonly ObserverBackpackView[] _packs = new ObserverBackpackView[2];
    private Label _info = null!, _status = null!, _pending = null!, _pendingStatus = null!, _recent = null!, _logStatus = null!;
    private RichTextLabel _log = null!, _expandedLog = null!;
    private Control _logOverlay = null!;
    private Button _expandLog = null!, _closeLog = null!;
    private ObserverPreviewSkin _skin = null!;
    private Button _play = null!, _pause = null!, _step = null!, _restart = null!;
    private OptionButton _cases = null!;
    private ObserverArtCatalog _art = null!;
    private StreamWriter? _recordsFile, _framesFile;
    private string _sessionDir = "", _verifyDir = "", _verifyMode = "step";
    private int _runIndex, _restartCount, _lastTick = -1, _recordCursor, _verifyStage;
    private double _pauseWait;
    private string _pauseSnapshot = "";
    private string? _viewError;
    private bool _captureBusy, _finishedVerification, _quitting;
    private bool _capturedMotion;
    private bool _restartForVerification;
    private bool _mousePrepared;
    private readonly HashSet<int> _captured = new();
    private static readonly Color Ink = new("e8e3ce");
    private static readonly Color Muted = new("afc7c5");
    private static readonly Color PaperInk = new(0.2f, 0.22f, 0.17f);
    private static readonly Color PaperMuted = new(0.32f, 0.35f, 0.27f);

    /// <summary>建立界面后初始化唯一观察会话，保持 Tick 0 暂停。</summary>
    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--core-smoke-test")) return;
        try
        {
            BuildView();
            Resized += FitCanvas;
            FitCanvas();
            _verifyDir = Argument("--observer-verify=") ?? "";
            _verifyMode = Argument("--observer-mode=") ?? "step";
            _runIndex = int.Parse(Argument("--observer-run=") ?? "0");
            _restartCount = int.Parse(Argument("--observer-restarts=") ?? "0");
            var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + System.Environment.ProcessId;
            _sessionDir = Path.Combine(ProjectSettings.GlobalizePath("user://Logs/observer"), id);
            Directory.CreateDirectory(_sessionDir);
            LogMgr.LogFilePath = Path.Combine(_sessionDir, "core.log");
            LogMgr.FileWriteEnabled = true;
            _recordsFile = new StreamWriter(Path.Combine(_sessionDir, "records.jsonl"), false, new UTF8Encoding(false)) { AutoFlush = true };
            if (_verifyDir.Length > 0)
            {
                Directory.CreateDirectory(_verifyDir);
                _framesFile = new StreamWriter(Path.Combine(_verifyDir, $"godot-{_verifyMode}-{_runIndex}.jsonl"), false, new UTF8Encoding(false)) { AutoFlush = true };
                _session.FrameCommitted += frame => _framesFile.WriteLine(BattleJson.SerializeFrame(frame));
            }
            string? encoded = Argument("--observer-case=");
            var battleCase = encoded == null
                ? BattleCaseCatalog.Load(ProjectSettings.GlobalizePath("res://assets/testing/battle_cases.json"), Argument("--observer-case-id=") ?? "console-basic-template")
                : BattleCase.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            string dataRoot = Argument("--observer-data-root=") ?? (OS.HasFeature("editor")
                ? ProjectSettings.GlobalizePath("res://UnityWorld")
                : Path.GetDirectoryName(OS.GetExecutablePath())!);
            _session.Initialize(battleCase, dataRoot);
            File.WriteAllText(Path.Combine(_sessionDir, "input.json"), _session.Case.ToJson());
            if (_verifyDir.Length > 0) File.WriteAllText(Path.Combine(_verifyDir, $"godot-{_verifyMode}-{_runIndex}.jsonl.case.json"), _session.Case.ToJson());
            Refresh();
            _pauseSnapshot = BattleJson.SerializeSnapshot(_session.CurrentSnapshot);
            GD.Print($"[BattleObserver] ready case={_session.Case.Id} tick={_session.CurrentSnapshot.Tick} data={_session.DataFingerprint} logs={_sessionDir}");
        }
        catch (Exception exception) { ShowFailure(exception); }
    }

    /// <summary>自动播放只调用会话时钟；验证模式通过相同按钮路径操作。</summary>
    public override void _Process(double delta)
    {
        if (_quitting || _status == null || _viewError != null) return;
        _session.Update(delta);
        if (_lastTick != _session.CurrentSnapshot.Tick || _session.Diagnostic != null) Refresh();
        if (_verifyDir.Length == 0 || _captureBusy || _finishedVerification) return;
        VerifyFrame(delta);
    }

    private void BuildView()
    {
        _skin = new ObserverPreviewSkin();
        Theme = _skin.Theme;
        AddChild(_canvas);
        _canvas.Size = new Vector2(1600, 900);
        Image(_canvas, _skin.Arena, new Rect2(0, 0, 1600, 900));
        _canvas.AddChild(new ColorRect { Color = new Color(0.06f, 0.11f, 0.13f, 0.16f), Size = new Vector2(1600, 900), MouseFilter = MouseFilterEnum.Ignore });
        Panel(_canvas, new Rect2(425, 20, 750, 72), _skin.Header);
        Text(_canvas, "青岚山 · 论剑台", new Rect2(445, 22, 710, 42), 29, PaperInk, HorizontalAlignment.Center);
        _info = Text(_canvas, "正在装配案例…", new Rect2(437, 62, 726, 26), 15, PaperMuted, HorizontalAlignment.Center);
        Text(_canvas, "战斗观察 · 真实模板", new Rect2(1224, 20, 350, 30), 16, Ink, HorizontalAlignment.Right);
        var cases = _cases = new OptionButton { Position = new Vector2(24, 22), Size = new Vector2(326, 38) };
        cases.AddThemeFontSizeOverride("font_size", 17);
        cases.AddThemeStyleboxOverride("normal", _skin.Dark);
        cases.AddThemeColorOverride("font_color", Ink);
        cases.AddItem("正式模板 A / B");
        foreach (string title in new[] { "布局/模板断言：无完整战斗", "背包战斗：手工布置待适配", "时钟/效果/事件：注入待适配" })
        {
            cases.AddItem(title);
            cases.SetItemDisabled(cases.ItemCount - 1, true);
        }
        cases.TooltipText = "只显示已适配的真实案例；未适配测试保留原控制台断言。";
        _canvas.AddChild(cases);
        Panel(_canvas, new Rect2(505, 104, 590, 52), _skin.Dark);
        _play = Button("播放", new Rect2(516, 113, 76, 34), () => { _session.Play(); SetPlaying(); });
        _pause = Button("暂停", new Rect2(602, 113, 76, 34), () => { _session.Pause(); SetPlaying(); });
        _step = Button("单步 +1", new Rect2(688, 113, 100, 34), () => { if (_session.Step()) Refresh(); SetPlaying(); });
        _restart = Button("重开当前案例 ↻", new Rect2(798, 113, 286, 34), () => RestartObserver(_restartForVerification));
        _restart.TooltipText = "保留当前案例完整输入，重启观察器游戏窗口。";
        _status = Text(_canvas, "正在初始化", new Rect2(390, 163, 820, 27), 16, Ink, HorizontalAlignment.Center);
        _art = new ObserverArtCatalog();
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? 24 : 1228;
            Image(_canvas, _skin.Backpack, new Rect2(x, 166, 348, 522));
            Text(_canvas, i == 0 ? "我方 · 储物阵" : "敌方 · 储物阵", new Rect2(x + 25, 195, 298, 44), 24, PaperInk, HorizontalAlignment.Center);
            _scans[i] = Text(_canvas, "等待快照", new Rect2(x + 24, 260, 300, 25), 15, PaperMuted, HorizontalAlignment.Center);
            _packs[i] = new ObserverBackpackView { Position = new Vector2(x + 24, 296), Size = new Vector2(300, 360) };
            _packs[i].Configure(_art, _skin.Cell);
            _canvas.AddChild(_packs[i]);
            Panel(_canvas, new Rect2(x + 4, 694, 340, 40), _skin.Dark);
            _scanStates[i] = Text(_canvas, "", new Rect2(x + 12, 698, 324, 32), 14, Ink, HorizontalAlignment.Center);

            float sx = i == 0 ? 22 : 1126;
            Image(_canvas, _skin.Stats, new Rect2(sx, 750, 452, 133));
            Image(_canvas, _skin.Portraits[i], new Rect2(sx + (i == 0 ? 29 : 22), 778, 70, 76), true);
            _names[i] = Text(_canvas, "", new Rect2(sx + 115, 761, 290, 27), 19, PaperInk);
            StatField(sx + 115, 791, 310, "气血", new Color("a83d31"), i, 0);
            StatField(sx + 115, 818, 310, "灵力", new Color("286c84"), i, 1);
            StatField(sx + 115, 845, 161, "SP 占用", new Color("917124"), i, 2);
            StatField(sx + 282, 845, 143, "护盾", new Color("586e65"), i, 3);

            float px = i == 0 ? 592 : 858;
            Panel(_canvas, new Rect2(px, 200, 150, 175), _skin.Header);
            var artBox = (StyleBoxFlat)_skin.Cell.Duplicate();
            artBox.BgColor = i == 0 ? new Color("91b9a0") : new Color("d39c79");
            Panel(_canvas, new Rect2(px + 10, 210, 130, 116), artBox);
            _pendingImages[i] = Image(_canvas, null, new Rect2(px + 22, 215, 106, 105), true);
            _pendingEmpty[i] = Text(_canvas, "尚无待发", new Rect2(px + 10, 242, 130, 36), 18, PaperMuted, HorizontalAlignment.Center);
            _pendingNames[i] = Text(_canvas, "空槽", new Rect2(px + 7, 328, 136, 26), 20, PaperInk, HorizontalAlignment.Center);
            _pendingDetails[i] = Text(_canvas, "", new Rect2(px + 5, 354, 140, 19), 13, PaperMuted, HorizontalAlignment.Center);
        }
        Text(_canvas, "待 发", new Rect2(746, 229, 108, 35), 24, Ink, HorizontalAlignment.Center);
        _pending = Text(_canvas, "— : —", new Rect2(744, 270, 112, 40), 26, new Color("ffe99a"), HorizontalAlignment.Center);
        _pendingStatus = Text(_canvas, "等待出招", new Rect2(744, 315, 112, 25), 14, Ink, HorizontalAlignment.Center);
        Panel(_canvas, new Rect2(530, 387, 540, 49), _skin.Dark);
        _recent = Text(_canvas, "开场记录", new Rect2(543, 392, 514, 39), 14, Ink, HorizontalAlignment.Center);
        Image(_canvas, _skin.Characters[0], new Rect2(458, 446, 230, 221), true);
        Image(_canvas, _skin.Characters[1], new Rect2(933, 474, 219, 184), true);
        for (int i = 0; i < 2; i++)
        {
            float ax = i == 0 ? 460 : 924;
            _actorNames[i] = Text(_canvas, "", new Rect2(ax, 668, 210, 28), 20, Ink, HorizontalAlignment.Center);
            _actorNames[i].AddThemeColorOverride("font_outline_color", new Color("3a463b"));
            _actorNames[i].AddThemeConstantOverride("outline_size", 3);
            _buffs[i] = Text(_canvas, "", new Rect2(ax - 40, 704, 290, 32), 15, Ink, HorizontalAlignment.Center);
        }
        Image(_canvas, _skin.Log, new Rect2(486, 750, 628, 133));
        _logStatus = Text(_canvas, "战斗记录", new Rect2(536, 768, 425, 28), 21, PaperInk);
        _expandLog = Button("展开", new Rect2(1000, 771, 82, 26), () => ToggleLog(true));
        _expandLog.AddThemeFontSizeOverride("font_size", 14);
        _log = new RichTextLabel { Position = new Vector2(536, 803), Size = new Vector2(540, 54), BbcodeEnabled = false, ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.Off, SelectionEnabled = true };
        _log.AddThemeFontSizeOverride("normal_font_size", 15);
        _log.AddThemeColorOverride("default_color", PaperInk);
        _canvas.AddChild(_log);
        BuildExpandedLog();
    }

    private void StatField(float x, float y, float width, string name, Color color, int participant, int field)
    {
        // 等宽文字底色属于装饰，不使用 ProgressBar 或未经审核的比例。
        _canvas.AddChild(new ColorRect { Position = new Vector2(x, y), Size = new Vector2(width, 22), Color = new Color(color, 0.13f), MouseFilter = MouseFilterEnum.Ignore });
        _canvas.AddChild(new ColorRect { Position = new Vector2(x, y + 2), Size = new Vector2(3, 18), Color = color, MouseFilter = MouseFilterEnum.Ignore });
        Text(_canvas, name, new Rect2(x + 9, y, width - 50, 22), 16, PaperInk);
        _values[participant, field] = Text(_canvas, "—", new Rect2(x + width - 70, y, 60, 22), 18, color, HorizontalAlignment.Right);
        _values[participant, field].TooltipText = "真实当前值；不以初始缓存上限冒充动态比例。";
    }

    private void BuildExpandedLog()
    {
        _logOverlay = new Control { Size = new Vector2(1600, 900), Visible = false };
        _canvas.AddChild(_logOverlay);
        _logOverlay.AddChild(new ColorRect { Size = new Vector2(1600, 900), Color = new Color(0.03f, 0.07f, 0.08f, 0.66f) });
        Panel(_logOverlay, new Rect2(240, 130, 1120, 630), _skin.Header);
        Text(_logOverlay, "战斗记录 · 完整历史可滚动查看", new Rect2(275, 151, 920, 42), 25, PaperInk);
        _closeLog = Button("收起", new Rect2(1235, 153, 90, 38), () => ToggleLog(false), _logOverlay);
        _expandedLog = new RichTextLabel { Position = new Vector2(275, 214), Size = new Vector2(1050, 510), BbcodeEnabled = false, ScrollFollowing = true, SelectionEnabled = true };
        _expandedLog.AddThemeFontSizeOverride("normal_font_size", 18);
        _expandedLog.AddThemeColorOverride("default_color", PaperInk);
        _logOverlay.AddChild(_expandedLog);
    }

    private void ToggleLog(bool expanded)
    {
        _logOverlay.Visible = expanded;
        if (expanded) _expandedLog.Text = string.Join("\n", _session.Records.Select(FormatRecord)) + "\n";
    }

    private static string FormatRecord(BattleRecord record) => $"[T{record.Tick} #{record.Sequence} {record.Source}] {record.Text}";

    private string CompactRecord(BattleRecord record)
    {
        string text = $"T{record.Tick} · {record.Text.Replace('\n', ' ').Replace('\r', ' ')}";
        var font = GetThemeDefaultFont();
        if (font.GetStringSize(text, fontSize: 15).X <= 512) return text;
        while (text.Length > 0 && font.GetStringSize(text + "…", fontSize: 15).X > 512) text = text[..^1];
        return text + "…";
    }

    private static string ContestName(string kind) => kind switch
    {
        "Zhan" => "斩击", "Da" => "打击", "Ci" => "刺击", "SheJi" => "射击",
        "Shield" => "护盾", "Block" => "格挡", "Dodge" => "闪避", _ => kind
    };

    private void Refresh()
    {
        var snapshot = _session.CurrentSnapshot;
        _lastTick = snapshot.Tick;
        _info.Text = $"{_session.Case.Name}   ·   Tick {snapshot.Tick}/{_session.Case.MaxTicks}   ·   种子 {_session.Case.WorldSeed} / {_session.Case.CombatSeed}";
        _info.TooltipText = _session.Case.ToJson();
        _cases.SetItemText(0, string.IsNullOrWhiteSpace(_session.Case.Name) ? _session.Case.Id : _session.Case.Name);
        var participants = snapshot.Participants.OrderBy(p => p.Team, StringComparer.Ordinal).ToArray();
        for (int i = 0; i < Math.Min(2, participants.Length); i++)
        {
            var npc = participants[i];
            _names[i].Text = npc.Name + " · 当前值";
            _actorNames[i].Text = npc.Name;
            _scans[i].Text = $"{npc.Backpack.Width}×{npc.Backpack.Height} · 第 {npc.Backpack.Round} 轮 · 第 {npc.Backpack.Row + 1} 行";
            _scanStates[i].Text = $"行进度 {npc.Backpack.ScanProgress:0.##}/{snapshot.TicksPerRow}   加速 {npc.Backpack.HasteTicks} · 减速 {npc.Backpack.SlowTicks}";
            _packs[i].Submit(npc, snapshot.TicksPerRow, _session.IsPlaying);
            _values[i, 0].Text = npc.Hp.ToString();
            _values[i, 1].Text = npc.Mp.ToString();
            _values[i, 2].Text = npc.Sp.ToString();
            _values[i, 3].Text = npc.Shield.ToString();
            string buffs = string.Join(" · ", npc.Buffs.Select(b => $"{b.Name}×{b.Stacks}"));
            _buffs[i].Text = buffs.Length == 0 ? (npc.Status == "Active" ? "" : npc.Status == "Defeated" ? "已败退" : npc.Status) : buffs;
            _buffs[i].TooltipText = "当前值来自安全读取；最终动态上限暂无无副作用接口，未显示比例条。\n" + buffs;
        }
        for (int i = 0; i < Math.Min(2, participants.Length); i++)
        {
            var pending = participants[i].Pending;
            _pendingImages[i].Texture = pending == null ? null : _art.Card(pending.DefineId);
            _pendingEmpty[i].Visible = pending == null;
            _pendingNames[i].Text = pending?.Name ?? "空槽";
            _pendingDetails[i].Text = pending == null ? "" : $"{ContestName(pending.ContestType)} · {ObserverArtCatalog.ElementName(pending.Element)}";
        }
        _pending.Text = string.Join(" : ", participants.Select(p => p.Pending?.Value.ToString() ?? "—"));
        _pendingStatus.Text = participants.All(p => p.Pending == null) ? "等待出招" : participants.All(p => p.Pending != null) ? "等待对拼" : "等待对手";
        var recent = _session.Records.Where(r => r.Source == "event").TakeLast(2);
        _recent.Text = string.Join("\n", recent.Select(r => $"T{r.Tick}  {r.Text}"));
        while (_recordCursor < _session.Records.Count)
        {
            var record = _session.Records[_recordCursor++];
            _recordsFile?.WriteLine(JsonSerializer.Serialize(record, BattleJson.Options));
            if (_logOverlay.Visible) _expandedLog.AppendText(FormatRecord(record) + "\n");
        }
        _log.Text = string.Join("\n", _session.Records.TakeLast(2).Select(CompactRecord));
        _log.TooltipText = string.Join("\n", _session.Records.TakeLast(2).Select(FormatRecord));
        _status.Text = _session.Diagnostic != null ? "观察停止：" + _session.Diagnostic.Split('\n')[0]
            : snapshot.Result is { } result ? $"战斗结束 · {result.WinnerTeam} · {result.Reason} · {result.TotalTicks} Tick  |  可查看战报或重开当前案例"
            : $"{(_session.IsPlaying ? "播放中" : "已暂停")} · 单步推进完整 Tick";
        _status.TooltipText = _session.Diagnostic ?? $"本会话日志：{_sessionDir}\n数据指纹：{_session.DataFingerprint}";
        SetPlaying();
    }

    private void SetPlaying()
    {
        _play.Disabled = !_session.CanAdvance || _session.IsPlaying;
        _pause.Disabled = !_session.CanAdvance || !_session.IsPlaying;
        _step.Disabled = !_session.CanAdvance || _session.IsPlaying;
        foreach (var pack in _packs) pack?.SetPlaying(_session.IsPlaying);
        if (_session.CanAdvance) _status.Text = $"{(_session.IsPlaying ? "播放中" : "已暂停")} · 单步推进完整 Tick";
    }

    private void RestartObserver(bool verification)
    {
        _session.Pause();
        SetPlaying();
        try
        {
            var args = new List<string>();
            if (OS.HasFeature("editor")) args.AddRange(new[] { "--path", ProjectSettings.GlobalizePath("res://"), "res://scenes/battle/battle_observer.tscn" });
            args.Add("--");
            args.Add("--battle-observer");
            args.Add("--observer-case=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(_session.Case.ToJson())));
            if (Argument("--observer-data-root=") is { } root) args.Add("--observer-data-root=" + root);
            if (verification)
            {
                args.Add("--observer-verify=" + _verifyDir);
                args.Add("--observer-mode=" + _verifyMode);
                args.Add("--observer-run=" + (_runIndex + 1));
                args.Add("--observer-restarts=" + (_restartCount - 1));
            }
            int pid = OS.CreateProcess(OS.GetExecutablePath(), args.ToArray());
            if (pid <= 0) throw new InvalidOperationException("新游戏进程创建失败，旧窗口保留且暂停。");
            GD.Print($"[BattleObserver] restart requested pid={pid} case={_session.Case.Id}");
            _quitting = true;
            _session.Dispose();
            GetTree().Quit();
        }
        catch (Exception exception) { _status.Text = "重开失败，旧会话已暂停：" + exception.Message; _status.TooltipText = exception.ToString(); }
    }

    private async void VerifyFrame(double delta)
    {
        try
        {
            if (_session.Diagnostic != null) throw new InvalidOperationException(_session.Diagnostic);
            if (_verifyStage == 0)
            {
                _pauseWait += delta;
                if (_pauseWait < 1) return;
                if (_pauseSnapshot != BattleJson.SerializeSnapshot(_session.CurrentSnapshot)) throw new InvalidOperationException("暂停等待时状态发生变化。");
                await Capture("initial");
                Click(_expandLog);
                if (!_logOverlay.Visible) throw new InvalidOperationException("展开战报按钮未命中。");
                await Capture("expanded-log");
                Click(_closeLog);
                if (_logOverlay.Visible || _pauseSnapshot != BattleJson.SerializeSnapshot(_session.CurrentSnapshot))
                    throw new InvalidOperationException("战报收起失败或展开过程改变战斗状态。");
                Click(_step);
                if (_session.CurrentSnapshot.Tick != 1 || _session.IsPlaying) throw new InvalidOperationException("单步按钮未恰好推进一个 Tick。");
                Click(_play);
                Click(_pause);
                if (_session.IsPlaying) throw new InvalidOperationException("暂停按钮未停止播放。");
                File.WriteAllText(Path.Combine(_verifyDir, $"controls-{_verifyMode}-{_runIndex}.txt"), "PASS: paused 1s, world/battle unchanged; viewport mouse press/release hit testing: expanded log opens/closes without state changes; Step +1 Tick; Play/Pause works. Input is synthetic, routed through Godot GUI (not direct Pressed signal).\n");
                _verifyStage = 1;
                if (_verifyMode == "auto") Click(_play);
                return;
            }
            int tick = _session.CurrentSnapshot.Tick;
            if (_session.CurrentSnapshot.IsFinished)
            {
                _finishedVerification = true;
                await Capture("result");
                if (_runIndex == 0) await CaptureAssetGallery();
                File.WriteAllText(Path.Combine(_verifyDir, $"godot-{_verifyMode}-{_runIndex}.result.json"), BattleJson.SerializeSnapshot(_session.CurrentSnapshot));
                _framesFile?.Flush();
                GD.Print($"OBSERVER_VERIFY_OK mode={_verifyMode} run={_runIndex} ticks={tick}");
                if (_restartCount > 0) { _restartForVerification = true; Click(_restart); }
                else { _quitting = true; GetTree().Quit(); }
                return;
            }
            if (_runIndex == 0 && _verifyMode == "auto" && tick >= 71 && !_capturedMotion)
            {
                _capturedMotion = true;
                await CaptureMotion();
            }
            if (_runIndex == 0 && new[] { 9, 10, 11, 29, 30, 31, 40, 60, 70, 71, 100, 315, 400, 500 }.Contains(tick) && _captured.Add(tick))
                await Capture("tick-" + tick.ToString("D4"));
            if (_verifyMode == "step") Click(_step);
        }
        catch (Exception exception)
        {
            _finishedVerification = true;
            ShowFailure(exception);
            File.WriteAllText(Path.Combine(_verifyDir, $"failure-{_verifyMode}-{_runIndex}.txt"), exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task Capture(string name)
    {
        _captureBusy = true;
        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = Path.Combine(_verifyDir, $"{_verifyMode}-{_runIndex}-{name}.png");
            var error = GetViewport().GetTexture().GetImage().SavePng(path);
            if (error != Error.Ok) throw new IOException($"截图失败：{error} {path}");
        }
        finally { _captureBusy = false; }
    }

    private async Task CaptureMotion()
    {
        _captureBusy = true;
        try
        {
            var images = new List<Godot.Image>();
            var metadata = new List<string>();
            for (int frame = 0; frame < 18; frame++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                images.Add(GetViewport().GetTexture().GetImage());
                metadata.Add(JsonSerializer.Serialize(new { frame, tick = _session.CurrentSnapshot.Tick, left = _packs[0].ScanLineFraction, right = _packs[1].ScanLineFraction, playing = _session.IsPlaying }));
            }
            Click(_pause);
            var frozen = BattleJson.SerializeSnapshot(_session.CurrentSnapshot);
            float left = _packs[0].ScanLineFraction, right = _packs[1].ScanLineFraction;
            // 播放时只取帧；暂停后压缩 PNG，避免截图编码拖慢被验收的动画。
            for (int frame = 0; frame < images.Count; frame++)
            {
                var error = images[frame].SavePng(Path.Combine(_verifyDir, $"scan-motion-{frame:D2}.png"));
                images[frame].Dispose();
                if (error != Error.Ok) throw new IOException("扫描连续帧保存失败：" + error);
            }
            File.WriteAllLines(Path.Combine(_verifyDir, "scan-motion.jsonl"), metadata);
            for (int frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (frozen != BattleJson.SerializeSnapshot(_session.CurrentSnapshot) || left != _packs[0].ScanLineFraction || right != _packs[1].ScanLineFraction)
                throw new InvalidOperationException("播放后暂停未冻结快照/光线。");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_verifyDir, "scan-paused.png"));
            Click(_play);
        }
        finally { _captureBusy = false; }
    }

    private async Task CaptureAssetGallery()
    {
        var gallery = new Control { Position = new Vector2(70, 110), Size = new Vector2(1460, 740) };
        _canvas.AddChild(gallery);
        Panel(gallery, new Rect2(0, 0, 1460, 740), new Color("172f33"));
        Text(gallery, "素材核验 · 静态容器预览（非战斗实例）", new Rect2(28, 16, 1400, 40), 26, Ink);
        using var manifest = JsonDocument.Parse(File.ReadAllText(ProjectSettings.GlobalizePath("res://docs/art/card_asset_manifest.json")));
        var items = manifest.RootElement.GetProperty("cards").EnumerateArray()
            .Where(c => c.TryGetProperty("texture", out var texture) && texture.ValueKind == JsonValueKind.String).ToArray();
        int index = 0;
        foreach (var item in items)
        {
            string id = item.GetProperty("id").GetString()!;
            int cells = item.GetProperty("size").GetInt32();
            var origin = new Vector2(30 + (index % 6) * 237, 80 + (index / 6) * 210);
            var size = new Vector2(86 * Math.Min(cells, 2), 86);
            Panel(gallery, new Rect2(origin, size), new Color("cfc49e"));
            var image = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = _art.Card(id), Position = origin + new Vector2(5, 5), Size = size - new Vector2(10, 10), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
            gallery.AddChild(image);
            Text(gallery, item.GetProperty("name").GetString()! + $" · {cells} 格", new Rect2(origin + new Vector2(0, 94), new Vector2(230, 28)), 18, Ink);
            Text(gallery, id, new Rect2(origin + new Vector2(0, 125), new Vector2(230, 25)), 14, Muted);
            index++;
        }
        index = 0;
        foreach (string element in new[] { "Jin", "Mu", "Shui", "Huo", "Tu" })
        {
            var origin = new Vector2(70 + index++ * 275, 560);
            gallery.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = _art.Mana(element), Position = origin, Size = new Vector2(90, 90), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
            Text(gallery, ObserverArtCatalog.ElementName(element) + "灵元", new Rect2(origin + new Vector2(0, 96), new Vector2(200, 30)), 18, Ink);
        }
        await Capture("asset-gallery");
        gallery.QueueFree();
    }

    private void Click(Button button)
    {
        // 使用 Godot 真实 GUI 命中路径，避免直接发 Pressed 绕过遮挡与坐标检查。
        var viewport = GetViewport();
        var point = button.GetGlobalRect().GetCenter();
        if (!_mousePrepared)
        {
            if (viewport.GuiGetHoveredControl() == null) viewport.NotifyMouseEntered();
            _mousePrepared = true;
        }
        viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true }, true);
        viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }

    private void ShowFailure(Exception exception)
    {
        _viewError ??= exception.ToString();
        _session.Pause();
        GD.PushError($"[BattleObserver] {exception}");
        if (_status != null) { _status.Text = "观察器初始化/运行失败：" + exception.Message; _status.TooltipText = exception.ToString(); }
        if (_play != null) _play.Disabled = true;
        if (_pause != null) _pause.Disabled = true;
        if (_step != null) _step.Disabled = true;
        if (_sessionDir.Length > 0) File.WriteAllText(Path.Combine(_sessionDir, "failure.txt"), exception.ToString());
        if (Argument("--observer-error-capture=") is { } capturePath) _ = CaptureFailure(capturePath);
    }

    private async Task CaptureFailure(string path)
    {
        for (int i = 0; i < 30; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(new { error = _viewError, displayed = _status.Text, playDisabled = _play.Disabled, stepDisabled = _step.Disabled, capture = error.ToString() }));
        GetTree().Quit(error == Error.Ok && _play.Disabled && _step.Disabled && _status.Text.StartsWith("观察器初始化/运行失败") ? 0 : 1);
    }

    /// <summary>退出只释放本会话资源，不清理其他宿主。</summary>
    public override void _ExitTree()
    {
        _quitting = true;
        _session.Dispose();
        _framesFile?.Dispose();
        _recordsFile?.Dispose();
    }

    private string? Argument(string prefix) => OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
    private void FitCanvas()
    {
        float scale = Mathf.Min(Size.X / 1600, Size.Y / 900);
        _canvas.Scale = new Vector2(scale, scale);
        _canvas.Position = (Size - new Vector2(1600, 900) * scale) / 2;
    }
    private static Label Text(Control parent, string text, Rect2 rect, int size, Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, Position = rect.Position, Size = rect.Size, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center, ClipText = true, MouseFilter = MouseFilterEnum.Pass };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        return label;
    }
    private Button Button(string text, Rect2 rect, Action action, Control? parent = null)
    {
        var button = new Button { Text = text, Position = rect.Position, Size = rect.Size };
        button.AddThemeFontSizeOverride("font_size", 17);
        button.AddThemeStyleboxOverride("normal", _skin.Dark);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeStyleboxOverride("hover", Style(new Color("3c6265")));
        button.AddThemeStyleboxOverride("pressed", Style(new Color("527879")));
        button.Pressed += action;
        (parent ?? _canvas).AddChild(button);
        return button;
    }
    private static void Panel(Control parent, Rect2 rect, Color color)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", Style(color));
        parent.AddChild(panel);
    }
    private static void Panel(Control parent, Rect2 rect, StyleBox style)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel);
    }

    private static TextureRect Image(Control parent, Texture2D? texture, Rect2 rect, bool keepAspect = false)
    {
        var image = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = texture,
            Position = rect.Position, Size = rect.Size, StretchMode = keepAspect ? TextureRect.StretchModeEnum.KeepAspectCentered : TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(image);
        return image;
    }

    private static StyleBoxFlat Style(Color color) => new()
    {
        BgColor = color, BorderColor = new Color("9d9267", 0.8f), BorderWidthLeft = 1, BorderWidthTop = 1,
        BorderWidthRight = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9,
        CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9
    };
    private static void Texture(Control parent, string path, Rect2 rect, bool keepAspect)
    {
        if (!ResourceLoader.Exists(path)) return;
        parent.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = GD.Load<Texture2D>(path), Position = rect.Position, Size = rect.Size,
            StretchMode = keepAspect ? TextureRect.StretchModeEnum.KeepAspectCentered : TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore });
    }
}
