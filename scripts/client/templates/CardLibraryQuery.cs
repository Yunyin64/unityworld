using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

public sealed class KeywordLabel
{
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; } = int.MaxValue;
    public string Description { get; set; } = "";
}
public sealed record KeywordOption(string Id, string DisplayName, int SortOrder);
public sealed class CardLibraryFilter
{
    public string Text { get; set; } = "";
    public HashSet<string> Keywords { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Empty = no restriction; "None" = no mana requirement; otherwise exact element ID.</summary>
    public string Element { get; set; } = "";
    public int? Size { get; set; }
    public bool OnlyAvailable { get; set; }
    public void Clear() { Text = ""; Keywords.Clear(); Element = ""; Size = null; OnlyAvailable = false; }
}

public static class CardLibraryQuery
{
    public static IReadOnlyDictionary<string, KeywordLabel> LoadLabels(string path) => File.Exists(path)
        ? JsonSerializer.Deserialize<Dictionary<string, KeywordLabel>>(File.ReadAllText(path), TemplateJson.Options) ?? new()
        : new Dictionary<string, KeywordLabel>();
    public static IReadOnlyList<KeywordOption> Keywords(IEnumerable<CardDefine> cards, IReadOnlyDictionary<string, KeywordLabel>? labels = null) => cards
        .SelectMany(c => c.Keywords).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal)
        .Select(k => { var label = labels?.GetValueOrDefault(k); return new KeywordOption(k, string.IsNullOrWhiteSpace(label?.DisplayName) ? k : label!.DisplayName, label?.SortOrder ?? int.MaxValue); })
        .OrderBy(k => k.SortOrder).ThenBy(k => k.Id, StringComparer.Ordinal).ToArray();

    public static IReadOnlyList<CardDefine> Search(IEnumerable<CardDefine> cards, CardLibraryFilter filter)
    {
        string text = filter.Text.Trim();
        return cards.Where(c =>
            (text.Length == 0 || c.ID.Contains(text, StringComparison.OrdinalIgnoreCase) || c.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) || c.Desc.Contains(text, StringComparison.OrdinalIgnoreCase)) &&
            filter.Keywords.All(k => c.Keywords.Contains(k, StringComparer.Ordinal)) &&
            (filter.Element.Length == 0 || (filter.Element == "None" ? c.ManaCost.Count == 0 : c.ManaCost.ContainsKey(filter.Element))) &&
            (!filter.Size.HasValue || c.Size == filter.Size) && (!filter.OnlyAvailable || UnavailableReason(c) == null))
            .OrderBy(c => c.DisplayName, StringComparer.Ordinal).ThenBy(c => c.ID, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<string> Reconcile(CardLibraryFilter filter, IEnumerable<CardDefine> cards)
    {
        var definitions = cards.ToArray(); var ids = Keywords(definitions).Select(k => k.Id).ToHashSet(StringComparer.Ordinal);
        var messages = filter.Keywords.Where(k => !ids.Contains(k)).Select(k => $"关键词 {k} 已不存在，已移除筛选。").ToList();
        filter.Keywords.IntersectWith(ids);
        if (filter.Size.HasValue && !definitions.Any(c => c.Size == filter.Size)) { filter.Size = null; messages.Add("原占格选项已不存在，已清除。"); }
        if (filter.Element.Length > 0 && filter.Element != "None" && !definitions.Any(c => c.ManaCost.ContainsKey(filter.Element)))
        { filter.Element = ""; messages.Add("原灵元需求选项已不存在，已清除。"); }
        return messages;
    }

    /// <summary>Per-card content availability. Missing artwork is deliberately never consulted.</summary>
    public static string? UnavailableReason(CardDefine card)
    {
        var errors = TemplateEditorValidator.ValidateCard(card).Where(i => i.Severity == TemplateIssueSeverity.Error).Select(i => i.Message).ToArray();
        return errors.Length == 0 ? null : string.Join("；", errors);
    }
}
