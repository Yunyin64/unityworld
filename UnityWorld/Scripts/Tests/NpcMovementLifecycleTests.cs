using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private static void FailureBoundaries()
    {
        var mgr=NpcMgr.Instance; var calendar=WorldTime.Calendar;
        var property=typeof(WorldTime).GetProperty(nameof(WorldTime.Calendar));
        try
        {
            property.SetValue(null,new WorldCalendar(long.MaxValue));
            Reject<OverflowException>(()=>WorldMgr.AdvanceWeek(),"最大周数预检");
            Check(WorldTime.CurrentDate.ElapsedWeeks==long.MaxValue && PlaneMgr.Instance.GetAll().Sum(p=>p.NpcCount)==0,"溢出不提交");
        }
        finally { property.SetValue(null,calendar); }
        var plane=Fixture(); var npc=Placed(plane,Plane.OffsetToAxial(10,10));
        long before=WorldTime.CurrentDate.ElapsedWeeks;
        var entities=mgr._allEntities;
        Action<WorldCalendarTransition> corrupt=t=>
        {
            Check(mgr.PositionSystem.GetPosition(npc.Id).TileId==Plane.OffsetToAxial(10,10),"日历通知尚未承诺NPC完成");
            Reject<InvalidOperationException>(()=>mgr.PositionSystem.TryPlace(npc.Id,plane.Id.Value,TileId.Zero),"日历通知不能写位置");
            // 故意越过正式API破坏旧公开容器，验证日期提交后的故障边界。
            mgr._allEntities=null;
        };
        calendar.WeekAdvanced+=corrupt;
        try
        {
            var failed=WorldMgr.AdvanceWeek();
            Check(!failed.Complete && failed.After.ElapsedWeeks==before+1 && failed.Error.Length>0,"已提交周的内部失败明确返回");
            Reject<InvalidOperationException>(()=>WorldMgr.AdvanceWeek(),"不能自动重试故障周");
        }
        finally { mgr._allEntities=entities; calendar.WeekAdvanced-=corrupt; }
    }
    private static void ReplayAndLifecycle()
    {
        List<string> Run(bool perturb,out int firstId)
        {
            NpcMgr oldMgr=null; Plane oldPlane=null; var transcript=new List<string>(); firstId=0;
            try
            {
                WorldMgr.Initialize(12345); oldMgr=NpcMgr.Instance; oldPlane=PlaneMgr.Instance.MainPlane;
                Check(oldPlane.NpcCount==0 && oldMgr.PositionSystem.RegisteredCount==0 && oldMgr.BehaviorSystem.RegisteredCount==0,"重建没有旧成员位置行为");
                if(perturb)
                {
                    var temp=oldMgr.Assemble(NpcDefineMgr.Instance.Get("test_npc_a"));
                    Check(oldPlane.NpcCount==0 && oldMgr.PositionSystem.GetPosition(temp.Id)==null,"临时模板不属于位面");
                    for(int i=0;i<3;i++) oldMgr.Birth(new BirthContext());
                    var tiles=oldPlane.Tiles.Values.Reverse().ToArray();
                    foreach(var tile in tiles) oldPlane.RemoveTile(tile.Id);
                    foreach(var tile in tiles) oldPlane.SetTile(tile);
                }
                var people=Enumerable.Range(0,8).Select(_=>oldPlane.NpcSystem.Spawn(new BirthContext())).ToArray();
                firstId=people[0].Id;
                foreach(var npc in people) transcript.Add("born:"+oldMgr.PositionSystem.GetPosition(npc.Id).TileId);
                if(perturb) oldMgr._allEntities=oldMgr._allEntities.Reverse().ToDictionary(x=>x.Key,x=>x.Value);
                for(int i=0;i<8;i++)
                    foreach(var result in Week().Npcs)
                        transcript.Add($"{result.BirthOrder}:{result.Kind}:{result.After.CurrentTile}:{result.After.Target}:{result.After.Progress}");
                Check(oldPlane.NpcCount==8,"未落地对象不计入位面人口");
            }
            finally { WorldMgr.End(); }
            Check(oldPlane.NpcCount==0 && oldMgr.PositionSystem.RegisteredCount==0 && oldMgr.BehaviorSystem.RegisteredCount==0,"世界结束释放位面成员和位置行为");
            Reject<InvalidOperationException>(()=>oldPlane.NpcSystem.Spawn(new BirthContext()),"不能在已销毁位面出生");
            return transcript;
        }
        var baseline=Run(false,out int oldId); var changed=Run(true,out int newId);
        Check(oldId!=newId && baseline.SequenceEqual(changed),"历史ID和容器顺序变化后随机出生及8周轨迹复现");
        Reject<InvalidOperationException>(()=>WorldMgr.AdvanceWeek(),"未初始化不许过周");
    }
}
