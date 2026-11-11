using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private static WorldWeekResult Week()
    {
        var result=WorldMgr.AdvanceWeek();
        Check(result.Complete && result.After.ElapsedWeeks==result.Before.ElapsedWeeks+1,"世界一周只加一周："+result.Error);
        return result;
    }
    private static void Speed(Npc npc,float value)
    {
        npc.Stats.RemoveModifiersBySource("movement-speed-test");
        npc.Stats.AddModifier("ExploreActionSpeed",StatModifier.Override(value,"movement-speed-test"));
    }
    private static void Start(Npc npc,TileId target)
        => Check(NpcMgr.Instance.BehaviorSystem.TryStartMove(npc.Id,target),"经既有行为系统启动移动");
    private static void MovementAndWeeks()
    {
        var mgr=NpcMgr.Instance; var position=mgr.PositionSystem; var behavior=mgr.BehaviorSystem;
        var plane=Fixture(); var origin=Plane.OffsetToAxial(10,10); var target=origin.Neighbor(0);
        Check(WorldTime.CurrentDate.ElapsedWeeks==0 && WorldTime.CurrentDate.MoonPhase==null,"开局新年空月相");
        Check(Week().Npcs.Count==0 && WorldTime.CurrentDate.MoonPhase==MoonPhase.NewMoon,"空世界一周新月");
        var npc=Placed(plane,origin);
        Check(position.GetMoveCandidates(npc.Id).Count==7,"自己和六邻格");
        TileValue(plane.GetTile(origin.Neighbor(1)),nameof(Tile.BlockAllMovement),true);
        TileValue(plane.GetTile(origin.Neighbor(2)),nameof(Tile.BlockAllMovement),true);
        Check(position.GetMoveCandidates(npc.Id).Count==5,"过滤阻挡剩五格");
        TileValue(plane.GetTile(origin.Neighbor(1)),nameof(Tile.BlockAllMovement),false);
        TileValue(plane.GetTile(origin.Neighbor(2)),nameof(Tile.BlockAllMovement),false);
        Check(!behavior.TryStartMove(npc.Id,origin) && !behavior.TryStartMove(npc.Id,target.Neighbor(0)),"本格和远格启动拒绝");
        Start(npc,target);
        int ticks=WorldTime.CurrentTick; float seconds=WorldTime.TotalTime;
        var first=Week().Npcs.Single();
        Check(first.Kind=="moving" && first.After.Progress==50 && first.After.CurrentTile==origin && first.After.State=="move","第一周50/100不换格");
        Check(position.GetMoveCandidates(npc.Id).Count==0 && first.Target==target,"途中锁定");
        Check(!behavior.TryStartMove(npc.Id,origin.Neighbor(1)) && !behavior.TryStartExplore(npc.Id)
            && !behavior.TryStartWork(npc.Id),"途中不能改目标或另起主行为");
        Reject<InvalidOperationException>(()=>behavior.InterruptPrimary(npc.Id),"途中不能普通打断");
        Check(!position.TryPlace(npc.Id,plane.Id.Value,target),"途中不能改位置");
        Check(WorldTime.CurrentTick==ticks && WorldTime.TotalTime==seconds,"周结算不改秒时钟");
        WorldMgr.Tick(.01f); WorldTime.AdvanceWeek();
        Check(behavior.GetAction(npc.Id)==first.After,"秒Tick与纯周历不推进移动");
        var second=Week().Npcs.Single();
        Check(second.Kind=="arrived" && second.CompletedProgress==100 && second.After.CurrentTile==target && second.After.State=="idle","第二个移动周到达且不连走");
        Start(npc,target.Neighbor(0));
        Week(); Speed(npc,25); Check(Week().Npcs.Single().After.Progress==75,"速度改25累到75");
        Speed(npc,0); Check(Week().Npcs.Single().After.Progress==75,"零速保留进度");
        Speed(npc,25); Check(Week().Npcs.Single().Kind=="arrived","恢复25后到达");
        foreach(float speed in new[]{100f,150f})
        {
            Speed(npc,speed); var from=position.GetPosition(npc.Id).TileId;
            Start(npc,from.Neighbor(0)); var result=Week().Npcs.Single();
            Check(result.Action=="move" && result.Kind=="arrived" && result.CompletedProgress==100
                && result.Target==from.Neighbor(0) && result.After.State=="idle","高速只走一格，多余速度不转入下一步");
        }
        Speed(npc,0);
        var stoppedAt=position.GetPosition(npc.Id).TileId;
        Check(position.GetMoveCandidates(npc.Id).Count==1 && !behavior.TryStartMove(npc.Id,stoppedAt.Neighbor(0)),"零速保留本格查询且不能启动移动");
        var stationary=Week().Npcs.Single();
        Check(stationary.Kind is "explored" or "worked" && stationary.After.CurrentTile==stoppedAt,"零速自动在本格探索或工作");
        Speed(npc,50);
        var current=position.GetPosition(npc.Id).TileId; Start(npc,current.Neighbor(0)); Week();
        var missing=plane.GetTile(current.Neighbor(0)); plane.RemoveTile(missing.Id);
        var blocked=Week().Npcs.Single();
        Check(blocked.Kind=="blocked" && blocked.After.CurrentTile==current && blocked.After.State=="idle","目标失效留原格");
        plane.SetTile(missing);
        Check(behavior.AddPrimary(npc.Id,new PracticeBehavior(100)),"可加入原秒行为");
        Check(Week().Npcs.Single().Kind=="busy","不抢占已有主行为");
        behavior.InterruptPrimary(npc.Id);
        mgr.BioSystem.GetData(npc.Id).IsAlive=false;
        Check(Week().Npcs.Single().Kind=="skipped","死亡跳过");
        mgr.BioSystem.GetData(npc.Id).IsAlive=true;
        Speed(npc,float.NaN); Check(Week().Npcs.Single().Kind=="invalid_state","非法最终属性隔离");
        Speed(npc,50);
        int notifications=0;
        Action<WorldWeekResult> broken=r=>throw new Exception("observer-test");
        Action<WorldWeekResult> observer=r=>
        {
            Reject<InvalidOperationException>(()=>WorldMgr.AdvanceWeek(),"完成通知不能重入");
            Reject<InvalidOperationException>(()=>mgr.Remove(npc.Id),"回调不能注销");
            Reject<InvalidOperationException>(()=>position.TryPlace(npc.Id,plane.Id.Value,current),"回调不能改位置");
            Reject<InvalidOperationException>(()=>plane.NpcSystem.Spawn(new BirthContext()),"回调不能出生");
            Reject<InvalidOperationException>(()=>WorldMgr.End(),"回调不能结束世界");
            Check(behavior.GetAction(npc.Id)==r.Npcs.Single().After,"完成通知看到最终行为");
            notifications++;
        };
        WorldMgr.WeekCompleted+=broken; WorldMgr.WeekCompleted+=observer;
        Week(); Check(notifications==1,"观察者异常隔离");
        WorldMgr.WeekCompleted-=broken; WorldMgr.WeekCompleted-=observer;
        var definition=StatDefineMgr.Instance.Get("ExploreActionSpeed"); string type=definition.Type; definition.Type="Card";
        var unchanged=behavior.GetAction(npc.Id); long oldWeek=WorldTime.CurrentDate.ElapsedWeeks;
        try { Reject<InvalidOperationException>(()=>WorldMgr.AdvanceWeek(),"非法配置提前失败"); }
        finally { definition.Type=type; }
        Check(oldWeek==WorldTime.CurrentDate.ElapsedWeeks && unchanged==behavior.GetAction(npc.Id),"预检失败日期位置不变");
        mgr.Remove(npc.Id); Check(Week().Npcs.Count==0 && !plane.ContainsNpc(npc.Id),"途中注销不复活");
        var stay=Fixture(1,1); var waiter=Placed(stay,TileId.Zero);
        var isolated=Week().Npcs.Single();
        Check(isolated.Kind is "explored" or "worked" && position.GetPosition(waiter.Id).TileId==TileId.Zero,"有速度但孤立的地块只能探索或工作");
        mgr.Remove(waiter.Id);
        var people=Enumerable.Range(0,20).Select(i=>Placed(i<12?plane:stay,i<12?origin:TileId.Zero)).ToArray();
        Speed(people[0],float.NaN);
        var many=Week();
        Check(many.Npcs.Count==20 && many.Npcs.Count(x=>x.Kind=="invalid_state")==1,"两个位面20人只结算一周，单人错误不阻断");
        Check(many.Npcs.Select(x=>x.NpcId).Distinct().Count()==20,"每个NPC只处理一次");
        foreach(var person in people) mgr.Remove(person.Id);
        var legacy=new MoveBehavior(2); legacy.OnTick(1); Check(!legacy.IsFinished,"旧秒移动不变");
        legacy.OnTick(1); Check(legacy.IsFinished,"旧duration自然结束");
    }
}
