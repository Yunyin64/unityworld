using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

public sealed class TemplateCardEntry
{
    public string EntryId { get; set; } = Guid.NewGuid().ToString("N");
    public string CardDefineId { get; set; } = "";
    public BackpackPosition? Position { get; set; }
    public int? GongFaProgress { get; set; }
    public string? EquipDefineId { get; set; }
    public string? ParentEntryId { get; set; }
}
public sealed class TemplatePresentation
{
    public string PortraitKey { get; set; } = "";
    public string ActorVisualKey { get; set; } = "";
}
public sealed class UnresolvedTemplateBinding
{
    public string FieldPath { get; set; } = "";
    public string Reason { get; set; } = "";
    public JsonElement Original { get; set; }
}
public sealed class TemplateDraft
{
    public int SchemaVersion { get; set; } = 1;
    public string DraftId { get; set; } = Guid.NewGuid().ToString("N");
    public int Revision { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Source { get; set; } = "New";
    public string SourceTemplateId { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string SourceFingerprint { get; set; } = "";
    // Only identity/stats/legacy fields are stored here; converter rebuilds placement/binding arrays.
    public NpcDefine NpcFields { get; set; } = new();
    public string? MainEntryId { get; set; }
    public List<TemplateCardEntry> Entries { get; set; } = new();
    public List<UnresolvedTemplateBinding> UnresolvedBindings { get; set; } = new();
    public TemplatePresentation Presentation { get; set; } = new();
    [JsonIgnore] public bool Dirty { get; set; } = true;
    public void MarkDirty() => Dirty = true;

    public static TemplateDraft New(TemplateCatalog catalog, NpcTypes.NpcType type, string templateId)
    {
        var draft = new TemplateDraft { NpcFields = new NpcDefine { ID = templateId, NpcType = type } };
        foreach (string id in new[] { "QiXue", "TiPo", "QiGan", "LingJi", "ShenShi", "WuXing", "JiYuan", "MeiLi" })
            draft.NpcFields.InitStat[id] = (int)(catalog.GetStat(id)?.DefaultValue ?? 0);
        return draft;
    }

    public bool TryAddCard(TemplateCatalog catalog, string cardId, BackpackPosition? position, out string error)
    {
        var card = catalog.GetCard(cardId);
        if (card == null) { error = "卡牌定义不存在。"; return false; }
        error = CardLibraryQuery.UnavailableReason(card) ?? "";
        if (error.Length != 0) return false;
        var entry = new TemplateCardEntry { CardDefineId = cardId, Position = position };
        if (position != null && !CanPlace(catalog, entry, position.Value, out error)) return false;
        Entries.Add(entry); Dirty = true; return true;
    }

    public bool TryMoveCard(TemplateCatalog catalog, string entryId, BackpackPosition position, out string error)
    {
        var entry = Entries.FirstOrDefault(e => e.EntryId == entryId);
        if (entry == null) { error = "卡牌条目不存在。"; return false; }
        if (entryId == MainEntryId) { error = "主功法位置固定，请通过主功法选择更换。"; return false; }
        if (!CanPlace(catalog, entry, position, out error)) return false;
        entry.Position = position; Dirty = true; return true;
    }

    public bool RemoveCard(string entryId, out string error)
    {
        if (MainEntryId == entryId) { error = "主功法不能直接移除。"; return false; }
        error = "";
        if (Entries.RemoveAll(e => e.EntryId == entryId) == 0) { error = "条目不存在。"; return false; }
        // Keep references to removed parents: invalidity survives saving/reloading.
        Dirty = true; return true;
    }

    public bool SetMainCard(TemplateCatalog catalog, string entryId, out string error)
    {
        error = "";
        var entry = Entries.FirstOrDefault(e => e.EntryId == entryId);
        var cultivation = entry == null ? null : catalog.GetCultivation(entry.CardDefineId);
        var card = entry == null ? null : catalog.GetCard(entry.CardDefineId);
        if (entry == null || cultivation == null || card == null) { error = "主功法必须同时有卡牌与功法定义。"; return false; }
        error = CardLibraryQuery.UnavailableReason(card) ?? "";
        if (error.Length != 0) return false;
        BackpackLevelDefine layout;
        try { layout = cultivation.GetBackpackDefinition(1); layout.GetValidCells(); }
        catch (Exception ex) { error = ex.Message; return false; }
        if (layout.MainCardCells.Count == 0) { error = "功法缺少固定占格。"; return false; }
        var origin = new BackpackCell(layout.MainCardCells.Min(c => c.X), layout.MainCardCells.Min(c => c.Y));
        var shape = card.GetShape().Select(c => new BackpackCell(origin.X + c.X, origin.Y + c.Y)).ToHashSet();
        if (!shape.SetEquals(layout.MainCardCells)) { error = "主功法形状与固定占格不符。"; return false; }
        var previous = Entries.FirstOrDefault(e => e.EntryId == MainEntryId);
        if (previous != null && previous != entry) previous.Position = null;
        MainEntryId = entry.EntryId;
        entry.Position = BackpackCoordinates.ToPosition(origin);
        entry.GongFaProgress ??= 0;
        // Maintain other positions when legal. Invalid positions remain visible as unplaced entries.
        foreach (var other in Entries.Where(e => e.EntryId != entryId && e.Position != null).ToArray())
            if (!CanPlace(catalog, other, other.Position!.Value, out _)) other.Position = null;
        Dirty = true; return true;
    }

    public BackpackLevelDefine? GetLayout(TemplateCatalog catalog)
    {
        var main = Entries.FirstOrDefault(e => e.EntryId == MainEntryId);
        return main == null ? null : catalog.GetCultivation(main.CardDefineId)?.GetBackpackDefinition(1);
    }

    private bool CanPlace(TemplateCatalog catalog, TemplateCardEntry entry, BackpackPosition position, out string error)
    {
        error = "";
        try
        {
            var layout = GetLayout(catalog);
            if (layout == null) { error = "请先选择主功法。"; return false; }
            var card = catalog.GetCard(entry.CardDefineId) ?? throw new InvalidOperationException("卡牌定义不存在。");
            var cells = Cells(card, position).ToHashSet();
            if (cells.Any(c => !layout.GetValidCells().Contains(c))) { error = "卡牌超出背包有效范围。"; return false; }
            foreach (var other in Entries.Where(e => e.EntryId != entry.EntryId && e.Position != null))
            {
                var definition = catalog.GetCard(other.CardDefineId);
                if (definition != null && Cells(definition, other.Position!.Value).Any(cells.Contains))
                { error = "卡牌位置与已有条目重叠。"; return false; }
            }
            // Only increasing placed occupancy is capacity-gated; existing invalid drafts remain repairable.
            bool increasesOccupancy = !Entries.Any(e => e.EntryId == entry.EntryId && e.Position != null);
            if (increasesOccupancy)
            {
                float? sp = TemplateEditorValidator.Validate(TemplateDocumentConverter.ToDefinition(this), catalog).SpMax;
                int occupied = Entries.Where(e => e.EntryId != entry.EntryId && e.Position != null).Sum(e => catalog.GetCard(e.CardDefineId)?.Size ?? 0) + card.Size;
                if (sp == null) { error = "SP 上限无法确定，请先修复属性配置。"; return false; }
                if (occupied > sp.Value) { error = $"占格 {occupied} 超出 SP 上限 {sp.Value}。"; return false; }
            }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
    private static IEnumerable<BackpackCell> Cells(CardDefine card, BackpackPosition position)
    {
        var start = BackpackCoordinates.ToCell(position);
        return card.GetShape().Select(c => new BackpackCell(start.X + c.X, start.Y + c.Y));
    }
}
