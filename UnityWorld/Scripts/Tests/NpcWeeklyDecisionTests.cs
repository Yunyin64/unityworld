using System.Reflection;
using UnityWorld.Core;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    // 只读取位面的随机源，不重写派生算法，也无须向正式 API 增加测试入口。
    private static Func<long, long, uint, Rng> GetWeeklyRandom(Plane plane)
    {
        var source = typeof(PlaneSystemNpc).GetMethod("CreateRandom", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return source.CreateDelegate<Func<long, long, uint, Rng>>(plane.NpcSystem);
    }

    private static long SelectTestWeek(Func<long, bool> accepts)
    {
        long first = WorldTime.CurrentDate.ElapsedWeeks + 1;
        for (long candidate = first; candidate < first + 2048; candidate++)
        {
            if (!accepts(candidate)) continue;
            while (WorldTime.CurrentDate.ElapsedWeeks + 1 < candidate) WorldTime.AdvanceWeek();
            return candidate;
        }
        throw new InvalidOperationException("未找到指定随机槽位的确定性测试输入");
    }

    private static void CheckDefaultActionDraws(List<string> transcript)
    {
        // 主位面在每次重放都有相同的 ID；另建的子位面有独立序号，会改变随机输入。
        var plane = PlaneMgr.Instance.MainPlane;
        var center = plane.Tiles.Keys.OrderBy(tile => tile.Q).ThenBy(tile => tile.R)
            .First(tile => plane.TileSystem.CanWalk(tile)
                && Enumerable.Range(0, 6).All(direction => plane.TileSystem.CanWalk(tile.Neighbor(direction))));
        var npc = Placed(plane, center);
        var position = NpcMgr.Instance.PositionSystem;
        long birthOrder = position.GetPosition(npc.Id).BirthOrder;
        Speed(npc, 100);

        // 直接核对每次抽取的槽位：六个邻格也只给 Move 一个名额。
        var random = GetWeeklyRandom(plane);
        foreach (int neighborCount in new[] { 6, 2, 0 })
        {
            var neighbors = Enumerable.Range(0, neighborCount).Select(center.Neighbor).ToArray();
            for (int direction = 0; direction < 6; direction++)
                TileValue(plane.GetTile(center.Neighbor(direction)), nameof(Tile.BlockAllMovement), direction >= neighborCount);
            string[] choices = neighborCount == 0 ? ["explore", "work"] : ["move", "explore", "work"];
            Check(position.GetMoveCandidates(npc.Id).SequenceEqual(new[] { center }.Concat(neighbors)),
                $"公开查询保留本格和 {neighborCount} 个合法邻格，顺序稳定");

            // 为每个可执行结果选择一个会准确命中它的周。跳过的日历周不让 NPC 行动。
            foreach (string action in choices)
            {
                int outcomes = action == "move" ? neighborCount : 1;
                for (int index = 0; index < outcomes; index++)
                {
                    long week = SelectTestWeek(w => choices[random(birthOrder, w, 3).Range(0, choices.Length)] == action
                        && (action != "move" || random(birthOrder, w, 2).Range(0, neighborCount) == index));
                    var result = Week().Npcs.Single();
                    var expectedTile = action == "move" ? neighbors[index] : center;
                    var kind = action switch { "move" => "arrived", "explore" => "explored", _ => "worked" };
                    Check(result.Action == action && result.Kind == kind && result.After.CurrentTile == expectedTile
                        && result.After.State == "idle", $"{neighborCount} 邻格时按独立随机用途抽中 {action}，落脚格为 {expectedTile}");
                    if (action == "move")
                    {
                        Check(result.Target == expectedTile && expectedTile != center, "自动 Move 的实际目标是抽中的邻格");
                        Check(position.TryPlace(npc.Id, plane.Id.Value, center), "下一次抽取从同一地形开始");
                    }
                    transcript.Add($"{week}/{neighborCount}/{result.Action}/{result.Target}/{result.After.CurrentTile}");
                }
            }
        }
        NpcMgr.Instance.Remove(npc.Id);
        for (int direction = 0; direction < 6; direction++)
            TileValue(plane.GetTile(center.Neighbor(direction)), nameof(Tile.BlockAllMovement), false);
    }
}
