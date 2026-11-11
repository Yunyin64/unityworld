using System.Globalization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.ConsoleHost;

/// <summary>每周展示 NPC 选择的主行为、实际落脚格和本地探索结果。</summary>
public static class NpcMovementConsole
{
    public static int Run(string[] args)
    {
        bool ownsWorld = false;
        try
        {
            int seed=12345, count=20, weeks=8;
            var seen = new HashSet<string>();
            for (int i=0;i<args.Length;i++)
            {
                string key = args[i];
                if (!seen.Add(key)) throw new ArgumentException($"重复参数：{key}");
                if (key == "--npc-movement-demo") continue;
                if (key is not ("--seed" or "--npc-count" or "--weeks") || ++i >= args.Length ||
                    !int.TryParse(args[i],NumberStyles.Integer,CultureInfo.InvariantCulture,out int value))
                    throw new ArgumentException($"非法演示参数：{key}");
                switch(key)
                {
                    case "--seed": seed=value; break;
                    case "--npc-count": count=value; break;
                    case "--weeks": weeks=value; break;
                }
            }
            if (count < 0 || weeks < 0) throw new ArgumentOutOfRangeException("人数与周数必须非负。");
            LogMgr.MinLevel = LogLevel.Warn;
            WorldMgr.Initialize(seed); ownsWorld = true;
            var mgr=NpcMgr.Instance; var plane=PlaneMgr.Instance.MainPlane;
            Console.WriteLine($"NPC_MOVEMENT_DEMO seed={seed} count={count} weeks={weeks} date={WorldTime.CurrentDate}");
            for (int i=0;i<count;i++)
            {
                var npc=plane.NpcSystem.Spawn(new BirthContext());
                var position=mgr.PositionSystem.GetPosition(npc.Id);
                Console.WriteLine($"BORN order={position.BirthOrder} id={npc.Id} plane={position.PlaneId} tile={position.TileId}");
            }
            for (int i=0;i<weeks;i++)
            {
                var result=WorldMgr.AdvanceWeek();
                if (!result.Complete) throw new InvalidOperationException("本周已提交但未完整完成，不能重试："+result.Error);
                Console.WriteLine($"WEEK elapsed={result.After.ElapsedWeeks} date={result.After} moon={result.After.MoonPhase?.ToString() ?? "none"}");
                foreach (var npc in result.Npcs)
                {
                    // 周末多种行为都回到 idle，本周做了什么应读周结果，不能从终态反推。
                    string details = npc.Action switch
                    {
                        "move" => FormattableString.Invariant($" target={npc.Target} progress={npc.CompletedProgress}/{MoveBehavior.AdjacentDistance} speed={npc.Speed}"),
                        "explore" => $" event={npc.EventId ?? "none"} card={npc.GrantedCardId ?? "none"}",
                        _ => ""
                    };
                    Console.WriteLine($"NPC order={npc.BirthOrder} id={npc.NpcId} action={npc.Action} result={npc.Kind} tile={npc.After.CurrentTile} state={npc.After.State}{details} reason={npc.Reason}");
                }
            }
            Console.WriteLine($"NPC_MOVEMENT_DEMO_OK population={plane.NpcCount} elapsed={WorldTime.CurrentDate.ElapsedWeeks}");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine("NPC_MOVEMENT_DEMO_FAILED "+ex); return 1; }
        finally { if (ownsWorld) WorldMgr.End(); }
    }
}
