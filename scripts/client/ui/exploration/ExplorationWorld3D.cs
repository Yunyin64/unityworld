using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Exploration;

/// <summary>独立的三维探索场景，使用现有探索会话，并提供可切换的地形样板。</summary>
public partial class ExplorationWorld3D : Node3D
{
    private ExplorationSession? _session;
    private ExplorationTerrainSource? _source;
    private ExplorationTerrainSurface? _surface;
    private ExplorationTerrainMesh? _mesh;
    private Node3D _terrain = null!;
    private Node3D _scenery = null!;
    private Camera3D _camera = null!;
    private ExplorationOutline3D _outline = null!;
    private DirectionalLight3D _sun = null!;
    private Godot.Environment _environment = null!;
    private Node3D? _mist;
    private MeshInstance3D? _backdrop;
    private MeshInstance3D _land = null!, _water = null!, _grid = null!, _rim = null!, _selection = null!, _outerOcean = null!;
    private Node3D _actor = null!;
    private Label _date = null!, _title = null!, _place = null!, _status = null!;
    private Button _move = null!, _mode = null!, _scenic = null!;
    private CheckButton _automatic = null!;
    private Timer _week = null!;
    private ExplorationSnapshot? _scheduled;
    private TileId _selected;
    private Vector3 _focus, _actorTarget;
    private Vector3 _sunAngle;
    private Color _sunColor;
    private float _fogNear, _fogFar, _fogDensity;
    private Viewport.Msaa _previousMsaa;
    private float _yaw, _pitch, _zoom;
    private bool _sample, _submitting, _autoPaused, _scenicView, _ownsMsaa;

    /// <summary>装配渲染节点并进入真实探索。</summary>
    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.Exploration3D) return;
        _camera = GetNode<Camera3D>("MapCamera");
        _sun = GetNode<DirectionalLight3D>("Sun");
        _sunAngle = _sun.RotationDegrees;
        _sunColor = _sun.LightColor;
        _environment = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        _fogNear = _environment.FogDepthBegin;
        _fogFar = _environment.FogDepthEnd;
        _fogDensity = _environment.FogDensity;
        _previousMsaa = GetViewport().Msaa3D;
        GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
        _ownsMsaa = true;
        _terrain = GetNode<Node3D>("Terrain");
        _land = GetNode<MeshInstance3D>("Terrain/Land");
        _water = GetNode<MeshInstance3D>("Terrain/Water");
        _rim = GetNode<MeshInstance3D>("Terrain/Rim");
        _grid = GetNode<MeshInstance3D>("Terrain/Grid");
        _selection = GetNode<MeshInstance3D>("Terrain/Selection");
        _outerOcean = GetNode<MeshInstance3D>("OuterOcean");
        _actor = GetNode<Node3D>("Actor");
        _date = GetNode<Label>("%Date");
        _title = GetNode<Label>("%WorldTitle");
        _place = GetNode<Label>("%Place");
        _status = GetNode<Label>("%Status");
        _move = GetNode<Button>("%Move");
        _mode = GetNode<Button>("%Mode");
        _scenic = GetNode<Button>("%Scenic");
        _automatic = GetNode<CheckButton>("%Automatic");
        _week = GetNode<Timer>("WeekTimer");
        _week.Timeout += AdvanceScheduled;
        GetNode<Button>("%Home").Pressed += GameBootstrap.OpenHome;
        GetNode<Button>("%Reset").Pressed += ResetCamera;
        GetNode<Button>("%ZoomIn").Pressed += () => Zoom(.82f);
        GetNode<Button>("%ZoomOut").Pressed += () => Zoom(1.22f);
        GetNode<CheckButton>("%Grid").Toggled += visible => _grid.Visible = visible;
        var outline = GetNode<MeshInstance3D>("MapCamera/OutlinePass");
        var outlineToggle = GetNode<CheckButton>("%Outline");
        _outline = new ExplorationOutline3D { Name = "OutlineProfiles" };
        AddChild(_outline);
        _outline.Configure(_camera, outline);
        _outline.Track(_land, OutlineProfile.Ground, terrain: true);
        _outline.Track(_rim, OutlineProfile.Stone);
        _outline.Track(_water, OutlineProfile.Water);
        _outline.Track(_outerOcean, OutlineProfile.Water);
        _outline.TrackTree(_actor, OutlineProfile.Figure);
        _outline.SetEnabled(outlineToggle.ButtonPressed);
        outlineToggle.Toggled += _outline.SetEnabled;
        _automatic.Toggled += OnAutomaticChanged;
        _move.Pressed += Act;
        _mode.Pressed += () => ShowMap(!_sample);
        _scenic.Pressed += ShowScenicView;

        try
        {
            _session = ExplorationSession.Open();
            ShowMap(OS.GetCmdlineUserArgs().Contains("--terrain-sample"));
        }
        catch (Exception ex)
        {
            GD.PushError($"[Exploration3D] {ex}");
            _title.Text = "探索启动失败";
            _status.Text = ex.Message;
            _move.Disabled = _mode.Disabled = true;
        }
    }

    private void ShowMap(bool sample)
    {
        if (_session == null) return;
        _sample = sample;
        var state = _session.Snapshot;
        var source = sample ? ExplorationTerrainSource.Sample() : ExplorationTerrainSource.FromSnapshot(state);
        _selected = sample ? source.Visible.First(id => source.Cells[id].Landmark.Length > 0) : state.Position;
        _outerOcean.Visible = sample;
        _rim.Visible = !sample;
        BuildTerrain(source);
        SetActor(state, true);
        _grid.Visible = GetNode<CheckButton>("%Grid").ButtonPressed;
        ResetCamera();
        Schedule(state);
        Refresh(state);
    }

    private void BuildTerrain(ExplorationTerrainSource source)
    {
        _source = source;
        _surface = new ExplorationTerrainSurface(source);
        _mesh = new ExplorationTerrainMesh(source, _surface);
        _land.Mesh = _mesh.LandMesh();
        _water.Mesh = _mesh.WaterMesh();
        _rim.Mesh = _sample ? null : _mesh.RimMesh();
        _grid.Mesh = _mesh.GridMesh(null);
        if (IsInstanceValid(_scenery))
        {
            _terrain.RemoveChild(_scenery);
            _scenery.QueueFree();
        }
        _scenery = ExplorationTerrainProps.Build(source, _surface);
        _terrain.AddChild(_scenery);
        _outline.TrackTree(_scenery, OutlineProfile.Building);
        ShowSelection();
        GD.Print($"[Exploration3D] map={(_sample ? "sample" : "world")} tiles={source.Visible.Count} " +
            $"land_triangles={_mesh.LandTriangles} water_triangles={_mesh.WaterTriangles}");
    }

    private void Select(TileId id)
    {
        if (_session == null || _source?.Visible.Contains(id) != true) return;
        var state = _session.Snapshot;
        if (!_sample && id == _selected && state.Action.State == "idle" && state.Candidates.Contains(id))
        {
            Act();
            return;
        }
        if (id == _selected) return;
        _selected = id;
        ShowSelection();
        Refresh(state);
    }

    private void ShowSelection()
    {
        if (_mesh != null) _selection.Mesh = _mesh.GridMesh(_selected);
    }

    private void Refresh(ExplorationSnapshot state)
    {
        if (_source == null) return;
        _title.Text = _sample ? "沧山入海" : "世界探索";
        _mode.Text = _sample ? "返回真实世界" : "地形样板";
        _scenic.Visible = _sample;
        _date.Visible = !_sample;
        _date.Text = $"{state.Name}   /   第 {state.Date.WeekOfYear} 周\n{state.Date}";
        _automatic.Visible = !_sample;
        _move.Visible = !_sample;
        if (_sample)
        {
            var cell = _source.Cells[_selected];
            _place.Text = cell.Landmark.Length > 0 ? $"{cell.Landmark} · {cell.Name}" : cell.Name;
            var neighbors = _selected.Neighbors().Where(_source.Cells.ContainsKey)
                .Select(id => _source.Cells[id].Name).Distinct();
            _status.Text = $"相邻地形   {string.Join(" / ", neighbors)}";
            return;
        }

        var tile = state.Tiles.GetValueOrDefault(_selected);
        _place.Text = tile == null ? "未知地块" : tile.LandmarkName.Length > 0
            ? $"{tile.LandmarkName} · {tile.Name}" : tile.Name;
        bool moving = state.CanContinueJourney;
        bool canDepart = state.CommandsEnabled && state.CanDrawActor && state.Action.State == "idle" &&
            state.Candidates.Contains(_selected);
        _move.Text = moving ? "继续一周" : "启程";
        _move.Disabled = !(canDepart || (moving && (!_automatic.ButtonPressed || _autoPaused)));
        _status.Text = moving
            ? $"行程 {state.Action.Progress:0}/{state.Action.TotalDistance:0}  {state.Message}"
            : state.Message.Length > 0 ? state.Message : _selected == state.Position ? "当前位置"
            : canDepart ? "可前往 · 再点此格或选择启程" : tile?.CanWalk == true ? "请选择相邻地块" : "此地不可步行";
        if (tile?.RegionName.Length > 0) _place.Text += $"   ·   {tile.RegionName}";
    }

    private void Act()
    {
        if (_sample || _submitting || _session?.Snapshot is not { } state) return;
        if (state.CanContinueJourney && (!_automatic.ButtonPressed || _autoPaused)) Submit(state, null);
        else if (state.CommandsEnabled && state.CanDrawActor && state.Action.State == "idle" && state.Candidates.Contains(_selected))
            Submit(state, _selected);
    }

    private void OnAutomaticChanged(bool enabled)
    {
        if (_session?.Snapshot is not { } state) return;
        _autoPaused = state.CanContinueJourney && !(state.Speed > 0);
        Schedule(state);
        Refresh(state);
    }

    private void Schedule(ExplorationSnapshot state)
    {
        _week.Stop();
        _scheduled = null;
        if (_sample || !_automatic.ButtonPressed || _autoPaused || !state.CanContinueJourney) return;
        _scheduled = state;
        _week.Start();
    }

    private void AdvanceScheduled()
    {
        var expected = _scheduled;
        _scheduled = null;
        if (!_sample && _automatic.ButtonPressed && expected != null && ReferenceEquals(expected, _session?.Snapshot))
            Submit(expected, null);
    }

    private void Submit(ExplorationSnapshot expected, TileId? target)
    {
        if (_session == null || _submitting) return;
        _submitting = true;
        try
        {
            var state = target is TileId id ? _session.StartMove(expected, id) : _session.ContinueMove(expected);
            _autoPaused = state.CanContinueJourney && (!state.MadeTravelProgress ||
                state.Action.Target != (target ?? expected.Action.Target) || !(state.Speed > 0));
            if (!state.Tiles.ContainsKey(_selected)) _selected = state.Position;
            var source = ExplorationTerrainSource.FromSnapshot(state);
            if (_source == null || !SameTerrain(_source, source)) BuildTerrain(source);
            SetActor(state, state.PlaneId != expected.PlaneId);
            if (state.Position != expected.Position || state.PlaneId != expected.PlaneId)
            {
                _focus = _actorTarget;
                PlaceCamera();
            }
            Schedule(state);
            Refresh(state);
        }
        finally { _submitting = false; }
    }

    private static bool SameTerrain(ExplorationTerrainSource old, ExplorationTerrainSource current)
    {
        if (old.PlaneId != current.PlaneId || old.Seed != current.Seed || !old.Visible.SetEquals(current.Visible) ||
            old.Cells.Count != current.Cells.Count) return false;
        foreach (var (id, cell) in current.Cells)
        {
            if (!old.Cells.TryGetValue(id, out var before) || (cell with { Water = before.Water }) != before) return false;
            var a = cell.Water;
            var b = before.Water;
            if (a.Kind != b.Kind || a.SurfaceLevel != b.SurfaceLevel || a.IsCenterline != b.IsCenterline ||
                a.IncomingMask != b.IncomingMask || a.OutDirection != b.OutDirection || a.IsSource != b.IsSource ||
                a.Width != b.Width) return false;
        }
        return true;
    }

    private void SetActor(ExplorationSnapshot state, bool instant)
    {
        _actor.Visible = !_sample && state.CanDrawActor;
        if (!_actor.Visible || _surface == null) return;
        var p = ExplorationTerrainSurface.Center(state.Position);
        if (state.CanContinueJourney && state.Action.Target is TileId target && state.Action.TotalDistance > 0)
        {
            float fraction = Mathf.Clamp((float)(state.Action.Progress / state.Action.TotalDistance), 0f, 1f);
            p = p.Lerp(ExplorationTerrainSurface.Center(target), fraction);
        }
        _actorTarget = ExplorationTerrainSurface.At(p, _surface.Sample(p).Surface);
        if (instant) _actor.Position = _actorTarget;
    }

    private void ResetCamera()
    {
        if (_source == null || _surface == null) return;
        _scenicView = false;
        SetScenicLighting(false);
        _yaw = -.18f;
        _pitch = .73f;
        _zoom = _sample ? 19.4f : 13.8f;
        _focus = _sample ? new Vector3(0f, .6f, -.7f)
            : _actorTarget;
        PlaceCamera();
    }

    private void ShowScenicView()
    {
        if (_scenicView)
        {
            ResetCamera();
            return;
        }
        if (!_sample || _source?.Gate is not TileId gate || _surface == null || _session == null) return;
        Select(gate);
        var point = ExplorationTerrainSurface.Center(gate);
        _focus = ExplorationTerrainSurface.At(point, _surface.Sample(point).Ground + .96f);
        _yaw = .15f;
        _pitch = .20f;
        _zoom = 6.35f;
        _scenicView = true;
        SetScenicLighting(true);
        PlaceCamera();
    }

    private void SetScenicLighting(bool enabled)
    {
        _environment.BackgroundMode = enabled ? Godot.Environment.BGMode.Sky : Godot.Environment.BGMode.Color;
        _environment.VolumetricFogEnabled = enabled;
        _environment.FogSkyAffect = enabled ? 0f : 1f;
        _environment.FogDepthBegin = enabled ? 11f : _fogNear;
        _environment.FogDepthEnd = enabled ? 45f : _fogFar;
        _environment.FogDensity = enabled ? .61f : _fogDensity;
        _sun.RotationDegrees = enabled ? new Vector3(-25, -56, 0) : _sunAngle;
        _sun.LightColor = enabled ? new Color("#ffc797") : _sunColor;
        _scenic.Text = enabled ? "返回全景" : "山门近景";
        if (!enabled)
        {
            _backdrop?.Hide();
            if (_mist is { } mist && IsInstanceValid(mist))
            {
                RemoveChild(mist);
                mist.QueueFree();
            }
            _mist = null;
            return;
        }

        if (_backdrop == null)
        {
            _backdrop = ExplorationScenicBackdrop.Build(_land.MaterialOverride);
            AddChild(_backdrop);
            _outline.Track(_backdrop, OutlineProfile.Mountain);
        }
        _backdrop.Show();
        var fog = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/presentation/exploration_mist_3d.gdshader") };
        _mist = new Node3D { Name = "ScenicMist", Position = new Vector3(_focus.X, 0, _focus.Z) };
        _mist.AddChild(new FogVolume
        {
            Shape = RenderingServer.FogVolumeShape.Box, Material = fog, Size = new Vector3(21, 3.2f, 8.2f),
            Position = new Vector3(.12f, 1.15f, -5.4f)
        });
        _mist.AddChild(new FogVolume
        {
            Shape = RenderingServer.FogVolumeShape.Box, Material = fog, Size = new Vector3(13, 1.6f, 8.5f),
            Position = new Vector3(-4.65f, .72f, 1.4f)
        });
        _mist.AddChild(new FogVolume
        {
            Shape = RenderingServer.FogVolumeShape.Box, Material = fog, Size = new Vector3(55, 7f, 17f),
            Position = new Vector3(-3f, 1.6f, -25f)
        });
        AddChild(_mist);
    }

    private void PlaceCamera()
    {
        var horizontal = new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw));
        var direction = horizontal * Mathf.Cos(_pitch) + Vector3.Up * Mathf.Sin(_pitch);
        _camera.Projection = _scenicView ? Camera3D.ProjectionType.Perspective : Camera3D.ProjectionType.Orthogonal;
        _camera.Far = _scenicView ? 285f : 110f;
        _camera.Position = _focus + direction * (_scenicView ? _zoom : 36f);
        _camera.LookAt(_focus, Vector3.Up);
        if (_scenicView) _camera.Fov = 43f;
        else _camera.Size = _zoom;
    }

    private void Zoom(float multiplier)
    {
        if (_source == null) return;
        _zoom = Mathf.Clamp(_zoom * multiplier, _scenicView ? 1.9f : 2.75f, _scenicView ? 11f : _sample ? 38f : 26f);
        PlaceCamera();
    }

    /// <summary>镜头操作不消费游戏周。左键从实际表面拾取所属格。</summary>
    public override void _UnhandledInput(InputEvent input)
    {
        if (_mesh == null || _camera == null) return;
        switch (input)
        {
            case InputEventMouseButton { Pressed: true } button:
                switch (button.ButtonIndex)
                {
                    case MouseButton.WheelUp: Zoom(.86f); break;
                    case MouseButton.WheelDown: Zoom(1.16f); break;
                    case MouseButton.Left:
                        if (_mesh.Pick(_camera.ProjectRayOrigin(button.Position),
                            _camera.ProjectRayNormal(button.Position), _camera.Far) is TileId id) Select(id);
                        break;
                    default: return;
                }
                break;
            case InputEventMouseMotion motion when (motion.ButtonMask & MouseButtonMask.Right) != 0:
                _yaw -= motion.Relative.X * .005f;
                _pitch = Mathf.Clamp(_pitch + motion.Relative.Y * .004f, _scenicView ? .10f : .42f, 1.48f);
                PlaceCamera();
                break;
            case InputEventMouseMotion motion when (motion.ButtonMask & MouseButtonMask.Middle) != 0:
                var basis = _camera.Basis;
                var forward = new Vector3(basis.Z.X, 0f, basis.Z.Z).Normalized();
                float factor = _zoom / GetViewport().GetVisibleRect().Size.Y;
                _focus += (-basis.X * motion.Relative.X + forward * motion.Relative.Y / Mathf.Sin(_pitch)) * factor;
                PlaceCamera();
                break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>跟随真实行程进度平滑移动立体角色标记。</summary>
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_actor) || !_actor.Visible || _surface == null) return;
        var current = new Vector2(_actor.Position.X, _actor.Position.Z);
        var target = new Vector2(_actorTarget.X, _actorTarget.Z);
        if (current.DistanceSquaredTo(target) < .000001f) return;
        var next = current.MoveToward(target, (float)delta * 2.7f);
        _actor.Position = ExplorationTerrainSurface.At(next, _surface.Sample(next).Surface);
    }

    /// <summary>返回首页或退出时取消周结算预约并归还真实世界。</summary>
    public override void _ExitTree()
    {
        if (IsInstanceValid(_week))
        {
            _week.Stop();
            _week.Timeout -= AdvanceScheduled;
        }
        _scheduled = null;
        if (_ownsMsaa) GetViewport().Msaa3D = _previousMsaa;
        _session?.Dispose();
        _session = null;
    }
}
