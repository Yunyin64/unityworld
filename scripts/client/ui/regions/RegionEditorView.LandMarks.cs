using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Xianxia.Client.UI.Regions;

public partial class RegionEditorView
{
    private LineEdit landMarkSearch = null!;
    private OptionButton landMarkCategory = null!;
    private Label landMarkCount = null!;
    private Label selectedCellLabel = null!;
    private VBoxContainer selectedLandMarks = null!;
    private VBoxContainer landMarkPalette = null!;
    private readonly List<RegionLandMarkCard> landMarkCards = new();

    private void BuildLandMarkLibrary(VBoxContainer column)
    {
        selectedCellLabel = Text("选中地块", 15, Gold);
        selectedCellLabel.Name = "SelectedLandMarkCell";
        column.AddChild(selectedCellLabel);
        var placed = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 112),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        column.AddChild(placed);
        selectedLandMarks = new VBoxContainer { Name = "SelectedLandMarks", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        placed.AddChild(selectedLandMarks);
        column.AddChild(new HSeparator());

        var heading = new HBoxContainer();
        column.AddChild(heading);
        var title = Text("地标库", 19, Ink);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        heading.AddChild(title);
        landMarkCount = Text("", 13, Gold);
        heading.AddChild(landMarkCount);
        landMarkSearch = new LineEdit { Name = "RegionLandMarkSearch", PlaceholderText = "搜索地标名称、ID 或标签", ClearButtonEnabled = true };
        column.AddChild(landMarkSearch);
        landMarkSearch.TextChanged += _ => RefreshLandMarkLibrary();
        landMarkCategory = new OptionButton { Name = "RegionLandMarkCategory" };
        landMarkCategory.AddItem("全部地标");
        landMarkCategory.AddItem("自然地标");
        landMarkCategory.AddItem("人工地标");
        column.AddChild(landMarkCategory);
        landMarkCategory.ItemSelected += _ => RefreshLandMarkLibrary();

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        landMarkPalette = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(landMarkPalette);
        RefreshSelectedLandMarks();
    }

    private void RefreshLandMarkLibrary()
    {
        if (catalog == null) return;
        foreach (var card in landMarkCards) { landMarkPalette.RemoveChild(card); card.QueueFree(); }
        landMarkCards.Clear();
        string query = landMarkSearch.Text.Trim();
        foreach (var definition in catalog.LandMarks.Values.OrderBy(item => item.DisplayName, StringComparer.CurrentCulture).ThenBy(item => item.ID, StringComparer.Ordinal))
        {
            if (landMarkCategory.Selected == 1 && !definition.IsNatural || landMarkCategory.Selected == 2 && definition.IsNatural) continue;
            if (!Matches(definition.ID, query) && !Matches(definition.DisplayName, query) && !definition.Tags.Any(tag => Matches(tag, query))) continue;
            var card = new RegionLandMarkCard
            {
                Name = "LandMark_" + definition.ID.ValidateNodeName(), LandMarkId = definition.ID,
                Text = definition.DisplayName, Alignment = HorizontalAlignment.Left, ToggleMode = true,
                ExpandIcon = true, Icon = string.IsNullOrWhiteSpace(definition.ImagePath) ? null : GameBootstrap.Assets.Texture(definition.ImagePath),
                CustomMinimumSize = new Vector2(0, 38), TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                TooltipText = $"{definition.DisplayName} · {definition.ID}\n{(definition.IsNatural ? "自然地标" : "人工地标")}{(definition.IsSingleton ? " · 全局唯一" : "")}",
                ButtonPressed = canvas.LandMarkBrush == definition.ID
            };
            card.AddThemeConstantOverride("icon_max_width", 30);
            card.Pressed += () =>
            {
                SetTool(null, false);
                canvas.LandMarkBrush = definition.ID;
                pointer.SetPressedNoSignal(false);
                card.SetPressedNoSignal(true);
            };
            landMarkCards.Add(card);
            landMarkPalette.AddChild(card);
        }
        landMarkCount.Text = $"{landMarkCards.Count} 种";
    }

    private void RefreshSelectedLandMarks()
    {
        foreach (Node child in selectedLandMarks.GetChildren()) { selectedLandMarks.RemoveChild(child); child.QueueFree(); }
        if (canvas.Selected is not { } cell)
        {
            selectedCellLabel.Text = "选中地块";
            selectedLandMarks.AddChild(Text("在画布上点击一格查看地标", 13, Muted));
            return;
        }
        selectedCellLabel.Text = $"选中格 ({cell.Q}, {cell.R})";
        if (!canvas.LandMarks.TryGetValue(cell, out var ids))
        {
            string hint = canvas.Layout.ContainsKey((cell.Q, cell.R)) ? "此格还没有地标" : "此格为空，先配置地形";
            selectedLandMarks.AddChild(Text(hint, 13, Muted));
            return;
        }
        for (int index = 0; index < ids.Length; index++)
        {
            string id = ids[index];
            string name = canvas.LandMarkDefinitions.TryGetValue(id, out var definition) ? definition.DisplayName : id;
            var row = new HBoxContainer();
            selectedLandMarks.AddChild(row);
            if (canvas.LandMarkTexture(id) is { } image)
            {
                row.AddChild(new TextureRect
                {
                    Texture = image, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(27, 27), MouseFilter = MouseFilterEnum.Ignore
                });
            }
            else
            {
                var symbol = Text("地", 12, Gold);
                symbol.CustomMinimumSize = new Vector2(27, 27);
                symbol.HorizontalAlignment = HorizontalAlignment.Center;
                row.AddChild(symbol);
            }
            var label = Text(index == 0 ? $"主 · {name}" : name, 13, Ink);
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.TooltipText = $"{name} · {id}";
            row.AddChild(label);
            var remove = AddButton(row, "移除", () => canvas.RemoveLandMark(cell, id));
            remove.Name = "Remove_" + id.ValidateNodeName();
            remove.TooltipText = $"从本格移除{name}";
        }
    }
}
