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

/// <summary>Ordered reference to an existing NPC template and its core team name.</summary>
public sealed record BattleParticipant(string TemplateId, string Team);
/// <summary>Complete serializable replay input. It contains references and parameters, never copied NPC rules.</summary>
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
        Participants = Array.AsReadOnly(Participants.Select(p => p with { }).ToArray()),
        Operations = Array.AsReadOnly(Operations.ToArray())
    };
    /// <summary>Serialize all replay parameters and verified identity.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, BattleJson.Options);
    /// <summary>Read and freeze a complete serialized case for a new process.</summary>
    public static BattleCase FromJson(string json) =>
        (JsonSerializer.Deserialize<BattleCase>(json, BattleJson.Options) ?? throw new InvalidDataException("案例 JSON 为空。" )).Copy();
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
        if (string.IsNullOrWhiteSpace(input.Id) || input.MaxTicks < 1 || input.TicksPerRow < 1 || input.Participants.Count < 2)
            throw new InvalidDataException("案例需要 ID、正步数参数和至少两位有序参战者。");
        if (input.Operations.Count != 0)
            throw new NotSupportedException("本版尚未适配带中途操作序列的案例；不能忽略操作继续播放。");
        foreach (var p in input.Participants)
            if (string.IsNullOrWhiteSpace(p.TemplateId) || !Enum.TryParse<CombatTeam>(p.Team, out var team) || team == CombatTeam.None)
                throw new InvalidDataException($"案例参战者无效：{p.TemplateId}/{p.Team}。");
    }

    // Called only after WorldMgr.Initialize -> Start. Assembly order matches the original console.
    internal static CombatScene Assemble(BattleCase input)
    {
        var participants = input.Participants.Select(p =>
        {
            var definition = NpcDefineMgr.Instance.Get(p.TemplateId)
                ?? throw new InvalidDataException($"NPC 模板不存在：{p.TemplateId}。");
            return (NpcMgr.Instance.Assemble(definition), Enum.Parse<CombatTeam>(p.Team));
        }).ToArray();
        var scene = new CombatScene { TicksPerRow = input.TicksPerRow };
        scene.Init(input.CombatSeed, participants, input.MaxTicks);
        return scene;
    }
}
