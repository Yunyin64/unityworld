using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;
using static Xianxia.Client.UI.Templates.TemplateEditorWidgets;

namespace Xianxia.Client.UI.Templates;

/// <summary>显示当前模板的背包、主功法和人物外观。编辑请求交给持有草稿的宿主处理。</summary>
public partial class TemplateLoadoutPanel : Panel
{
    private OptionButton _main = null!;
    private TemplateBackpackView _backpack = null!;
    private Label _summary = null!, _actorName = null!;
    private TextureRect _actor = null!;
    private VBoxContainer _unplaced = null!;

    public event Action<string>? MainRequested;
    public event Action<string>? EntrySelected;
    public event Action<string>? UnplacedSelected;
    public event Action<string>? RemoveRequested;
    public event Action<string, int, int>? MoveRequested;
    public event Action<string, int, int>? AddRequested;

    public override void _Ready()
    {
        _main = GetNode<OptionButton>("%MainChoice");
        _backpack = GetNode<TemplateBackpackView>("%Backpack");
        _summary = GetNode<Label>("%PackSummary");
        _actorName = GetNode<Label>("%ActorName");
        _actor = GetNode<TextureRect>("%ActorImage");
        _unplaced = GetNode<VBoxContainer>("%UnplacedEntries");

        // 网格、画像框等样例由场景组标记，美术可重命名或删除它们。实际背包按运行数据绘制。
        foreach (var sample in GetTree().GetNodesInGroup("template_layout_preview"))
        {
            if (!IsAncestorOf(sample)) continue;
            sample.GetParent().RemoveChild(sample);
            sample.QueueFree();
        }
        _main.Disabled = true;
        _main.Text = _summary.Text = _actorName.Text = "";
        _main.ItemSelected += index =>
        {
            if (index > 0) MainRequested?.Invoke(_main.GetItemMetadata((int)index).AsString());
        };
        _backpack.Selected += id => EntrySelected?.Invoke(id);
        _backpack.Removed += id => RemoveRequested?.Invoke(id);
        _backpack.MoveRequested += (id, x, y) => MoveRequested?.Invoke(id, x, y);
        _backpack.AddRequested += (id, x, y) => AddRequested?.Invoke(id, x, y);
    }

    public void SetMainChoices(TemplateCatalog catalog, TemplateDraft? draft, bool readOnly)
    {
        _main.Clear();
        _main.AddItem("请选择主功法");
        string? selected = draft?.Entries.FirstOrDefault(entry => entry.EntryId == draft.MainEntryId)?.CardDefineId;
        foreach (var cultivation in catalog.Cultivations)
        {
            var card = catalog.GetCard(cultivation.ID);
            if (card == null || CardLibraryQuery.UnavailableReason(card) != null) continue;
            _main.AddItem(DefinitionName(cultivation));
            int index = _main.ItemCount - 1;
            _main.SetItemMetadata(index, cultivation.ID);
            if (cultivation.ID == selected) _main.Select(index);
        }
        _main.Disabled = readOnly || draft == null;
    }

    public void SetPreview(TemplateCatalog catalog, TemplateDraft? draft, bool readOnly,
        string? selectedEntryId, IReadOnlyList<BackpackPosition> problemCells, Texture2D? actorTexture)
    {
        _backpack.Configure(catalog, draft, readOnly, selectedEntryId, problemCells);
        _actor.Texture = draft == null ? null : actorTexture;
        _actorName.Text = draft == null ? "选择一个模板" : DefinitionName(draft.NpcFields);
        Clear(_unplaced);
        if (draft == null) return;
        foreach (var entry in draft.Entries.Where(entry => entry.Position == null))
        {
            var card = catalog.GetCard(entry.CardDefineId);
            string id = entry.EntryId;
            string title = "未放置 · " + (card == null ? entry.CardDefineId : DefinitionName(card));
            var button = FlowButton(_unplaced, title, () => UnplacedSelected?.Invoke(id), height: 30);
            if (readOnly) continue;
            // 未放置条目与格内实例共用 entry 载荷，目标背包沿用草稿的格形、占用与 SP 校验。
            button.SetDragForwarding(Callable.From<Vector2, Variant>(_ =>
                new Godot.Collections.Dictionary { { "kind", "entry" }, { "id", id } }), default, default);
        }
    }

    public void SetSummary(TemplateValidationReport? report)
    {
        _summary.Text = report?.Layout is { } layout
            ? $"{layout.Width}×{layout.Height}   SP {report.OccupiedSp}/{report.SpMax?.ToString("0.##") ?? "?"}"
            : "请选择主功法";
    }
}
