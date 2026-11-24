#nullable enable annotations
using UnityWorld.Core;

namespace UnityWorld.Game.Domain;

/// <summary>工作占用主行为槽。世界周中的工作在原格结束，首版只记录完成。</summary>
public sealed class WorkBehavior : BehaviorBase
{
    /// <summary>工作行为的固定标识</summary>
    public const string BehaviorIdConstant = "Work";
    /// <summary>工作行为的类型标识</summary>
    public override string BehaviorId => BehaviorIdConstant;

    /// <summary>创建绑定当前位面与地块的周制工作行为</summary>
    internal WorkBehavior(int planeId, TileId from) : base(planeId, from) { }

    /// <summary>创建带时长和可选剧情的秒制工作行为</summary>
    public WorkBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
        : base(duration, storyEntries, rng) { }
}
