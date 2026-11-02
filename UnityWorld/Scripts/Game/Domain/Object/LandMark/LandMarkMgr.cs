using System.Collections;
using System.Globalization;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>创世自然阶段仅由生成器使用；普通放置始终走动态变化。</summary>
internal enum LandMarkPlacementPhase { CreationNatural, Dynamic }

/// <summary>地标占位、单例与来源修正索引；候选地图可以准备实体而不向本管理器注册。</summary>
public class LandMarkMgr : IDomainMgrBase
{
    public static LandMarkMgr Instance { get; private set; }
    private readonly Dictionary<int, LandMark> _allLandMarks = new();
    private readonly Dictionary<int, Tile> _tiles = new();
    private Rng _rng = new();
    private MapCatalog _catalog;
    public string Name => "LandMarkMgr";
    public string Desc => "地标管理器";
    public int Count => _allLandMarks.Count;
    public void Init() => Instance = this;
    public void SetRng(Rng rng) => _rng = rng;
    /// <summary>使用已通过严格审计的地图定义库。</summary>
    public void SetCatalog(MapCatalog catalog) => _catalog = catalog;

    private LandMarkDefine Resolve(string id)
        => _catalog != null ? _catalog.LandMarks.GetValueOrDefault(id) : LandMarkDefineMgr.Instance?.Get(id);
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

    /// <summary>
    /// 独立刷新扩展入口：按地标定义的类型、标签、占位与唯一性筛选候选，不要求来自地区模板。
    /// 后续可在这里之上配置“指定类型上每次刷新 min..max 个”、随机选择、周期和失败策略，
    /// 选定后逐个调用 Place；不要调用创世阶段或修改原初快照。刷新不以 SpawnWeight 判断资格。
    /// 创世随机散布仍由地图生成器执行，保留其只使用 Fill 空格的规则。
    /// </summary>
    public IReadOnlyList<Tile> GetIndependentSpawnCandidates(Plane plane, string defineId)
    {
        var define = Resolve(defineId);
        if (define == null || (define.IsSingleton && HasInstance(defineId))) return Array.Empty<Tile>();
        return plane.Tiles.Values.Where(tile =>
        {
            if (tile.LandMarkId.HasValue) return false;
            var type = _catalog != null ? _catalog.Tiles.GetValueOrDefault(tile.TypeId) : TileDefineMgr.Instance?.Get(tile.TypeId);
            if (type == null || (define.PlacementTypeIds.Count > 0 && !define.PlacementTypeIds.Contains(type.ID))
                || !define.PlacementTags.All(type.Tags.Contains)) return false;
            if (define.PlacementExtraTerrains.Count == 0) return true;
            var region = tile.RegionId.HasValue ? RegionMgr.Instance?.GetById(tile.RegionId.Value) : null;
            return region != null && _catalog != null && _catalog.Regions.TryGetValue(region.DefineId, out var definition)
                && define.PlacementExtraTerrains.All(definition.ExtraTerrainIds.Contains);
        }).OrderBy(tile => tile.Id.R).ThenBy(tile => tile.Id.Q).ToArray();
    }

    /// <summary>在候选格构建地标。只有创世自然阶段立即偏移，其余阶段挂每秒速率。</summary>
    internal LandMark Prepare(Plane plane, Tile tile, string defineId, LandMarkPlacementPhase phase = LandMarkPlacementPhase.Dynamic)
    {
        var define = Resolve(defineId) ?? throw new InvalidDataException($"未知地标 {defineId}");
        if (phase == LandMarkPlacementPhase.CreationNatural && (!define.IsNatural || AuraDaoMgr.Instance?.HasSnapshot(plane.Id) == true))
            throw new InvalidOperationException("创世自然地标必须在原初快照之前生成。");
        if (tile.LandMarkId.HasValue) throw new InvalidOperationException($"地块 {tile.Id} 已有地标。");
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
        tile.LandMarkId = instanceId;
        if (phase == LandMarkPlacementPhase.CreationNatural) tile.CurrentAura.AddFrom(define.AuraOffset);
        return landmark;
    }

    internal void Publish(LandMark landmark, Tile tile)
    {
        var define = Resolve(landmark.DefineId) ?? throw new InvalidDataException($"未知地标 {landmark.DefineId}");
        if (_allLandMarks.ContainsKey(landmark.Id) || (define.IsSingleton && HasInstance(landmark.DefineId)))
            throw new InvalidOperationException($"地标 {landmark.DefineId} 发布冲突。");
        _allLandMarks.Add(landmark.Id, landmark);
        _tiles.Add(landmark.Id, tile);
        if (tile.RegionId.HasValue) RegionMgr.Instance?.GetById(tile.RegionId.Value)?.AddLandMark(landmark.Id);
    }

    public bool Remove(int id)
    {
        if (!_allLandMarks.Remove(id, out var landmark)) return false;
        if (_tiles.Remove(id, out var tile))
        {
            tile.RemoveModifiersFromSource(id.ToString(CultureInfo.InvariantCulture));
            if (tile.LandMarkId == id) tile.LandMarkId = null;
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
    public LandMark GetById(int id) => _allLandMarks.GetValueOrDefault(id);
    public IEnumerable<LandMark> GetByPlane(int planeId) => _allLandMarks.Values.Where(lm => lm.PlaneId == planeId);
    public IEnumerable<LandMark> GetNaturalLandMarks(int planeId) => GetByPlane(planeId).Where(lm => Resolve(lm.DefineId)?.IsNatural == true);
    public IEnumerable<LandMark> GetArtificialLandMarks(int planeId) => GetByPlane(planeId).Where(lm => Resolve(lm.DefineId)?.IsNatural == false);
    public void Begin() { }
    public void Tick(float deltaTime) { }
    public void Update() { }
    public void Render(float dt) { }
    public void End()
    {
        foreach (var id in _allLandMarks.Keys.ToArray()) Remove(id);
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
    public IEnumerator Save() { yield break; }
    public IEnumerator Load() { yield break; }
    public void Log() => LogMgr.Instance.Dbg("[{0}] {1} | LandMarks={2}", Name, Desc, Count);
}
