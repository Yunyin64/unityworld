using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Battle;
using Xianxia.Client.BattleWindows;
using Xianxia.Client.Templates;

namespace Xianxia.Client.UI.Templates;

public partial class TemplateEditorView
{
    private readonly TemplateCatalogSession _catalogSession = new();
    private TemplateCatalog Catalog => _catalogSession.Current!;
    private TemplateDraftStore _store = null!;
    private List<TemplateDraft> _personal = new();
    private TemplateDraft? _draft;
    private bool _readOnly;
    private string? _selectedEntryId, _selectedCardId;
    private string _notice = "", _dataRoot = "", _opponentKey = "project:monster_wolf_0";
    private TemplateDraftValidation? _validation;
    private TemplateTrialSettings _trialSettings = new();
    private bool _leaveDialogOpen;

    private void InitializeDocuments()
    {
        try
        {
            _dataRoot = OS.HasFeature("editor") ? ProjectSettings.GlobalizePath("res://UnityWorld")
                : Path.GetDirectoryName(OS.GetExecutablePath()) ?? throw new InvalidOperationException("无法定位玩法数据目录。");
            _store = new(ProjectSettings.GlobalizePath("user://TemplateEditor"), Path.Combine(_dataRoot, "Data"));
            if (!_catalogSession.Refresh(_dataRoot)) throw new InvalidDataException(_catalogSession.LastError);
            ReloadPersonal();
            string? first = Catalog.GetNpc("test_npc_a")?.ID ?? Catalog.Npcs.FirstOrDefault()?.ID;
            if (first != null) SelectProjectDirect(first);
            else { _notice = "项目模板为空，可新建人物或怪物。"; RefreshEditor(); }
        }
        catch (Exception ex) { ShowNotice("加载编辑目录失败：" + ex.Message); }
    }

    private void ReloadPersonal()
    {
        var result = _store.LoadAll();
        _personal = result.Drafts.ToList();
        if (result.Errors.Count > 0) _notice = "部分个人草稿无法读取：" + string.Join("；", result.Errors);
    }

    private void SelectProjectDirect(string id)
    {
        var definition = Catalog.GetNpc(id) ?? throw new InvalidDataException("项目模板不存在：" + id);
        _draft = TemplateDocumentConverter.FromDefinition(definition, "Project", Catalog.DataFingerprint, Catalog.GetSource("Npc", id)?.File ?? "");
        _draft.Dirty = false; _readOnly = true; _selectedEntryId = null; _selectedCardId = null;
        Revalidate(); RefreshEditor();
    }

    private void OpenProject(string id) => ConfirmLeave(() =>
    {
        try { SelectProjectDirect(id); }
        catch (Exception ex) { ShowNotice("无法打开项目模板：" + ex.Message); }
    });

    private void OpenPersonal(string id)
    {
        if (!_readOnly && _draft?.DraftId == id) return;
        ConfirmLeave(() =>
        {
            try
            {
                var source = _personal.FirstOrDefault(d => d.DraftId == id) ?? throw new InvalidDataException("个人草稿不存在。");
                _draft = TemplateJson.Clone(source); _draft.Dirty = source.Dirty;
                _readOnly = false; _selectedEntryId = null; _selectedCardId = null;
                Revalidate(); RefreshEditor();
            }
            catch (Exception ex) { ShowNotice("无法打开个人草稿：" + ex.Message); }
        });
    }

    private void NewTemplate(NpcTypes.NpcType type) => ConfirmLeave(() =>
    {
        try
        {
            string id = TemplateDocumentConverter.UniqueId(type == NpcTypes.NpcType.Human ? "my_character" : "my_monster", AllTemplateIds());
            _draft = TemplateDraft.New(Catalog, type, id);
            _draft.NpcFields.DisplayName = type == NpcTypes.NpcType.Human ? "新人物" : "新怪物";
            _personal.Add(_draft); _readOnly = false; _selectedEntryId = null; _selectedCardId = null;
            _notice = "新建草稿：选择主功法后，可从卡牌库拖入卡牌。";
            Revalidate(); RefreshEditor();
        }
        catch (Exception ex) { ShowNotice("新建失败：" + ex.Message); }
    });

    private void CopyTemplate()
    {
        if (_draft == null) { ShowNotice("请先选择一个模板。" ); return; }
        ConfirmLeave(() =>
        {
            try
            {
                if (_draft == null) return;
                string id = TemplateDocumentConverter.UniqueId(_draft.NpcFields.ID + "_copy", AllTemplateIds());
                var copy = TemplateDocumentConverter.Copy(_draft, id);
                copy.Source = _readOnly ? "ProjectCopy" : "PersonalCopy";
                copy.NpcFields.DisplayName = DisplayTemplateName(_draft) + " 副本";
                _draft = copy; _personal.Add(copy); _readOnly = false;
                _selectedEntryId = null; _selectedCardId = null;
                _notice = "已建立独立个人草稿，保存后可再次打开。";
                Revalidate(); RefreshEditor();
            }
            catch (Exception ex) { ShowNotice("复制失败：" + ex.Message); }
        });
    }

    private bool SaveTemplate()
    {
        if (_draft == null) { ShowNotice("没有可保存的草稿。" ); return false; }
        if (_readOnly) { ShowNotice("项目模板为只读，请先复制到“我的模板”。" ); return false; }
        try
        {
            string path = _store.Save(_draft);
            _personal.RemoveAll(d => d.DraftId == _draft.DraftId);
            var saved = TemplateJson.Clone(_draft); saved.Dirty = false; _personal.Add(saved);
            _notice = "草稿已保存：" + path;
            Revalidate(); RefreshEditor(); return true;
        }
        catch (Exception ex) { ShowNotice("保存失败，编辑内容已保留：" + ex.Message); return false; }
    }

    private void RefreshCatalog()
    {
        try
        {
            if (!_catalogSession.Refresh(_dataRoot)) { ShowNotice("刷新失败，继续使用原目录：" + _catalogSession.LastError); return; }
            var changes = _cardLibrary.ReconcileFilters(Catalog.Cards);
            if (_readOnly && _draft != null)
            {
                var current = Catalog.GetNpc(_draft.NpcFields.ID);
                if (current != null)
                {
                    _draft = TemplateDocumentConverter.FromDefinition(current, "Project", Catalog.DataFingerprint,
                        Catalog.GetSource("Npc", current.ID)?.File ?? "");
                    _draft.Dirty = false; _selectedEntryId = null;
                }
            }
            _notice = "目录已刷新，草稿保留。" + (changes.Count > 0 ? " " + string.Join("；", changes) : "");
            Revalidate(); RefreshEditor();
        }
        catch (Exception ex) { ShowNotice("刷新后检查失败：" + ex.Message); }
    }

    private void ImportTemplates() => ConfirmLeave(() =>
    {
        var file = new FileDialog
        {
            Title = "导入 NPC 模板或个人草稿", Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile, Filters = new[] { "*.json ; JSON 模板" },
            MinSize = new Vector2I(720, 480)
        };
        AddChild(file);
        file.Canceled += file.QueueFree;
        file.FileSelected += path =>
        {
            // 先交还模态焦点，随后可在同一窗口立即打开候选列表。
            file.Hide();
            file.QueueFree();
            try { ShowImportCandidates(TemplateDraftStore.ImportText(File.ReadAllText(path))); }
            catch (Exception ex) { ShowNotice("无法读取导入文件：" + ex.Message); }
        };
        TemplateDialogLayout.Show(file, new(820, 560));
    });

    private void ShowImportCandidates(IReadOnlyList<TemplateImportItem> candidates)
    {
        var dialog = new ConfirmationDialog { Title = "选择要导入的模板", OkButtonText = "导入所选", CancelButtonText = "取消", DialogHideOnOk = false };
        AddChild(dialog);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(rows); dialog.AddChild(scroll);
        var selections = new List<(CheckBox Check, TemplateDraft Draft)>();
        var existing = AllTemplateIds().ToHashSet(StringComparer.Ordinal);
        foreach (var item in candidates)
        {
            if (item.Draft == null)
            {
                rows.AddChild(new Label { Text = $"第 {item.Index} 项无法导入：{item.Error}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
                continue;
            }
            var check = new CheckBox
            {
                Text = $"{DisplayTemplateName(item.Draft)}  [{item.Draft.NpcFields.ID}]" +
                    (existing.Contains(item.Draft.NpcFields.ID) ? "  · ID 冲突，导入后需改名" : ""), ButtonPressed = true
            };
            check.ClipText = true;
            check.TooltipText = check.Text;
            rows.AddChild(check); selections.Add((check, item.Draft)); existing.Add(item.Draft.NpcFields.ID);
        }
        if (candidates.Count == 0) rows.AddChild(new Label { Text = "文件中没有模板。" });
        dialog.GetOkButton().Disabled = selections.Count == 0;
        dialog.Canceled += dialog.QueueFree;
        dialog.Confirmed += () =>
        {
            var selected = selections.Where(s => s.Check.ButtonPressed).Select(s => s.Draft).ToArray();
            if (selected.Length == 0) { ShowNotice("请勾选至少一个可解析模板。" ); return; }
            var saved = new List<TemplateDraft>(); var errors = new List<string>();
            foreach (var draft in selected)
            {
                try { _store.Save(draft); saved.Add(draft); }
                catch (Exception ex) { errors.Add(DisplayTemplateName(draft) + "：" + ex.Message); }
            }
            if (saved.Count == 0) { ShowNotice("导入保存失败：" + string.Join("；", errors)); return; }
            dialog.QueueFree(); ReloadPersonal();
            _draft = saved[0]; _draft.Dirty = false; _readOnly = false; _selectedEntryId = null; _selectedCardId = null;
            _notice = $"已导入 {saved.Count} 个独立个人草稿。" + (errors.Count > 0 ? " 部分失败：" + string.Join("；", errors) : "");
            Revalidate(); RefreshEditor();
        };
        TemplateDialogLayout.Show(dialog, new(740, 440));
    }

    private void ExportTemplate()
    {
        if (_draft == null) { ShowNotice("请先选择个人草稿。" ); return; }
        if (_readOnly) { ShowNotice("项目模板请先复制到“我的模板”再导出。" ); return; }
        Revalidate();
        if (_validation?.IsValid != true) { ShowNotice("请先修复阻塞问题，再导出正式模板。" ); return; }
        var snapshot = TemplateJson.Clone(_draft);
        var file = new FileDialog
        {
            Title = "导出标准 NPC JSON", Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile, Filters = new[] { "*.json ; NPC JSON" },
            CurrentFile = SafeFileName(snapshot.NpcFields.ID) + ".json"
        };
        try { Directory.CreateDirectory(_store.ExportDirectory); file.CurrentDir = _store.ExportDirectory; }
        catch (Exception ex) { ShowNotice("无法准备导出目录：" + ex.Message); file.QueueFree(); return; }
        AddChild(file); file.Canceled += file.QueueFree;
        file.FileSelected += path =>
        {
            file.QueueFree();
            try
            {
                string destination = _store.Export(snapshot, Catalog, path, ConflictingTemplateIds(snapshot));
                ShowNotice("已导出标准 NPC JSON：" + destination);
            }
            catch (Exception ex) { ShowNotice("导出失败：" + ex.Message); }
        };
        TemplateDialogLayout.Show(file, new(820, 560));
    }

    private void BeginTrial()
    {
        try
        {
            if (_draft == null) throw new InvalidOperationException("请先选择模板。");
            Revalidate();
            if (_validation?.IsValid != true) throw new InvalidOperationException("当前模板仍有阻塞问题，请先修复。");
            var unreadable = _store.LoadAll().Errors;
            if (unreadable.Count > 0) throw new InvalidDataException("个人库存在无法读取的草稿，请先处理：" + string.Join("；", unreadable));
            BattleParticipant opponent;
            if (_opponentKey.StartsWith("project:", StringComparison.Ordinal))
            {
                string id = _opponentKey[8..];
                if (Catalog.GetNpc(id) == null) throw new InvalidDataException("所选项目对手已不存在，请重新选择。");
                opponent = new BattleParticipant(id, "TeamB");
            }
            else if (_opponentKey.StartsWith("draft:", StringComparison.Ordinal))
            {
                string id = _opponentKey[6..];
                var other = !_readOnly && _draft.DraftId == id ? _draft : _personal.FirstOrDefault(d => d.DraftId == id)
                    ?? throw new InvalidDataException("所选个人对手已不存在。");
                var report = TemplateDocumentConverter.Validate(other, Catalog, ConflictingTemplateIds(other));
                if (!report.IsValid) throw new InvalidDataException("对手草稿存在阻塞问题：" + string.Join("；", report.Issues.Where(i => i.Severity == TemplateIssueSeverity.Error).Select(i => i.Message)));
                opponent = BattleParticipant.FromSnapshot(TemplateDocumentConverter.ToDefinition(other), "TeamB", other.DraftId);
            }
            else throw new InvalidDataException("请选择试战对手。");
            var input = TemplateTrialService.Freeze(Catalog, TemplateDocumentConverter.ToDefinition(_draft), opponent, _trialSettings, _draft.DraftId);
            var launch = BattleLauncher.OpenBattle(input, BattleWindowMode.NewWindow);
            ShowNotice($"已启动原战斗观察界面（进程 {launch.ProcessId}），编辑草稿保留。输入：{launch.InputPath}");
        }
        catch (Exception ex) { ShowNotice("无法启动试战：" + ex.Message); }
    }

    private void OpenDefaultBattle()
    {
        try
        {
            Catalog.AssertDiskUnchanged();
            string path = ProjectSettings.GlobalizePath("res://assets/testing/battle_cases.json");
            var input = BattleCaseCatalog.Load(path) with { DataFingerprint = Catalog.DataFingerprint, CoreVersion = BattleCaseFactory.CoreVersion };
            var launch = BattleLauncher.OpenBattle(input, BattleWindowMode.NewWindow);
            ShowNotice($"已打开原默认战斗案例（进程 {launch.ProcessId}），当前编辑保留。");
        }
        catch (Exception ex) { ShowNotice("无法打开战斗观察：" + ex.Message); }
    }

    private void SelectMainDefinition(string cardId)
    {
        if (!CanEdit()) return;
        try
        {
            var matches = _draft!.Entries.Where(e => e.CardDefineId == cardId).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("该功法有多个实例，主功法定义必须唯一，请先处理重复条目。");
            bool wasDirty = _draft.Dirty; TemplateCardEntry? added = null;
            if (matches.Length == 0)
            {
                if (!_draft.TryAddCard(Catalog, cardId, null, out var error)) throw new InvalidOperationException(error);
                added = _draft.Entries.Last();
            }
            var entry = added ?? matches[0];
            if (!_draft.SetMainCard(Catalog, entry.EntryId, out var reason))
            {
                if (added != null) _draft.Entries.Remove(added);
                _draft.Dirty = wasDirty; throw new InvalidOperationException(reason);
            }
            _selectedEntryId = entry.EntryId; _selectedCardId = entry.CardDefineId;
            _notice = "已设置主功法；原主功法与受影响卡保留在未放置列表，可移除或重排。";
            RefreshEditor();
        }
        catch (Exception ex) { ShowNotice("设置主功法失败：" + ex.Message); }
    }

    private void AddCard(string cardId, int x, int y)
    {
        if (!CanEdit()) return;
        try
        {
            if (!_draft!.TryAddCard(Catalog, cardId, new BackpackPosition(x, y), out var error)) { ShowNotice(error); return; }
            _selectedEntryId = _draft.Entries.Last().EntryId; _selectedCardId = cardId; _notice = "已添加独立卡牌实例。";
            RefreshEditor();
        }
        catch (Exception ex) { ShowNotice("添加失败：" + ex.Message); }
    }

    private void MoveCard(string entryId, int x, int y)
    {
        if (!CanEdit()) return;
        try
        {
            if (!_draft!.TryMoveCard(Catalog, entryId, new BackpackPosition(x, y), out var error)) { ShowNotice(error); return; }
            _selectedEntryId = entryId; _notice = "已调整卡牌位置。"; RefreshAfterEdit(); RefreshSelection();
        }
        catch (Exception ex) { ShowNotice("移动失败：" + ex.Message); }
    }

    private void RemoveEntry(string entryId)
    {
        if (!CanEdit()) return;
        try
        {
            if (!_draft!.RemoveCard(entryId, out var error)) { ShowNotice(error); return; }
            if (_selectedEntryId == entryId) _selectedEntryId = null;
            _notice = "已移除该实例；依赖它的招式保留并提示重新绑定。"; RefreshEditor();
        }
        catch (Exception ex) { ShowNotice("移除失败：" + ex.Message); }
    }

    private bool CanEdit()
    {
        if (_draft == null) { ShowNotice("请先新建或选择模板。" ); return false; }
        if (_readOnly) { ShowNotice("项目模板为只读，请先复制到“我的模板”。" ); return false; }
        return true;
    }

    private void Revalidate()
    {
        if (_draft == null || _catalogSession.Current == null) { _validation = null; return; }
        _validation = TemplateDocumentConverter.Validate(_draft, Catalog, _readOnly ? null : ConflictingTemplateIds(_draft));
    }

    private IEnumerable<string> AllTemplateIds() => Catalog.Npcs.Select(n => n.ID).Concat(_personal.Select(d => d.NpcFields.ID));
    private IEnumerable<string> ConflictingTemplateIds(TemplateDraft draft) => Catalog.Npcs.Select(n => n.ID)
        .Concat(_personal.Where(d => d.DraftId != draft.DraftId).Select(d => d.NpcFields.ID));

    private void LocateIssue(TemplateIssue issue)
    {
        if (_draft != null && issue.PlacementIndex is int index && index > 0 && index <= _draft.Entries.Count)
        {
            var entry = _draft.Entries[index - 1];
            _selectedEntryId = entry.EntryId; _selectedCardId = entry.CardDefineId; RefreshAfterEdit();
        }
        ShowNotice(issue.Message + (issue.FieldPath.Length > 0 ? " · " + issue.FieldPath : ""));
    }

    private void ConfirmLeave(Action next)
    {
        if (_leaveDialogOpen) return;
        if (_draft == null || _readOnly || !_draft.Dirty) { next(); return; }
        _leaveDialogOpen = true;
        var dialog = new ConfirmationDialog
        {
            Title = "草稿尚未保存",
            OkButtonText = "保存并继续", CancelButtonText = "取消", DialogHideOnOk = false
        };
        AddChild(dialog);
        var message = TemplateDialogLayout.AddScrollableText(dialog, $"“{DisplayTemplateName(_draft)}”有未保存修改。");
        var discard = dialog.AddButton("放弃修改", false, "discard");
        void Close() { _leaveDialogOpen = false; dialog.QueueFree(); }
        dialog.Canceled += Close;
        dialog.Confirmed += () =>
        {
            if (SaveTemplate()) { Close(); next(); }
            else TemplateDialogLayout.SetMessage(dialog, message, _notice);
        };
        discard.Pressed += () =>
        {
            try
            {
                string id = _draft!.DraftId;
                bool saved = _draft.Revision > 0;
                if (saved) _draft = _store.Load(id);
                else
                {
                    var fallback = Catalog.Npcs.FirstOrDefault();
                    _draft = fallback == null ? null : TemplateDocumentConverter.FromDefinition(fallback, "Project", Catalog.DataFingerprint);
                    if (_draft != null) _draft.Dirty = false;
                    _readOnly = true;
                }
                ReloadPersonal(); _selectedEntryId = null; _selectedCardId = null;
                Close(); Revalidate(); RefreshEditor(); next();
            }
            catch (Exception ex) { ShowNotice("无法恢复已保存草稿：" + ex.Message); }
        };
        TemplateDialogLayout.Show(dialog, compactMessage: message);
    }

    private static string DisplayTemplateName(TemplateDraft draft) => string.IsNullOrWhiteSpace(draft.NpcFields.DisplayName) ? draft.NpcFields.ID : draft.NpcFields.DisplayName;
    private static string SafeFileName(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(id.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(name) ? "npc_template" : name;
    }
}
