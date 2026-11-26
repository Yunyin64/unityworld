using Godot;

namespace Xianxia.Client.UI;

/// <summary>人物场景提供姿态贴图，舞台负责选择动作、站位与播放时序。</summary>
public partial class ObserverDuelActor : Control
{
    /// <summary>直刺姿态。</summary>
    [Export] public Texture2D? Strike { get; set; }
    /// <summary>架势姿态。</summary>
    [Export] public Texture2D? Guard { get; set; }
    /// <summary>命中后仰的第一帧。</summary>
    [Export] public Texture2D? Recoil { get; set; }
    /// <summary>原地结印的姿态。</summary>
    [Export] public Texture2D? Cast { get; set; }
    /// <summary>面向对手的进步。</summary>
    [Export] public Texture2D? Advance { get; set; }
    /// <summary>面向对手的撤步。</summary>
    [Export] public Texture2D? Withdraw { get; set; }
    /// <summary>斩击姿态。</summary>
    [Export] public Texture2D? Slash { get; set; }
    /// <summary>重击姿态。</summary>
    [Export] public Texture2D? Smash { get; set; }
    /// <summary>射击或将法术推出的姿态。</summary>
    [Export] public Texture2D? Shot { get; set; }
    /// <summary>成功闪避的姿态。</summary>
    [Export] public Texture2D? Evade { get; set; }
    /// <summary>败退时的单膝姿态。</summary>
    [Export] public Texture2D? Kneel { get; set; }
    /// <summary>近身出招前的蓄劲姿态。</summary>
    [Export] public Texture2D? Windup { get; set; }
    /// <summary>出招后收势的姿态。</summary>
    [Export] public Texture2D? FollowThrough { get; set; }
    /// <summary>击退后撑住地面的姿态。</summary>
    [Export] public Texture2D? Brace { get; set; }
    /// <summary>闪回时屈膝落地的姿态。</summary>
    [Export] public Texture2D? Land { get; set; }

    internal Texture2D? TextureFor(ObserverDuelStage.Pose pose, Texture2D? ready) => pose switch
    {
        ObserverDuelStage.Pose.Strike => Strike ?? ready,
        ObserverDuelStage.Pose.Guard => Guard ?? ready,
        ObserverDuelStage.Pose.Recoil => Recoil ?? ready,
        ObserverDuelStage.Pose.Cast => Cast ?? Guard ?? ready,
        ObserverDuelStage.Pose.Advance => Advance ?? ready,
        ObserverDuelStage.Pose.Withdraw => Withdraw ?? ready,
        ObserverDuelStage.Pose.Slash => Slash ?? Strike ?? ready,
        ObserverDuelStage.Pose.Smash => Smash ?? Strike ?? ready,
        ObserverDuelStage.Pose.Shot => Shot ?? Strike ?? ready,
        ObserverDuelStage.Pose.Evade => Evade ?? Guard ?? ready,
        ObserverDuelStage.Pose.Kneel => Kneel ?? Recoil ?? Guard ?? ready,
        ObserverDuelStage.Pose.Windup => Windup ?? Guard ?? ready,
        ObserverDuelStage.Pose.FollowThrough => FollowThrough ?? Strike ?? ready,
        ObserverDuelStage.Pose.Brace => Brace ?? Guard ?? ready,
        ObserverDuelStage.Pose.Land => Land ?? ready,
        _ => ready
    };
}
