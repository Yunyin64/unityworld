using System;
using Godot;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Exploration;

/// <summary>在地图上确认目的地，自动或手动逐周结算这段行程。</summary>
public partial class ExplorationView : Control
{
    private ExplorationSession? _session;
    private ExplorationArt _art = null!;
    private ExplorationMap _map = null!;
    private ExplorationHud _hud = null!;
    private Timer _weekTimer = null!;
    private ExplorationSnapshot? _scheduledWeek;
    private TileId _selected;
    private bool _submitting, _automatic = true, _autoPaused;

    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.Exploration) return;
        GetNode<Button>("%ReturnHome").Pressed += GameBootstrap.OpenHome;
        try
        {
            _art = new ExplorationArt();
            _map = GetNode<ExplorationMap>("%LocalMap");
            _map.Configure(_art);
            _weekTimer = GetNode<Timer>("%WeekTimer");
            _weekTimer.Timeout += AdvanceScheduledWeek;
            _hud = new ExplorationHud(this, _art, _map, SetAutomatic, ContinueManually);
            _map.TileSelected += SelectTile;
            _map.ZoomChanged += _hud.ShowZoom;

            _session = ExplorationSession.Open();
            var state = _session.Snapshot;
            _selected = state.Position;
            Present(state);
            GD.Print($"[Exploration] ready npc={state.NpcId} tile={state.Position} radius={state.Radius} visible={state.Tiles.Count} speed={state.Speed} candidates={state.Candidates.Count}");
        }
        catch (Exception ex)
        {
            GD.PushError($"[Exploration] {ex}");
            GetNode<Control>("Hud/Place").Hide();
            GetNode<Control>("Hud/IdentityPlate").Hide();
            GetNode<Control>("Hud/PortraitFrame").Hide();
            GetNode<Control>("Hud/Calendar").Hide();
            GetNode<Control>("Hud/Controls").Hide();
            var diagnostic = GetNode<Label>("%Diagnostic");
            diagnostic.Text = "探索启动失败：" + ex.Message;
            diagnostic.Show();
        }
    }

    private void SelectTile(TileId tile)
    {
        if (_session == null || _submitting) return;
        var state = _session.Snapshot;
        if (!state.Tiles.ContainsKey(tile)) return;
        if (tile != _selected)
        {
            _selected = tile;
            Present(state);
            return;
        }

        // 只有空闲时再点合法邻格才启程。途中可以自由查看地点，续周由当前模式决定。
        if (!state.CommandsEnabled || !state.CanDrawActor || _map.IsTravelAnimating) return;
        if (state.Action.State == "idle" && state.Candidates.Contains(tile)) Submit(state, tile);
    }

    private void Present(ExplorationSnapshot state)
    {
        if (!state.Tiles.ContainsKey(_selected)) _selected = state.Position;
        _map.SetState(state, _selected);
        _hud.Show(state, _selected, _automatic, _autoPaused);
    }

    private void SetAutomatic(bool enabled)
    {
        if (_session == null || _automatic == enabled) return;
        _automatic = enabled;
        var state = _session.Snapshot;
        _autoPaused = state.CanContinueJourney && !(state.Speed > 0);
        ScheduleNextWeek(state);
        Present(state);
    }

    private void ContinueManually()
    {
        if (!_automatic && _session?.Snapshot is { CanContinueJourney: true } state)
            Submit(state, null);
    }

    private void ScheduleNextWeek(ExplorationSnapshot state)
    {
        _weekTimer.Stop();
        _scheduledWeek = null;
        if (!_automatic || _autoPaused || !state.CanContinueJourney) return;

        // 每次只预约当前行程的下一周，给玩家留出查看进度、切到手动的时间。
        _scheduledWeek = state;
        _weekTimer.Start();
    }

    private void AdvanceScheduledWeek()
    {
        var expected = _scheduledWeek;
        _scheduledWeek = null;
        if (_automatic && expected != null && ReferenceEquals(expected, _session?.Snapshot))
            Submit(expected, null);
    }

    private void Submit(ExplorationSnapshot expected, TileId? destination)
    {
        if (_session == null || _submitting) return;
        _submitting = true;
        try
        {
            var state = destination is TileId target
                ? _session.StartMove(expected, target)
                : _session.ContinueMove(expected);
            // 预检拒绝、行程被改或本周无进展时停在现场，避免自动把世界刷过多个空周。
            // 核心已抵达或已故障时 CanContinueJourney 为 false，自然结束计时。
            _autoPaused = state.CanContinueJourney &&
                (!state.MadeTravelProgress || state.Action.Target != (destination ?? expected.Action.Target) || !(state.Speed > 0));
            ScheduleNextWeek(state);
            Present(state);
            GD.Print($"[Exploration] week={state.Date.ElapsedWeeks} npc={state.NpcId} tile={state.Position} action={state.Action.State} progress={state.Action.Progress} {state.Message}");
        }
        finally { _submitting = false; }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_weekTimer))
        {
            _weekTimer.Stop();
            _weekTimer.Timeout -= AdvanceScheduledWeek;
        }
        _scheduledWeek = null;
        _session?.Dispose();
        _session = null;
    }
}
