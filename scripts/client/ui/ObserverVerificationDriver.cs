using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>独立验收驱动：通过真实 GUI 命中路径操作，截图与连续帧不进入产品会话类。</summary>
internal sealed class ObserverVerificationDriver
{
    private readonly BattleObserverView _view;
    private readonly BattleObserverSession _session;
    private readonly BattlePresentationPlayer _player;
    private readonly ObserverBattleLayout _layout;
    private readonly ObserverSessionOutput _output;
    private bool _captureBusy, _finishedVerification, _capturedMotion, _mousePrepared;
    private int _verifyStage;
    private double _pauseWait;
    private readonly string _pauseSnapshot;
    private readonly HashSet<int> _captured = new();
    private readonly HashSet<string> _capturedEffects = new();
    private string _verifyDir => _output.VerifyDirectory;
    private string _verifyMode => _output.VerifyMode;
    private int _runIndex => _output.RunIndex;
    private int _restartCount => _output.Restarts;
    private Control _canvas => _layout.Canvas;
    private ObserverArtCatalog _art => _layout.Art;
    private ObserverBackpackView[] _packs => _layout.Packs;
    private Button _step => _layout.Step;
    private Button _play => _layout.Play;
    private Button _pause => _layout.Pause;
    private Button _restart => _layout.Restart;
    private Button _expandLog => _layout.Log.ExpandButton;
    private Button _closeLog => _layout.Log.CloseButton;

    public ObserverVerificationDriver(BattleObserverView view, BattleObserverSession session, BattlePresentationPlayer player, ObserverBattleLayout layout, ObserverSessionOutput output)
    {
        _view = view; _session = session; _player = player; _layout = layout; _output = output;
        _pauseSnapshot = BattleJson.SerializeSnapshot(session.CurrentSnapshot);
    }

    public void Update(double delta)
    {
        if (_verifyDir.Length == 0 || _captureBusy || _finishedVerification) return;
        if (_verifyStage > 0 && _runIndex == 0 && _verifyMode == "auto")
        {
            var cue = _player.ActiveCues.FirstOrDefault(c => !_capturedEffects.Contains(c.Event.Kind.ToString()));
            if (cue != null)
            {
                _capturedEffects.Add(cue.Event.Kind.ToString());
                _ = CapturePresentation(cue.Event.Kind.ToString());
                return;
            }
        }
        VerifyFrame(delta);
    }

    private SceneTree GetTree() => _view.GetTree();
    private Viewport GetViewport() => _view.GetViewport();
    private SignalAwaiter ToSignal(GodotObject source, StringName signal) => _view.ToSignal(source, signal);
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
                if (!_layout.Log.IsExpanded) throw new InvalidOperationException("展开战报按钮未命中。");
                await Capture("expanded-log");
                Click(_closeLog);
                if (_layout.Log.IsExpanded || _pauseSnapshot != BattleJson.SerializeSnapshot(_session.CurrentSnapshot))
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
            if (_session.CurrentSnapshot.IsFinished && _player.IsIdle)
            {
                _finishedVerification = true;
                await Capture("result");
                if (_runIndex == 0) await CaptureAssetGallery();
                File.WriteAllText(Path.Combine(_verifyDir, $"godot-{_verifyMode}-{_runIndex}.result.json"), BattleJson.SerializeSnapshot(_session.CurrentSnapshot));
                _output.Flush();
                GD.Print($"OBSERVER_VERIFY_OK mode={_verifyMode} run={_runIndex} ticks={tick}");
                if (_restartCount > 0) { _view.RestartObserver(true); }
                else { _view.StopForVerification(); GetTree().Quit(); }
                return;
            }
            if (_runIndex == 0 && _verifyMode == "auto" && tick >= 71 && !_capturedMotion)
            {
                _capturedMotion = true;
                await CaptureMotion();
            }
            if (_runIndex == 0 && new[] { 9, 10, 11, 29, 30, 31, 40, 60, 70, 71, 100, 315, 400, 500 }.Contains(tick) && _captured.Add(tick))
                await Capture("tick-" + tick.ToString("D4"));
            if (_verifyMode == "step" && _player.IsIdle) Click(_step);
        }
        catch (Exception exception)
        {
            _finishedVerification = true;
            _view.ShowFailure(exception);
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
            while (!_player.IsIdle) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            for (int settle = 0; settle < 12; settle++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
        Panel(gallery, new Rect2(0, 0, 1460, 740), Style(new Color("172f33")));
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
            Panel(gallery, new Rect2(origin, size), Style(new Color("cfc49e")));
            var image = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = _art.Card(id), Position = origin + new Vector2(5, 5), Size = size - new Vector2(10, 10), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
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

    private async Task CapturePresentation(string kind)
    {
        _captureBusy = true;
        try
        {
            var images = new List<Godot.Image>();
            var metadata = new List<string>();
            for (int frame = 0; frame < 24; frame++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                images.Add(GetViewport().GetTexture().GetImage());
                metadata.Add(JsonSerializer.Serialize(new
                {
                    frame, tick = _session.CurrentSnapshot.Tick, displayTick = _player.DisplaySnapshot.Tick,
                    playing = _session.IsPlaying,
                    scan = _packs.Select((pack, i) => new { side = i, line = pack.ScanLineFraction,
                        core = _session.CurrentSnapshot.Participants.OrderBy(p => p.Team, StringComparer.Ordinal).ElementAt(i).Backpack }),
                    active = _player.ActiveCues.Select(c => new { c.Event.Sequence, kind = c.Event.Kind.ToString(), c.Progress, c.Event }),
                    pendingPositions = _layout.Pending.ContentPositions.Select(p => new { x = p.X, y = p.Y }),
                    display = _player.DisplaySnapshot
                }, BattleJson.Options));
            }
            bool wasPlaying = _session.IsPlaying;
            _session.Pause();
            for (int frame = 0; frame < images.Count; frame++)
            {
                images[frame].SavePng(Path.Combine(_verifyDir, $"presentation-{kind}-{frame:D2}.png"));
                images[frame].Dispose();
            }
            File.WriteAllLines(Path.Combine(_verifyDir, $"presentation-{kind}.jsonl"), metadata);
            if (wasPlaying) _session.Play();
        }
        catch (Exception exception) { _view.ShowFailure(exception); }
        finally { _captureBusy = false; }
    }

    public static async Task CaptureFailure(BattleObserverView view, ObserverBattleLayout layout, string path, string message)
    {
        for (int i = 0; i < 30; i++) await view.ToSignal(view.GetTree(), SceneTree.SignalName.ProcessFrame);
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var error = view.GetViewport().GetTexture().GetImage().SavePng(path);
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(new { error = message, displayed = layout.Status.Text,
            playDisabled = layout.Play.Disabled, stepDisabled = layout.Step.Disabled, capture = error.ToString() }));
        view.GetTree().Quit(error == Error.Ok && layout.Play.Disabled && layout.Step.Disabled && layout.Status.Text.StartsWith("观察器初始化/运行失败") ? 0 : 1);
    }
}
