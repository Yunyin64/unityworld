using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>Ordered project reference or private definition snapshot; old two-field inputs stay project references.</summary>
public sealed record BattleParticipant(string TemplateId, string Team)
{
    public const string ProjectSource = "ProjectTemplate";
    public const string SnapshotSource = "DraftSnapshot";
    public string Source { get; init; } = ProjectSource;
    public NpcDefine? Definition { get; init; }
    public string DefinitionHash { get; init; } = "";
    public string SnapshotId { get; init; } = "";

    public BattleParticipant Copy() => this with { Definition = Definition == null ? null : BattleTemplateSnapshot.Copy(Definition) };

    public static BattleParticipant FromSnapshot(NpcDefine definition, string team, string snapshotId = "")
    {
        var copy = BattleTemplateSnapshot.Copy(definition);
        return new(copy.ID, team)
        {
            Source = SnapshotSource, Definition = copy,
            DefinitionHash = BattleTemplateSnapshot.Hash(copy), SnapshotId = snapshotId
        };
    }
}

/// <summary>Definition-only copy and canonical content identity. Object keys sort ordinally; list order is significant.</summary>
public static class BattleTemplateSnapshot
{
    public static NpcDefine Copy(NpcDefine definition) =>
        JsonSerializer.Deserialize<NpcDefine>(JsonSerializer.Serialize(definition, BattleJson.Options), BattleJson.Options)
        ?? throw new InvalidDataException("模板快照为空。");

    public static string Hash(NpcDefine definition)
    {
        var element = JsonSerializer.SerializeToElement(definition, BattleJson.Options);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(writer, element);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            { writer.WritePropertyName(property.Name); WriteCanonical(writer, property.Value); }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var value in element.EnumerateArray()) WriteCanonical(writer, value);
            writer.WriteEndArray();
        }
        else element.WriteTo(writer);
    }
}

/// <summary>Complete replay input with project references or deeply copied private template definitions.</summary>
public sealed record BattleCase
{
    /// <summary>Stable playable case identity.</summary>
    public string Id { get; init; } = "";
    /// <summary>Client-facing case label.</summary>
    public string Name { get; init; } = "";
    /// <summary>Existing console method used as the compatibility reference.</summary>
    public string SourceMethod { get; init; } = "";
    /// <summary>Seed passed to WorldMgr.Initialize.</summary>
    public int WorldSeed { get; init; }
    /// <summary>Seed passed to CombatScene.Init.</summary>
    public int CombatSeed { get; init; }
    /// <summary>Core termination cap; not a display-frame cap.</summary>
    public int MaxTicks { get; init; } = 5000;
    /// <summary>Normal-speed scan progress consumed per row.</summary>
    public int TicksPerRow { get; init; } = 10;
    /// <summary>Template assembly order, preserved on replay.</summary>
    public IReadOnlyList<BattleParticipant> Participants { get; init; } = Array.Empty<BattleParticipant>();
    /// <summary>Expected or verified aggregate JSON/Lua SHA256; empty only before first initialization.</summary>
    public string DataFingerprint { get; init; } = "";
    /// <summary>Expected or verified core assembly module identity.</summary>
    public string CoreVersion { get; init; } = "";
    /// <summary>Reserved operation references; nonempty sequences are explicitly unsupported in this release.</summary>
    public IReadOnlyList<string> Operations { get; init; } = Array.Empty<string>();
    /// <summary>Freeze input collections into independent read-only copies.</summary>
    public BattleCase Copy() => this with
    {
        Participants = Array.AsReadOnly((Participants ?? throw new InvalidDataException("参与者列表为空。"))
            .Select(p => p?.Copy() ?? throw new InvalidDataException("参与者条目为空。")).ToArray()),
        Operations = Array.AsReadOnly((Operations ?? throw new InvalidDataException("操作列表为空。" )).ToArray())
    };
    /// <summary>Serialize all replay parameters and verified identity.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, BattleJson.Options);
    /// <summary>Read and freeze a complete serialized case for a new process.</summary>
    public static BattleCase FromJson(string json) =>
        (JsonSerializer.Deserialize<BattleCase>(json, BattleJson.Options) ?? throw new InvalidDataException("案例 JSON 为空。" )).Copy();
}

/// <summary>Observer input routing shared by the UI and small command-line checks.</summary>
public static class BattleCaseInput
{
    public static BattleCase Load(IEnumerable<string> arguments, string catalogPath)
    {
        var args = arguments.ToArray();
        string? file = Value(args, "--observer-case-input=");
        string? encoded = Value(args, "--observer-case=");
        if (file != null && encoded != null)
            throw new InvalidDataException("不能同时指定 --observer-case-input 与 --observer-case。");
        BattleCase result;
        if (file != null)
        {
            if (string.IsNullOrWhiteSpace(file) || !Path.IsPathFullyQualified(file))
                throw new InvalidDataException("--observer-case-input 必须是非空绝对文件路径。");
            result = BattleCase.FromJson(File.ReadAllText(file));
        }
        else if (encoded != null)
            result = BattleCase.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
        else result = BattleCaseCatalog.Load(catalogPath, Value(args, "--observer-case-id=") ?? "console-basic-template");
        BattleCaseFactory.Validate(result);
        return result;
    }

    private static string? Value(string[] arguments, string prefix)
    {
        var matches = arguments.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (matches.Length > 1) throw new InvalidDataException($"重复的观察器输入参数：{prefix}");
        return matches.Length == 0 ? null : matches[0][prefix.Length..];
    }
}

/// <summary>Reads only explicitly listed playable cases from the client manifest.</summary>
public static class BattleCaseCatalog
{
    /// <summary>Load the current case list without creating any core objects.</summary>
    public static IReadOnlyList<BattleCase> LoadAll(string path) => Array.AsReadOnly(
        (JsonSerializer.Deserialize<BattleCase[]>(File.ReadAllText(path), BattleJson.Options)
         ?? throw new InvalidDataException("案例清单为空。")).Select(c => c.Copy()).ToArray());
    /// <summary>Resolve one supported case; unknown IDs fail instead of creating approximate test data.</summary>
    public static BattleCase Load(string path, string id = "console-basic-template") =>
        LoadAll(path).SingleOrDefault(c => c.Id == id) ?? throw new InvalidDataException($"未支持案例：{id}。");
}

/// <summary>Relative path under Data and exact file-content hash.</summary>
public sealed record DataFileFingerprint(string Path, string Sha256);
/// <summary>Aggregate identity with a sorted immutable file manifest.</summary>
public sealed record BattleDataFingerprint(string Sha256, IReadOnlyList<DataFileFingerprint> Files);

/// <summary>Shared source-identity and exact-order assembly adapter for all observer hosts.</summary>
public static class BattleCaseFactory
{
    /// <summary>Hash all JSON/Lua files under dataRoot/Data, including their relative path names.</summary>
    public static BattleDataFingerprint Fingerprint(string dataRoot)
    {
        var data = Path.Combine(Path.GetFullPath(dataRoot), "Data");
        if (!Directory.Exists(data)) throw new DirectoryNotFoundException($"找不到核心数据目录：{data}");
        var files = Directory.EnumerateFiles(data, "*", SearchOption.AllDirectories)
            .Where(p => Path.GetExtension(p).Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(p).Equals(".lua", StringComparison.OrdinalIgnoreCase))
            .Select(p => new DataFileFingerprint(Path.GetRelativePath(data, p).Replace('\\', '/'),
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant()))
            .OrderBy(p => p.Path, StringComparer.Ordinal).ToArray();
        var canonical = string.Concat(files.Select(p => p.Path + "\t" + p.Sha256 + "\n"));
        return new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), Array.AsReadOnly(files));
    }

    /// <summary>Assembly identity usable even when Godot loads the DLL from memory.</summary>
    public static string CoreVersion => typeof(CombatScene).Assembly.ManifestModule.ModuleVersionId.ToString("D");

    /// <summary>Reject unsupported input before initializing the global core host.</summary>
    public static void Validate(BattleCase input)
    {
        if (string.IsNullOrWhiteSpace(input.Id) || input.MaxTicks < 1 || input.TicksPerRow < 1 || input.Participants == null || input.Participants.Count < 2)
            throw new InvalidDataException("案例需要 ID、正步数参数和至少两位有序参战者。");
        if (input.Operations == null || input.Operations.Count != 0)
            throw new NotSupportedException("本版尚未适配带中途操作序列的案例；不能忽略操作继续播放。");
        foreach (var p in input.Participants)
        {
            if (p == null) throw new InvalidDataException("案例包含空参与者。");
            if (string.IsNullOrWhiteSpace(p.TemplateId) || !Enum.TryParse<CombatTeam>(p.Team, out var team) || !Enum.IsDefined(team) || team == CombatTeam.None)
                throw new InvalidDataException($"案例参战者无效：{p.TemplateId}/{p.Team}。");
            if (p.Source == BattleParticipant.ProjectSource)
            {
                if (p.Definition != null || !string.IsNullOrEmpty(p.DefinitionHash) || !string.IsNullOrEmpty(p.SnapshotId))
                    throw new InvalidDataException($"项目引用 {p.TemplateId} 不能同时携带个人快照。");
            }
            else if (p.Source == BattleParticipant.SnapshotSource)
            {
                if (p.Definition == null || p.Definition.ID != p.TemplateId)
                    throw new InvalidDataException($"个人快照 {p.TemplateId} 缺少匹配的模板定义。");
                if (string.IsNullOrWhiteSpace(p.DefinitionHash) || p.DefinitionHash != BattleTemplateSnapshot.Hash(p.Definition))
                    throw new InvalidDataException($"个人快照 {p.TemplateId} 内容哈希不匹配。");
                if (string.IsNullOrWhiteSpace(input.DataFingerprint) || string.IsNullOrWhiteSpace(input.CoreVersion))
                    throw new InvalidDataException("个人快照试战必须记录数据指纹和核心版本。");
            }
            else throw new InvalidDataException($"不支持的参与者来源：{p.Source}。");
        }
    }

    // Called only after WorldMgr.Initialize -> Start. Assembly order matches the original console.
    internal static CombatScene Assemble(BattleCase input)
    {
        var participants = input.Participants.Select(p =>
        {
            var definition = p.Source == BattleParticipant.SnapshotSource
                ? BattleTemplateSnapshot.Copy(p.Definition ?? throw new InvalidDataException("个人快照定义缺失。"))
                : NpcDefineMgr.Instance.Get(p.TemplateId)
                    ?? throw new InvalidDataException($"NPC 模板不存在：{p.TemplateId}。");
            return (NpcMgr.Instance.Assemble(definition), Enum.Parse<CombatTeam>(p.Team));
        }).ToArray();
        var scene = new CombatScene { TicksPerRow = input.TicksPerRow };
        scene.Init(input.CombatSeed, participants, input.MaxTicks);
        return scene;
    }
}
