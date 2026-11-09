using UnityWorld.Core;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private static void Radius(Npc npc,float value)
    {
        npc.Stats.RemoveModifiersBySource("radius-test");
        npc.Stats.AddModifier("ExploreDisplayRadius",StatModifier.Override(value,"radius-test"));
    }
    private static void LocalMap()
    {
        var mgr=NpcMgr.Instance; var position=mgr.PositionSystem;
        var plane=Fixture(); var center=Plane.OffsetToAxial(10,10); var npc=Placed(plane,center);
        var cache=npc.Stats.GetFinalCache().ToDictionary(x=>x.Key,x=>x.Value);
        var tiles=position.GetVisibleTiles(npc.Id);
        Check(tiles.Count==91 && plane.TileSystem.GetTilesInRange(center,5).Count==91,"NPC查询复用位面范围查询");
        Check(!npc.Stats.Has("ExploreDisplayRadius") && cache.Count==npc.Stats.GetFinalCache().Count,"半径读取不建条目");
        Check(position.GetMoveCandidates(npc.Id).Count==7 && tiles.Any(tile=>tile.Id.DistanceTo(center)==3),"看得远但行动仍七格");
        Reject<NotSupportedException>(()=>((IList<Tile>)tiles).Clear(),"范围集合只读");
        var tileCopy=tiles.First(tile=>tile.Id==center);
        float oldAura=plane.GetTile(center).CurrentAura.Huo;
        tileCopy.CurrentAura.Huo+=100; tileCopy.Environment.Elevation+=100;
        var water=tileCopy.Water; water.Kind="ocean";
        Check(plane.GetTile(center).CurrentAura.Huo==oldAura && plane.GetTile(center).Environment.Elevation!=tileCopy.Environment.Elevation && plane.GetTile(center).Water.Kind=="none","既有Tile观察副本不修改真实数据");
        npc.Stats.AddModifier("ExploreDisplayRadius",StatModifier.Flat(1));
        Check(position.GetVisibleTiles(npc.Id).Count==127,"属性加值后127格");
        Radius(npc,5.9f); Check(position.GetVisibleTiles(npc.Id).Count==91,"5.9向下取整");
        Radius(npc,0); Check(position.GetVisibleTiles(npc.Id).Count==1,"零半径只自己");
        Radius(npc,float.MaxValue); Check(position.GetVisibleTiles(npc.Id).Count==400,"大半径只查实际400格");
        Radius(npc,5); Check(position.TryPlace(npc.Id,plane.Id.Value,TileId.Zero),"设置边缘位置");
        Check(position.GetVisibleTiles(npc.Id).Count<91,"边缘不补虚拟格");
        Check(position.TryPlace(npc.Id,plane.Id.Value,center),"回中心");
        Radius(npc,0); Start(npc,center.Neighbor(0)); Week();
        var moving=mgr.BehaviorSystem.GetAction(npc.Id); long week=WorldTime.CurrentDate.ElapsedWeeks;
        var before=npc.Stats.GetFinalCache().OrderBy(x=>x.Key).ToArray();
        Check(position.GetVisibleTiles(npc.Id).Single().Id==center && mgr.BehaviorSystem.GetAction(npc.Id)==moving,"途中观察中心仍原格");
        Check(WorldTime.CurrentDate.ElapsedWeeks==week && before.SequenceEqual(npc.Stats.GetFinalCache().OrderBy(x=>x.Key)),"读取不改时间缓存");
        Week(); Check(position.GetVisibleTiles(npc.Id).Single().Id==center.Neighbor(0),"到达才换查询中心");
        mgr.Remove(npc.Id);
        var raw=mgr.Birth(new BirthContext());
        Reject<InvalidOperationException>(()=>position.GetVisibleTiles(raw.Id),"未落地查询明确失败");
        mgr.Remove(raw.Id);
    }
}
