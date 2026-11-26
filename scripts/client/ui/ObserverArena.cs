using Godot;
using System;

namespace Xianxia.Client.UI;

/// <summary>让固定舞台、滚动远山和云层共用战斗镜头与表现时钟。</summary>
public partial class ObserverArena : TextureRect
{
    private const float ReferenceWidth = 1920f;

    /// <summary>山脉向左滚动的速度，以 1920 像素画宽为基准。</summary>
    [Export(PropertyHint.Range, "0,40,0.5")] public float MountainPixelsPerSecond { get; set; } = 9f;
    /// <summary>云纹向任一方向漂移的最大像素数，以 1920 像素画宽为基准。</summary>
    [Export(PropertyHint.Range, "0,40,1")] public float CloudDriftPixels { get; set; } = 21f;
    /// <summary>云纹缓慢往返一轮的时长，单位为秒。</summary>
    [Export(PropertyHint.Range, "4,60,1")] public float CloudCycleSeconds { get; set; } = 20f;
    /// <summary>每秒切换的云层采样帧数。</summary>
    [Export(PropertyHint.Range, "1,60,1")] public int CloudFramesPerSecond { get; set; } = 18;
    private ShaderMaterial _sky = null!;
    private ShaderMaterial _mountains = null!;
    private double _elapsed;
    internal bool PresentationFrozen { get; set; }
    internal int CloudFrame { get; private set; }
    internal float CloudOffset { get; private set; }
    internal float MountainOffset { get; private set; }

    /// <summary>复制材质参数，让同窗观察器各自持有环境时钟。</summary>
    public override void _Ready()
    {
        var sky = GetNode<TextureRect>("Sky");
        var mountains = GetNode<TextureRect>("Mountains");
        sky.Material = _sky = (ShaderMaterial)sky.Material.Duplicate();
        mountains.Material = _mountains = (ShaderMaterial)mountains.Material.Duplicate();
    }

    /// <summary>连续滚动山脉，并按固定帧率更新两片原图云纹。</summary>
    public override void _Process(double delta)
    {
        if (PresentationFrozen) return;
        _elapsed += delta;

        // 全景图覆盖两个画宽，到末端后从另一侧无缝接回。
        MountainOffset = (float)(_elapsed * MountainPixelsPerSecond / (2 * ReferenceWidth) % 1);
        _mountains.SetShaderParameter("panorama_offset", MountainOffset);

        int fps = Math.Max(1, CloudFramesPerSecond);
        int cloudFrame = (int)Math.Floor(_elapsed * fps + 1e-9);
        if (cloudFrame == CloudFrame) return;
        CloudFrame = cloudFrame;
        double phase = (CloudFrame / (fps * (double)Math.Max(1, CloudCycleSeconds))) % 1;
        CloudOffset = (float)(Math.Sin(phase * Math.Tau) * CloudDriftPixels / ReferenceWidth);
        _sky.SetShaderParameter("cloud_offset", CloudOffset);
        _sky.SetShaderParameter("upper_cloud_offset", (float)(Math.Cos(phase * Math.Tau) - 1) * CloudDriftPixels / (2 * ReferenceWidth));
    }
}
