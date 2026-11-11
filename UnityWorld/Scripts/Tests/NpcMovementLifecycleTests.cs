using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private static void StationaryLifecycle(Plane plane, TileId center)
    {
        var mgr = NpcMgr.Instance;
        var actions = mgr.BehaviorSystem;
        var cards = BehaviorCardMgr.Instance;
        var npc = mgr.Birth(new BirthContext());
        TileValue(plane.GetTile(center), nameof(Tile.TypeId), "plain");
        Check(!actions.TryStartExplore(npc.Id) && !actions.TryStartWork(npc.Id), "未落地不能显式开始原地行动");
        Check(mgr.PositionSystem.TryPlace(npc.Id, plane.Id.Value, center), "实际落在有奖励事件的本格");
        Speed(npc, 0);
        Check(actions.TryStartExplore(npc.Id), "零移速仍可开始探索");
        mgr.BioSystem.GetData(npc.Id).IsAlive = false;
        var dead = Week().Npcs.Single();
        Check(dead.Kind == "skipped" && dead.After.State == "idle" && dead.EventId == null
            && !actions.TryStartWork(npc.Id) && !actions.TryStartExplore(npc.Id) && cards.GetCards(npc.Id).Count == 0,
            "死亡后释放绑定，不能发卡或接受新行动");
        mgr.BioSystem.GetData(npc.Id).IsAlive = true;

        Check(actions.TryStartExplore(npc.Id), "准备在过周前移除来源格");
        var source = plane.GetTile(center);
        plane.RemoveTile(center);
        var blocked = Week().Npcs.Single();
        Check(blocked.Kind == "blocked" && blocked.Action == "explore" && blocked.After.State == "idle"
            && blocked.After.CurrentTile == center && blocked.EventId == null && cards.GetCards(npc.Id).Count == 0,
            "锁定本格消失，探索受阻后当周不再决定或发卡");
        Check(!actions.TryStartWork(npc.Id) && !actions.TryStartExplore(npc.Id), "本格无效时拒绝原地命令");
        plane.SetTile(source);

        Check(actions.TryStartWork(npc.Id), "零移速仍可工作");
        var pending = actions.GetAction(npc.Id);
        plane.Seal();
        var paused = Week().Npcs.Single();
        Check(paused.Kind == "skipped" && paused.After == pending && cards.GetCards(npc.Id).Count == 0,
            "暂停保留待结算工作和所在格");
        plane.Unseal();
        var resumed = Week().Npcs.Single();
        Check(resumed.Kind == "worked" && resumed.After.State == "idle" && resumed.GrantedCardId == null,
            "位面恢复后才完成一次工作");
        plane.Seal();
        Check(!actions.TryStartWork(npc.Id) && !actions.TryStartExplore(npc.Id), "暂停时空闲角色也不能接单");
        plane.Unseal();

        Check(actions.TryStartExplore(npc.Id) && plane.RemoveNpc(npc.Id), "已开始探索的角色离开位面");
        Check(!actions.GetAction(npc.Id).IsPlaced && actions.IsIdle(npc.Id) && Week().Npcs.Count == 0
            && cards.GetCards(npc.Id).Count == 0, "离位解除原地绑定，原地点的奖励不再发放");
        Check(mgr.PositionSystem.TryPlace(npc.Id, plane.Id.Value, center.Neighbor(0))
            && actions.TryStartWork(npc.Id), "重新落地可以开始新工作");
        mgr.Remove(npc.Id);
        Check(!actions.TryStartExplore(npc.Id) && !actions.TryStartWork(npc.Id) && Week().Npcs.Count == 0,
            "注销释放待执行主槽，之后不能以旧 ID 启动");
    }

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
                        transcript.Add($"{result.BirthOrder}:{result.Action}:{result.Kind}:{result.After.CurrentTile}:{result.Target}:{result.After.Progress}:{result.EventId}:{result.GrantedCardId}");
                Check(oldPlane.NpcCount==8,"未落地对象不计入位面人口");
                foreach (var person in people) oldMgr.Remove(person.Id);
                CheckDefaultActionDraws(transcript);
            }
            finally { WorldMgr.End(); }
            Check(oldPlane.NpcCount==0 && oldMgr.PositionSystem.RegisteredCount==0 && oldMgr.BehaviorSystem.RegisteredCount==0,"世界结束释放位面成员和位置行为");
            Reject<InvalidOperationException>(()=>oldPlane.NpcSystem.Spawn(new BirthContext()),"不能在已销毁位面出生");
            return transcript;
        }
        var baseline=Run(false,out int oldId); var changed=Run(true,out int newId);
        Check(oldId != newId, "重放实际使用了不同历史实体 ID");
        Check(baseline.Count == changed.Count, "重放记录的条数一致");
        for (int i = 0; i < baseline.Count; i++)
            Check(baseline[i] == changed[i], $"重放第 {i} 条记录不同：{baseline[i]} / {changed[i]}");
        Reject<InvalidOperationException>(()=>WorldMgr.AdvanceWeek(),"未初始化不许过周");
    }
}
