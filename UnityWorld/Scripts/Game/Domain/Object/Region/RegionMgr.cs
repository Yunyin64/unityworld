using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain;

/// <summary>仅索引成功发布的地区，地理随机选择不依赖运行时实体 ID。</summary>
public sealed class RegionMgr : IDomainMgrBase
{
    public static RegionMgr Instance { get; private set; }
    private readonly Dictionary<int, Region> _regions = new();
    private readonly Dictionary<(int Plane, TileId Tile), int> _positions = new();
    private int _nextId;
    public string Name => "RegionMgr";
    public string Desc => "地区归属查询";
    public int Count => _regions.Count;
    public void Init() => Instance = this;
    internal Region Prepare(int planeId, GeneratedRegion plan, GeneratedWorld world)
        => new(++_nextId, planeId, plan.DefineId, plan.Index, plan.Anchor, plan.CellIndices.Select(i => world.Cells[i].Id), plan.IsFixed);
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
    public void Begin() { }
    public void Tick(float deltaTime) { }
    public void Update() { }
    public void Render(float dt) { }
    public void End()
    {
        _regions.Clear(); _positions.Clear();
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
    public IEnumerator Save() { yield break; }
    public IEnumerator Load() { yield break; }
}
