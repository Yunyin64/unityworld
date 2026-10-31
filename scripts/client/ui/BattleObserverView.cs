using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>观察宿主只组合会话、播放器、视图及验证驱动；不持有核心实体。</summary>
public partial class BattleObserverView : Control
{
    private readonly BattleObserverSession _session = new();
    private readonly BattlePresentationPlayer _player = new();
    private ObserverBattleLayout? _layout;
    private ObserverSessionOutput? _output;
    private ObserverVerificationDriver? _verification;
    private string? _viewError;
    private bool _quitting;
    private bool _fixture;

    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--core-smoke-test")) return;
        try
        {
            _layout = new ObserverBattleLayout(this,
                () => { _session.Play(); Refresh(); },
                () => { _session.Pause(); Refresh(); },
                () => { _session.Step(); Refresh(); },
                () => RestartObserver(false));
            Resized += () => _layout.Fit(Size);
            _layout.Fit(Size);
            _output = new ObserverSessionOutput();
            _output.Attach(_session);
            _player.AnimationsEnabled = !OS.GetCmdlineUserArgs().Contains("--observer-no-animations");
            // 每个 Tick 都交付，不能只在 Update 后读取最后一帧。
            _session.PresentationCommitted += _player.Submit;
            string? encoded = ObserverSessionOutput.Argument("--observer-case=");
            var battleCase = encoded == null
                ? BattleCaseCatalog.Load(ProjectSettings.GlobalizePath("res://assets/testing/battle_cases.json"), ObserverSessionOutput.Argument("--observer-case-id=") ?? "console-basic-template")
                : BattleCase.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            string dataRoot = ObserverSessionOutput.Argument("--observer-data-root=") ?? (OS.HasFeature("editor")
                ? ProjectSettings.GlobalizePath("res://UnityWorld")
                : Path.GetDirectoryName(OS.GetExecutablePath())!);
            _session.Initialize(battleCase, dataRoot);
            _output.SaveInput(_session.Case);
            Refresh();
            if (ObserverSessionOutput.Argument("--observer-presentation-fixture=") is { } fixtureDirectory)
            {
                _fixture = true;
                _ = new ObserverPresentationFixture(this, _layout, _player, fixtureDirectory).Run();
                return;
            }
            _verification = new ObserverVerificationDriver(this, _session, _player, _layout, _output);
            GD.Print($"[BattleObserver] ready case={_session.Case.Id} tick={_session.CurrentSnapshot.Tick} data={_session.DataFingerprint} logs={_output.SessionDirectory}");
        }
        catch (Exception exception) { ShowFailure(exception); }
    }

    public override void _Process(double delta)
    {
        if (_quitting || _fixture || _layout == null || _viewError != null) return;
        try
        {
            _session.Update(delta);
            // 暂停或正常结束只停逻辑；已经交付的表现继续走客户端时钟。
            _player.Update(delta);
            Refresh();
            _verification?.Update(delta);
        }
        catch (Exception exception) { ShowFailure(exception); }
    }

    private void Refresh()
    {
        if (_layout == null || _output == null) return;
        _layout.Submit(_session, _player, _output.SessionDirectory);
        _output.AppendRecords(_session);
    }

    internal void RefreshForFixture() => Refresh();

    internal void RestartObserver(bool verification)
    {
        _session.Pause();
        Refresh();
        try
        {
            var args = new List<string>();
            if (OS.HasFeature("editor")) args.AddRange(new[] { "--path", ProjectSettings.GlobalizePath("res://"), "res://scenes/battle/battle_observer.tscn" });
            args.Add("--");
            args.Add("--battle-observer");
            args.Add("--observer-case=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(_session.Case.ToJson())));
            if (ObserverSessionOutput.Argument("--observer-data-root=") is { } root) args.Add("--observer-data-root=" + root);
            if (!_player.AnimationsEnabled) args.Add("--observer-no-animations");
            if (verification && _output != null)
            {
                args.Add("--observer-verify=" + _output.VerifyDirectory);
                args.Add("--observer-mode=" + _output.VerifyMode);
                args.Add("--observer-run=" + (_output.RunIndex + 1));
                args.Add("--observer-restarts=" + (_output.Restarts - 1));
            }
            int pid = OS.CreateProcess(OS.GetExecutablePath(), args.ToArray());
            if (pid <= 0) throw new InvalidOperationException("新游戏进程创建失败，旧窗口保留且暂停。");
            GD.Print($"[BattleObserver] restart requested pid={pid} case={_session.Case.Id}");
            StopForVerification();
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            if (_layout == null) return;
            _layout.Status.Text = "重开失败，旧会话已暂停：" + exception.Message;
            _layout.Status.TooltipText = exception.ToString();
        }
    }

    internal void StopForVerification()
    {
        _quitting = true;
        _player.Cancel();
        _session.Dispose();
    }

    internal void ShowFailure(Exception exception)
    {
        _viewError ??= exception.ToString();
        _session.Pause();
        GD.PushError($"[BattleObserver] {exception}");
        if (_layout != null)
        {
            _layout.Status.Text = "观察器初始化/运行失败：" + exception.Message;
            _layout.Status.TooltipText = exception.ToString();
            _layout.Play.Disabled = _layout.Pause.Disabled = _layout.Step.Disabled = true;
        }
        if (_output != null) File.WriteAllText(Path.Combine(_output.SessionDirectory, "failure.txt"), exception.ToString());
        if (_layout != null && ObserverSessionOutput.Argument("--observer-error-capture=") is { } path)
            _ = ObserverVerificationDriver.CaptureFailure(this, _layout, path, _viewError);
    }

    public override void _ExitTree()
    {
        _quitting = true;
        _session.PresentationCommitted -= _player.Submit;
        _player.Cancel();
        _session.Dispose();
        _output?.Dispose();
    }
}
