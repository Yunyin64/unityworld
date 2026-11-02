namespace UnityWorld.Game.Data;

/// <summary>代码默认参数，无独立 DefineMgr 或 JSON。调用方可以通过 PlaneConfig 提供自己的参数。</summary>
public static class WorldGenerationPresets
{
    public static WorldGenerationOptions CreateDefault(IEnumerable<RegionDefine> regions) => new()
    {
        RegionIds = regions.Where(r => r.ForcedPositions.Count == 0).Select(r => r.ID).OrderBy(id => id, StringComparer.Ordinal).ToList(),
        RequiredRegionAttempts = 5000,
        Hydrology = new() { RiverCount = 4 },
        ConnectivityGroups = new() { new() { ID = "required-mountain-flight", MovementMode = "fly", CanCrossRivers = true,
            RegionPoints = new() { "region_taihang:center", "region_qinling:center", "region_qilian:center" } } },
    };
}
