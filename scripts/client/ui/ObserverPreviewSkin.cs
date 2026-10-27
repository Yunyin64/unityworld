using Godot;

namespace Xianxia.Client.UI;

/// <summary>从原素材预览取出可复用的 UI 资源；预览节点不进入场景树，也不运行示例脚本。</summary>
public sealed class ObserverPreviewSkin
{
    /// <summary>原场景字体与默认字号。</summary>
    public Theme Theme { get; }
    /// <summary>原场地背景及裁切。</summary>
    public Texture2D Arena { get; }
    /// <summary>原卷轴背包底图。</summary>
    public Texture2D Backpack { get; }
    /// <summary>原画像属性框。</summary>
    public Texture2D Stats { get; }
    /// <summary>原战报卷轴。</summary>
    public Texture2D Log { get; }
    /// <summary>原人物裁切资源。</summary>
    public Texture2D[] Characters { get; }
    /// <summary>原画像裁切资源。</summary>
    public Texture2D[] Portraits { get; }
    /// <summary>原纸质牌面样式。</summary>
    public StyleBox Header { get; }
    /// <summary>原深色小信息条样式。</summary>
    public StyleBox Dark { get; }
    /// <summary>原圆角背包格样式。</summary>
    public StyleBoxFlat Cell { get; }

    /// <summary>资源共享于两个场景；后续修改样式须先 Duplicate。</summary>
    public ObserverPreviewSkin()
    {
        var source = GD.Load<PackedScene>("res://scenes/tests/battle_assets_preview.tscn").Instantiate<Control>();
        try
        {
            Theme = source.Theme;
            Arena = source.GetNode<TextureRect>("Arena").Texture;
            Backpack = source.GetNode<TextureRect>("Hud/PlayerBackpack/Base").Texture;
            Stats = source.GetNode<TextureRect>("Hud/PlayerStats/Base").Texture;
            Log = source.GetNode<TextureRect>("Hud/CombatLog/Base").Texture;
            Characters = new[] { source.GetNode<TextureRect>("Combatants/Swordsman").Texture,
                source.GetNode<TextureRect>("Combatants/CrimsonCultivator").Texture };
            Portraits = new[] { source.GetNode<TextureRect>("Hud/PlayerStats/Portrait").Texture,
                source.GetNode<TextureRect>("Hud/EnemyStats/Portrait").Texture };
            Header = source.GetNode<Panel>("Hud/Header").GetThemeStylebox("panel");
            Dark = source.GetNode<Panel>("Hud/GlobalEffects").GetThemeStylebox("panel");
            Cell = (StyleBoxFlat)source.GetNode<Panel>("Hud/PlayerBackpack/Grid/Slot0_0").GetThemeStylebox("panel");
        }
        finally { source.Free(); }
    }
}
