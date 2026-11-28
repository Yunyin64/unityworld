using Godot;
using Godot.Collections;

namespace Xianxia.Client.UI.Regions;

/// <summary>可拖入区域占地格的地标定义。</summary>
public partial class RegionLandMarkCard : Button
{
    internal string LandMarkId { get; init; } = "";

    /// <summary>携带地标定义 ID 并复用该项的图片作为拖放预览。</summary>
    public override Variant _GetDragData(Vector2 atPosition)
    {
        var preview = new Button
        {
            Text = Text,
            Icon = Icon,
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(150, 42),
            MouseFilter = MouseFilterEnum.Ignore
        };
        preview.AddThemeConstantOverride("icon_max_width", 34);
        SetDragPreview(preview);
        return new Dictionary { ["region_landmark_id"] = LandMarkId };
    }
}
