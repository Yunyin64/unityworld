using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

/// <summary>按既有位面/位置/行为入口验收，不用策略interface或额外管理器搭测试。</summary>
public static partial class NpcMovementTests
{
    private static int _assertions;
    private static void Check(bool value, string reason)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException("NPC移动断言失败：" + reason);
    }
    private static void Reject<T>(Action action, string reason) where T : Exception
    {
        try { action(); }
        catch (T) { _assertions++; return; }
        throw new InvalidOperationException("应该拒绝：" + reason);
    }
    public static void Run()
    {
        _assertions = 0;
        LogMgr.MinLevel = LogLevel.Warn;
        try
        {
            WorldMgr.Initialize(12345);
            Attributes();
            Presence();
            MovementAndWeeks();
            LocalMap();
            FailureBoundaries();
        }
        finally { WorldMgr.End(); }
        ReplayAndLifecycle();
        ConfiguredExploration();
        Console.WriteLine($"NPC_MOVEMENT_OK assertions={_assertions}");
    }
    private static void Attributes()
    {
        var mgr=NpcMgr.Instance;
        var npc=mgr.Birth(new BirthContext());
        try
        {
            Check(mgr.PositionSystem.GetActionSpeed(npc)==50, "JSON默认速度50");
            Check(mgr.PositionSystem.GetDisplayRadius(npc)==5, "JSON默认半径5");
            Check(!npc.Stats.Has("ExploreActionSpeed") && !npc.Stats.Has("ExploreDisplayRadius"), "读取不创建探索属性条目");
            npc.Stats.AddModifier("ExploreActionSpeed",StatModifier.Flat(50));
            Check(mgr.PositionSystem.GetActionSpeed(npc)==100, "沿用真实属性加值");
            npc.Stats=new StatBlock([]);
            Reject<InvalidOperationException>(()=>mgr.PositionSystem.GetActionSpeed(npc), "缺定义不默认为0");
            var bad=new StatDefine { ID="ExploreActionSpeed", Type="Card", DefaultValue=50, MinValue=0 };
            npc.Stats=new StatBlock([bad]);
            Reject<InvalidOperationException>(()=>mgr.PositionSystem.GetActionSpeed(npc), "错误归属");
            bad.Type="Npc"; bad.DefaultValue=float.NaN;
            Reject<InvalidOperationException>(()=>mgr.PositionSystem.GetActionSpeed(npc), "非有限默认值");
            bad.DefaultValue=50;
            npc.Stats.AddModifier("ExploreActionSpeed",StatModifier.Override(float.NaN));
            Reject<InvalidOperationException>(()=>mgr.PositionSystem.GetActionSpeed(npc), "非有限最终值");
        }
        finally { mgr.Remove(npc.Id); }
    }
}
