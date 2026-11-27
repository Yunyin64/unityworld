using Godot;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI.Templates;

/// <summary>模板编辑与战斗舞台复用同一套 Spine 皮肤和动作。</summary>
public partial class TemplateSpinePreview : Control
{
    private ClientAssetPool.Lease<DuelSpineActor>? _actor;
    private string _pose = "Ready";
    private float _seconds;

    internal bool SetLook(BattleActorLook? look)
    {
        string? rig = look == null ? null : DuelSpineActor.ResolveRig(look.Rig);
        if (_actor?.Instance.Rig != rig)
        {
            ReleaseActor();
            if (rig != null) _actor = DuelSpineActor.Rent(rig);
        }
        if (look != null) _actor?.Instance.Equip(look);
        Visible = _actor != null;
        SetProcess(Visible);
        QueueRedraw();
        return Visible;
    }

    /// <summary>切换场景时把预览演员交还给全局池。</summary>
    public override void _ExitTree() => ReleaseActor();

    private void ReleaseActor()
    {
        var actor = _actor;
        _actor = null;
        actor?.Dispose();
    }

    internal void Preview(string pose)
    {
        _pose = pose;
        _seconds = 0;
    }

    /// <summary>预览只消耗界面时间。</summary>
    public override void _Process(double delta)
    {
        _seconds += (float)delta;
        QueueRedraw();
    }

    /// <summary>在编辑器留出的立绘位置绘制人物及脚下阴影。</summary>
    public override void _Draw()
    {
        if (_actor?.Instance is not { } actor) return;
        float height = Mathf.Min(Size.Y * .90f, Size.X * .75f);
        var foot = new Vector2(Size.X * .53f, Size.Y * .92f - 4);
        DrawSetTransform(foot, 0, new Vector2(1, .18f));
        for (int i = 0; i < 3; i++)
            DrawCircle(Vector2.Zero, height * (.37f - i * .058f), new Color("2e2a20", .085f), true, -1, true);
        DrawSetTransform(foot);
        float cycle = _seconds % 1.25f;
        actor.Sample(_pose != "Ready" && cycle < .68f ? _pose : "Ready", _pose == "Ready" ? _seconds % 2.4f : cycle);
        actor.Draw(this, height, false, Colors.White);
        DrawSetTransform(Vector2.Zero);
    }
}
