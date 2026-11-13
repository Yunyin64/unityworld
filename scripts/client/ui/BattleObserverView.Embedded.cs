using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using Xianxia.Client.Battle;
using Xianxia.Client.BattleWindows;

namespace Xianxia.Client.UI;

public partial class BattleObserverView
{
    private Node? _source;
    private Control? _originalFocus;
    private ProcessModeEnum _originalProcessMode;
    private bool _forwardClose;
    private readonly List<CanvasItem> _hiddenItems = new();
    private readonly List<CanvasLayer> _hiddenLayers = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _independentProcesses = new();
    private string? _inputPath;
    private RemoteObserver? _remote, _replacement;

    internal IBattleObserverState? DisplayedSession => _source == null ? _session : _remote;
    internal ObserverBattleLayout? Layout => _layout;
    internal int WorkerPid => _remote?.ProcessId ?? _replacement?.ProcessId ?? 0;
    internal string? Failure => _viewError;
    internal string? WorkerLogPath => _remote?.Directory;

    internal void ConfigureEmbedded(Node source, string inputPath, string dataRoot, bool animations)
    {
        _source = source;
        _inputPath = inputPath;
        _dataRoot = dataRoot;
        _player.AnimationsEnabled = animations;
    }

    public override void _EnterTree()
    {
        // 仅在观察视图真正挂载成功后暂停来源，挂载失败时不会留下不可操作的窗口。
        if (_source == null) return;
        _originalProcessMode = _source.ProcessMode;
        _originalFocus = _source.GetViewport().GuiGetFocusOwner();
        _source.ProcessMode = ProcessModeEnum.Disabled;
        HideVisibleBranches(_source);
    }

    private void HideVisibleBranches(Node node)
    {
        // 显式 Always/Pausable 的子节点不会继承来源页的 Disabled，返回时恢复原模式。
        if (node != _source && node.ProcessMode is not (ProcessModeEnum.Inherit or ProcessModeEnum.Disabled))
        {
            _independentProcesses.Add((node, node.ProcessMode));
            node.ProcessMode = ProcessModeEnum.Disabled;
        }
        // 一页可能以普通 Node 为根，或混用独立 CanvasLayer。只恢复原先真正显示的分支。
        if (node is CanvasLayer layer && layer.Visible)
        {
            _hiddenLayers.Add(layer);
            layer.Hide();
        }
        if (node is CanvasItem item && item.IsVisibleInTree())
        {
            _hiddenItems.Add(item);
            item.Hide();
        }
        foreach (Node child in node.GetChildren()) HideVisibleBranches(child);
    }

    private void ControlSession(string command)
    {
        if (_source == null)
        {
            switch (command)
            {
                case "play": _session.Play(); break;
                case "pause": _session.Pause(); break;
                case "step": _session.Step(); break;
            }
        }
        else if (_replacement == null && _remote != null)
            _remote.Command(command);
        Refresh();
    }

    private void BeginEmbedded(string inputPath)
    {
        _viewError = null;
        _layout!.Play.Disabled = _layout.Pause.Disabled = _layout.Step.Disabled = _layout.Restart.Disabled = true;
        _layout.Status.Text = "正在装配战斗…";
        _replacement = new RemoteObserver(inputPath, _dataRoot!);
    }

    private void UpdateEmbedded()
    {
        if (_replacement != null)
        {
            BattlePresentationFrame? initial = null;
            _replacement.Poll(frame => initial = frame);
            if (_replacement.Diagnostic is { } failure)
            {
                _replacement.Dispose();
                _replacement = null;
                _layout!.Restart.Disabled = false;
                ShowFailure(new IOException(failure));
                return;
            }
            if (initial == null) return;
            // 只有替代进程交付了真正的 Tick 0 才切换，启动失败保留上一帧与原调用页。
            _remote?.Dispose();
            _remote = _replacement;
            _replacement = null;
            _player.Reset();
            _layout!.Log.Reset();
            _player.Submit(initial);
            _layout.Restart.Disabled = false;
            GD.Print($"[BattleObserver] current-window ready case={_remote.Case.Id} pid={_remote.ProcessId} logs={_remote.Directory}");
            return;
        }
        _remote?.Poll(_player.Submit);
        if (_remote?.Diagnostic is { } diagnostic) ShowFailure(new IOException(diagnostic));
    }

    private void CloseEmbedded()
    {
        if (_source == null || _quitting) return;
        _quitting = true;
        QueueFree();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest || _source == null || !IsInsideTree() || GetTree().AutoAcceptQuit) return;
        // 来源可能要弹草稿的保存确认。先结束战斗并恢复来源，再把关窗请求交还给它。
        _forwardClose = true;
        CloseEmbedded();
    }

    private void ReleaseEmbedded()
    {
        _replacement?.Dispose();
        _remote?.Dispose();
        if (!GodotObject.IsInstanceValid(_source) || !_source!.IsInsideTree() || _source.IsQueuedForDeletion()) return;
        foreach (var item in _hiddenItems)
            if (GodotObject.IsInstanceValid(item)) item.Show();
        foreach (var layer in _hiddenLayers)
            if (GodotObject.IsInstanceValid(layer)) layer.Show();
        foreach (var (node, mode) in _independentProcesses)
            if (GodotObject.IsInstanceValid(node)) node.ProcessMode = mode;
        _source.ProcessMode = _originalProcessMode;
        if (GodotObject.IsInstanceValid(_originalFocus) && _originalFocus!.IsInsideTree() && _originalFocus.IsVisibleInTree())
            _originalFocus.GrabFocus();
        if (_forwardClose)
        {
            var original = _source;
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(original) && original.IsInsideTree())
                    original.PropagateNotification((int)NotificationWMCloseRequest);
            }).CallDeferred();
        }
    }
}
