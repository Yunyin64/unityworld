using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;

namespace Xianxia.Client.UI.Templates;

/// <summary>Template editor host. Only detached drafts are edited; trial runs in the existing observer.</summary>
public partial class TemplateEditorView : Control
{
    private TemplateEditorSkin _skin = null!;
    private readonly ObserverArtCatalog _art = new();
    private readonly CardLibraryFilter _filter = new();
    private IReadOnlyDictionary<string, KeywordLabel> _keywordLabels = new Dictionary<string, KeywordLabel>();
    private Control _canvas = null!;
    private VBoxContainer _templates = null!, _details = null!, _unplaced = null!;
    private HFlowContainer _keywords = null!, _cards = null!;
    private ScrollContainer _keywordScroll = null!;
    private TemplateBackpackView _backpack = null!;
    private TextureRect _actor = null!;
    private Label _title = null!, _packSummary = null!, _actorName = null!, _status = null!, _count = null!, _limits = null!;
    private Button _save = null!, _copy = null!, _export = null!, _trial = null!, _expandKeywords = null!;
    private OptionButton _main = null!, _opponents = null!, _element = null!, _size = null!, _compatibility = null!;
    private LineEdit _search = null!, _templateSearch = null!;
    private string _templateKind = "";
    private bool _projectExpanded = true, _personalExpanded = true, _keywordsExpanded, _binding;
    private double _searchDelay = -1;
    private IReadOnlyList<BackpackPosition> _problemCells = Array.Empty<BackpackPosition>();
    private readonly List<string> _mainIds = new(), _opponentIds = new(), _elements = new();
    private readonly List<Control> _filterCaptions = new();
    private readonly Dictionary<string, Control> _fieldControls = new();

    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.TemplateEditor) return;
        GetTree().AutoAcceptQuit = false;
        try
        {
            _skin = new TemplateEditorSkin();
            Theme = _skin.Theme;
            BuildPage();
            Resized += FitCanvas;
            FitCanvas();
            _keywordLabels = CardLibraryQuery.LoadLabels(ProjectSettings.GlobalizePath("res://assets/presentation/keyword_labels.json"));
            InitializeDocuments();
            if(_catalogSession.Current!=null) GD.Print($"[TemplateEditor] ready templates={Catalog.Npcs.Count} cards={Catalog.Cards.Count} personal={_personal.Count}");
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            if (_status != null) ShowNotice("编辑器加载失败：" + ex.Message);
            else AddChild(new Label { Text = "编辑器加载失败：" + ex.Message, Position = new(24,24), Size = new(1100,120), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            ConfirmLeave(() => GetTree().Quit());
    }

    public override void _Process(double delta)
    {
        if (_searchDelay < 0) return;
        _searchDelay -= delta;
        if (_searchDelay <= 0) { _searchDelay = -1; RefreshCards(); }
    }

    private void FitCanvas()
    {
        if (_canvas == null) return;
        float factor = Math.Max(0.1f, Math.Min(Size.X / 1600f, Size.Y / 900f));
        _canvas.Scale = new(factor,factor);
        _canvas.Position = (Size - new Vector2(1600,900) * factor) / 2;
    }

    private void BuildPage()
    {
        _canvas = new Control { Name = "EditorCanvas", Size = new(1600,900) };
        AddChild(_canvas);
        _canvas.AddChild(new TextureRect { Texture = _skin.Background, Size = new(1600,900), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore });
        _canvas.AddChild(new ColorRect { Color = new Color(0.05f,0.1f,0.12f,0.16f), Size = new(1600,900), MouseFilter = MouseFilterEnum.Ignore });
        var title = Paper(_canvas,new(390,6,670,64));
        Text(title,"人物模板编辑器",new(24,7,360,44),29);
        MakeButton(title,"模板编辑",new(384,12,120,38),()=>{},true);
        MakeButton(title,"战斗观察",new(512,12,136,38),OpenDefaultBattle);
        MakeButton(_canvas,"新建",new(1150,16,90,40),ShowNewDialog);
        _copy = MakeButton(_canvas,"复制模板",new(1250,16,138,40),CopyTemplate);
        _save = MakeButton(_canvas,"保存草稿",new(1398,16,186,40),()=>SaveTemplate(),true);
        _title=Text(_canvas,"选择模板开始",new(24,22,350,30),17,new Color("e8e3ce"));

        var left=Paper(_canvas,new(16,82,292,744));
        Text(left,"模板库",new(22,16,246,34),25);
        _templateSearch=Edit(left,"搜索名称或 ID",new(20,60,252,36));
        _templateSearch.TextChanged += _ => RefreshTemplateList();
        string[] types={"全部","人物","怪物"};
        for(int i=0;i<types.Length;i++)
        {
            string kind=i==0?"":i==1?"Human":"Monster";
            MakeButton(left,types[i],new(20+i*86,106,80,34),()=>{_templateKind=kind;RefreshTemplateList();});
        }
        var listScroll=Scroll(left,new(18,152,256,518));
        _templates=new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill, CustomMinimumSize=new(238,0) };
        _templates.AddThemeConstantOverride("separation",9); listScroll.AddChild(_templates);
        MakeButton(left,"导入",new(20,684,118,38),ImportTemplates);
        _export=MakeButton(left,"导出 JSON",new(148,684,124,38),ExportTemplate);

        var pack=Paper(_canvas,new(320,82,800,420));
        Text(pack,"背包装配",new(22,16,150,36),25);
        Text(pack,"主功法",new(194,18,80,34),17);
        _main=Choice(pack,new(270,17,268,36));
        _main.ItemSelected += index => { if(!_binding && index>0 && index<_mainIds.Count) SelectMainDefinition(_mainIds[(int)index]); };
        _packSummary=Text(pack,"未选择主功法",new(558,18,220,34),18);
        _backpack=new TemplateBackpackView { Position=new(34,64), Size=new(360,320) };
        pack.AddChild(_backpack);
        _backpack.Selected += id => { _selectedEntryId=id;_selectedCardId=null;_problemCells=Array.Empty<BackpackPosition>();RefreshSelection(); };
        _backpack.Removed += RemoveEntry;
        _backpack.MoveRequested += MoveCard;
        _backpack.AddRequested += AddCard;
        _actor=new TextureRect { Position=new(444,98),Size=new(300,222),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter=MouseFilterEnum.Ignore };
        pack.AddChild(_actor);
        _actorName=Text(pack,"外观预览",new(428,65,324,40),23,null,HorizontalAlignment.Center);
        var unplacedScroll=Scroll(pack,new(420,322,350,62));
        _unplaced=new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new(328,0) }; unplacedScroll.AddChild(_unplaced);
        Text(pack,"拖入添加 · 拖动摆放 · 右键移除",new(32,384,730,22),14,new Color("59614b"));

        var library=Paper(_canvas,new(320,512,800,314));
        Text(library,"卡牌库",new(22,14,122,34),25);
        _search=Edit(library,"搜索名称 / ID / 描述",new(154,16,342,34));
        _search.TextChanged+=text=>{_filter.Text=text;_searchDelay=0.18;};
        MakeButton(library,"刷新",new(510,16,82,34),RefreshCatalog);
        MakeButton(library,"清空筛选",new(600,16,108,34),ClearFilters);
        _count=Text(library,"",new(714,16,62,34),15,null,HorizontalAlignment.Right);
        Text(library,"关键词",new(22,62,80,25),18);
        Text(library,"同时满足",new(22,90,80,20),13,new Color("62664f"));
        _keywordScroll=Scroll(library,new(106,58,584,80));
        _keywords=new HFlowContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill, CustomMinimumSize=new(560,0) };
        _keywords.AddThemeConstantOverride("h_separation",6);_keywords.AddThemeConstantOverride("v_separation",6);_keywordScroll.AddChild(_keywords);
        _expandKeywords=MakeButton(library,"展开",new(700,62,76,30),()=>{_keywordsExpanded=!_keywordsExpanded;ResizeCardArea();});
        _filterCaptions.Add(Text(library,"需求",new(22,145,48,30),16));
        _element=Choice(library,new(70,144,170,32));
        _element.ItemSelected += i=>{if(!_binding){_filter.Element=_elements[(int)i];RefreshCards();}};
        _filterCaptions.Add(Text(library,"占格",new(254,145,48,30),16));
        _size=Choice(library,new(306,144,110,32));
        _size.ItemSelected += i=>{if(!_binding){_filter.Size=i==0?null:_size.GetItemId((int)i);RefreshCards();}};
        _compatibility=Choice(library,new(430,144,178,32));_compatibility.AddItem("全部状态");_compatibility.AddItem("可装配");
        _compatibility.ItemSelected += i=>{if(!_binding){_filter.OnlyAvailable=i==1;RefreshCards();}};
        var cardScroll=Scroll(library,new(20,184,760,112));cardScroll.Name="CardResults";
        _cards=new HFlowContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill, CustomMinimumSize=new(736,0) };
        _cards.AddThemeConstantOverride("h_separation",8);_cards.AddThemeConstantOverride("v_separation",8);cardScroll.AddChild(_cards);

        var right=Paper(_canvas,new(1132,82,452,744));
        var detailScroll=Scroll(right,new(20,20,412,704));
        _details=new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new(390,0) };
        _details.AddThemeConstantOverride("separation",9);detailScroll.AddChild(_details);
        var bottom=new Panel { Position=new(16,840),Size=new(1568,46) };bottom.AddThemeStyleboxOverride("panel",_skin.Button);_canvas.AddChild(bottom);
        _status=Text(bottom,"正在加载",new(16,4,754,38),16,new Color("eee7d1"));
        MakeButton(bottom,"检查配置",new(788,5,116,36),ShowDiagnostics);
        _opponents=Choice(bottom,new(918,5,266,36));
        _opponents.ItemSelected+=i=>{if(!_binding&&i<_opponentIds.Count)_opponentKey=_opponentIds[(int)i];};
        MakeButton(bottom,"参数",new(1196,5,84,36),ShowTrialSettings);
        _trial=MakeButton(bottom,"进入试战",new(1294,5,256,36),BeginTrial,true);
    }

    private void RefreshEditor()
    {
        if(_catalogSession.Current==null) return;
        Revalidate();
        RefreshTemplateList(); RefreshMainChoices(); RefreshFilters(); RefreshCards(); RefreshOpponents();
        RefreshDetails(); RefreshPreview(); UpdateSummary();
    }
    private void RefreshAfterEdit()
    {
        Revalidate(); RefreshPreview(); UpdateSummary();
    }
    private void RefreshSelection() { RefreshDetails();RefreshPreview();UpdateSummary(); }
    private void ShowNotice(string message) { _notice=message; if(_status!=null) UpdateSummary(); }

    private void RefreshTemplateList()
    {
        if(_catalogSession.Current==null||_templates==null) return;
        Clear(_templates);
        bool Match(NpcDefine n) => (string.IsNullOrEmpty(_templateKind)||n.NpcType.ToString()==_templateKind) &&
            (DefinitionName(n).Contains(_templateSearch.Text,StringComparison.OrdinalIgnoreCase)||n.ID.Contains(_templateSearch.Text,StringComparison.OrdinalIgnoreCase));
        var projects=Catalog.Npcs.Where(Match).ToArray();
        FlowButton(_templates,(_projectExpanded?"▾ ":"▸ ")+"项目模板",()=>{_projectExpanded=!_projectExpanded;RefreshTemplateList();},false,36);
        if(_projectExpanded) foreach(var n in projects)
        {
            var row=FlowButton(_templates,DefinitionName(n),()=>OpenProject(n.ID),_readOnly&&_draft?.SourceTemplateId==n.ID,64);
            row.TooltipText=$"{n.ID}\n项目模板 · 复制后编辑";
        }
        FlowButton(_templates,(_personalExpanded?"▾ ":"▸ ")+"我的模板",()=>{_personalExpanded=!_personalExpanded;RefreshTemplateList();},false,36);
        if(_personalExpanded)
        {
            foreach(var d in _personal.Where(d=>Match(d.NpcFields)))
            {
                var row=FlowButton(_templates,DefinitionName(d.NpcFields)+(d.Dirty?"  •":"")+"\n个人草稿",()=>OpenPersonal(d.DraftId),!_readOnly&&_draft?.DraftId==d.DraftId,64);
                row.TooltipText=d.NpcFields.ID;
            }
            if(_personal.Count==0) FlowText(_templates,"点击“新建”或复制项目模板",14);
        }
    }

    private void RefreshMainChoices()
    {
        _binding=true;_main.Clear();_mainIds.Clear();_main.AddItem("请选择主功法");_mainIds.Add("");
        string? selected=_draft?.Entries.FirstOrDefault(e=>e.EntryId==_draft.MainEntryId)?.CardDefineId;
        foreach(var c in Catalog.Cultivations.Where(c=>Catalog.GetCard(c.ID) is { } card && CardLibraryQuery.UnavailableReason(card)==null))
        { _mainIds.Add(c.ID);_main.AddItem(DefinitionName(c));if(c.ID==selected)_main.Select(_mainIds.Count-1); }
        _main.Disabled=_readOnly||_draft==null;_binding=false;
    }
    private void RefreshFilters()
    {
        _binding=true;Clear(_keywords);
        var all=new Button { Text="全部",ToggleMode=true,ButtonPressed=_filter.Keywords.Count==0,CustomMinimumSize=new(70,30) }; StyleButton(all,true);_keywords.AddChild(all);
        all.Pressed+=()=>{_filter.Keywords.Clear();RefreshFilters();RefreshCards();};
        foreach(var option in CardLibraryQuery.Keywords(Catalog.Cards,_keywordLabels))
        {
            var button=new Button { Text=option.DisplayName,TooltipText=option.Id,ToggleMode=true,ButtonPressed=_filter.Keywords.Contains(option.Id),CustomMinimumSize=new(70,30) };
            StyleButton(button,true);_keywords.AddChild(button);
            button.Toggled+=pressed=>{if(pressed)_filter.Keywords.Add(option.Id);else _filter.Keywords.Remove(option.Id);RefreshFilters();RefreshCards();};
        }
        _elements.Clear();_element.Clear();_elements.Add("");_element.AddItem("全部元素");_elements.Add("None");_element.AddItem("无灵元需求");
        foreach(string e in Catalog.Cards.SelectMany(c=>c.ManaCost.Keys).Distinct().OrderBy(e=>e,StringComparer.Ordinal))
        { _elements.Add(e);_element.AddItem(ObserverArtCatalog.ElementName(e)); }
        _element.Select(Math.Max(0,_elements.IndexOf(_filter.Element)));
        _size.Clear();_size.AddItem("不限",0);
        foreach(int size in Catalog.Cards.Select(c=>c.Size).Distinct().OrderBy(s=>s)) { _size.AddItem(size+" 格",size);if(_filter.Size==size)_size.Select(_size.ItemCount-1); }
        _compatibility.Select(_filter.OnlyAvailable?1:0);_binding=false;
    }
    private void RefreshCards()
    {
        if(_catalogSession.Current==null||_cards==null)return;
        Clear(_cards);
        var matches=CardLibraryQuery.Search(Catalog.Cards,_filter);_count.Text=matches.Count+" 张";
        foreach(var c in matches)
        {
            var tile=new TemplateCardTile { CustomMinimumSize=new(132,102) };_cards.AddChild(tile);
            tile.Configure(c,_art.Card(c.ID),_art.CardTranspose(c.ID),!_readOnly&&_draft!=null,CardLibraryQuery.UnavailableReason(c));
            tile.Selected+=id=>{_selectedCardId=id;_selectedEntryId=null;RefreshSelection();};
        }
        if(matches.Count==0)FlowText(_cards,"没有符合条件的卡牌，可清空筛选。",17);
    }
    private void ResizeCardArea()
    {
        // Keywords scroll inside a bounded area; result list always remains accessible.
        _keywordScroll.Size=new(584,_keywordsExpanded?112:80);
        _expandKeywords.Text=_keywordsExpanded?"收起":"展开";
        var results=_cards.GetParent<ScrollContainer>();
        results.Position=new(20,_keywordsExpanded?216:184);results.Size=new(760,_keywordsExpanded?80:112);
        foreach(var c in new Control[]{_element,_size,_compatibility}) c.Position=new(c.Position.X,_keywordsExpanded?176:144);
        foreach(var c in _filterCaptions)c.Position=new(c.Position.X,_keywordsExpanded?177:145);
    }
    private void ClearFilters()
    { _filter.Clear();_search.Text="";RefreshFilters();RefreshCards(); }

    private void RefreshOpponents()
    {
        _binding=true;_opponents.Clear();_opponentIds.Clear();
        foreach(var n in Catalog.Npcs) { _opponentIds.Add("project:"+n.ID);_opponents.AddItem("对手："+DefinitionName(n)); }
        foreach(var d in _personal) { _opponentIds.Add("draft:"+d.DraftId);_opponents.AddItem("个人："+DefinitionName(d.NpcFields)); }
        int selected=_opponentIds.IndexOf(_opponentKey);if(selected<0)selected=0;
        if(_opponentIds.Count>0){_opponents.Select(selected);_opponentKey=_opponentIds[selected];}_binding=false;
    }

    private void RefreshPreview()
    {
        _backpack.Configure(Catalog,_draft,_readOnly,_selectedEntryId,_problemCells);
        _actor.Texture=_draft==null?null:_skin.Actor(EffectiveVisual(_draft));
        _actorName.Text=_draft==null?"选择一个模板":DefinitionName(_draft.NpcFields);
        Clear(_unplaced);
        if(_draft==null)return;
        foreach(var e in _draft.Entries.Where(e=>e.Position==null))
        {
            var b=FlowButton(_unplaced,"未放置 · "+(Catalog.GetCard(e.CardDefineId)?.DisplayName??e.CardDefineId),()=>{_selectedEntryId=e.EntryId;_selectedCardId=null;RefreshSelection();},false,30);
            // Dragging from a compact unplaced button uses the same payload as grid instances.
            if(!_readOnly)b.SetDragForwarding(Callable.From<Vector2,Variant>(_=>new Godot.Collections.Dictionary{{"kind","entry"},{"id",e.EntryId}}),default,default);
        }
    }
    private void UpdateSummary()
    {
        if(_title==null)return;
        _title.Text=_draft==null?"请选择模板":DefinitionName(_draft.NpcFields)+(_readOnly?" · 项目模板":_draft.Dirty?" · 未保存":" · 已保存");
        _save.Disabled=_readOnly||_draft==null;_copy.Disabled=_draft==null;
        _export.Disabled=_draft==null||_readOnly||_validation?.IsValid!=true;
        _trial.Disabled=_draft==null||_validation?.IsValid!=true;
        var r=_validation?.Template;
        _packSummary.Text=r?.Layout is {} layout?$"{layout.Width}×{layout.Height}   SP {r.OccupiedSp}/{Number(r.SpMax)}":"请选择主功法";
        if(_limits!=null)_limits.Text=$"HP  {Number(r?.HpMax)}      MP  {Number(r?.MpMax)}      SP  {Number(r?.SpMax)}";
        int errors=_validation?.Issues.Count(i=>i.Severity==TemplateIssueSeverity.Error)??0;
        int warnings=_validation?.Issues.Count(i=>i.Severity==TemplateIssueSeverity.Warning)??0;
        string check=_draft==null?"选择模板开始":errors>0?$"配置待修复：{errors} 项":"布局检查通过";
        _status.Text=string.IsNullOrEmpty(_notice)?check+(warnings>0?$" · {warnings} 项支持提示":""):_notice;
        _status.TooltipText=string.Join("\n",new[]{_notice,check}.Concat(_validation?.Issues.Select(i=>i.Message)??Array.Empty<string>()));
    }

    private void ShowNewDialog()
    {
        var popup=new ConfirmationDialog { Title="新建模板",DialogText="选择模板种类",OkButtonText="新建人物",CancelButtonText="取消" };
        popup.AddButton("新建怪物",true,"monster");
        popup.Confirmed+=()=>{NewTemplate(NpcTypes.NpcType.Human);popup.QueueFree();};
        popup.CustomAction+=action=>{if(action=="monster"){NewTemplate(NpcTypes.NpcType.Monster);popup.Hide();popup.QueueFree();}};
        popup.Canceled+=()=>popup.QueueFree();AddChild(popup);popup.PopupCentered(new(420,160));
    }
    private void ShowDiagnostics()
    {
        Revalidate();UpdateSummary();
        var popup=new AcceptDialog {Title="配置检查",OkButtonText="关闭"};
        var scroll=new ScrollContainer {CustomMinimumSize=new(680,360)};popup.AddChild(scroll);
        var list=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(list);
        if(_validation==null)FlowText(list,"尚未选择模板。",18);
        else foreach(var issue in _validation.Issues)
        {
            var b=FlowButton(list,(issue.Severity==TemplateIssueSeverity.Error?"错误 · ":"提示 · ")+issue.Message,()=>{FocusIssue(issue);popup.Hide();},false,40);
            b.TooltipText=issue.FieldPath;
        }
        if(_validation?.Issues.Count==0)FlowText(list,"配置检查通过。",18);
        popup.Confirmed+=()=>popup.QueueFree();AddChild(popup);popup.PopupCentered(new(740,450));
    }
    private void FocusIssue(TemplateIssue issue)
    {
        if(_draft!=null&&issue.PlacementIndex is int i&&i>=1&&i<=_draft.Entries.Count)
        {_selectedEntryId=_draft.Entries[i-1].EntryId;_selectedCardId=null;}
        _problemCells=issue.Cells??Array.Empty<BackpackPosition>();RefreshSelection();
        if(_fieldControls.TryGetValue(issue.FieldPath,out var control))control.GrabFocus();
        ShowNotice(issue.Message);
    }
    private void ShowTrialSettings()
    {
        var dialog=new ConfirmationDialog {Title="试战参数",OkButtonText="应用",CancelButtonText="取消"};
        var list=new VBoxContainer {CustomMinimumSize=new(420,180)};dialog.AddChild(list);
        var world=Setting(list,"世界种子",_trialSettings.WorldSeed,0,int.MaxValue);
        var combat=Setting(list,"战斗种子",_trialSettings.CombatSeed,0,int.MaxValue);
        var ticks=Setting(list,"步数上限",_trialSettings.MaxTicks,1,100000);
        var rows=Setting(list,"每行 Tick",_trialSettings.TicksPerRow,1,1000);
        dialog.Confirmed+=()=>{_trialSettings=new((int)world.Value,(int)combat.Value,(int)ticks.Value,(int)rows.Value);dialog.QueueFree();};
        dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);dialog.PopupCentered(new(480,280));
    }
    private SpinBox Setting(Control parent,string label,double value,double min,double max)
    {var spin=new SpinBox{MinValue=min,MaxValue=max,Step=1,Value=value,SizeFlagsHorizontal=SizeFlags.ExpandFill};Row(parent,label,spin);return spin;}

    private Panel Paper(Control parent,Rect2 rect)
    {var p=new Panel{Position=rect.Position,Size=rect.Size};p.AddThemeStyleboxOverride("panel",_skin.PanelStyle);parent.AddChild(p);return p;}
    private static ScrollContainer Scroll(Control parent,Rect2 rect)
    {var s=new ScrollContainer{Position=rect.Position,Size=rect.Size,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};parent.AddChild(s);return s;}
    private static Label Text(Control parent,string text,Rect2 rect,int size=18,Color? color=null,HorizontalAlignment align=HorizontalAlignment.Left)
    {var label=new Label{Text=text,Position=rect.Position,Size=rect.Size,ClipText=true,HorizontalAlignment=align,VerticalAlignment=VerticalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore};label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",color??new Color("33382b"));parent.AddChild(label);return label;}
    private static Label FlowText(Control parent,string text,int size=18,bool wrap=true)
    {var label=new Label{Text=text,CustomMinimumSize=new(0,28),SizeFlagsHorizontal=SizeFlags.ExpandFill,AutowrapMode=wrap?TextServer.AutowrapMode.WordSmart:TextServer.AutowrapMode.Off};label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",new Color("33382b"));parent.AddChild(label);return label;}
    private void StyleButton(Button b,bool keyword=false)
    {b.AddThemeStyleboxOverride("normal",keyword?_skin.Keyword:_skin.Button);b.AddThemeStyleboxOverride("hover",_skin.SelectedButton);b.AddThemeStyleboxOverride("pressed",keyword?_skin.SelectedKeyword:_skin.SelectedButton);b.AddThemeFontSizeOverride("font_size",16);b.AddThemeColorOverride("font_color",new Color(keyword?"33382b":"eee7d1"));b.AddThemeColorOverride("font_pressed_color",new Color("fff5d5"));}
    private Button MakeButton(Control parent,string title,Rect2 rect,Action action,bool selected=false)
    {var b=new Button{Text=title,Position=rect.Position,Size=rect.Size};StyleButton(b);if(selected)b.AddThemeStyleboxOverride("normal",_skin.SelectedButton);b.Pressed+=action;parent.AddChild(b);return b;}
    private Button FlowButton(Control parent,string title,Action action,bool selected=false,int height=34)
    {var b=new Button{Text=title,CustomMinimumSize=new(0,height),SizeFlagsHorizontal=SizeFlags.ExpandFill,ClipText=true};StyleButton(b,true);if(selected){b.AddThemeStyleboxOverride("normal",_skin.SelectedKeyword);b.AddThemeColorOverride("font_color",new Color("fff5d5"));}b.Pressed+=action;parent.AddChild(b);return b;}
    private LineEdit Edit(Control parent,string placeholder,Rect2 rect)
    {var edit=new LineEdit{PlaceholderText=placeholder,Position=rect.Position,Size=rect.Size};parent.AddChild(edit);return edit;}
    private OptionButton Choice(Control parent,Rect2 rect)
    {var choice=new OptionButton{Position=rect.Position,Size=rect.Size};StyleButton(choice);parent.AddChild(choice);return choice;}
    private static void Clear(Node parent) {foreach(var child in parent.GetChildren()){parent.RemoveChild(child);child.QueueFree();}}
    private static string DefinitionName(DefineBase def)=>string.IsNullOrWhiteSpace(def.DisplayName)?def.ID:def.DisplayName;
    private static string Number(float? value)=>value?.ToString("0.##")??"?";
    private static HBoxContainer Row(Control parent,string label,Control input)
    {var row=new HBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};parent.AddChild(row);var text=FlowText(row,label,16,false);text.CustomMinimumSize=new(88,32);text.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;input.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(input);return row;}
}
