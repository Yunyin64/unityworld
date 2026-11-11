using System;
using System.Collections.Frozen;
using System.Linq;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;
using Plane = UnityWorld.Game.Domain.Plane;

namespace Xianxia.Client.Exploration;

/// <summary>拥有真实世界和受控角色，每次确认行走只请求核心结算一周。</summary>
public sealed class ExplorationSession : IDisposable
{
    private readonly int _seed;
    private int _npcId;
    private bool _ownsWorld;
    private bool _inCommand;
    private bool _weekFaulted;

    private ExplorationSession(int seed) => _seed = seed;

    public ExplorationSnapshot Snapshot { get; private set; } = null!;

    public static ExplorationSession Open(int seed = 12345)
    {
        var session = new ExplorationSession(seed);
        try
        {
            // Initialize 负责清理由它自身初始化失败的部分；只有成功返回后才由本会话负责 End。
            WorldMgr.Initialize(seed, GameBootstrap.ResolveDataRoot());
            session._ownsWorld = true;
            WorldMgr.Start();
            var actor = PlaneMgr.Instance.MainPlane.NpcSystem.Spawn(new BirthContext());
            session._npcId = actor.Id;
            session.Snapshot = session.ReadSnapshot("");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    /// <summary>绑定显示时的快照与目标。确认后先启动邻格移动，再提交第一周。</summary>
    public ExplorationSnapshot StartMove(ExplorationSnapshot expected, TileId target) => Execute(expected, target);

    /// <summary>继续该快照上已经锁定的行程，和玩家当前正在查看哪一格无关。</summary>
    public ExplorationSnapshot ContinueMove(ExplorationSnapshot expected) => Execute(expected, null);

    private ExplorationSnapshot Execute(ExplorationSnapshot expected, TileId? destination)
    {
        ObjectDisposedException.ThrowIf(!_ownsWorld, this);
        // 结算后拒绝基于旧快照的命令，确保一次地图点击只处理一周。
        if (_inCommand || _weekFaulted || !ReferenceEquals(expected, Snapshot)) return Snapshot;
        _inCommand = true;
        try
        {
            var mgr = NpcMgr.Instance;
            var action = mgr.BehaviorSystem.GetAction(_npcId);
            if (!expected.CommandsEnabled || expected.Date != WorldTime.CurrentDate || action != expected.Action ||
                !action.IsPlaced || action.PlaneId != expected.PlaneId || action.CurrentTile != expected.Position ||
                mgr.GetById(_npcId) == null || PlaneMgr.Instance.GetPlaneById(expected.PlaneId)?.ContainsNpc(_npcId) != true)
                return Refresh("角色或行程已经变化，请重新确认。");

            if (destination is TileId target)
            {
                // 视野与候选各自来自核心。再次核对，以防角色属性、地图或占用状态在查看后发生变化。
                if (action.State != "idle" || !expected.Candidates.Contains(target) ||
                    !mgr.PositionSystem.GetVisibleTiles(_npcId).Any(tile => tile.Id == target) ||
                    !mgr.BehaviorSystem.TryStartMove(_npcId, target))
                    return Refresh("当前无法前往该地。");
            }
            else if (action.State != "move" || action.Target == null || !mgr.BioSystem.GetData(_npcId).IsAlive)
            {
                return Refresh("当前没有可继续的行程。");
            }

            // 一次调用处理整个世界；返回后才读取受控角色的终态。普通绘制和点击查看只消费 Snapshot。
            var week = WorldMgr.AdvanceWeek();
            _weekFaulted = !week.Complete;
            var outcome = week.Npcs.FirstOrDefault(item => item.NpcId == _npcId);
            string feedback = WeekMessage(outcome);
            if (_weekFaulted)
            {
                GD.PushError(week.Error);
                // 完整异常保留在日志中；地点栏只显示错误原因，给实际日期和行动结果留出空间。
                var reason = week.Error.Split('\n', 2)[0].Trim();
                feedback = Join(feedback, $"本周已提交，世界结算未完成。{reason}");
            }
            GD.Print($"[Exploration] settled week={week.After.ElapsedWeeks} npc={_npcId} outcome={outcome?.Kind} tile={outcome?.After.CurrentTile} progress={outcome?.CompletedProgress} complete={week.Complete}");
            return Refresh(feedback, outcome);
        }
        catch (Exception ex)
        {
            // 启动移动与过周是两个提交点。后者预检失败时仍可能有一个 0/100 的真实行动。
            return Refresh($"行走未能完成：{ex.Message}");
        }
        finally { _inCommand = false; }
    }

    private static string WeekMessage(NpcWeekResult? outcome) => outcome?.Kind switch
    {
        "moving" => "",
        "arrived" => "已抵达目的地。",
        null => "未取得该角色的本周结果。",
        _ => string.IsNullOrWhiteSpace(outcome.Reason) ? $"本周行动：{outcome.Kind}" : outcome.Reason
    };

    private ExplorationSnapshot ReadSnapshot(string message)
    {
        var mgr = NpcMgr.Instance;
        var npc = mgr.GetById(_npcId) ?? throw new InvalidOperationException("角色已离开世界。");
        var position = mgr.PositionSystem.GetPosition(_npcId) ?? throw new InvalidOperationException("角色尚未落地。");
        var plane = PlaneMgr.Instance.GetPlaneById(position.PlaneId) ?? throw new InvalidOperationException("角色所在位面已不存在。");
        var bio = mgr.BioSystem.GetData(_npcId);
        var catalog = GameDataMgr.Instance.MapCatalog;
        var tiles = mgr.PositionSystem.GetVisibleTiles(_npcId).ToFrozenDictionary(tile => tile.Id, tile => DescribeTile(plane, catalog, tile));
        var candidates = mgr.PositionSystem.GetMoveCandidates(_npcId)
            .Where(id => id != position.TileId && tiles.ContainsKey(id)).ToFrozenSet();
        var name = npc.GetName();
        var identity = bio.NameData.DaoTitle;
        if (string.IsNullOrWhiteSpace(identity)) identity = bio.NpcType switch
        {
            NpcTypes.NpcType.Human => "人族", NpcTypes.NpcType.Monster => "妖族", NpcTypes.NpcType.Animal => "兽类", _ => ""
        };
        return new(_seed, _npcId, string.IsNullOrWhiteSpace(name) ? $"修士 #{_npcId}" : name, identity,
            plane.Id.Value, position.TileId, WorldTime.CurrentDate, mgr.BehaviorSystem.GetAction(_npcId),
            mgr.PositionSystem.GetActionSpeed(npc), mgr.PositionSystem.GetDisplayRadius(npc), tiles, candidates,
            !_weekFaulted && bio.IsAlive && plane.State == PlaneTypes.PlaneState.Active, true, message);
    }

    private static ExplorationTile DescribeTile(Plane plane, MapCatalog catalog, Tile tile)
    {
        catalog.Tiles.TryGetValue(tile.TypeId, out var terrain);
        string regionName = "", landmarkName = "";
        if (tile.RegionId is int regionId && RegionMgr.Instance.GetById(regionId) is { } region)
            regionName = catalog.Regions.TryGetValue(region.DefineId, out var regionDefinition) ? regionDefinition.DisplayName : region.DefineId;
        if (tile.LandMarkId is int landmarkId && LandMarkMgr.Instance.GetById(landmarkId) is { } landmark)
            landmarkName = catalog.LandMarks.TryGetValue(landmark.DefineId, out var landmarkDefinition) ? landmarkDefinition.DisplayName : landmark.DefineId;
        return new(tile, string.IsNullOrWhiteSpace(terrain?.DisplayName) ? tile.TypeId : terrain.DisplayName,
            terrain?.Symbol ?? "", terrain?.Color ?? "#888888", regionName, landmarkName, plane.TileSystem.CanWalk(tile.Id));
    }

    private ExplorationSnapshot Refresh(string message, NpcWeekResult? outcome = null)
    {
        try
        {
            // 自动续周只认本角色当周的真实移动结果，错误文案或预检失败都不能触发重试。
            return Snapshot = ReadSnapshot(message) with
            {
                MadeTravelProgress = outcome is { Kind: "moving" } && outcome.After.Progress > outcome.Before.Progress
            };
        }
        catch (Exception ex)
        {
            // 大图读取失败仍保留最后一份可用地图。位置和日期单独复核，避免把旧角色画在错误的位面／格子上。
            var mgr = NpcMgr.Instance;
            NpcActionSnapshot? current = null;
            try { current = mgr?.BehaviorSystem.GetAction(_npcId); }
            catch (Exception stateError) { message = Join(message, stateError.Message); }
            var tile = current?.CurrentTile;
            bool located = current?.IsPlaced == true && current.PlaneId == Snapshot.PlaneId &&
                tile.HasValue && Snapshot.Tiles.ContainsKey(tile.Value) && mgr?.GetById(_npcId) != null;
            return Snapshot = Snapshot with
            {
                CanDrawActor = located,
                Date = WorldTime.CurrentDate,
                Action = current ?? outcome?.After ?? Snapshot.Action,
                Position = tile ?? Snapshot.Position,
                Speed = float.NaN,
                Candidates = FrozenSet<TileId>.Empty,
                MadeTravelProgress = false,
                CommandsEnabled = !_weekFaulted && located && current?.State == "move",
                Message = Join(message, $"角色视野读取失败：{ex.Message}")
            };
        }
    }

    private static string Join(string first, string last) => string.IsNullOrWhiteSpace(first) ? last : first + " " + last;

    public void Dispose()
    {
        if (!_ownsWorld) return;
        _ownsWorld = false;
        WorldMgr.End();
    }
}
