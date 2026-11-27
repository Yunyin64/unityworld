using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>客户端外观目录，组合 Spine 皮肤时使用稳定部件 ID。</summary>
internal static class DuelWardrobe
{
    internal const string Directory = "res://assets/art/battle/spine/";
    internal sealed record Option(string Id, string Label);
    internal sealed record Group(string Id, string Label, bool Dyeable, Option[] Options);
    private sealed record Catalog(Group[] Groups, Dictionary<string, Dictionary<string, string>> Presets);

    private static readonly Lazy<Catalog> Data = new(() => JsonSerializer.Deserialize<Catalog>(
        FileAccess.GetFileAsString(Directory + "wardrobe.json")) ?? throw new InvalidOperationException("人物部件目录为空。"));

    internal static IReadOnlyList<Group> Groups => Data.Value.Groups;

    internal static BattleActorLook? For(NpcDefine definition, string sourceId, BattleActorLook? saved = null)
    {
        if (definition.NpcType != NpcTypes.NpcType.Human) return null;
        int seed = StableSeed(definition.ID);
        string rig = definition.Gender switch
        {
            NpcTypes.Gender.Male => "male",
            NpcTypes.Gender.Female => "female",
            _ => (seed & 1) == 0 ? "male" : "female"
        };
        var defaults = Generate(rig, seed);
        Data.Value.Presets.TryGetValue(sourceId, out var preset);
        foreach (var group in Groups)
        {
            var part = saved?.Parts?.GetValueOrDefault(group.Id);
            // 早期预览稿使用掌火 ID，保留草稿染色并换到本角色的手持木符。
            if (group.Id == "weapon" && part?.Id == "flame") part = part with { Id = "talisman" };
            if (part != null && group.Options.Any(o => o.Id == part.Id)) defaults.Parts[group.Id] = part;
            else if (preset?.GetValueOrDefault(group.Id) is { } id && group.Options.Any(o => o.Id == id))
                defaults.Parts[group.Id] = new(id);
        }
        return defaults;
    }

    internal static BattleActorLook Generate(string rig, int seed)
    {
        var rng = new Rng(seed);
        return new(rig, Groups.ToDictionary(group => group.Id,
            group => new BattleActorPart(group.Options[rng.Range(0, group.Options.Length)].Id), StringComparer.Ordinal));
    }

    internal static int StableSeed(string id)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char character in id) hash = (hash ^ character) * 16777619;
            return hash;
        }
    }
}
