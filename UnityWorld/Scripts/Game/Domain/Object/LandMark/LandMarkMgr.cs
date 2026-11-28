using System.Collections;
using System.Globalization;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>创世自然阶段仅由生成器使用；普通放置始终走动态变化。</summary>
internal enum LandMarkPlacementPhase
{
    /// <summary>创世阶段的天然地标放置</summary>
    CreationNatural,
    /// <summary>运行期地标放置</summary>
    Dynamic
}

/// <summary>地标占位、单例与来源修正索引；候选地图可以准备实体而不向本管理器注册。</summary>
public class LandMarkMgr : IDomainMgrBase
{
    /// <summary>当前地标管理器</summary>
    public static LandMarkMgr Instance { get; private set; }
    /// <summary>按实例编号索引的地标</summary>
    private readonly Dictionary<int, LandMark> _allLandMarks = new();
    /// <summary>按地标编号索引的落点地块</summary>
    private readonly Dictionary<int, Tile> _tiles = new();
    /// <summary>地标放置使用的随机源</summary>
    private Rng _rng = new();
    /// <summary>地图数据目录</summary>
    private MapCatalog _catalog;
    /// <summary>管理器名称</summary>
    public string Name => "LandMarkMgr";
    /// <summary>地标管理器职责说明</summary>
    public string Desc => "地标管理器";
    /// <summary>已登记的地标数量</summary>
    public int Count => _allLandMarks.Count;
    /// <summary>登记当前地标管理器实例</summary>
    public void Init() => Instance = this;
    /// <summary>更换地标管理器的随机源</summary>
    public void SetRng(Rng rng) => _rng = rng;
    /// <summary>使用已通过严格审计的地图定义库。</summary>
    public void SetCatalog(MapCatalog catalog) => _catalog = catalog;

    /// <summary>从地图目录或定义管理器读取地标定义</summary>
    private LandMarkDefine Resolve(string id)
        => _catalog != null ? _catalog.LandMarks.GetValueOrDefault(id) : LandMarkDefineMgr.Instance?.Get(id);
    /// <summary>从地图目录或定义管理器读取地块修正定义</summary>
    private TileModifierDefine ResolveModifier(string id)
        => _catalog != null ? _catalog.Modifiers.GetValueOrDefault(id) : TileModifierDefineMgr.Instance?.Get(id);

    /// <summary>只包括已发布有效实例的全局单例预留，不包含生成草稿。</summary>
    public IReadOnlySet<string> ReservedSingletons => _allLandMarks.Values
        .Where(lm => Resolve(lm.DefineId)?.IsSingleton == true).Select(lm => lm.DefineId).ToHashSet(StringComparer.Ordinal);

    /// <summary>运行时放置（包括新出现的自然地标）：忽略散布过滤，元气始终按时间变化。</summary>
    public LandMark Place(Plane plane, Tile tile, string defineId)
    {
        if (!ReferenceEquals(plane.GetTile(tile.Id), tile) || tile.LandMarkId.HasValue) return null;
        var define = Resolve(defineId);
        if (define == null || (define.IsSingleton && HasInstance(defineId))) return null;
        if (define.ModifierDefineIds.Any(id => ResolveModifier(id) == null)) return null;
        var landmark = Prepare(plane, tile, defineId);
        Publish(landmark, tile);
        return landmark;
    }

    /// <summary>查找运行时可供调用方选址的空格，检查定义、占位和全局唯一性。</summary>
    public IReadOnlyList<Tile> GetIndependentSpawnCandidates(Plane plane, string defineId)
    {
        var define = Resolve(defineId);
        if (define == null || (define.IsSingleton && HasInstance(defineId))) return Array.Empty<Tile>();
        return plane.Tiles.Values.Where(tile => !tile.LandMarkId.HasValue &&
                (_catalog != null ? _catalog.Tiles.ContainsKey(tile.TypeId) : TileDefineMgr.Instance?.Get(tile.TypeId) != null))
            .OrderBy(tile => tile.Id.R).ThenBy(tile => tile.Id.Q).ToArray();
    }

    /// <summary>在候选格构建地标。只有创世自然阶段立即偏移，其余阶段挂每秒速率。</summary>
    internal LandMark Prepare(Plane plane, Tile tile, string defineId, LandMarkPlacementPhase phase = LandMarkPlacementPhase.Dynamic,
        bool fromLandMarkGroup = false)
    {
        var define = Resolve(defineId) ?? throw new InvalidDataException($"未知地标 {defineId}");
        if (phase == LandMarkPlacementPhase.CreationNatural && (!define.IsNatural || AuraDaoMgr.Instance?.HasSnapshot(plane.Id) == true))
            throw new InvalidOperationException("创世自然地标必须在原初快照之前生成。");
        if (!fromLandMarkGroup && tile.LandMarkId.HasValue) throw new InvalidOperationException($"地块 {tile.Id} 已有地标。");
        if (define.IsSingleton && HasInstance(defineId)) throw new InvalidOperationException($"单例地标 {defineId} 已存在。");
        var definitions = define.ModifierDefineIds.Select(id => ResolveModifier(id)
            ?? throw new InvalidDataException($"地标 {defineId} 缺失修正引用 {id}")).ToArray();
        int instanceId = _rng.NewId();
        var sourceId = instanceId.ToString(CultureInfo.InvariantCulture);
        var modifiers = definitions.Select(d => d.CreateModifier(sourceId)).ToList();
        if (phase == LandMarkPlacementPhase.Dynamic)
            modifiers.Add(new TileModifier("landmark_aura_offset", sourceId, define.AuraOffset));
        var landmark = new LandMark(instanceId, defineId, tile.Id, plane.Id);
        tile.Modifiers.AddRange(modifiers);
        tile.AddLandMark(instanceId);
        if (phase == LandMarkPlacementPhase.CreationNatural) tile.CurrentAura.AddFrom(define.AuraOffset);
        return landmark;
    }

    /// <summary>校验实例及单例冲突后登记地标和落点</summary>
    internal void Publish(LandMark landmark, Tile tile)
    {
        var define = Resolve(landmark.DefineId) ?? throw new InvalidDataException($"未知地标 {landmark.DefineId}");
        if (_allLandMarks.ContainsKey(landmark.Id) || (define.IsSingleton && HasInstance(landmark.DefineId)))
            throw new InvalidOperationException($"地标 {landmark.DefineId} 发布冲突。");
        _allLandMarks.Add(landmark.Id, landmark);
        _tiles.Add(landmark.Id, tile);
        if (tile.RegionId.HasValue) RegionMgr.Instance?.GetById(tile.RegionId.Value)?.AddLandMark(landmark.Id);
    }

    /// <summary>移除地标并清理关联地块、区域和修正</summary>
    public bool Remove(int id)
    {
        if (!_allLandMarks.Remove(id, out var landmark)) return false;
        if (_tiles.Remove(id, out var tile))
        {
            tile.RemoveModifiersFromSource(id.ToString(CultureInfo.InvariantCulture));
            tile.RemoveLandMark(id);
            if (tile.RegionId.HasValue) RegionMgr.Instance?.GetById(tile.RegionId.Value)?.RemoveLandMark(id);
        }
        landmark.IsActive = false;
        return true;
    }
    /// <summary>注销该位面的地标、地块关联、地区列表及来源修正。</summary>
    public void RemovePlane(int planeId)
    {
        foreach (var id in GetByPlane(planeId).Select(lm => lm.Id).ToArray()) Remove(id);
    }
    /// <summary>在全部已发布位面中检查某定义是否存在有效实例。</summary>
    public bool HasInstance(string defineId) => _allLandMarks.Values.Any(lm => lm.DefineId == defineId && lm.IsActive);
    /// <summary>按实例编号查询地标</summary>
    public LandMark GetById(int id) => _allLandMarks.GetValueOrDefault(id);
    /// <summary>地标是否在玩家和 NPC 的普通观察中隐藏。</summary>
    internal bool IsHidden(int id) => GetById(id) is { } landmark && Resolve(landmark.DefineId)?.IsHide == true;
    /// <summary>枚举指定位面的已发布地标</summary>
    public IEnumerable<LandMark> GetByPlane(int planeId) => _allLandMarks.Values.Where(lm => lm.PlaneId == planeId);
    /// <summary>筛出指定位面的天然地标</summary>
    public IEnumerable<LandMark> GetNaturalLandMarks(int planeId) => GetByPlane(planeId).Where(lm => Resolve(lm.DefineId)?.IsNatural == true);
    /// <summary>筛出指定位面的人工地标</summary>
    public IEnumerable<LandMark> GetArtificialLandMarks(int planeId) => GetByPlane(planeId).Where(lm => Resolve(lm.DefineId)?.IsNatural == false);
    /// <summary>保留世界启动回调</summary>
    public void Begin() { }
    /// <summary>保留时步回调</summary>
    public void Tick(float deltaTime) { }
    /// <summary>保留帧更新回调</summary>
    public void Update() { }
    /// <summary>保留渲染回调</summary>
    public void Render(float dt) { }
    /// <summary>移除所有地标及关联数据，再释放当前管理器</summary>
    public void End()
    {
        foreach (var id in _allLandMarks.Keys.ToArray()) Remove(id);
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
    /// <summary>结束存档流程，当前无写入</summary>
    public IEnumerator Save() { yield break; }
    /// <summary>结束读档流程，当前无读取</summary>
    public IEnumerator Load() { yield break; }
    /// <summary>记录地标管理器状态和地标总数</summary>
    public void Log() => LogMgr.Instance.Dbg("[{0}] {1} | LandMarks={2}", Name, Desc, Count);
}
