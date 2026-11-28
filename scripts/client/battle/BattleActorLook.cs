using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Battle;

/// <summary>显示部件地址、染色和解析到的来源，跟随冻结的试战输入。</summary>
public sealed record BattleActorPart(string Id, string? Dye = null, string? SourceId = null);

/// <summary>一个人物的组装结果，键为外观目录中的部位 ID。</summary>
public sealed record BattleActorLook(string Rig, Dictionary<string, BattleActorPart> Parts)
{
    /// <summary>试战和重放保留独立副本。</summary>
    public BattleActorLook Copy() => this with { Parts = new(Parts ?? new(), StringComparer.Ordinal) };

    /// <summary>把实际 NPC 已选定的外观复制成客户端或试战输入。</summary>
    public static BattleActorLook? FromData(NpcAppearanceData appearance) => appearance.Rig.Length == 0 ? null :
        new(appearance.Rig, appearance.Parts.ToDictionary(pair => pair.Key,
            pair => new BattleActorPart(pair.Value.Address, pair.Value.Dye, pair.Value.SourceId), StringComparer.Ordinal));

    /// <summary>转成定义地址，兼容旧草稿和案例中的整组款式及无井号色值。</summary>
    public Dictionary<string, string> ToConfiguration(string rig)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (slot, part) in Parts ?? new())
        {
            if (part == null || string.IsNullOrWhiteSpace(part.Id)) continue;
            string dye = string.IsNullOrWhiteSpace(part.Dye) ? "" : "[#" + part.Dye.Trim().TrimStart('#') + "]";
            if (part.Id.Contains('/') || part.SourceId != null)
            {
                result[slot] = part.Id + dye;
                continue;
            }
            foreach (var choice in NpcAppearanceCatalog.Default.LegacyParts(rig, slot, part.Id))
                result[choice.Slot] = choice.Address + (choice.Dyeable ? dye : "");
        }
        return result;
    }

    /// <summary>只替换指定部位，避免共享草稿的字典被修改。</summary>
    public BattleActorLook WithPart(string group, BattleActorPart part)
    {
        var copy = Copy();
        copy.Parts[group] = part;
        return copy;
    }
}
