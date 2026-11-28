using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.UI.Landmarks;

/// <summary>编辑地标配置和图片，保存已有定义或将新定义导出到当前项目。</summary>
public partial class LandmarkEditorView : Control
{
    private readonly Dictionary<string, LineEdit> fields = new(StringComparer.Ordinal);
    private readonly List<Button> actions = new();
    private MapCatalog? catalog;
    private LandmarkExport.Source? editSource;
    private Button commitButton = null!;
    private ItemList library = null!;
    private Label title = null!, status = null!, imageAddress = null!;
    private TextureRect preview = null!;
    private CheckButton natural = null!, singleton = null!, hidden = null!;
    private TextEdit positions = null!, extraCondition = null!;
    private ConfirmationDialog discard = null!;
    private FileDialog picker = null!;
    private Action? afterDiscard;
    private byte[]? importedImage;
    private string sourceImage = "", selectedId = "";
    private bool filling, dirty, exporting;

    /// <summary>构建编辑工作区并加载现有定义。</summary>
    public override void _Ready()
    {
        var backdrop = new ColorRect { Color = new Color("132320"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = new MarginContainer();
        AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 18);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        margin.AddChild(layout);
        var toolbar = new HBoxContainer();
        layout.AddChild(toolbar);
        ActionButton(toolbar, "ReturnHome", "返回首页", () => RequestReplace(GameBootstrap.OpenHome));
        title = new Label { Text = "地标编辑器", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color("F1DDB0"));
        toolbar.AddChild(title);
        commitButton = ActionButton(toolbar, "Export", "导出新地标", Commit);
        layout.AddChild(new HSeparator());

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 12);
        layout.AddChild(columns);
        var left = Column(columns, 168, 0.9f);
        Heading(left, "地标库");
        ActionButton(left, "NewLandmark", "新建地标", () => RequestReplace(() => LoadDraft(new())));
        ActionButton(left, "CopyLandmark", "复制所选定义", CopySelected);
        library = new ItemList { Name = "Library", SizeFlagsVertical = SizeFlags.ExpandFill };
        left.AddChild(library);
        library.ItemSelected += index =>
        {
            if (filling || exporting || catalog == null) return;
            string id = library.GetItemMetadata((int)index).AsString();
            RestoreSelection();
            RequestReplace(() => OpenLibraryDraft(id));
        };

        var middle = Column(columns, 310, 2.7f);
        Heading(middle, "地标配置");
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        middle.AddChild(scroll);
        var form = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        form.AddThemeConstantOverride("separation", 9);
        scroll.AddChild(form);
        Field(form, "ID", "定义 ID", "例：spirit_stone");
        Field(form, "DisplayName", "显示名称", "例：悟道石");
        var toggles = new HBoxContainer();
        form.AddChild(toggles);
        natural = Toggle(toggles, "自然地标", "创世自然地标的偏移一次性写入原初。人工和运行中新增地标按秒累积。");
        natural.Toggled += naturalValue => natural.Text = naturalValue ? "自然地标" : "人工地标";
        singleton = Toggle(toggles, "全世界唯一", "同一地标定义最多有一个有效实例。");
        Field(form, "Tags", "叙事标签", "逗号分隔，例如 宗门,修炼");
        hidden = Toggle(form, "隐藏", "玩家和 NPC 的普通观察会省略该地标。");
        hidden.Name = "IsHide";
        Field(form, "DrawType", "地图画法", "Icon / TerrainMask / Shader / Atmosphere / None");
        form.AddChild(new HSeparator());
        Heading(form, "创世选址");
        Field(form, "PerTileChance", "逐格概率", "普通散布开启后，0 或留空时全部通过概率关");
        Field(form, "MinCount", "正常调节下限", "例：2，候选不足时照常生成");
        Field(form, "MaxCount", "总量上限", "例：8，含地区自带数量");
        Field(form, "NeedCount", "强制保底数量", "单填此项时只补缺口。补位可放宽选址条件");
        Field(form, "MinSpacing", "同类最小间距", "例：6，按六边形格距");
        Field(form, "TerrainConditions", "地形条件", "例：plain,mountain,No:river");
        Field(form, "LandMarkConditions", "地标条件", "例：Near(3):natural_leyline_eye");
        Field(form, "TagConditions", "地形标签条件", "例：山类,No:寒冷");
        form.AddChild(new Label { Text = "额外条件（预留）" });
        extraCondition = new TextEdit { Name = "ExtraCondition", PlaceholderText = "只保存文本，当前不参与选址", CustomMinimumSize = new Vector2(0, 74) };
        extraCondition.TextChanged += MarkDirty;
        form.AddChild(extraCondition);
        Heading(form, "固定坐标");
        form.AddChild(new Label { Text = "世界列,行，每行一格。优先于随机散布。", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        positions = new TextEdit { Name = "ForcedPositions", PlaceholderText = "例：100,100", CustomMinimumSize = new Vector2(0, 74) };
        positions.TextChanged += MarkDirty;
        form.AddChild(positions);
        form.AddChild(new HSeparator());
        Heading(form, "五行元气偏移");
        var elements = new HBoxContainer();
        form.AddChild(elements);
        foreach (var (key, label) in new[] { ("Jin", "金"), ("Mu", "木"), ("Shui", "水"), ("Huo", "火"), ("Tu", "土") })
        {
            var value = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            elements.AddChild(value);
            Field(value, key, label, "0");
        }
        Field(form, "ModifierDefineIds", "附加修正 ID", "例：aura_leyline_spring，逗号分隔");

        var right = Column(columns, 192, 1.2f);
        Heading(right, "地图图片");
        var plate = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        right.AddChild(plate);
        preview = new TextureRect
        {
            Name = "ImagePreview", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(120, 132)
        };
        plate.AddChild(preview);
        imageAddress = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        right.AddChild(imageAddress);
        ActionButton(right, "ImportImage", "导入 PNG", () => picker.PopupCenteredRatio(0.7f));
        ActionButton(right, "ClearImage", "清除图片", () =>
        {
            ClearImage();
            MarkDirty();
        });
        right.AddChild(new Label
        {
            Text = "选择透明背景 PNG，图案会同时出现在 2D 地图和地点缩略图。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        layout.AddChild(new HSeparator());
        status = new Label { Name = "Status", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        layout.AddChild(status);

        picker = new FileDialog { Name = "ImagePicker", Title = "选择地标图片", FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem, Filters = new[] { "*.png ; PNG 图片" } };
        picker.FileSelected += ImportImage;
        AddChild(picker);
        discard = new ConfirmationDialog { Name = "DiscardChanges", Title = "放弃地标草稿？", DialogText = "当前修改尚未保存。", OkButtonText = "放弃修改" };
        discard.Confirmed += () =>
        {
            var next = afterDiscard;
            afterDiscard = null;
            next?.Invoke();
        };
        discard.Canceled += () => afterDiscard = null;
        AddChild(discard);

        try
        {
            LoadLibrary();
            LoadDraft(new());
        }
        catch (Exception ex) { status.Text = $"无法加载地标目录。{ex.Message}"; }
    }

    private static VBoxContainer Column(Container parent, int width, float ratio)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(width, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = ratio };
        parent.AddChild(panel);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);
        return content;
    }

    private static void Heading(Container parent, string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color("F1DDB0"));
        parent.AddChild(label);
    }

    private void Field(Container parent, string key, string label, string hint)
    {
        parent.AddChild(new Label { Text = label });
        var input = new LineEdit { Name = key, PlaceholderText = hint, TooltipText = hint };
        input.TextChanged += _ => MarkDirty();
        fields.Add(key, input);
        parent.AddChild(input);
    }

    private Button ActionButton(Container parent, string name, string label, Action callback)
    {
        var button = new Button { Name = name, Text = label, CustomMinimumSize = new Vector2(0, 32) };
        button.Pressed += callback;
        actions.Add(button);
        parent.AddChild(button);
        return button;
    }

    private CheckButton Toggle(Container parent, string label, string tip)
    {
        var toggle = new CheckButton { Text = label, TooltipText = tip };
        toggle.Toggled += _ => MarkDirty();
        parent.AddChild(toggle);
        return toggle;
    }

    private void LoadLibrary()
    {
        catalog = MapCatalog.Load(GameBootstrap.ResolveDataRoot());
        library.Clear();
        foreach (var entry in catalog.LandMarks.Values.OrderBy(d => d.DisplayName, StringComparer.CurrentCulture).ThenBy(d => d.ID))
        {
            int index = library.AddItem(entry.DisplayName);
            library.SetItemMetadata(index, entry.ID);
            library.SetItemTooltip(index, entry.ID);
        }
    }

    private void RestoreSelection()
    {
        library.DeselectAll();
        for (int i = 0; i < library.ItemCount; i++)
            if (library.GetItemMetadata(i).AsString() == selectedId) library.Select(i);
    }

    private void CopySelected()
    {
        if (catalog == null || !catalog.LandMarks.ContainsKey(selectedId))
        {
            status.Text = "先从地标库选择要复制的定义。";
            return;
        }
        string id = selectedId;
        RequestReplace(() => OpenLibraryDraft(id, copy: true));
    }

    private void OpenLibraryDraft(string id, bool copy = false)
    {
        try
        {
            var (source, draft) = LandmarkExport.Open(catalog!, id);
            LoadDraft(draft, source, copy);
        }
        catch (Exception ex) { status.Text = $"无法打开地标。{ex.Message}"; }
    }

    private void RequestReplace(Action next)
    {
        if (exporting) return;
        if (!dirty) { next(); return; }
        afterDiscard = next;
        discard.PopupCentered();
    }

    private void LoadDraft(LandMarkDefine draft, LandmarkExport.Source? source = null, bool copy = false)
    {
        filling = true;
        selectedId = source?.Id ?? "";
        editSource = copy ? null : source;
        RestoreSelection();
        fields["ID"].Text = copy ? draft.ID + "_copy" : draft.ID;
        fields["ID"].Editable = !exporting && editSource == null;
        commitButton.Text = editSource == null ? "导出新地标" : "保存修改";
        fields["DisplayName"].Text = draft.DisplayName;
        natural.SetPressedNoSignal(draft.IsNatural);
        natural.Text = draft.IsNatural ? "自然地标" : "人工地标";
        singleton.SetPressedNoSignal(draft.IsSingleton);
        hidden.SetPressedNoSignal(draft.IsHide);
        fields["DrawType"].Text = draft.DrawType;
        fields["Tags"].Text = string.Join(", ", draft.Tags);
        var placement = draft.InitialPlacement ?? new InitialPlacement();
        fields["PerTileChance"].Text = placement.PerTileChance?.ToString(CultureInfo.InvariantCulture) ?? "";
        fields["MinCount"].Text = placement.MinCount?.ToString(CultureInfo.InvariantCulture) ?? "";
        fields["MaxCount"].Text = placement.MaxCount?.ToString(CultureInfo.InvariantCulture) ?? "";
        fields["NeedCount"].Text = placement.NeedCount?.ToString(CultureInfo.InvariantCulture) ?? "";
        fields["MinSpacing"].Text = placement.MinSpacing == 0 ? "" : placement.MinSpacing.ToString(CultureInfo.InvariantCulture);
        fields["TerrainConditions"].Text = string.Join(", ", placement.TerrainConditions);
        fields["LandMarkConditions"].Text = string.Join(", ", placement.LandMarkConditions);
        fields["TagConditions"].Text = string.Join(", ", placement.TagConditions);
        extraCondition.Text = placement.ExtraCondition;
        fields["ModifierDefineIds"].Text = string.Join(", ", draft.ModifierDefineIds);
        fields["Jin"].Text = draft.AuraOffset.Jin.ToString(CultureInfo.InvariantCulture);
        fields["Mu"].Text = draft.AuraOffset.Mu.ToString(CultureInfo.InvariantCulture);
        fields["Shui"].Text = draft.AuraOffset.Shui.ToString(CultureInfo.InvariantCulture);
        fields["Huo"].Text = draft.AuraOffset.Huo.ToString(CultureInfo.InvariantCulture);
        fields["Tu"].Text = draft.AuraOffset.Tu.ToString(CultureInfo.InvariantCulture);
        positions.Text = copy ? "" : string.Join('\n', placement.ForcedPositions.Select(p => $"{p.Col},{p.Row}"));
        ClearImage();
        sourceImage = draft.ImagePath;
        if (sourceImage.Length != 0)
        {
            preview.Texture = GameBootstrap.Assets.Texture(sourceImage);
            imageAddress.Text = preview.Texture == null ? "图片不可用 · " + sourceImage : sourceImage;
        }
        status.Text = copy ? "已复制为新草稿。固定坐标已留空。" : source == null ? "新建草稿" : $"正在编辑 {Path.GetFileName(source.Path)}";
        filling = false;
        dirty = copy;
        title.Text = dirty ? "地标编辑器 *" : "地标编辑器";
    }

    private void MarkDirty()
    {
        if (filling) return;
        dirty = true;
        title.Text = "地标编辑器 *";
    }

    private void ClearImage()
    {
        sourceImage = "";
        importedImage = null;
        preview.Texture = null;
        imageAddress.Text = "未选择图片";
    }

    private void ImportImage(string path)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 16 * 1024 * 1024)
                throw new InvalidDataException("请选择一张不超过 16 MiB 的 PNG。");
            byte[] bytes = File.ReadAllBytes(path);
            using var image = new Image();
            if (image.LoadPngFromBuffer(bytes) != Error.Ok || image.IsEmpty() ||
                image.GetWidth() is < 16 or > 4096 || image.GetHeight() is < 16 or > 4096)
                throw new InvalidDataException("PNG 必须能正常解码，宽高各为 16 到 4096 像素。");
            preview.Texture = ImageTexture.CreateFromImage(image);
            importedImage = bytes;
            sourceImage = "";
            imageAddress.Text = file.Name;
            MarkDirty();
            status.Text = "图片已导入草稿。";
        }
        catch (Exception ex) { status.Text = $"图片导入失败。{ex.Message}"; }
    }

    private LandMarkDefine ReadDraft()
    {
        string Value(string key) => fields[key].Text.Trim();
        List<string> List(string key) => Value(key).Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).ToList();
        double Number(string key)
        {
            if (!double.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
                throw new InvalidDataException($"{key} 须填写有限数字。");
            return value;
        }
        int? Count(string key)
        {
            if (Value(key).Length == 0) return null;
            if (!int.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                throw new InvalidDataException($"{key} 须填写整数。");
            return count;
        }

        var draft = new LandMarkDefine
        {
            ID = editSource?.Id ?? Value("ID"), DisplayName = Value("DisplayName"), ImagePath = sourceImage,
            IsNatural = natural.ButtonPressed, IsSingleton = singleton.ButtonPressed, Tags = List("Tags"),
            IsHide = hidden.ButtonPressed, DrawType = Value("DrawType").Length == 0 ? "Icon" : Value("DrawType"),
            ModifierDefineIds = List("ModifierDefineIds"),
            AuraOffset = new TileAura { Jin = (float)Number("Jin"), Mu = (float)Number("Mu"), Shui = (float)Number("Shui"),
                Huo = (float)Number("Huo"), Tu = (float)Number("Tu") }
        };
        var placement = new InitialPlacement
        {
            PerTileChance = Value("PerTileChance").Length == 0 ? null : Number("PerTileChance"),
            MinCount = Count("MinCount"), MaxCount = Count("MaxCount"), NeedCount = Count("NeedCount"),
            MinSpacing = Count("MinSpacing") ?? 0, TerrainConditions = List("TerrainConditions"),
            LandMarkConditions = List("LandMarkConditions"), TagConditions = List("TagConditions"),
            ExtraCondition = extraCondition.Text
        };
        foreach (string line in positions.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] pieces = line.Split(',', '，');
            if (pieces.Length != 2 || !int.TryParse(pieces[0], out int col) || !int.TryParse(pieces[1], out int row))
                throw new InvalidDataException($"固定坐标应为「列,行」：{line}");
            placement.ForcedPositions.Add(new OffsetPointDefine { Col = col, Row = row });
        }
        if (placement.PerTileChance.HasValue || placement.MinCount.HasValue || placement.MaxCount.HasValue || placement.NeedCount.HasValue ||
            placement.MinSpacing != 0 || placement.ForcedPositions.Count > 0 || placement.TerrainConditions.Count > 0 ||
            placement.LandMarkConditions.Count > 0 || placement.TagConditions.Count > 0 || placement.ExtraCondition.Length > 0)
            draft.InitialPlacement = placement;
        return draft;
    }

    private async void Commit()
    {
        if (exporting) return;
        var source = editSource;
        string action = source == null ? "导出" : "保存";
        string? writtenPath = null;
        try
        {
            var draft = ReadDraft();
            if (source == null && importedImage == null && draft.ImagePath.Length != 0 && preview.Texture == null)
                throw new InvalidDataException("引用的图片不可用，请更换或清除。");
            SetBusy(true);
            status.Text = $"正在校验并{action}…";
            string dataRoot = GameBootstrap.ResolveDataRoot();
            string installRoot = GameBootstrap.ResolveInstallRoot();
            byte[]? bytes = importedImage;
            writtenPath = await Task.Run(() => LandmarkExport.Write(dataRoot, installRoot, draft, bytes, source));
            GameBootstrap.ReloadAssets();
            LoadLibrary();
            var (savedSource, savedDraft) = LandmarkExport.Open(catalog!, draft.ID);
            LoadDraft(savedDraft, savedSource);
            status.Text = $"已{action} {draft.DisplayName} · {Path.GetFileName(writtenPath)}。下次创建世界时应用生成规则。";
        }
        catch (Exception ex)
        {
            status.Text = writtenPath == null ? $"{action}失败。{ex.Message}" : $"已写入 {writtenPath}，刷新目录失败。{ex.Message}";
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool value)
    {
        exporting = value;
        foreach (var action in actions) action.Disabled = value;
        foreach (var field in fields.Values) field.Editable = !value;
        fields["ID"].Editable = !value && editSource == null;
        natural.Disabled = singleton.Disabled = hidden.Disabled = value;
        extraCondition.Editable = !value;
        positions.Editable = !value;
    }
}
