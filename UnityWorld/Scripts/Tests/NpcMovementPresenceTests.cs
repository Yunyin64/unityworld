using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

public static partial class NpcMovementTests
{
    private static void TileValue(Tile tile, string property, object value)
        // 仅测试夹具使用现有内部发布属性造坏数据，不扩大正式Tile写接口。
        => typeof(Tile).GetProperty(property).SetValue(tile,value);
    private static Plane Fixture(int width=20,int height=20)
        => PlaneMgr.Instance.CreatePlane(new PlaneConfig { Name="movement fixture",Width=width,Height=height });
    private static Npc Placed(Plane plane,TileId tile)
    {
        var npc=NpcMgr.Instance.Birth(new BirthContext());
        Check(NpcMgr.Instance.PositionSystem.TryPlace(npc.Id,plane.Id.Value,tile), "通过位置系统放入位面");
        return npc;
    }
    private static void Presence()
    {
        var mgr=NpcMgr.Instance; var plane=Fixture();
        var origin=Plane.OffsetToAxial(10,10); var tile=plane.GetTile(origin);
        Check(ReferenceEquals(plane.Tiles,plane.TileData.Tiles) && plane.NpcData.NpcIds.Count==plane.NpcCount, "Plane唯一集合归Data");
        Check(plane.TileSystem.CanWalk(origin), "基础填充继承plain权限");
        TileValue(tile,nameof(Tile.AllowedMovement),Array.Empty<string>());
        Check(!plane.TileSystem.CanWalk(origin), "显式空权限不能回退");
        TileValue(tile,nameof(Tile.AllowedMovement),null);
        TileValue(tile,nameof(Tile.BlockAllMovement),true);
        Check(!plane.TileSystem.CanWalk(origin), "绝对阻挡");
        TileValue(tile,nameof(Tile.BlockAllMovement),false);
        foreach(var kind in new[]{"river","lake","ocean"})
        {
            TileValue(tile,nameof(Tile.Water),new MapWater { Kind=kind });
            Check(!plane.TileSystem.CanWalk(origin),"无方向边水面阻挡："+kind);
        }
        TileValue(tile,nameof(Tile.Water),new MapWater { IsSource=true });
        Check(!plane.TileSystem.CanWalk(origin), "实际水文覆盖陆地类型");
        TileValue(tile,nameof(Tile.Water),new MapWater());
        TileValue(tile,nameof(Tile.TypeId),"river");
        Check(!plane.TileSystem.CanWalk(origin), "类型水文和权限");
        TileValue(tile,nameof(Tile.TypeId),"plain");
        Check(!plane.TileSystem.CanWalk(new TileId(999,999)), "越界");
        var raw=mgr.Birth(new BirthContext());
        Check(mgr.PositionSystem.GetPosition(raw.Id)==null && !plane.ContainsNpc(raw.Id),"默认位置未落地");
        Check(!plane.AddNpc(raw.Id),"不能仅添加成员而跳过位置落地");
        Check(mgr.PositionSystem.TryPlace(raw.Id,plane.Id.Value,origin),"位置系统统一登记");
        var other=Placed(plane,origin);
        Check(plane.ContainsNpc(raw.Id) && plane.ContainsNpc(other.Id),"同格允许多个NPC");
        Check(plane.RemoveNpc(other.Id) && mgr.PositionSystem.GetPosition(other.Id)==null,"Plane移出成员也通过位置系统解除落地");
        Check(mgr.PositionSystem.TryPlace(other.Id,plane.Id.Value,origin),"移出后可以重新放置");
        long order=mgr.PositionSystem.GetPosition(raw.Id).BirthOrder;
        var target=origin.Neighbor(0);
        Check(PlaneMgr.Instance.MoveNpc(raw.Id,plane.Id,target),"兼容入口放置");
        Check(mgr.PositionSystem.GetPosition(raw.Id).TileId==target && mgr.PositionSystem.GetPosition(raw.Id).BirthOrder==order,"同位面换格保留序号");
        Check(!PlaneMgr.Instance.MoveNpc(raw.Id,new PlaneId(-99),target),"不存在位面拒绝");
        Check(!mgr.PositionSystem.TryPlace(raw.Id,plane.Id.Value,new TileId(999,999)),"不存在地块拒绝");
        Check(mgr.PositionSystem.GetPosition(raw.Id).TileId==target && plane.ContainsNpc(raw.Id),"失败没有部分改变");
        var copy=mgr.PositionSystem.GetPosition(raw.Id); copy.TileId=new(999,999);
        Check(mgr.PositionSystem.GetPosition(raw.Id).TileId==target,"位置返回副本");
        var second=Fixture();
        Check(mgr.PositionSystem.TryPlace(raw.Id,second.Id.Value,origin),"显式跨位面设置");
        Check(!plane.ContainsNpc(raw.Id) && second.ContainsNpc(raw.Id) && mgr.PositionSystem.GetPosition(raw.Id).PlaneId==second.Id.Value,"跨位面成员位置一致");
        Start(raw,origin.Neighbor(0));
        Check(PlaneMgr.Instance.Destroy(second.Id),"销毁非主位面");
        Check(second.NpcCount==0 && mgr.PositionSystem.GetPosition(raw.Id)==null && mgr.BehaviorSystem.GetAction(raw.Id).State=="idle" && mgr.GetById(raw.Id)!=null,"位面销毁解除成员落地和移动，NPC实体保留");
        DomainMgrBase<Npc> asBase=mgr; asBase.Remove(raw.Id);
        Check(!second.ContainsNpc(raw.Id) && !mgr.PositionSystem.Contains(raw.Id),"基类删除清理成员和数据");
        mgr.Remove(other.Id);
        int count=mgr.Count;
        var empty=Fixture(1,1); TileValue(empty.GetTile(TileId.Zero),nameof(Tile.BlockAllMovement),true);
        Reject<InvalidOperationException>(()=>empty.NpcSystem.Spawn(new BirthContext()),"没有合法落点");
        Check(mgr.Count==count && empty.NpcCount==0,"无落点不留下实体");
        var failing=new BirthContext(); failing.Set("InitCardDeck",new[]{"__missing_card__"});
        Reject<InvalidOperationException>(()=>plane.NpcSystem.Spawn(failing),"创建中途失败");
        Check(mgr.Count==count && plane.NpcCount==0 && !mgr.PositionSystem.Contains(failing.MainNpc.Id),"失败创建清理");
        var born=PlaneMgr.Instance.MainPlane.NpcSystem.Spawn(new BirthContext());
        var pos=mgr.PositionSystem.GetPosition(born.Id);
        Check(pos!=null && PlaneMgr.Instance.MainPlane.TileSystem.CanWalk(pos.TileId) && PlaneMgr.Instance.MainPlane.ContainsNpc(born.Id),"真实主大陆落地");
        mgr.Remove(born.Id);
        Check(PlaneMgr.Instance.GetAll().Sum(p=>p.NpcCount)==0,"不保留第二份成员名单");
    }
}
