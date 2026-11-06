using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

public sealed record TemplateSource(string File, int Index);

/// <summary>A successfully read immutable catalog generation; getters return detached definitions.</summary>
public sealed class TemplateCatalog : ITemplateEditorDefinitions
{
    private readonly Dictionary<string, NpcDefine> _npcs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CardDefine> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CultivationDefine> _cultivations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipDefine> _equips = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StatDefine> _stats = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TemplateSource> _sources = new(StringComparer.Ordinal);
    public string DataRoot { get; }
    public string DataFingerprint { get; private set; } = "";
    private TemplateCatalog(string root) { DataRoot = Path.GetFullPath(root); }
    public IReadOnlyList<NpcDefine> Npcs => Copies(_npcs);
    public IReadOnlyList<CardDefine> Cards => Copies(_cards);
    public IReadOnlyList<CultivationDefine> Cultivations => Copies(_cultivations);
    public IReadOnlyList<EquipDefine> Equips => Copies(_equips);
    public IReadOnlyList<StatDefine> Stats => Copies(_stats);
    private static IReadOnlyList<T> Copies<T>(Dictionary<string, T> items) => items.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => TemplateJson.Clone(p.Value)).ToArray();
    private static T? Copy<T>(Dictionary<string, T> items, string id) where T : class => items.TryGetValue(id, out var item) ? TemplateJson.Clone(item) : null;
    public NpcDefine? GetNpc(string id) => Copy(_npcs, id);
    public CardDefine? GetCard(string id) => Copy(_cards, id);
    public CultivationDefine? GetCultivation(string id) => Copy(_cultivations, id);
    public EquipDefine? GetEquip(string id) => Copy(_equips, id);
    public StatDefine? GetStat(string id) => Copy(_stats, id);
    public IEnumerable<StatDefine> GetStats() => Stats;
    public TemplateSource? GetSource(string kind, string id) => _sources.GetValueOrDefault(kind + ":" + id);

    public static TemplateCatalog Load(string dataRoot)
    {
        var catalog = new TemplateCatalog(dataRoot);
        string before = Fingerprint(catalog.DataRoot);
        catalog.Read("Npc", catalog._npcs);
        catalog.Read("Card", catalog._cards);
        catalog.Read("Practice/GongFa", catalog._cultivations);
        catalog.Read("Equip", catalog._equips);
        catalog.Read("Stat", catalog._stats);
        string after = Fingerprint(catalog.DataRoot);
        if (before != after) throw new IOException("读取期间玩法数据发生变化，请重新刷新。");
        catalog.DataFingerprint = after;
        return catalog;
    }

    private void Read<T>(string directory, Dictionary<string, T> target) where T : DefineBase
    {
        string root = Path.Combine(DataRoot, "Data", directory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"缺少定义目录：{root}");
        foreach (string file in Directory.EnumerateFiles(root, "*.json", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.Ordinal))
        {
            string json = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(json)) continue; // Existing loader ignores empty files.
            try
            {
                var records = JsonSerializer.Deserialize<List<T>>(json, TemplateJson.Options)
                    ?? throw new InvalidDataException("定义根节点必须是数组。");
                for (int index = 0; index < records.Count; index++)
                {
                    var value = records[index];
                    if (value == null || string.IsNullOrWhiteSpace(value.ID)) throw new InvalidDataException($"条目 {index + 1} 缺少 ID。");
                    if (!target.TryAdd(value.ID, value)) throw new InvalidDataException($"重复 ID：{value.ID}");
                    _sources[directory + ":" + value.ID] = new(Path.GetRelativePath(DataRoot, file).Replace('\\', '/'), index + 1);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            { throw new InvalidDataException($"{Path.GetRelativePath(DataRoot, file)}：{ex.Message}", ex); }
        }
    }

    public void AssertDiskUnchanged()
    {
        if (Fingerprint(DataRoot) != DataFingerprint)
            throw new InvalidDataException("玩法数据已变化，请刷新目录并重新检查配置后试战。");
    }

    /// <summary>Same canonical full JSON/Lua identity as BattleCaseFactory, without creating a world.</summary>
    public static string Fingerprint(string dataRoot)
    {
        string root = Path.Combine(Path.GetFullPath(dataRoot), "Data");
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var lines = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => Path.GetExtension(p).Equals(".json", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(p).Equals(".lua", StringComparison.OrdinalIgnoreCase))
            .Select(p => (Path: Path.GetRelativePath(root, p).Replace('\\', '/'), Hash: Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant()))
            .OrderBy(p => p.Path, StringComparer.Ordinal).Select(p => p.Path + "\t" + p.Hash + "\n");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(lines)))).ToLowerInvariant();
    }
}

/// <summary>Only replace a successful generation; refresh failure preserves catalog and drafts.</summary>
public sealed class TemplateCatalogSession
{
    public TemplateCatalog? Current { get; private set; }
    public string? LastError { get; private set; }
    public bool Refresh(string dataRoot)
    {
        try { var next = TemplateCatalog.Load(dataRoot); Current = next; LastError = null; return true; }
        catch (Exception ex) { LastError = ex.Message; return false; }
    }
}
