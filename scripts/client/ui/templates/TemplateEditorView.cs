using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;
using static Xianxia.Client.UI.Templates.TemplateEditorWidgets;

namespace Xianxia.Client.UI.Templates;

/// <summary>Template editor host. Only detached drafts are edited; trial runs in the existing observer.</summary>
public partial class TemplateEditorView : Control
{
    private TemplateEditorSkin _skin = null!;
    private readonly ObserverArtCatalog _art = new();
    private IReadOnlyDictionary<string, KeywordLabel> _keywordLabels = new Dictionary<string, KeywordLabel>();
    private TemplateLibraryPanel _library = null!;
    private TemplateLoadoutPanel _loadout = null!;
    private TemplateCardLibraryPanel _cardLibrary = null!;
    private VBoxContainer _details = null!;
    private Label _title = null!, _status = null!, _limits = null!;
    private Button _save = null!, _copy = null!, _trial = null!;
    private OptionButton _opponents = null!;
    private IReadOnlyList<BackpackPosition> _problemCells = Array.Empty<BackpackPosition>();
    private readonly List<string> _opponentIds = new();
    private bool _ownsQuitSetting, _previousAutoAcceptQuit;
    private readonly Dictionary<string, Control> _fieldControls = new();

    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.TemplateEditor) return;
        _previousAutoAcceptQuit = GetTree().AutoAcceptQuit;
        _ownsQuitSetting = true;
        GetTree().AutoAcceptQuit = false;
        try
        {
            BindPage();
            _skin = new TemplateEditorSkin();
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
        if (what == NotificationWMCloseRequest && _ownsQuitSetting && IsVisibleInTree())
            ConfirmLeave(() => GetTree().Quit());
    }

    public override void _ExitTree()
    {
        // 关闭确认属于模板页面，退出后把 SceneTree 的设置交还给首页。
        if (_ownsQuitSetting) GetTree().AutoAcceptQuit = _previousAutoAcceptQuit;
        _ownsQuitSetting = false;
    }

    private void BindPage()
    {
        // 每个子场景绑定自己的唯一节点。页面只连接跨面板操作，持有草稿、选择和试战状态。
        _library = GetNode<TemplateLibraryPanel>("%TemplateLibrary");
        _loadout = GetNode<TemplateLoadoutPanel>("%BackpackPanel");
        _cardLibrary = GetNode<TemplateCardLibraryPanel>("%CardLibrary");
        _details = GetNode<VBoxContainer>("%CharacterDetailsMount");
        _title = GetNode<Label>("%CurrentTemplate");
        _status = GetNode<Label>("%Status");
        _save = GetNode<Button>("%SaveDraft");
        _copy = GetNode<Button>("%CopyTemplate");
        _trial = GetNode<Button>("%StartTrial");
        _opponents = GetNode<OptionButton>("%Opponent");

        Clear(_details);
        _title.Text = _opponents.Text = "";
        _status.Text = "正在加载";
        _save.Disabled = _copy.Disabled = _trial.Disabled = true;

        _library.ProjectSelected += OpenProject;
        _library.PersonalSelected += OpenPersonal;
        _library.ImportRequested += ImportTemplates;
        _library.ExportRequested += ExportTemplate;
        _cardLibrary.RequestedRefresh += RefreshCatalog;
        _cardLibrary.CardSelected += id =>
        {
            _selectedCardId = id;
            _selectedEntryId = null;
            RefreshSelection();
        };
        _loadout.MainRequested += SelectMainDefinition;
        _loadout.EntrySelected += id =>
        {
            _problemCells = Array.Empty<BackpackPosition>();
            SelectEntry(id);
        };
        _loadout.UnplacedSelected += SelectEntry;
        _loadout.RemoveRequested += RemoveEntry;
        _loadout.MoveRequested += MoveCard;
        _loadout.AddRequested += AddCard;

        GetNode<Button>("%ReturnHome").Pressed += () => ConfirmLeave(() => GameBootstrap.OpenHome());
        GetNode<Button>("%DefaultBattle").Pressed += OpenDefaultBattle;
        GetNode<Button>("%NewTemplate").Pressed += ShowNewDialog;
        GetNode<Button>("%CheckDraft").Pressed += ShowDiagnostics;
        GetNode<Button>("%TrialSettings").Pressed += ShowTrialSettings;
        _copy.Pressed += CopyTemplate;
        _save.Pressed += () => SaveTemplate();
        _trial.Pressed += BeginTrial;
        _opponents.ItemSelected += index =>
        {
            if (index >= 0 && index < _opponentIds.Count) _opponentKey = _opponentIds[(int)index];
        };
    }

    private void SelectEntry(string id)
    {
        _selectedEntryId = id;
        _selectedCardId = null;
        RefreshSelection();
    }

    private void RefreshEditor()
    {
        if (_catalogSession.Current == null) return;
        Revalidate();
        _library.SetTemplates(Catalog.Npcs, _personal, _draft, _readOnly);
        _loadout.SetMainChoices(Catalog, _draft, _readOnly);
        _cardLibrary.Bind(Catalog.Cards, _keywordLabels, _art, !_readOnly && _draft != null);
        RefreshOpponents();
        RefreshSelection();
    }

    private void RefreshAfterEdit()
    {
        Revalidate(); RefreshPreview(); UpdateSummary();
    }
    private void RefreshSelection() { RefreshDetails();RefreshPreview();UpdateSummary(); }
    private void ShowNotice(string message) { _notice=message; if(_status!=null) UpdateSummary(); }

    private void RefreshOpponents()
    {
        _opponents.Clear();
        _opponentIds.Clear();
        foreach (var npc in Catalog.Npcs)
        {
            _opponentIds.Add("project:" + npc.ID);
            _opponents.AddItem("对手：" + DefinitionName(npc));
        }
        foreach (var draft in _personal)
        {
            _opponentIds.Add("draft:" + draft.DraftId);
            _opponents.AddItem("个人：" + DefinitionName(draft.NpcFields));
        }
        if (_opponentIds.Count == 0) return;
        int selected = Math.Max(0, _opponentIds.IndexOf(_opponentKey));
        _opponents.Select(selected);
        _opponentKey = _opponentIds[selected];
    }

    private void RefreshPreview()
    {
        var actorTexture = _draft == null ? null : _skin.Actor(EffectiveVisual(_draft));
        _loadout.SetPreview(Catalog, _draft, _readOnly, _selectedEntryId, _problemCells, actorTexture);
    }

    private void UpdateSummary()
    {
        if(_title==null)return;
        _title.Text=_draft==null?"请选择模板":DefinitionName(_draft.NpcFields)+(_readOnly?" · 项目模板":_draft.Dirty?" · 未保存":" · 已保存");
        _save.Disabled=_readOnly||_draft==null;_copy.Disabled=_draft==null;
        _library.SetExportEnabled(_draft != null && !_readOnly && _validation?.IsValid == true);
        _trial.Disabled=_draft==null||_validation?.IsValid!=true;
        var r=_validation?.Template;
        _loadout.SetSummary(r);
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
        popup.Canceled+=()=>popup.QueueFree();AddChild(popup);TemplateDialogLayout.Show(popup);
    }
    private void ShowDiagnostics()
    {
        Revalidate();UpdateSummary();
        var popup=new AcceptDialog {Title="配置检查",OkButtonText="关闭"};
        var scroll=new ScrollContainer();popup.AddChild(scroll);
        var list=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(list);
        if(_validation==null)FlowText(list,"尚未选择模板。",18);
        else foreach(var issue in _validation.Issues)
        {
            var b=FlowButton(list,(issue.Severity==TemplateIssueSeverity.Error?"错误 · ":"提示 · ")+issue.Message,()=>{FocusIssue(issue);popup.QueueFree();},false,40);
            b.TooltipText=issue.FieldPath;
        }
        if(_validation?.Issues.Count==0)FlowText(list,"配置检查通过。",18);
        popup.Confirmed+=()=>popup.QueueFree();AddChild(popup);TemplateDialogLayout.Show(popup,new(740,450));
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
        dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);TemplateDialogLayout.Show(dialog,new(480,280));
    }
    private SpinBox Setting(Control parent,string label,double value,double min,double max)
    {var spin=new SpinBox{MinValue=min,MaxValue=max,Step=1,Value=value,SizeFlagsHorizontal=SizeFlags.ExpandFill};Row(parent,label,spin);return spin;}

    private static string Number(float? value)=>value?.ToString("0.##")??"?";
    private static HBoxContainer Row(Control parent,string label,Control input)
    {var row=new HBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};parent.AddChild(row);var text=FlowText(row,label,16,false);text.CustomMinimumSize=new(88,32);text.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;input.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(input);return row;}
}
