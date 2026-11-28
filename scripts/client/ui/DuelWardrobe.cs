using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>把 Game 的逐槽外观目录投影成 Godot 可选部件与预览输入。</summary>
internal static class DuelWardrobe
{
    internal sealed record Group(string Id, string Label, NpcAppearanceCatalog.Part[] Options);

    internal static NpcAppearanceCatalog Catalog => GameBootstrap.Assets.Appearance(GameBootstrap.ResolveDataRoot());

    internal static Group[] Groups(string rig) => Catalog.Parts(rig).GroupBy(part => part.Slot, StringComparer.Ordinal)
        .Select(slot => new Group(slot.Key, slot.First().SlotLabel, slot.ToArray())).ToArray();

    internal static BattleActorLook? For(NpcDefine definition, string previewId)
    {
        int seed = NpcAppearanceCatalog.StableSeed(previewId);
        string rig = NpcAppearanceCatalog.RigFor(definition.NpcType, definition.Gender, definition.ID, seed);
        return BattleActorLook.FromData(Catalog.Resolve(rig, definition.Appearance, seed));
    }

    internal static BattleActorLook? Generate(string rig, int seed) =>
        BattleActorLook.FromData(Catalog.Resolve(rig, null, seed));
}
