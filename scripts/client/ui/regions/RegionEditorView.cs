using System;
using Godot;

namespace Xianxia.Client.UI.Regions;

/// <summary>区域模板编辑器的三栏工作区。</summary>
public partial class RegionEditorView : Control
{
    private static readonly Color Ink = new("efe8d4");
    private static readonly Color Muted = new("aebcae");
    private static readonly Color Gold = new("e4cb8c");
    private static readonly Color Border = new("3e5145");
    private static readonly Color Panel = new("222f29");
    private static readonly Color Field = new("17221d");

    private LineEdit regionSearch = null!;
    private OptionButton regionPicker = null!;
    private LineEdit idField = null!;
    private LineEdit nameField = null!;
    private TextEdit description = null!;
    private TextEdit tagsField = null!;
    private TabContainer tabs = null!;
    private OptionButton groupPicker = null!;
    private TextEdit advanced = null!;
    private Label advancedKeys = null!;
    private Label sourceLabel = null!;
    private Label footprint = null!;
    private Label canvasTitle = null!;
    private Label cursorLabel = null!;
    private Label canvasHelp = null!;
    private Label status = null!;
    private Button save = null!;
    private Button archive = null!;
    private Button undo = null!;
    private Button copy = null!;
    private Button pointer = null!;
    private Button eraser = null!;
    private RegionEditorCanvas canvas = null!;
    private LineEdit tileSearch = null!;
    private OptionButton tagFilter = null!;
    private OptionButton waterFilter = null!;
    private Label tileCount = null!;
    private GridContainer tileGrid = null!;
    private ScrollContainer libraryScroll = null!;
    private ConfirmationDialog confirmation = null!;
    private AcceptDialog errorDialog = null!;
    private TextEdit errorDetail = null!;

    private void Build()
    {
        Theme = Theme.Duplicate() as Theme ?? new Theme();
        Theme.DefaultFontSize = 15;
        Theme.SetStylebox("normal", "LineEdit", Surface(Field, Border, 5, 8));
        Theme.SetStylebox("read_only", "LineEdit", Surface(Field, Border, 5, 8));
        Theme.SetStylebox("normal", "TextEdit", Surface(Field, Border, 5, 8));
        Theme.SetStylebox("focus", "LineEdit", Surface(Colors.Transparent, Gold, 5));
        Theme.SetStylebox("focus", "TextEdit", Surface(Colors.Transparent, Gold, 5));
        Theme.SetStylebox("panel", "TabContainer", Surface(Panel, Border, 5));
        Theme.SetStylebox("tab_selected", "TabContainer", Surface(new Color("34483c"), Border, 5, 8));
        Theme.SetStylebox("tab_unselected", "TabContainer", Surface(Field, Border, 5, 8));
        foreach (string type in new[] { "Button", "OptionButton" })
        {
            Theme.SetStylebox("normal", type, Surface(new Color("30453a"), Border, 5, 8));
            Theme.SetStylebox("hover", type, Surface(new Color("425c4c"), new Color("79927e"), 5, 8));
            Theme.SetStylebox("pressed", type, Surface(new Color("435944"), Gold, 5, 8));
            Theme.SetStylebox("disabled", type, Surface(Field, Border, 5, 8));
            Theme.SetStylebox("focus", type, Surface(Colors.Transparent, Gold, 5));
        }

        var backdrop = new ColorRect { Color = new Color("131b18"), MouseFilter = MouseFilterEnum.Ignore };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);
        var page = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Auto, VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(page);
        var margin = new MarginContainer { CustomMinimumSize = new Vector2(995, 570), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 16);
        page.AddChild(margin);
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 11);
        margin.AddChild(body);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 9);
        body.AddChild(header);
        AddButton(header, "返回首页", () => Leave(GameBootstrap.OpenHome));
        var title = Text("区域模板编辑器", 28, Ink);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        AddButton(header, "重新读取", Reload);
        AddButton(header, "新建", () => Leave(() => NewDraft(null)));
        copy = AddButton(header, "复制", Duplicate);
        archive = AddButton(header, "归档…", ConfirmArchive);
        save = AddButton(header, "保存模板", Save);
        save.AddThemeStyleboxOverride("normal", Surface(new Color("ba9f64"), Gold, 5, 11));
        save.AddThemeStyleboxOverride("hover", Surface(new Color("d2b97e"), Gold, 5, 11));
        save.AddThemeColorOverride("font_color", Field);
        save.AddThemeColorOverride("font_hover_color", Field);
        save.TooltipText = "Ctrl + S";

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 11);
        body.AddChild(columns);
        BuildProperties(Column(columns, 270));
        BuildCanvas(Column(columns, 0));
        BuildLibrary(Column(columns, 264));
        status = Text("正在读取地图定义…", 13, Muted);
        status.Name = "EditorStatus";
        status.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        body.AddChild(status);

        confirmation = new ConfirmationDialog { Title = "区域模板编辑器", OkButtonText = "确认", CancelButtonText = "取消" };
        AddChild(confirmation);
        confirmation.Confirmed += () => { var next = pendingAction; pendingAction = null; next?.Invoke(); };
        confirmation.Canceled += () => pendingAction = null;
        errorDialog = new AcceptDialog { Title = "区域配置校验", OkButtonText = "关闭" };
        errorDetail = new TextEdit { Editable = false, CustomMinimumSize = new Vector2(595, 270), WrapMode = TextEdit.LineWrappingMode.Boundary };
        errorDialog.AddChild(errorDetail);
        AddChild(errorDialog);
    }

    private void BuildProperties(VBoxContainer column)
    {
        column.AddChild(Text("区域属性", 19, Ink));
        regionSearch = new LineEdit { Name = "RegionSearch", PlaceholderText = "搜索模板名称或 ID", ClearButtonEnabled = true };
        column.AddChild(regionSearch);
        regionSearch.TextChanged += _ => RefreshRegionPicker();
        regionPicker = new OptionButton { Name = "RegionPicker", FitToLongestItem = false, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        column.AddChild(regionPicker);
        regionPicker.ItemSelected += index => SwitchRegion((int)index);
        sourceLabel = Text("", 12, Muted);
        sourceLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        column.AddChild(sourceLabel);

        tabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddChild(tabs);
        var basic = TabScroll("基本信息");
        idField = new LineEdit { Name = "RegionId", PlaceholderText = "例如 mist_valley" };
        nameField = new LineEdit { Name = "DisplayName", PlaceholderText = "显示名称" };
        description = new TextEdit { Name = "Description", PlaceholderText = "填写这块区域的描述", CustomMinimumSize = new Vector2(0, 110), WrapMode = TextEdit.LineWrappingMode.Boundary };
        tagsField = new TextEdit { Name = "Tags", PlaceholderText = "每行一个标签", CustomMinimumSize = new Vector2(0, 70) };
        AddField(basic, "模板 ID", idField);
        AddField(basic, "显示名称", nameField);
        AddField(basic, "区域描述", description);
        AddField(basic, "区域标签", tagsField);
        idField.TextChanged += _ => OnFormEdited();
        nameField.TextChanged += _ => OnFormEdited();
        description.TextChanged += OnFormEdited;
        tagsField.TextChanged += OnFormEdited;

        var details = TabScroll("高级配置");
        groupPicker = new OptionButton { Name = "AdvancedGroup", FitToLongestItem = false };
        foreach (var group in Groups) groupPicker.AddItem(group.Name);
        details.AddChild(groupPicker);
        advancedKeys = Text("", 12, Muted);
        advancedKeys.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(advancedKeys);
        advanced = new TextEdit { Name = "AdvancedJson", CustomMinimumSize = new Vector2(0, 300), WrapMode = TextEdit.LineWrappingMode.Boundary };
        details.AddChild(advanced);
        groupPicker.ItemSelected += index => ShowGroup((int)index);
        advanced.TextChanged += OnAdvancedEdited;
    }

    private void BuildCanvas(VBoxContainer column)
    {
        canvasTitle = Text("区域画布", 19, Ink);
        canvasTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        column.AddChild(canvasTitle);
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 5);
        column.AddChild(toolbar);
        pointer = AddButton(toolbar, "选择", () => SetTool(null, false));
        eraser = AddButton(toolbar, "擦除", () => SetTool(null, true));
        pointer.ToggleMode = eraser.ToggleMode = true;
        undo = AddButton(toolbar, "撤销", () => canvas.Undo());
        undo.TooltipText = "撤销最近一次地形操作（Ctrl + Z）";
        toolbar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var modes = new ButtonGroup();
        var illustrated = AddButton(toolbar, "地形图", () => ShowTerrainArt(true));
        var symbols = AddButton(toolbar, "字标", () => ShowTerrainArt(false));
        illustrated.ToggleMode = symbols.ToggleMode = true;
        illustrated.ButtonGroup = symbols.ButtonGroup = modes;
        illustrated.SetPressedNoSignal(true);
        illustrated.TooltipText = "显示探索地图的地形图片";
        symbols.TooltipText = "显示地形诊断色和字标";
        toolbar.AddChild(new VSeparator());
        AddButton(toolbar, "−", () => canvas.Zoom(1 / 1.2f));
        AddButton(toolbar, "+", () => canvas.Zoom(1.2f));
        AddButton(toolbar, "适配", () => canvas.Fit());

        canvas = new RegionEditorCanvas { Name = "RegionCanvas", ClipContents = true, SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddChild(canvas);
        canvas.Edited += OnCanvasEdited;
        canvas.Inspected += text => cursorLabel.Text = text;
        canvas.SelectionChanged += RefreshSelectedLandMarks;

        var row = new HBoxContainer();
        column.AddChild(row);
        cursorLabel = Text("原点 (0, 0)", 13, Muted);
        cursorLabel.Name = "RegionCursor";
        cursorLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        cursorLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(cursorLabel);
        footprint = Text("0 格", 13, Gold);
        row.AddChild(footprint);
        canvasHelp = Text(TerrainHelp, 12, Muted);
        canvasHelp.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        column.AddChild(canvasHelp);
    }

    private void BuildLibrary(VBoxContainer column)
    {
        var libraryTabs = new TabContainer { Name = "RegionContentTabs", SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddChild(libraryTabs);
        var terrain = LibraryPage(libraryTabs, "地形");
        BuildTileLibrary(terrain);
        BuildLandMarkLibrary(LibraryPage(libraryTabs, "地标"));
        var events = LibraryPage(libraryTabs, "事件");
        events.AddChild(Text("事件布置", 19, Ink));
        var eventHint = Text("预留页签。区域事件接入后，可在这里编排事件落点。", 14, Muted);
        eventHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        events.AddChild(eventHint);
        libraryTabs.TabChanged += index =>
        {
            canvas.RightClickErasesTerrain = index == 0;
            canvasHelp.Text = index == 1 ? "拖地标到占地格，或选中后点格放置  ·  右栏可逐个移除" : TerrainHelp;
            SetTool(null, false);
            RefreshSelectedLandMarks();
        };
    }

    private const string TerrainHelp = "拖入 Tile 或选中后点击放置  ·  滚轮缩放  ·  中键平移  ·  右键擦除";

    private static VBoxContainer LibraryPage(TabContainer parent, string title)
    {
        var margin = new MarginContainer { Name = title };
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 7);
        parent.AddChild(margin);
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        margin.AddChild(body);
        return body;
    }

    private void BuildTileLibrary(VBoxContainer column)
    {
        var heading = new HBoxContainer();
        column.AddChild(heading);
        var title = Text("地块库", 19, Ink);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        heading.AddChild(title);
        tileCount = Text("", 13, Gold);
        heading.AddChild(tileCount);
        tileSearch = new LineEdit { Name = "TileSearch", PlaceholderText = "搜索 Tile 名称、ID 或标签", ClearButtonEnabled = true };
        column.AddChild(tileSearch);
        tileSearch.TextChanged += _ => RefreshTiles();
        var filters = new HBoxContainer();
        column.AddChild(filters);
        tagFilter = new OptionButton { Name = "TerrainFilter", FitToLongestItem = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = "按地形标签筛选" };
        waterFilter = new OptionButton { Name = "WaterFilter", FitToLongestItem = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = "按水体类型筛选" };
        filters.AddChild(tagFilter);
        filters.AddChild(waterFilter);
        tagFilter.ItemSelected += _ => RefreshTiles();
        waterFilter.ItemSelected += _ => RefreshTiles();
        libraryScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(libraryScroll);
        tileGrid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tileGrid.AddThemeConstantOverride("h_separation", 7);
        tileGrid.AddThemeConstantOverride("v_separation", 7);
        libraryScroll.AddChild(tileGrid);
    }

    private VBoxContainer TabScroll(string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        tabs.AddChild(scroll);
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 9);
        scroll.AddChild(margin);
        var form = new VBoxContainer();
        form.AddThemeConstantOverride("separation", 8);
        margin.AddChild(form);
        return form;
    }

    private static VBoxContainer Column(HBoxContainer parent, int width)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
        if (width == 0) panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride("panel", Surface(Panel, Border, 8, 12));
        parent.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 9);
        panel.AddChild(column);
        return column;
    }

    internal static StyleBoxFlat Surface(Color fill, Color outline, int radius, int padding = 0)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = outline };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(radius);
        foreach (var side in new[] { Side.Left, Side.Right, Side.Top, Side.Bottom }) style.SetContentMargin(side, padding);
        return style;
    }

    private static Label Text(string value, int fontSize, Color color)
    {
        var label = new Label { Text = value };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button AddButton(Container parent, string caption, Action action)
    {
        var button = new Button { Text = caption, CustomMinimumSize = new Vector2(0, 32) };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private static void AddField(VBoxContainer parent, string caption, Control field)
    {
        parent.AddChild(Text(caption, 13, Gold));
        parent.AddChild(field);
    }
}
