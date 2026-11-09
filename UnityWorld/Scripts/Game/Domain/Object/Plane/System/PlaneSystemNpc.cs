using UnityWorld.Core;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain;

/// <summary>位面自己的NPC系统：出生落点和成员遍历；坐标写入交给NPC位置系统。</summary>
public sealed class PlaneSystemNpc
{
    private readonly Plane _plane;
    internal PlaneSystemNpc(Plane plane) { _plane = plane; }

    public bool AddNpc(int id)
    {
        WorldMgr.EnsureMutable();
        var position = NpcMgr.Instance?.PositionSystem.GetPosition(id);
        if (position == null || position.PlaneId != _plane.Id.Value || !_plane.TileSystem.CanWalk(position.TileId)) return false;
        return AddMember(id);
    }
    public bool RemoveNpc(int id)
    {
        WorldMgr.EnsureMutable();
        if (!RemoveMember(id)) return false;
        NpcMgr.Instance?.PositionSystem.LeavePlane(id, _plane.Id.Value);
        return true;
    }
    internal bool AddMember(int id) => _plane.NpcData.Members.Add(id);
    internal bool RemoveMember(int id) => _plane.NpcData.Members.Remove(id);

    internal void ClearMembers()
    {
        foreach (int id in _plane.NpcIds) NpcMgr.Instance?.PositionSystem.LeavePlane(id, _plane.Id.Value);
        _plane.NpcData.Members.Clear();
    }

    public Npc Spawn(BirthContext context)
    {
        WorldMgr.EnsureMutable();
        ArgumentNullException.ThrowIfNull(context);
        if (context.AssemblySource != NpcAssemblySource.Generated || context.MainNpc != null)
            throw new ArgumentException("随机出生需要新的Generated创建上下文。");
        var candidates = _plane.Tiles.Values.Where(tile => _plane.TileSystem.CanWalk(tile.Id))
            .OrderBy(tile => Plane.AxialToOffset(tile.Id).row).ThenBy(tile => Plane.AxialToOffset(tile.Id).col).ToArray();
        if (candidates.Length == 0) throw new InvalidOperationException("此位面没有合法的可步行陆地出生格。");
        // 先找落点再造NPC；失败不留下空壳，也不消耗成功出生序号。
        var target = candidates[CreateRandom(_plane.NpcData.NextBirthOrder, 0, 1).Range(0, candidates.Length)].Id;
        var mgr = NpcMgr.Instance;
        try
        {
            var npc = mgr.Birth(context);
            if (!mgr.PositionSystem.TryPlace(npc.Id, _plane.Id.Value, target))
                throw new InvalidOperationException("随机出生落地失败。");
            return npc;
        }
        catch
        {
            if (context.MainNpc != null) mgr.CleanupFailedWorldBirth(context.MainNpc);
            throw;
        }
    }

    internal int[] GetWeeklyNpcIds()
        => _plane.NpcIds.OrderBy(id => NpcMgr.Instance.PositionSystem.GetPosition(id)?.BirthOrder ?? long.MaxValue)
            .ThenBy(id => id).ToArray();

    internal void AdvanceWeek(long week, IEnumerable<int> ids, List<NpcWeekResult> results)
    {
        foreach (int id in ids)
            results.Add(NpcMgr.Instance.BehaviorSystem.OnWeek(_plane, id, week));
    }

    /// <summary>随机数只是位面系统的局部辅助；不用实体ID或墙钟决定移动。</summary>
    internal Rng CreateRandom(long birthOrder, long week, uint purpose)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (uint word in new[] { (uint)WorldTime.RandomSeed, (uint)_plane.Id.Value, (uint)birthOrder,
                (uint)(birthOrder >> 32), (uint)week, (uint)(week >> 32), purpose }) h = (h ^ word) * 16777619;
            return new Rng((int)h);
        }
    }
}
