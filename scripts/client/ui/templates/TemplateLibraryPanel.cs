using Godot;
using System;
using System.Collections.Generic;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;
using static Xianxia.Client.UI.Templates.TemplateEditorWidgets;

namespace Xianxia.Client.UI.Templates;

/// <summary>浏览项目与个人模板。点击只通知页面，草稿离开确认仍由编辑器处理。</summary>
public partial class TemplateLibraryPanel : Panel
{
    public event Action<string>? ProjectSelected;
    public event Action<string>? PersonalSelected;
    public event Action? ImportRequested;
    public event Action? ExportRequested;

    private LineEdit _search = null!;
    private VBoxContainer _list = null!;
    private Button _export = null!;
    private IReadOnlyList<NpcDefine> _projects = Array.Empty<NpcDefine>();
    private IReadOnlyList<TemplateDraft> _personal = Array.Empty<TemplateDraft>();
    private string? _selectedProjectId, _selectedDraftId;
    private NpcTypes.NpcType? _kind;
    private bool _projectsExpanded = true, _personalExpanded = true;

    public override void _Ready()
    {
        _search = GetNode<LineEdit>("%TemplateSearch");
        _list = GetNode<VBoxContainer>("%TemplateList");
        _export = GetNode<Button>("%ExportTemplate");
        _export.Disabled = true;
        GetNode<Button>("%ImportTemplates").Pressed += () => ImportRequested?.Invoke();
        _export.Pressed += () => ExportRequested?.Invoke();
        _search.TextChanged += _ => RefreshRows();
        GetNode<Button>("%AllKinds").Pressed += () => SetKind(null);
        GetNode<Button>("%HumanKind").Pressed += () => SetKind(NpcTypes.NpcType.Human);
        GetNode<Button>("%MonsterKind").Pressed += () => SetKind(NpcTypes.NpcType.Monster);
        Clear(_list); // 即使目录加载失败也不显示编辑器样文。
    }

    public void SetTemplates(IReadOnlyList<NpcDefine> projects, IReadOnlyList<TemplateDraft> personal,
        TemplateDraft? current, bool readOnly)
    {
        _projects = projects;
        _personal = personal;
        // 项目按定义 ID 选中，个人库按草稿 ID 选中，允许多个草稿源自同一项目模板。
        _selectedProjectId = readOnly ? current?.SourceTemplateId : null;
        _selectedDraftId = readOnly ? null : current?.DraftId;
        RefreshRows();
    }

    public void SetExportEnabled(bool enabled) => _export.Disabled = !enabled;

    private void SetKind(NpcTypes.NpcType? kind)
    {
        _kind = kind;
        RefreshRows();
    }

    private void RefreshRows()
    {
        Clear(_list);
        FlowButton(_list, (_projectsExpanded ? "▾ " : "▸ ") + "项目模板", () =>
        {
            _projectsExpanded = !_projectsExpanded;
            RefreshRows();
        }, height: 36);
        if (_projectsExpanded)
        {
            foreach (var npc in _projects)
            {
                if (!Matches(npc)) continue;
                var row = FlowButton(_list, DefinitionName(npc), () => ProjectSelected?.Invoke(npc.ID),
                    _selectedProjectId == npc.ID, 64);
                row.TooltipText = $"{npc.ID}\n项目模板 · 复制后编辑";
            }
        }

        FlowButton(_list, (_personalExpanded ? "▾ " : "▸ ") + "我的模板", () =>
        {
            _personalExpanded = !_personalExpanded;
            RefreshRows();
        }, height: 36);
        if (!_personalExpanded) return;
        foreach (var draft in _personal)
        {
            if (!Matches(draft.NpcFields)) continue;
            string title = DefinitionName(draft.NpcFields) + (draft.Dirty ? "  •" : "") + "\n个人草稿";
            var row = FlowButton(_list, title, () => PersonalSelected?.Invoke(draft.DraftId),
                _selectedDraftId == draft.DraftId, 64);
            row.TooltipText = draft.NpcFields.ID;
        }
        if (_personal.Count == 0)
            FlowText(_list, "点击“新建”或复制项目模板", 14);
    }

    private bool Matches(NpcDefine npc) =>
        (_kind == null || npc.NpcType == _kind) &&
        (DefinitionName(npc).Contains(_search.Text, StringComparison.OrdinalIgnoreCase) ||
         npc.ID.Contains(_search.Text, StringComparison.OrdinalIgnoreCase));
}
