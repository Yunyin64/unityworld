using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain;

/// <summary>仅索引成功发布的地区，地理随机选择不依赖运行时实体 ID。</summary>
public sealed class RegionMgr : IDomainMgrBase
{
    /// <summary>当前地区管理器</summary>
    public static RegionMgr Instance { get; private set; }
    /// <summary>按地区编号索引的实例</summary>
    private readonly Dictionary<int, Region> _regions = new();
    /// <summary>按位面和格索引的地区编号</summary>
    private readonly Dictionary<(int Plane, TileId Tile), int> _positions = new();
    /// <summary>地区实例编号计数器</summary>
    private int _nextId;
    /// <summary>管理器名称</summary>
    public string Name => "RegionMgr";
    /// <summary>地区管理器职责说明</summary>
    public string Desc => "地区归属查询";
    /// <summary>已发布的地区数量</summary>
    public int Count => _regions.Count;
    /// <summary>登记当前地区管理器实例</summary>
    public void Init() => Instance = this;
    /// <summary>按生成方案分配地区编号并构造待发布实例</summary>
    internal Region Prepare(int planeId, GeneratedRegion plan, GeneratedWorld world)
        => new(++_nextId, planeId, plan.DefineId, plan.Index, plan.Anchor, plan.CellIndices.Select(i => world.Cells[i].Id), plan.IsFixed);
    /// <summary>检查编号及占地冲突后登记地区和地块归属</summary>
    internal void Publish(Region region)
    {
        if (_regions.ContainsKey(region.Id) || region.OwnedTiles.Any(t => _positions.ContainsKey((region.PlaneId, t))))
            throw new InvalidOperationException("地区身份或实际占地发生冲突。");
        _regions.Add(region.Id, region);
        foreach (var tile in region.OwnedTiles) _positions.Add((region.PlaneId, tile), region.Id);
    }
    /// <summary>查询已发布地区；不存在时返回 null。</summary>
    public Region GetById(int id) => _regions.GetValueOrDefault(id);
    /// <summary>按位面和实际地块查询地区，不将外接框孔洞算入归属。</summary>
    public Region GetAt(int planeId, TileId tileId)
        => _positions.TryGetValue((planeId, tileId), out int id) ? _regions[id] : null;
    /// <summary>查询指定位面的全部已发布地区。</summary>
    public IEnumerable<Region> GetByPlane(int planeId) => _regions.Values.Where(r => r.PlaneId == planeId);
    /// <summary>查询指定位面中某一定义的实例。</summary>
    public IEnumerable<Region> GetByDefine(int planeId, string defineId)
        => GetByPlane(planeId).Where(r => r.DefineId == defineId);
    /// <summary>定义数量按位面统计，不把其他位面的实例计入上限。</summary>
    public int CountByDefine(int planeId, string defineId) => GetByDefine(planeId, defineId).Count();
    /// <summary>注销整个位面的地区和反向坐标索引。</summary>
    public void RemovePlane(int planeId)
    {
        foreach (var region in GetByPlane(planeId).ToArray())
        {
            foreach (var tile in region.OwnedTiles) _positions.Remove((planeId, tile));
            _regions.Remove(region.Id);
        }
    }
    /// <summary>保留世界启动回调</summary>
    public void Begin() { }
    /// <summary>保留时步回调</summary>
    public void Tick(float deltaTime) { }
    /// <summary>保留帧更新回调</summary>
    public void Update() { }
    /// <summary>保留渲染回调</summary>
    public void Render(float dt) { }
    /// <summary>清空地区和地块归属，并释放当前管理器</summary>
    public void End()
    {
        _regions.Clear(); _positions.Clear();
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
    /// <summary>结束存档流程，当前无写入</summary>
    public IEnumerator Save() { yield break; }
    /// <summary>结束读档流程，当前无读取</summary>
    public IEnumerator Load() { yield break; }
}
