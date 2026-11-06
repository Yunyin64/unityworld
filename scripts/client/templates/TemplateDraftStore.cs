using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityWorld.Game.Data;

namespace Xianxia.Client.Templates;

public sealed record TemplateImportItem(int Index, TemplateDraft? Draft, string? Error);
public sealed record TemplateStoreRead(IReadOnlyList<TemplateDraft> Drafts, IReadOnlyList<string> Errors);

/// <summary>Host passes a resolved user://TemplateEditor directory. No Godot dependency or project writes.</summary>
public sealed class TemplateDraftStore
{
    public string RootDirectory { get; }
    public string DraftDirectory => Path.Combine(RootDirectory, "drafts");
    public string ExportDirectory => Path.Combine(RootDirectory, "exports");
    public string ProtectedDataDirectory { get; }
    public TemplateDraftStore(string rootDirectory, string protectedDataDirectory)
    { RootDirectory = Path.GetFullPath(rootDirectory); ProtectedDataDirectory = Path.GetFullPath(protectedDataDirectory); GuardDestination(RootDirectory); }

    public string Save(TemplateDraft draft)
    {
        EnsureStructure(draft);
        string destination = DraftPath(draft.DraftId);
        var saved = TemplateJson.Clone(draft);
        saved.Revision = draft.Revision + 1; saved.UpdatedAt = DateTimeOffset.UtcNow;
        WriteAtomic(destination, JsonSerializer.Serialize(saved, TemplateJson.Options));
        draft.Revision = saved.Revision; draft.UpdatedAt = saved.UpdatedAt; draft.Dirty = false;
        return destination;
    }

    public TemplateDraft Load(string draftId)
    {
        var draft = ParseDraft(File.ReadAllText(DraftPath(draftId)));
        if (draft.DraftId != draftId) throw new InvalidDataException("草稿文件名与文档身份不一致。");
        draft.Dirty = false; return draft;
    }

    public TemplateStoreRead LoadAll()
    {
        var drafts = new List<TemplateDraft>(); var errors = new List<string>();
        if (!Directory.Exists(DraftDirectory)) return new(drafts, errors);
        foreach (string path in Directory.EnumerateFiles(DraftDirectory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            try { drafts.Add(Load(Path.GetFileNameWithoutExtension(path))); }
            catch (Exception ex) { errors.Add(Path.GetFileName(path) + "：" + ex.Message); }
        }
        return new(drafts, errors);
    }

    public static IReadOnlyList<TemplateImportItem> ImportText(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToArray() : new[] { root };
        var result = new List<TemplateImportItem>();
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                var value = items[i];
                if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("导入项必须是对象。");
                RejectDuplicateProperties(value, "$");
                TemplateDraft draft;
                if (value.EnumerateObject().Any(p => p.Name.Equals("SchemaVersion", StringComparison.OrdinalIgnoreCase)))
                {
                    var original = ParseDraft(value.GetRawText());
                    draft = TemplateDocumentConverter.Copy(original, original.NpcFields.ID);
                }
                else
                {
                    var definition = JsonSerializer.Deserialize<NpcDefine>(value.GetRawText(), TemplateJson.StrictOptions) ?? throw new InvalidDataException("空模板。");
                    draft = TemplateDocumentConverter.FromDefinition(definition);
                }
                draft.Source = "Imported"; draft.Dirty = true;
                result.Add(new(i + 1, draft, null));
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException)
            { result.Add(new(i + 1, null, ex.Message)); }
        }
        return result;
    }

    public string Export(TemplateDraft draft, TemplateCatalog catalog, string? destination = null, IEnumerable<string>? otherTemplateIds = null)
    {
        EnsureStructure(draft);
        var read = LoadAll();
        if (read.Errors.Count > 0) throw new InvalidDataException("个人库存在无法读取的草稿，无法可靠检查 ID 冲突：" + string.Join("；", read.Errors));
        var ids = catalog.Npcs.Select(n => n.ID).Concat(read.Drafts.Where(d => d.DraftId != draft.DraftId).Select(d => d.NpcFields.ID)).Concat(otherTemplateIds ?? Array.Empty<string>());
        var report = TemplateDocumentConverter.Validate(draft, catalog, ids);
        if (!report.IsValid) throw new InvalidDataException("配置有阻塞问题，不能正式导出：" + string.Join("；", report.Issues.Where(i => i.Severity == TemplateIssueSeverity.Error).Select(i => i.Message)));
        destination ??= Path.Combine(ExportDirectory, draft.DraftId + ".json");
        GuardDestination(destination);
        WriteAtomic(destination, JsonSerializer.Serialize(new[] { TemplateDocumentConverter.ToDefinition(draft) }, TemplateJson.Options));
        return Path.GetFullPath(destination);
    }

    public static TemplateDraft ParseDraft(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        RejectDuplicateProperties(document.RootElement, "$");
        var draft = JsonSerializer.Deserialize<TemplateDraft>(json, TemplateJson.StrictOptions) ?? throw new InvalidDataException("草稿为空。");
        EnsureStructure(draft); return draft;
    }

    private static void EnsureStructure(TemplateDraft draft)
    {
        if (draft.SchemaVersion != 1) throw new InvalidDataException($"不支持草稿版本 {draft.SchemaVersion}，当前版本为 1。");
        if (!Guid.TryParseExact(draft.DraftId, "N", out _)) throw new InvalidDataException("草稿 ID 必须为 GUID N 格式。");
        if (draft.NpcFields == null || draft.Entries == null || draft.UnresolvedBindings == null || draft.Presentation == null)
            throw new InvalidDataException("草稿结构不能为 null。");
        TemplateDocumentConverter.EnsureDefinitionStructure(draft.NpcFields);
        if (draft.NpcFields.InitCardPlacements.Count != 0 || draft.NpcFields.InitEquip.Count != 0 || draft.NpcFields.InitGongFa.Count != 0 || draft.NpcFields.MainGongFaId.Length != 0)
            throw new InvalidDataException("草稿装配必须保存在 Entries/MainEntryId，不允许重复保存在 NpcFields。");
        if (draft.Entries.Any(e => e == null || string.IsNullOrWhiteSpace(e.EntryId) || e.CardDefineId == null) || draft.Entries.Select(e => e.EntryId).Distinct(StringComparer.Ordinal).Count() != draft.Entries.Count)
            throw new InvalidDataException("草稿存在空条目或重复条目标识。");
        if (draft.UnresolvedBindings.Any(b => b == null || b.Original.ValueKind == JsonValueKind.Undefined))
            throw new InvalidDataException("未解析绑定缺少原始片段。");
    }

    private string DraftPath(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("草稿 ID 无效。");
        return Path.Combine(DraftDirectory, id + ".json");
    }
    private void GuardDestination(string destination)
    {
        string path = Path.GetFullPath(destination);
        string data = Path.TrimEndingDirectorySeparator(ProtectedDataDirectory);
        if (path.Equals(data, StringComparison.OrdinalIgnoreCase) || path.StartsWith(data + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("请导出到个人目录；编辑器不覆盖项目 Data。");
    }
    private void WriteAtomic(string destination, string text)
    {
        GuardDestination(destination);
        destination = Path.GetFullPath(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
            { writer.Write(text); writer.Flush(); stream.Flush(true); }
            if (File.Exists(destination)) File.Replace(temporary, destination, destination + ".bak");
            else File.Move(temporary, destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void RejectDuplicateProperties(JsonElement node, string path)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in node.EnumerateObject())
            {
                if (!keys.Add(property.Name)) throw new InvalidDataException($"重复 JSON 字段：{path}.{property.Name}");
                RejectDuplicateProperties(property.Value, path + "." + property.Name);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        { int index = 0; foreach (var item in node.EnumerateArray()) RejectDuplicateProperties(item, path + "[" + index++ + "]"); }
    }
}
