using UnityWorld.Game.Domain;

namespace UnityWorld.Game.World;

/// <summary>世界完成一周后的只读结果，不承担管理或执行职责。</summary>
public sealed record WorldWeekResult(WorldDate Before, WorldDate After, bool Complete,
    IReadOnlyList<NpcWeekResult> Npcs, string Error);
