#nullable enable annotations

namespace UnityWorld.Game.Domain;

/// <summary>行为的只读输出。真实状态仍保存于既有位置数据和主行为。</summary>
/// <param name="NpcId">被采样的角色编号</param>
/// <param name="IsPlaced">采样时是否已放置</param>
/// <param name="PlaneId">采样时所在位面编号</param>
/// <param name="CurrentTile">采样时的实际所在格</param>
/// <param name="State">采样时的行动状态</param>
/// <param name="From">行动起始地块</param>
/// <param name="Target">当前行动目标地块</param>
/// <param name="Progress">采样时的移动累计进度</param>
/// <param name="TotalDistance">此段移动的总距离</param>
public sealed record NpcActionSnapshot(int NpcId, bool IsPlaced, int? PlaneId, TileId? CurrentTile,
    string State, TileId? From, TileId? Target, double Progress, double TotalDistance);

/// <summary>记录一个角色单周行动的前后快照和结算结果</summary>
/// <param name="NpcId">结算角色编号</param>
/// <param name="BirthOrder">角色在位面内的出生顺序</param>
/// <param name="Kind">本周结果类型</param>
/// <param name="Target">本周行动的目标格</param>
/// <param name="Speed">本周结算使用的移动速度</param>
/// <param name="Before">行动前的状态快照</param>
/// <param name="After">行动后的状态快照</param>
/// <param name="CompletedProgress">本周结算后该段移动累计进度</param>
/// <param name="Reason">产生当前结果的原因</param>
public sealed record NpcWeekResult(int NpcId, long BirthOrder, string Kind, TileId? Target, float Speed,
    NpcActionSnapshot Before, NpcActionSnapshot After, double CompletedProgress, string Reason)
{
    // 周末即使都为空闲，也能从本周行动及实际授卡事实区分抵达、探索和工作。
    /// <summary>本周结算的行动类型</summary>
    public string? Action { get; init; }
    /// <summary>触发的探索遭遇编号</summary>
    public string? EventId { get; init; }
    /// <summary>探索授予的行为卡编号</summary>
    public string? GrantedCardId { get; init; }
}
