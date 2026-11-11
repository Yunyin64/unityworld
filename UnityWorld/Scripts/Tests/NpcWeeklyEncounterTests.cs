using System.Text.Json;
using System.Text.Json.Nodes;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private const string RewardCard = "ac_explore_surroundings";

    private static void ConfiguredExploration()
    {
        // 修改临时副本，让启动校验、地形引用、授卡和以后用卡都经过游戏实际使用的入口。
        var fixture = Directory.CreateTempSubdirectory("unityworld-weekly-actions-");
        try
        {
            var source = Path.Combine(AppContext.BaseDirectory, "Data");
            var dataDir = Path.Combine(fixture.FullName, "Data");
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(dataDir, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            void SetRefs(string terrain, params string[] encounters)
            {
                var path = Path.Combine(dataDir, "TileDefines", terrain + ".json");
                var definitions = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
                var tile = definitions.Single(node => (string)node["ID"] == terrain)!;
                tile["EncounterRefs"] = JsonSerializer.SerializeToNode(encounters);
                File.WriteAllText(path, definitions.ToJsonString());
            }
            var table = Path.Combine(dataDir, "ExploreEncounters.json");
            void CheckBadConfig(string json, string field, string id)
            {
                File.WriteAllText(table, json);
                var date = WorldTime.CurrentDate;
                try { WorldMgr.Initialize(12345, fixture.FullName); }
                catch (InvalidDataException ex)
                {
                    Check(ex.Message.Contains(id, StringComparison.Ordinal) && ex.Message.Contains(field, StringComparison.OrdinalIgnoreCase),
                        $"错误标明 {field} 和来源 ID {id}，实际为 {ex.Message}");
                    Check(WorldTime.CurrentDate == date, "无效遭遇配置不提交世界日期");
                    return;
                }
                throw new InvalidOperationException("未拒绝无效配置 " + id);
            }
            SetRefs("plain", "test_reward");
            CheckBadConfig("[]", "EncounterRefs", "test_reward");
            CheckBadConfig("""[{"ID":"test_reward","Chance":1.1}]""", "Chance", "test_reward");
            CheckBadConfig("""[{"ID":"test_reward","Chance":1,"BehaviorCardId":"missing_card"}]""", "BehaviorCardId", "missing_card");
            File.WriteAllText(table, """
                [
                  {"ID":"test_reward","Chance":1,"BehaviorCardId":"ac_explore_surroundings"},
                  {"ID":"test_no_card","Chance":1},
                  {"ID":"test_miss","Chance":0,"BehaviorCardId":"ac_explore_surroundings"}
                ]
                """);
            SetRefs("grassland");
            SetRefs("forest", "test_no_card");
            SetRefs("hill", "test_miss");
            SetRefs("basin", "test_miss", "test_reward");

            WorldMgr.Initialize(12345, fixture.FullName);
            var plane = Fixture(7, 7);
            var center = Plane.OffsetToAxial(3, 3);
            RewardTravelAndWork(plane, center);
            LocalFindings(plane, center);
            SameWeekActions(plane, center);
            StationaryLifecycle(plane, center);
        }
        finally
        {
            WorldMgr.End();
            if (fixture.Parent?.FullName != Path.TrimEndingDirectorySeparator(Path.GetTempPath())
                || !fixture.Name.StartsWith("unityworld-weekly-actions-", StringComparison.Ordinal))
                throw new InvalidOperationException("临时测试目录路径异常：" + fixture.FullName);
            fixture.Delete(recursive: true);
        }
    }

    private static void RewardTravelAndWork(Plane plane, TileId center)
    {
        var mgr = NpcMgr.Instance;
        var actions = mgr.BehaviorSystem;
        var cards = BehaviorCardMgr.Instance;
        var npc = Placed(plane, center);
        var destination = center.Neighbor(0);
        TileValue(plane.GetTile(center), nameof(Tile.TypeId), "plain");
        TileValue(plane.GetTile(destination), nameof(Tile.TypeId), "plain");
        var startDate = WorldTime.CurrentDate;
        var secondary = new SocialBehavior(3);
        actions.AddSecondary(npc.Id, secondary);
        Check(actions.TryStartExplore(npc.Id), "只有次要行为也能主动开始探索");
        var pending = actions.GetAction(npc.Id);
        Check(pending.State == "explore" && pending.CurrentTile == center && pending.TotalDistance == 0 && pending.Progress == 0,
            "查询到待结算的原地探索，没有移动距离和进度");
        Check(!actions.TryStartWork(npc.Id) && !actions.TryStartMove(npc.Id, destination) && !actions.TryStartExplore(npc.Id)
            && actions.GetAction(npc.Id) == pending && WorldTime.CurrentDate == startDate && cards.GetCards(npc.Id).Count == 0,
            "占主槽后重复或竞争指令被拒绝，日期、位置和持卡都未变");
        Check(!mgr.PositionSystem.TryPlace(npc.Id, plane.Id.Value, destination), "不能把待结算的探索换到邻格");
        WorldMgr.Tick(1);
        WorldTime.AdvanceWeek();
        Check(secondary.ElapsedTime == 1 && actions.GetAction(npc.Id) == pending && cards.GetCards(npc.Id).Count == 0,
            "经过秒 Tick 与纯历法后，只推进了次要行为的秒数");

        // 周完成通知内的异常会被生产代码隔离，先记录看到的值，回调退出后再做断言。
        var observations = new List<(NpcWeekResult Action, int Cards)>();
        void Observe(WorldWeekResult week) => observations.Add((week.Npcs.Single(), cards.GetCards(npc.Id).Count));
        var ticks = WorldTime.CurrentTick;
        var seconds = WorldTime.TotalTime;
        WorldWeekResult explored;
        WorldMgr.WeekCompleted += Observe;
        WorldMgr.WeekCompleted += Observe;
        try { explored = Week(); }
        finally { WorldMgr.WeekCompleted -= Observe; WorldMgr.WeekCompleted -= Observe; }
        var action = explored.Npcs.Single();
        Check(action.Action == "explore" && action.Kind == "explored" && action.Before == pending
            && action.After.State == "idle" && action.After.CurrentTile == center
            && action.EventId == "test_reward" && action.GrantedCardId == RewardCard,
            "周探索命中本格的确定事件，原地结束并记录实际所得卡");
        var granted = cards.GetCards(npc.Id).Single();
        Check(granted.OwnerId == npc.Id && granted.DefineId == RewardCard && granted.UsageCount == 0,
            "卡已进入本 NPC 实际持卡列表且尚未使用");
        Check(observations.Count == 2 && observations.All(read => read.Action == action && read.Cards == 1)
            && WorldMgr.LastWeekResult.Npcs.Single() == action && actions.GetAction(npc.Id) == action.After
            && cards.GetCards(npc.Id).Count == 1 && WorldTime.CurrentDate == explored.After, "两个完成通知及后续查询都不重复结算");
        Check(WorldTime.CurrentTick == ticks && WorldTime.TotalTime == seconds && secondary.ElapsedTime == 1,
            "过周不推进秒时钟或次要行为");

        Start(npc, destination);
        var first = Week().Npcs.Single();
        var arrival = Week().Npcs.Single();
        Check(first.Action == "move" && first.Kind == "moving" && first.After.Progress == 50
            && arrival.Kind == "arrived" && arrival.After.CurrentTile == destination && arrival.After.State == "idle"
            && arrival.EventId == null && cards.GetCards(npc.Id).Count == 1, "拿卡以后明确走两周，途经必中奖地形也不触发探索");
        CultivationMgr.Instance.AddCultivation(npc, "jin_fa", 25);
        Check(CultivationMgr.Instance.SwitchCultivation(npc, "jin_fa"), "为工作验收选定未修满的实际功法");
        var gongFa = npc.GetNowGongFaData();
        var oldPoints = gongFa.CurrentPoint;
        var realmProgress = npc.PracticeData.RealmProgress;
        var workDate = WorldTime.CurrentDate;
        Check(actions.TryStartWork(npc.Id) && actions.GetAction(npc.Id).State == "work" && WorldTime.CurrentDate == workDate,
            "显式工作只登记待执行状态");
        var work = Week().Npcs.Single();
        Check(work.Action == "work" && work.Kind == "worked" && work.EventId == null && work.GrantedCardId == null
            && work.After.CurrentTile == destination && work.After.State == "idle"
            && npc.PracticeData.RealmProgress == realmProgress && gongFa.CurrentPoint == oldPoints
            && cards.GetCards(npc.Id).Count == 1 && granted.UsageCount == 0,
            "同样的必中奖地形上，工作一周不判探索、不授卡、不加修炼收益");

        // 用刚才真正获得的卡来核实同名 Explore 的两种时钟，而非手动 new 旧行为代替用卡。
        cards.UseCard(npc.Id, RewardCard);
        var oldExplore = actions.GetPrimary(npc.Id) as ExploreBehavior;
        Check(oldExplore != null && !oldExplore.IsWeeklyManaged && granted.UsageCount == 1 && actions.GetAction(npc.Id).State == "busy",
            "主动用探索四方卡进入原秒制探索");
        var busy = Week().Npcs.Single();
        Check(busy.Kind == "busy" && busy.EventId == null && busy.GrantedCardId == null && oldExplore.ElapsedTime == 0
            && cards.GetCards(npc.Id).Count == 1 && secondary.ElapsedTime == 1, "世界周跳过旧探索卡和次要行为");
        WorldMgr.Tick(2);
        Check(oldExplore.ElapsedTime == 2 && actions.GetSecondaries(npc.Id).Count == 0 && secondary.IsFinished, "只有秒 Tick 令次要行为结束");
        WorldMgr.Tick(oldExplore.Duration - oldExplore.ElapsedTime);
        Check(oldExplore.IsFinished && actions.IsIdle(npc.Id) && cards.GetCards(npc.Id).Count == 1, "探索四方卡依配置秒数自然结束");
        mgr.Remove(npc.Id);
    }

    private static void LocalFindings(Plane plane, TileId center)
    {
        var npc = Placed(plane, center);
        var actions = NpcMgr.Instance.BehaviorSystem;
        var cards = BehaviorCardMgr.Instance;
        TileValue(plane.GetTile(center.Neighbor(0)), nameof(Tile.TypeId), "plain");
        foreach (var (terrain, eventId) in new (string, string)[] { ("grassland", null), ("forest", "test_no_card"), ("hill", null) })
        {
            TileValue(plane.GetTile(center), nameof(Tile.TypeId), terrain);
            Check(actions.TryStartExplore(npc.Id), "下周可重新探索 " + terrain);
            var result = Week().Npcs.Single();
            Check(result.Kind == "explored" && result.Action == "explore" && result.EventId == eventId
                && result.GrantedCardId == null && result.After.CurrentTile == center && result.After.State == "idle"
                && cards.GetCards(npc.Id).Count == 0, $"{terrain} 只按本格空引用、无卡事件或零命中率结算");
        }

        // basin 同时有未命中和必中奖两条，选到前者也必须当周收手。
        TileValue(plane.GetTile(center), nameof(Tile.TypeId), "basin");
        var random = GetWeeklyRandom(plane);
        var birthOrder = NpcMgr.Instance.PositionSystem.GetPosition(npc.Id).BirthOrder;
        for (int slot = 0; slot < 2; slot++)
        {
            SelectTestWeek(w => random(birthOrder, w, 4).Range(0, 2) == slot);
            Check(actions.TryStartExplore(npc.Id), "显式探索多个候选事件的同一格");
            int before = cards.GetCards(npc.Id).Count;
            var result = Week().Npcs.Single();
            bool rewarded = slot == 1;
            Check(result.Kind == "explored" && result.EventId == (rewarded ? "test_reward" : null)
                && result.GrantedCardId == (rewarded ? RewardCard : null) && cards.GetCards(npc.Id).Count == before + (rewarded ? 1 : 0),
                "单周选一个本地事件，未命中就不重抽，所得卡与实际增量相同");
        }
        NpcMgr.Instance.Remove(npc.Id);
    }

    private static void SameWeekActions(Plane plane, TileId center)
    {
        TileValue(plane.GetTile(center), nameof(Tile.TypeId), "plain");
        var mgr = NpcMgr.Instance;
        var cards = BehaviorCardMgr.Instance;
        var explorer = Placed(plane, center);
        var worker = Placed(plane, center);
        var traveler = Placed(plane, center);
        var destination = center.Neighbor(0);
        Check(mgr.BehaviorSystem.TryStartExplore(explorer.Id) && mgr.BehaviorSystem.TryStartWork(worker.Id),
            "不同 NPC 同时登记原地探索与工作");
        Speed(traveler, 150);
        Start(traveler, destination);
        var week = Week();
        Check(week.Npcs.Select(r => r.Kind).SequenceEqual(new[] { "explored", "worked", "arrived" })
            && week.Npcs.All(r => r.After.State == "idle"), "同周按出生顺序结算三个人，周末空闲也能区分各自结果");
        Check(cards.GetCards(explorer.Id).Count == 1 && cards.GetCards(worker.Id).Count == 0 && cards.GetCards(traveler.Id).Count == 0
            && week.Npcs[0].After.CurrentTile == center && week.Npcs[1].After.CurrentTile == center
            && week.Npcs[2].After.CurrentTile == destination, "同格只给探索者发卡，移动者独自换格");
        mgr.Remove(explorer.Id);
        mgr.Remove(worker.Id);
        mgr.Remove(traveler.Id);
    }
}
