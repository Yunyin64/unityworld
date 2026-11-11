#nullable enable annotations
using UnityWorld.Core;

namespace UnityWorld.Game.Domain;

/// <summary>工作占用主行为槽。世界周中的工作在原格结束，首版只记录完成。</summary>
public sealed class WorkBehavior : BehaviorBase
{
    public const string BehaviorIdConstant = "Work";
    public override string BehaviorId => BehaviorIdConstant;

    internal WorkBehavior(int planeId, TileId from) : base(planeId, from) { }

    public WorkBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
        : base(duration, storyEntries, rng) { }
}
