using Godot;
using Godot.Collections;
using UnityWorld.Game.Data;
using Xianxia.Client.Exploration;

namespace Xianxia.Client.UI.Regions;

/// <summary>Tile 库的可拖动卡片，与画布共用地形颜色和符号。</summary>
public partial class RegionTileCard : Button
{
    internal TileDefine Tile { get; set; } = null!;
    internal bool ShowTerrainArt { get; set; }

    internal static Color TileColor(TileDefine tile) => Color.FromString(tile.Color, new Color("737a72"));

    /// <summary>创建带 Tile ID 的拖放负载和光标预览。</summary>
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Tile == null) return default;
        var preview = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(112, 38)
        };
        preview.AddThemeStyleboxOverride("panel", RegionEditorView.Surface(TileColor(Tile).Darkened(0.58f), new Color("d3c291"), 5));
        var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        preview.AddChild(content);
        if (ShowTerrainArt && GameBootstrap.Assets.Terrain.Resolve(Tile.ID, Tile.AssetRef) is { } material)
        {
            content.AddChild(new TextureRect
            {
                Texture = new AtlasTexture { Atlas = material.Texture, Region = material.Region, FilterClip = true },
                CustomMinimumSize = new Vector2(104, 40),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore
            });
        }
        content.AddChild(new Label { Text = Tile.DisplayName, HorizontalAlignment = HorizontalAlignment.Center });
        SetDragPreview(preview);
        return new Dictionary { ["region_tile_id"] = Tile.ID };
    }

    /// <summary>画出独立的地形色样和配置符号，缺图时仍可辨认。</summary>
    public override void _Draw()
    {
        if (Tile == null) return;
        var area = new Rect2(6, 6, Size.X - 12, 43);
        Color terrain = TileColor(Tile);
        DrawRect(area, terrain.Darkened(0.25f));
        if (ShowTerrainArt && GameBootstrap.Assets.Terrain.Resolve(Tile.ID, Tile.AssetRef) is { } material)
            material.DrawThumbnail(this, area);
        else
        {
            var points = ExplorationHexProjection.Outline(area.GetCenter(), 18);
            DrawColoredPolygon(points, terrain);
            DrawPolyline([.. points, points[0]], terrain.Lightened(0.34f), 1, true);
            var font = GetThemeDefaultFont();
            float width = font.GetStringSize(Tile.Symbol, fontSize: 16).X;
            DrawString(font, area.GetCenter() + new Vector2(-width / 2, 6), Tile.Symbol, fontSize: 16,
                modulate: terrain.Luminance > 0.4f ? new Color("13201a") : new Color("f4eddd"));
        }
    }
}
