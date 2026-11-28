using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.UI.Regions;

public partial class RegionEditorView
{
    private static readonly (string Name, string[] Keys)[] Groups =
    [
        ("生成与尺寸", ["GenerationPhase", "MinCount", "MaxCount", "SpawnWeight", "MinSpacing", "AllowInitialLandMarks", "Width", "Height", "PlacementTypeIds", "ExtraTerrainIds"]),
        ("固定锚点", ["ForcedPositions", "FixedEnvironment", "FixedEnvironmentRange", "FixedBlendDistance", "BlendFixedEnvironment", "AllowEnvironmentCliff", "FixedVariation"]),
        ("环境与元气", ["AnchorEnvironment", "OverallEnvironment", "RequiredEnvironmentFraction", "KeyEnvironment", "EnvironmentAdjustments", "AuraProfile"]),
        ("关键格", ["KeyPoints"]),
        ("湖泊与河网", ["Lake", "NarrowRiverCells", "RiverCells", "RiverPorts"])
    ];
    private static readonly JsonObject Defaults = JsonSerializer.SerializeToNode(new RegionDefine(), MapCatalog.JsonOptions)!.AsObject();
    private static readonly Dictionary<string, string> WaterNames = new()
        { ["none"] = "陆地", ["river"] = "河流", ["lake"] = "湖泊", ["ocean"] = "海洋" };

    private readonly Dictionary<int, string> groupText = new();
    private readonly Dictionary<int, JsonObject> initialGroups = new();
    private readonly List<RegionTileCard> tileCards = new();
    private RegionTemplateStore store = null!;
    private RegionTemplateStore.Source? source;
    private MapCatalog? catalog;
    private JsonObject document = new();
    private Dictionary<TileId, string[]> originalLandMarks = new();
    private int groupIndex;
    private bool updating;
    private bool dirty;
    private bool unsavedCopy;
    private bool autoAcceptQuit;
    private Action? pendingAction;

    /// <summary>建立编辑器并读取正式地图目录。</summary>
    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.RegionEditor) return;
        store = new(GameBootstrap.ResolveDataRoot());
        Build();
        RefreshDocumentState();
        autoAcceptQuit = GetTree().AutoAcceptQuit;
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += CloseWindow;
        Reload();
    }

    /// <summary>恢复宿主的窗口关闭行为。</summary>
    public override void _ExitTree()
    {
        if (store == null) return;
        GetWindow().CloseRequested -= CloseWindow;
        GetTree().AutoAcceptQuit = autoAcceptQuit;
    }

    /// <summary>接收编辑器快捷键，文本编辑框继续处理自己的撤销。</summary>
    public override void _UnhandledInput(InputEvent input)
    {
        if (store == null || confirmation.Visible || errorDialog.Visible) return;
        if (input is not InputEventKey { Pressed: true, Echo: false, CtrlPressed: true, AltPressed: false } key) return;
        if (key.Keycode == Key.S && !save.Disabled) Save();
        else if (key.Keycode == Key.Z && GetViewport().GuiGetFocusOwner() is not (LineEdit or TextEdit)) canvas.Undo();
        else return;
        GetViewport().SetInputAsHandled();
    }

    private void CloseWindow() => Leave(() => GetTree().Quit());

    private void Reload() => Leave(() =>
    {
        try
        {
            string? lastId = source?.Id;
            catalog = store.Load();
            RefreshTileFilters();
            RefreshLandMarkLibrary();
            if (lastId != null && catalog.Regions.ContainsKey(lastId)) OpenRegion(lastId);
            else if (catalog.Regions.ContainsKey("region_guilin")) OpenRegion("region_guilin");
            else if (SortedRegions().FirstOrDefault() is { } first) OpenRegion(first.ID);
            else NewDraft(null);
        }
        catch (Exception exception) { ReportError(exception); }
    });

    private IEnumerable<RegionDefine> SortedRegions() => catalog?.Regions.Values
        .OrderBy(region => region.DisplayName, StringComparer.CurrentCulture).ThenBy(region => region.ID, StringComparer.Ordinal)
        ?? Enumerable.Empty<RegionDefine>();

    private void RefreshRegionPicker()
    {
        regionPicker.Clear();
        int selectedIndex = -1;
        string query = regionSearch.Text.Trim();
        foreach (var region in SortedRegions())
        {
            if (!Matches(region.ID, query) && !Matches(region.DisplayName, query)) continue;
            int index = regionPicker.ItemCount;
            if (source?.Id == region.ID) selectedIndex = index;
            regionPicker.AddItem($"{region.DisplayName}  ·  {region.ID}");
            regionPicker.SetItemMetadata(index, region.ID);
        }
        regionPicker.Disabled = regionPicker.ItemCount == 0;
        regionPicker.Select(selectedIndex);
    }

    private void SwitchRegion(int index)
    {
        string id = regionPicker.GetItemMetadata(index).AsString();
        RefreshRegionPicker();
        if (id != source?.Id) Leave(() => OpenRegion(id));
    }

    private void OpenRegion(string id)
    {
        try
        {
            var (origin, config) = RegionTemplateStore.Open(catalog!, id);
            LoadDocument(config, origin, false);
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private void NewDraft(JsonObject? template)
    {
        if (catalog == null) return;
        var config = template ?? new JsonObject { ["SchemaVersion"] = 2, ["TerrainLayout"] = new JsonObject() };
        config["ID"] = "";
        if (template != null)
        {
            config["DisplayName"] = (config["DisplayName"]?.GetValue<string>() ?? "区域") + "（副本）";
            // 世界绝对锚点留给作者重新分配；布局与其他生成条件从源模板继承。
            config.Remove("ForcedPositions");
        }
        LoadDocument(config, null, template != null);
        idField.GrabFocus();
    }

    private void Duplicate()
    {
        try
        {
            var draft = BuildDefinition();
            if (dirty) Confirm("把当前草稿复制到新模板？原模板的文件会保留保存时的内容。", () => NewDraft(draft));
            else NewDraft(draft);
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private void LoadDocument(JsonObject config, RegionTemplateStore.Source? origin, bool hasChanges)
    {
        var model = config.Deserialize<RegionDefine>(MapCatalog.JsonOptions)!;
        updating = true;
        document = config;
        source = origin;
        idField.Editable = origin == null;
        idField.Text = model.ID;
        nameField.Text = model.DisplayName;
        description.Text = model.Description ?? "";
        tagsField.Text = string.Join('\n', model.Tags);
        sourceLabel.Text = origin == null ? "新模板  ·  Schema 2" : $"{Path.GetFileName(origin.Path)}  ·  Schema {model.SchemaVersion}";
        sourceLabel.TooltipText = origin?.Path ?? "保存后写入 Data/RegionDefines";
        groupText.Clear();
        initialGroups.Clear();
        for (int index = 0; index < Groups.Length; index++)
        {
            var fields = new JsonObject();
            foreach (string key in Groups[index].Keys)
                if ((config[key] ?? Defaults[key]) is { } value) fields[key] = value.DeepClone();
            initialGroups.Add(index, fields);
            groupText.Add(index, fields.ToJsonString(RegionTemplateStore.DocumentOptions));
        }
        ShowGroup(0);
        tabs.CurrentTab = 0;
        canvas.Tiles = catalog!.Tiles;
        canvas.LandMarkDefinitions = catalog.LandMarks;
        var placements = model.LandMarkLayout.ToDictionary(pair => new TileId(pair.Key.dq, pair.Key.dr), pair => new List<string> { pair.Value });
        foreach (var placement in model.AdditionalLandMarks ?? [])
            placements[new TileId(placement.Dq, placement.Dr)].Add(placement.LandMarkId);
        originalLandMarks = placements.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        canvas.Load(model.TerrainLayout, BuildMarkers(model), originalLandMarks);
        Callable.From(canvas.Fit).CallDeferred();
        cursorLabel.Text = "原点 (0, 0)";
        SetTool(null, false);
        dirty = unsavedCopy = hasChanges;
        updating = false;
        RefreshRegionPicker();
        RefreshSelectedLandMarks();
        RefreshDocumentState();
        SetStatus(dirty ? "有未保存的修改" : origin == null ? "新模板尚未保存" : "已读取模板");
    }

    private void ShowGroup(int index)
    {
        bool previous = updating;
        updating = true;
        groupIndex = index;
        groupPicker.Select(index);
        advancedKeys.Text = string.Join(", ", Groups[index].Keys);
        advanced.Text = groupText.GetValueOrDefault(index, "{}");
        updating = previous;
    }

    private void OnAdvancedEdited()
    {
        if (updating || catalog == null) return;
        groupText[groupIndex] = advanced.Text;
        OnFormEdited();
        try
        {
            var config = BuildDefinition().Deserialize<RegionDefine>(MapCatalog.JsonOptions)!;
            canvas.Markers = BuildMarkers(config);
            canvas.QueueRedraw();
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or InvalidDataException)
        {
            // 输入进行中允许暂时无效，保存时展示完整的校验错误。
        }
    }

    private JsonObject BuildDefinition()
    {
        var config = (JsonObject)document.DeepClone();
        config["ID"] = source?.Id ?? idField.Text.Trim();
        if (nameField.Text != (document["DisplayName"]?.GetValue<string>() ?? "")) config["DisplayName"] = nameField.Text;
        if (description.Text != (document["Description"]?.GetValue<string>() ?? ""))
        {
            if (description.Text.Length == 0) config.Remove("Description");
            else config["Description"] = description.Text;
        }
        var originalTags = document["Tags"]?.Deserialize<string[]>() ?? [];
        if (tagsField.Text != string.Join('\n', originalTags))
            config["Tags"] = JsonSerializer.SerializeToNode(tagsField.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        for (int index = 0; index < Groups.Length; index++)
        {
            JsonObject fields;
            try
            {
                fields = JsonNode.Parse(groupText[index]) as JsonObject ?? throw new JsonException("请输入 JSON 对象。");
                foreach (var field in fields)
                {
                    if (!Groups[index].Keys.Contains(field.Key, StringComparer.Ordinal)) throw new JsonException($"字段 {field.Key} 不属于此组。");
                    if (field.Value == null) throw new JsonException($"字段 {field.Key} 的值为空，请移除该字段以使用默认配置。");
                }
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                throw new InvalidDataException($"{Groups[index].Name}：{exception.Message}", exception);
            }
            foreach (string key in Groups[index].Keys)
            {
                if (!fields.TryGetPropertyValue(key, out var value)) config.Remove(key);
                else if (!JsonNode.DeepEquals(value, initialGroups[index][key])) config[key] = value?.DeepClone();
            }
        }
        if (!config.TryGetPropertyValue("TerrainLayout", out var original) ||
            !SameLayout(original, canvas.Layout))
            config["TerrainLayout"] = JsonSerializer.SerializeToNode(canvas.Layout, LayoutOptions);
        if (!SameLandMarks())
        {
            var primary = new JsonObject();
            var extras = new List<RegionLandMarkPlacement>();
            foreach (var (cell, ids) in canvas.LandMarks.OrderBy(pair => pair.Key.R).ThenBy(pair => pair.Key.Q))
            {
                primary[string.Create(CultureInfo.InvariantCulture, $"{cell.Q},{cell.R}")] = ids[0];
                foreach (string id in ids.Skip(1)) extras.Add(new() { Dq = cell.Q, Dr = cell.R, LandMarkId = id });
            }
            config["LandMarkLayout"] = primary;
            if (extras.Count > 0) config["AdditionalLandMarks"] = JsonSerializer.SerializeToNode(extras, MapCatalog.JsonOptions);
            else config.Remove("AdditionalLandMarks");
        }
        return config;
    }

    private bool SameLandMarks() => originalLandMarks.Count == canvas.LandMarks.Count &&
        originalLandMarks.All(pair => canvas.LandMarks.TryGetValue(pair.Key, out var ids) && ids.SequenceEqual(pair.Value));

    private static readonly JsonSerializerOptions LayoutOptions = new(MapCatalog.JsonOptions) { Converters = { new AxialOffsetStringDictConverter() } };

    private static bool SameLayout(JsonNode? original, Dictionary<(int, int), string> current)
    {
        if (original is not JsonObject prior || prior.Count != current.Count) return false;
        return prior.All(field => current.TryGetValue(AxialOffsetStringDictConverter.Parse(field.Key), out var value) &&
            value == field.Value?.GetValue<string>());
    }

    private static Dictionary<TileId, string> BuildMarkers(RegionDefine region)
    {
        var notes = new Dictionary<TileId, string>();
        void Mark(int q, int r, string label)
        {
            var cell = new TileId(q, r);
            notes[cell] = notes.TryGetValue(cell, out string? text) ? text + " / " + label : label;
        }
        foreach (var point in region.KeyPoints ?? []) if (point != null) Mark(point.Dq, point.Dr, $"关键点 {point.ID}");
        foreach (var point in region.KeyEnvironment ?? []) if (point != null) Mark(point.Dq, point.Dr, "环境条件");
        foreach (var cell in region.RiverCells ?? []) if (cell != null) Mark(cell.Dq, cell.Dr, "河段");
        foreach (var port in region.RiverPorts ?? []) if (port != null) Mark(port.Dq, port.Dr, "河流端口");
        foreach (var port in region.Lake?.Ports ?? []) if (port != null) Mark(port.Dq, port.Dr, "湖泊端口");
        return notes;
    }

    private void OnFormEdited()
    {
        if (updating || catalog == null) return;
        dirty = unsavedCopy ||
            idField.Text != (document["ID"]?.GetValue<string>() ?? "") ||
            nameField.Text != (document["DisplayName"]?.GetValue<string>() ?? "") ||
            description.Text != (document["Description"]?.GetValue<string>() ?? "") ||
            tagsField.Text != string.Join('\n', document["Tags"]?.Deserialize<string[]>() ?? []) ||
            groupText.Any(group => group.Value != initialGroups[group.Key].ToJsonString(RegionTemplateStore.DocumentOptions)) ||
            !SameLandMarks() ||
            !SameLayout(document["TerrainLayout"], canvas.Layout);
        RefreshDocumentState();
        SetStatus(dirty ? "有未保存的修改" : source == null ? "新模板尚未保存" : "已读取模板");
    }

    private void OnCanvasEdited()
    {
        OnFormEdited();
        RefreshSelectedLandMarks();
    }

    private void RefreshDocumentState()
    {
        save.Disabled = catalog == null || (source != null && !dirty);
        archive.Disabled = source == null || dirty;
        copy.Disabled = catalog == null;
        undo.Disabled = !canvas.CanUndo;
        canvasTitle.Text = string.IsNullOrWhiteSpace(nameField.Text) ? "区域画布" : nameField.Text;
        footprint.Text = $"{canvas.Layout.Count} 格";
    }

    private void Save()
    {
        if (catalog == null) return;
        try
        {
            var definition = BuildDefinition();
            string id = definition["ID"]!.GetValue<string>();
            var saved = store.Save(source, definition);
            catalog = saved.Catalog;
            LoadDocument(definition, new RegionTemplateStore.Source(id, saved.Path, saved.Content), false);
            SetStatus($"已保存 {id}  ·  下次加载地图时生效", saved.Path);
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private void ConfirmArchive()
    {
        if (source is not { } target || dirty) return;
        Confirm($"归档「{target.Id}」？原文件会保留一份 .bak 备份。", () =>
        {
            try
            {
                string backup = store.Archive(target);
                source = null;
                Reload();
                SetStatus($"已归档 {target.Id}  ·  备份 {Path.GetFileName(backup)}", backup);
            }
            catch (Exception exception) { ReportError(exception); }
        });
    }

    private void Leave(Action action)
    {
        if (dirty) Confirm("有未保存的修改。放弃这些修改并继续？", action);
        else action();
    }

    private void Confirm(string text, Action action)
    {
        pendingAction = action;
        confirmation.DialogText = text;
        confirmation.PopupCentered();
    }

    private void SetStatus(string text, string? tooltip = null)
    {
        status.Text = text;
        status.TooltipText = tooltip ?? text;
    }

    private void ReportError(Exception exception)
    {
        SetStatus(exception.Message.Split('\n')[0], exception.Message);
        errorDetail.Text = exception.Message;
        errorDialog.PopupCentered();
    }

    private static bool Matches(string value, string query) => value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void RefreshTileFilters()
    {
        var tiles = catalog!.Tiles.Values;
        tagFilter.Clear();
        tagFilter.AddItem("全部地形");
        foreach (string tag in tiles.SelectMany(tile => tile.Tags).Distinct().Order(StringComparer.CurrentCulture)) tagFilter.AddItem(tag);
        tagFilter.Select(0);
        waterFilter.Clear();
        waterFilter.AddItem("全部水体");
        waterFilter.SetItemMetadata(0, "");
        foreach (string water in tiles.Select(tile => tile.WaterKind).Distinct().Order(StringComparer.Ordinal))
        {
            int index = waterFilter.ItemCount;
            waterFilter.AddItem(WaterNames.GetValueOrDefault(water, water));
            waterFilter.SetItemMetadata(index, water);
        }
        waterFilter.Select(0);
        RefreshTiles();
    }

    private void RefreshTiles()
    {
        if (catalog == null) return;
        foreach (var card in tileCards) { tileGrid.RemoveChild(card); card.QueueFree(); }
        tileCards.Clear();
        libraryScroll.ScrollVertical = 0;
        string query = tileSearch.Text.Trim();
        string? tag = tagFilter.Selected > 0 ? tagFilter.Text : null;
        string? water = waterFilter.Selected > 0 ? waterFilter.GetSelectedMetadata().AsString() : null;
        var tiles = catalog.Tiles.Values.Where(tile => (tag == null || tile.Tags.Contains(tag)) && (water == null || tile.WaterKind == water) &&
            (Matches(tile.ID, query) || Matches(tile.DisplayName, query) || Matches(tile.Description, query) || tile.Tags.Any(t => Matches(t, query))))
            .OrderBy(tile => tile.DisplayName, StringComparer.CurrentCulture).ThenBy(tile => tile.ID, StringComparer.Ordinal);
        foreach (var tile in tiles)
        {
            var card = new RegionTileCard
            {
                Name = "Tile_" + tile.ID.ValidateNodeName(), Tile = tile, ToggleMode = true,
                ButtonPressed = canvas.Brush == tile.ID && !canvas.Eraser,
                CustomMinimumSize = new Vector2(110, 83), SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = $"{tile.DisplayName}  ·  {tile.ID}\n{string.Join(" / ", tile.Tags)}\n{tile.Description}",
                ShowTerrainArt = canvas.ShowTerrainArt
            };
            var cardText = Text(tile.DisplayName, 14, Ink);
            cardText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            cardText.HorizontalAlignment = HorizontalAlignment.Center;
            cardText.MouseFilter = MouseFilterEnum.Ignore;
            card.AddChild(cardText);
            cardText.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            cardText.OffsetTop = -30;
            cardText.OffsetBottom = -7;
            cardText.OffsetLeft = 7;
            cardText.OffsetRight = -7;
            card.Pressed += () => SetTool(tile.ID, false);
            tileCards.Add(card);
            tileGrid.AddChild(card);
        }
        tileCount.Text = $"{tileCards.Count} 种";
    }

    private void ShowTerrainArt(bool show)
    {
        canvas.ShowTerrainArt = show;
        canvas.QueueRedraw();
        foreach (var card in tileCards)
        {
            card.ShowTerrainArt = show;
            card.QueueRedraw();
        }
    }

    private void SetTool(string? brush, bool erase)
    {
        canvas.Brush = brush;
        canvas.Eraser = erase;
        canvas.LandMarkBrush = null;
        pointer.SetPressedNoSignal(brush == null && !erase);
        eraser.SetPressedNoSignal(erase);
        foreach (var card in tileCards) card.SetPressedNoSignal(card.Tile.ID == brush && !erase);
        foreach (var card in landMarkCards) card.SetPressedNoSignal(false);
    }
}
