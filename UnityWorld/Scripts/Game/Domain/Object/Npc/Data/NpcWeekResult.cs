namespace UnityWorld.Game.Domain;

/// <summary>行为的只读输出。真实状态仍保存于既有位置数据和主行为。</summary>
public sealed record NpcActionSnapshot(int NpcId, bool IsPlaced, int? PlaneId, TileId? CurrentTile,
    string State, TileId? From, TileId? Target, double Progress, double TotalDistance);

public sealed record NpcWeekResult(int NpcId, long BirthOrder, string Kind, TileId? Target, float Speed,
    NpcActionSnapshot Before, NpcActionSnapshot After, double CompletedProgress, string Reason);
