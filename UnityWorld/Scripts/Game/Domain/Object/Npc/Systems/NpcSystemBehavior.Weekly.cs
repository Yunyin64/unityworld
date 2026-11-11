// 重要 TODO：未来建立独立的 NPC 周行动系统时，将周决策和结算流程迁入该系统。主次行为槽继续由 NpcSystemBehavior 管理。
#nullable enable annotations

using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain;

public partial class NpcSystemBehavior
{
    private const string Move = MoveBehavior.BehaviorIdConstant;
    private const string Explore = ExploreBehavior.BehaviorIdConstant;
    private const string Work = WorkBehavior.BehaviorIdConstant;

    // 每种行动只有一个名额。可走邻格的数量只影响第二次抽选。
    private static readonly IReadOnlyList<string> AllActions = Array.AsReadOnly(new[] { Move, Explore, Work });
    private static readonly IReadOnlyList<string> LocalActions = Array.AsReadOnly(new[] { Explore, Work });

    public NpcActionSnapshot GetAction(int id)
    {
        var position = NpcMgr.Instance.PositionSystem.GetPosition(id);
        var primary = GetPrimary(id);
        var weekly = primary?.IsWeeklyManaged == true ? primary : null;
        var move = weekly as MoveBehavior;
        string state = primary == null ? "idle" : weekly?.BehaviorId.ToLowerInvariant() ?? "busy";
        return new(id, position != null, position?.PlaneId, position?.TileId, state,
            weekly?.FromTile, move?.TargetTile ?? weekly?.FromTile,
            move?.Progress ?? 0, move != null ? MoveBehavior.AdjacentDistance : 0);
    }

    public bool TryStartMove(int id, TileId target) => TryStartExplicit(id, Move, target);
    public bool TryStartExplore(int id) => TryStartExplicit(id, Explore, null);
    public bool TryStartWork(int id) => TryStartExplicit(id, Work, null);

    private bool TryStartExplicit(int id, string action, TileId? target)
    {
        // 公开命令只占主槽，不推周；自动决策已经处在周结算内，直接共用下方的校验与提交。
        WorldMgr.EnsureMutable();
        return TryStartWeeklyAction(id, action, target) != null;
    }

    private static bool IsLiveMember(Npc? npc, NpcPositionData? position, Plane? plane)
        => npc != null && position != null && plane != null && position.PlaneId == plane.Id.Value
            && plane.ContainsNpc(npc.Id) && NpcMgr.Instance.BioSystem.Contains(npc.Id)
            && NpcMgr.Instance.BioSystem.GetData(npc.Id).IsAlive;

    private BehaviorBase? TryStartWeeklyAction(int id, string action, TileId? target)
    {
        var mgr = NpcMgr.Instance;
        if (mgr == null || !IsIdle(id)) return null;
        var npc = mgr.GetById(id);
        var position = mgr.PositionSystem.GetPosition(id);
        var plane = position == null ? null : PlaneMgr.Instance?.GetPlaneById(position.PlaneId);
        if (!IsLiveMember(npc, position, plane) || plane.State != PlaneTypes.PlaneState.Active
            || !plane.TileSystem.CanWalk(position.TileId)) return null;

        BehaviorBase next;
        switch (action)
        {
            case Move:
                // 对外的位置查询含本格；真正启动一步移动只接受有正速度的合法邻格。
                if (!target.HasValue || target.Value == position.TileId
                    || !NpcMgr.Instance.PositionSystem.GetMoveCandidates(npc.Id).Contains(target.Value)) return null;
                next = new MoveBehavior(position.PlaneId, position.TileId, target.Value);
                NpcMgr.Instance.PositionSystem.SetMoving(npc.Id, true);
                break;
            case Explore:
                next = new ExploreBehavior(position.PlaneId, position.TileId);
                break;
            case Work:
                next = new WorkBehavior(position.PlaneId, position.TileId);
                break;
            default:
                return null;
        }
        // 周主行为的开始和结束都不调用旧秒制行为的故事回调。
        next.Ownerint = npc.Id;
        _dataTable[npc.Id].PrimaryBehavior = next;
        return next;
    }

    internal void StopWeeklyAction(int id)
    {
        // 离位、注销或源格失效时释放同一个绑定。只有走路才持有位置系统的 Walking 标记。
        if (GetPrimary(id) is not { IsWeeklyManaged: true } weekly) return;
        _dataTable[id].PrimaryBehavior = null;
        if (weekly is MoveBehavior) NpcMgr.Instance.PositionSystem.SetMoving(id, false);
    }

    private static void GetPreferredDestinations(Npc npc, Plane plane, long week,
        List<(int PlaneId, TileId Tile)> destinations)
    {
        // TODO: 空闲决策时根据角色和地图提供零到多个意向点，可跨多步。当前留空供后两步读取。
    }

    private static void SelectWeeklyBehavior(Npc npc, IReadOnlyList<(int PlaneId, TileId Tile)> destinations,
        IReadOnlyList<string> availableActions, ref string? selected)
    {
        // TODO: 按角色意向从本周可用选项中选一个主行为。当前不填写，由调用方执行默认抽选。
    }

    private static void SelectMoveTargetByStrategy(Npc npc, IReadOnlyList<(int PlaneId, TileId Tile)> destinations,
        IReadOnlyList<TileId> neighbors, ref TileId? target)
    {
        // TODO: 仅在新选 Move 时结合角色意向挑一个可走邻格。当前不填写，由外层抽选和校验。
    }

    private static (string Action, TileId? Target) ChooseWeeklyAction(Npc npc, Plane plane,
        NpcPositionData position, long week, IReadOnlyList<TileId> neighbors)
    {
        var destinations = new List<(int PlaneId, TileId Tile)>();
        GetPreferredDestinations(npc, plane, week, destinations);

        var available = neighbors.Count > 0 ? AllActions : LocalActions;
        string? action = null;
        SelectWeeklyBehavior(npc, destinations, available, ref action);
        // 选种类、选邻格和探索遇事分别使用独立随机流，后续某一步的抽数不会改变其余结果。
        action ??= available[plane.NpcSystem.CreateRandom(position.BirthOrder, week, 3).Range(0, available.Count)];
        TileId? target = null;
        if (action == Move && neighbors.Count > 0)
        {
            SelectMoveTargetByStrategy(npc, destinations, neighbors, ref target);
            target ??= neighbors[plane.NpcSystem.CreateRandom(position.BirthOrder, week, 2).Range(0, neighbors.Count)];
        }
        // 非法的非空答案原样交给启动校验，以便产生 decision_error，不能回退重抽。
        return (action, target);
    }

    internal NpcWeekResult OnWeek(Plane plane, int id, long week)
    {
        var mgr = NpcMgr.Instance;
        var before = GetAction(id);
        var npc = mgr.GetById(id);
        var position = mgr.PositionSystem.GetPosition(id);
        var primary = GetPrimary(id);
        var weekly = primary?.IsWeeklyManaged == true ? primary : null;
        float speed = 0;

        NpcWeekResult Result(string kind, string reason = "", TileId? rejectedTarget = null,
            string? eventId = null, string? cardId = null)
        {
            var move = weekly as MoveBehavior;
            return new(id, position?.BirthOrder ?? -1, kind, rejectedTarget ?? move?.TargetTile ?? weekly?.FromTile,
                move != null ? speed : 0, before, GetAction(id), move?.Progress ?? 0, reason)
            {
                Action = weekly?.BehaviorId.ToLowerInvariant(),
                EventId = eventId,
                GrantedCardId = cardId
            };
        }

        if (!IsLiveMember(npc, position, plane))
        {
            StopWeeklyAction(id);
            return Result("skipped", "NPC不存在、死亡或不属于此位面。");
        }
        if (plane.State != PlaneTypes.PlaneState.Active) return Result("skipped", "位面未激活。");
        if (primary != null && weekly == null) return Result("busy", "已有秒制主行为。");
        if (!plane.TileSystem.CanWalk(position.TileId)
            || weekly != null && (weekly.PlaneId != position.PlaneId || weekly.FromTile != position.TileId)
            || weekly is MoveBehavior route && !plane.TileSystem.CanWalk(route.TargetTile))
        {
            StopWeeklyAction(id);
            return Result("blocked", "周行动的来源或目标已失效。");
        }

        if (weekly == null || weekly is MoveBehavior)
        {
            // 空闲者用速度确定可选种类，途中 Move 每周重新读速度。原地行动与速度无关。
            try { speed = mgr.PositionSystem.GetActionSpeed(npc); }
            catch (Exception ex) { return Result("invalid_state", ex.Message); }
        }
        if (weekly == null)
        {
            var neighbors = mgr.PositionSystem.GetMoveCandidates(id).Where(tile => tile != position.TileId).ToArray();
            var choice = ChooseWeeklyAction(npc, plane, position, week, neighbors);
            weekly = TryStartWeeklyAction(id, choice.Action, choice.Target);
            if (weekly == null)
                return Result("decision_error", $"不合法的周行动或目标：{choice.Action} / {choice.Target}。", choice.Target);
        }

        // 每一条分支只办一次当前行动。即使到达或本格行动结束，也到下周才会重新选择。
        switch (weekly)
        {
            case MoveBehavior move:
                string outcome = move.AdvanceWeek(npc, speed);
                if (outcome != "moving") StopWeeklyAction(id);
                return Result(outcome, outcome == "blocked" ? "目标无法到达。" : "");
            case ExploreBehavior explore:
                var random = plane.NpcSystem.CreateRandom(position.BirthOrder, week, 4);
                var encounter = explore.AdvanceWeek(plane, random);
                StopWeeklyAction(id);
                return Result("explored", eventId: encounter.EventId, cardId: encounter.GrantedCardId);
            case WorkBehavior:
                StopWeeklyAction(id);
                return Result("worked");
            default:
                return Result("invalid_state", $"未识别的周行为：{weekly.BehaviorId}。");
        }
    }
}
