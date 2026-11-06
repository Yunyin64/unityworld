using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

public sealed class TemplateDraftValidation
{
    public TemplateValidationReport Template { get; }
    public IReadOnlyList<TemplateIssue> Issues { get; }
    public bool IsValid => Issues.All(i => i.Severity != TemplateIssueSeverity.Error);
    public TemplateDraftValidation(TemplateValidationReport template, IEnumerable<TemplateIssue> extra)
    { Template = template; Issues = template.Issues.Concat(extra).ToArray(); }
}

public static class TemplateDocumentConverter
{
    public static TemplateDraft FromDefinition(NpcDefine definition, string source = "Imported", string fingerprint = "", string sourceFile = "")
    {
        EnsureDefinitionStructure(definition);
        var draft = new TemplateDraft
        {
            Source = source, SourceTemplateId = definition.ID, SourceFingerprint = fingerprint,
            SourceFile = sourceFile, NpcFields = TemplateJson.Clone(definition),
            Entries = definition.InitCardPlacements.Select(p => new TemplateCardEntry { CardDefineId = p.Key, Position = p.Value }).ToList()
        };
        draft.NpcFields.InitCardPlacements.Clear(); draft.NpcFields.InitEquip.Clear(); draft.NpcFields.InitGongFa.Clear(); draft.NpcFields.MainGongFaId = "";
        void Unresolved(string field, object raw, string reason) => draft.UnresolvedBindings.Add(new()
        { FieldPath = field, Original = JsonSerializer.SerializeToElement(raw, TemplateJson.Options), Reason = reason });
        TemplateCardEntry? Resolve(string id, int? index)
        {
            if (index.HasValue) return index > 0 && index <= draft.Entries.Count && draft.Entries[index.Value - 1].CardDefineId == id ? draft.Entries[index.Value - 1] : null;
            var matches = draft.Entries.Where(e => e.CardDefineId == id).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
        if (!string.IsNullOrEmpty(definition.MainGongFaId))
        {
            draft.MainEntryId = Resolve(definition.MainGongFaId, null)?.EntryId;
            if (draft.MainEntryId == null) Unresolved("MainGongFaId", definition.MainGongFaId, "主功法未唯一匹配位置条目。");
        }
        for (int i = 0; i < definition.InitGongFa.Count; i++)
        {
            var value = definition.InitGongFa[i]; var entry = Resolve(value.DefineId, value.PlacementIndex);
            if (entry == null || entry.GongFaProgress != null) Unresolved($"InitGongFa[{i}]", value, "功法绑定无匹配实例或同实例重复绑定。");
            else entry.GongFaProgress = value.CurrentPoint;
        }
        for (int i = 0; i < definition.InitEquip.Count; i++)
        {
            var value = definition.InitEquip[i]; var entry = Resolve(value.CardID, value.PlacementIndex);
            if (entry == null || entry.EquipDefineId != null) Unresolved($"InitEquip[{i}]", value, "装备绑定无匹配实例或同实例重复绑定。");
            else entry.EquipDefineId = value.EquipID;
        }
        for (int i = 0; i < definition.InitCardPlacements.Count; i++)
        {
            int? parent = definition.InitCardPlacements[i].ParentPlacementIndex;
            if (!parent.HasValue) continue;
            if (parent > 0 && parent <= draft.Entries.Count) draft.Entries[i].ParentEntryId = draft.Entries[parent.Value - 1].EntryId;
            else
            {
                draft.Entries[i].ParentEntryId = "unresolved-parent-" + parent;
                Unresolved($"InitCardPlacements[{i}].ParentPlacementIndex", parent, "父装备序号超出条目范围。");
            }
        }
        return draft;
    }

    public static TemplateDraft Copy(TemplateDraft source, string newTemplateId)
    {
        var copy = TemplateJson.Clone(source);
        copy.DraftId = Guid.NewGuid().ToString("N"); copy.Revision = 0; copy.UpdatedAt = default; copy.Dirty = true;
        copy.NpcFields.ID = newTemplateId;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in copy.Entries)
        {
            string old = entry.EntryId; entry.EntryId = Guid.NewGuid().ToString("N"); map.TryAdd(old, entry.EntryId);
        }
        if (copy.MainEntryId != null && map.TryGetValue(copy.MainEntryId, out var main)) copy.MainEntryId = main;
        foreach (var entry in copy.Entries)
            if (entry.ParentEntryId != null && map.TryGetValue(entry.ParentEntryId, out var parent)) entry.ParentEntryId = parent;
        return copy;
    }

    /// <summary>Conversion also supports invalid drafts for diagnostics; callers must validate before export/trial.</summary>
    public static NpcDefine ToDefinition(TemplateDraft draft)
    {
        var definition = TemplateJson.Clone(draft.NpcFields);
        definition.InitCardPlacements = new(); definition.InitGongFa = new(); definition.InitEquip = new();
        definition.MainGongFaId = draft.Entries.FirstOrDefault(e => e.EntryId == draft.MainEntryId)?.CardDefineId ?? "";
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < draft.Entries.Count; i++) indices.TryAdd(draft.Entries[i].EntryId, i + 1);
        for (int i = 0; i < draft.Entries.Count; i++)
        {
            var entry = draft.Entries[i];
            definition.InitCardPlacements.Add(new InitCardPlacement
            {
                Key = entry.CardDefineId, Value = entry.Position,
                ParentPlacementIndex = entry.ParentEntryId == null ? null : indices.GetValueOrDefault(entry.ParentEntryId, -1)
            });
            if (entry.GongFaProgress is int progress) definition.InitGongFa.Add(new()
            { DefineId = entry.CardDefineId, CurrentPoint = progress, PlacementIndex = i + 1 });
            if (entry.EquipDefineId != null) definition.InitEquip.Add(new()
            { CardID = entry.CardDefineId, EquipID = entry.EquipDefineId, PlacementIndex = i + 1 });
        }
        return definition;
    }

    public static TemplateDraftValidation Validate(TemplateDraft draft, TemplateCatalog catalog, IEnumerable<string>? otherTemplateIds = null)
    {
        var extra = new List<TemplateIssue>();
        void Error(string code, string message, string field, int? index = null) => extra.Add(new(code, TemplateIssueSeverity.Error, message, field, index));
        if (draft.SchemaVersion != 1) Error("DraftVersion", "不支持的草稿版本。", "SchemaVersion");
        if (!Guid.TryParse(draft.DraftId, out _)) Error("DraftIdentity", "草稿标识无效。", "DraftId");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < draft.Entries.Count; i++)
        {
            var entry = draft.Entries[i];
            if (string.IsNullOrWhiteSpace(entry.EntryId) || !seen.Add(entry.EntryId)) Error("EntryIdentity", "条目标识为空或重复。", $"Entries[{i}].EntryId", i + 1);
            if (entry.ParentEntryId != null && !draft.Entries.Any(e => e.EntryId == entry.ParentEntryId))
                Error("MissingParentEntry", "原父装备已移除，请重新绑定或明确解除关联。", $"Entries[{i}].ParentEntryId", i + 1);
        }
        foreach (var unresolved in draft.UnresolvedBindings) Error("UnresolvedBinding", unresolved.Reason + "（保留原片段，修复后请显式移除该问题）", unresolved.FieldPath);
        if (otherTemplateIds?.Contains(draft.NpcFields.ID, StringComparer.Ordinal) == true)
            Error("TemplateIdConflict", "模板 ID 与现有模板冲突，请改为唯一 ID。", "ID");
        return new(TemplateEditorValidator.Validate(ToDefinition(draft), catalog), extra);
    }

    public static string UniqueId(string preferred, IEnumerable<string> used)
    {
        var ids = used.ToHashSet(StringComparer.Ordinal);
        string candidate = preferred; int suffix = 2;
        while (ids.Contains(candidate)) candidate = preferred + "_" + suffix++;
        return candidate;
    }

    public static void EnsureDefinitionStructure(NpcDefine value)
    {
        if (value.ID == null || value.DisplayName == null || value.Surname == null || value.GivenName == null || value.DaoTitle == null || value.MainGongFaId == null ||
            value.InitStat == null || value.Tags == null || value.InitialTraits == null || value.InitCardDeck == null || value.InitCardPlacements == null || value.InitGongFa == null || value.InitEquip == null ||
            value.InitCardPlacements.Any(p => p == null || p.Key == null))
            throw new InvalidDataException("NPC 包含不允许为 null 的字段。");
    }
}
