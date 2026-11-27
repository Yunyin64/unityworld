using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>让《潜鳞》的应龙沿栖霞山门盘旋，用实时飞行姿态驱动原骨骼。</summary>
public partial class ExplorationInkDragon : Node3D
{
    private const float OrbitPeriod = 15f;
    private Node3D _flyer = null!;
    private AnimationPlayer _animation = null!;
    private ExplorationInkDragonFlight _flight = null!;
    private float _phase = .34f;

    /// <summary>装配原始龙模型并启动飞行。</summary>
    public override void _Ready()
    {
        _flyer = ExplorationInkDragonMesh.Instantiate(out _animation, out var skeleton);
        _flyer.Name = "TidebornYinglong";
        AddChild(_flyer);
        _animation.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        _animation.Play(ExplorationInkDragonMesh.FlightClip);
        _flight = new ExplorationInkDragonFlight(skeleton, _animation);
        _flyer.Transform = _flight.Update(_phase, 0);
    }

    internal void SetFlying(bool flying)
    {
        Visible = flying;
        SetProcess(flying);
        _animation.Active = flying;
    }

    /// <summary>更新相对山门的绕飞位置和迎着航向的倾角。</summary>
    public override void _Process(double delta)
    {
        _phase = Mathf.PosMod(_phase + (float)delta * Mathf.Tau / OrbitPeriod, Mathf.Tau);
        _flyer.Transform = _flight.Update(_phase, (float)delta);
    }
}
