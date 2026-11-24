using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块管理器：跨位面 Tile 查询的全局入口。
    /// 内部通过 <see cref="PlaneMgr.Instance"/> 访问各位面数据，
    /// 不替代 <see cref="Plane.GetTile"/> 的位面内部查询。
    /// </summary>
    public class TileMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────
        /// <summary>当前地块管理器</summary>
        public static TileMgr? Instance { get; private set; }

        // ── 生命周期 ──────────────────────────────────────

        /// <summary>初始化</summary>
        public void Init()
        {
            Instance = this;
        }

        // ── 查询 ─────────────────────────────────────────

        /// <summary>
        /// 跨位面按坐标获取 Tile。
        /// 位面不存在或坐标无效时返回 null。
        /// </summary>
        public Tile? GetTile(int planeId, TileId tileId)
            => PlaneMgr.Instance?.GetPlaneById(planeId)?.GetTile(tileId);

        /// <summary>按独立类型 ID 查询，不自动包含同标签的其他变体。</summary>
        public IEnumerable<Tile> GetByType(int planeId, string typeId)
            => (PlaneMgr.Instance?.GetPlaneById(planeId)?.Tiles.Values ?? Enumerable.Empty<Tile>())
                .Where(tile => tile.TypeId == typeId);

        /// <summary>按共同标签查询所有匹配类型。</summary>
        public IEnumerable<Tile> GetByTag(int planeId, string tag)
        {
            var catalog = GameDataMgr.Instance?.MapCatalog;
            return (PlaneMgr.Instance?.GetPlaneById(planeId)?.Tiles.Values ?? Enumerable.Empty<Tile>())
                .Where(tile => catalog?.Tiles.TryGetValue(tile.TypeId, out var define) == true && define.Tags.Contains(tag));
        }

        /// <summary>查询地区所属位面的实际拥有格；未知地区返回空集合。</summary>
        public IEnumerable<Tile> GetByRegion(int regionId)
        {
            var region = RegionMgr.Instance?.GetById(regionId);
            var plane = region == null ? null : PlaneMgr.Instance?.GetPlaneById(region.PlaneId);
            return plane == null ? Enumerable.Empty<Tile>() : region.OwnedTiles
                .Select(plane.GetTile).Where(tile => tile != null);
        }

        /// <summary>无地区归属包括固定边界与基础填充，不把两种来源混同。</summary>
        public IEnumerable<Tile> GetWithNoRegion(int planeId)
            => (PlaneMgr.Instance?.GetPlaneById(planeId)?.Tiles.Values ?? Enumerable.Empty<Tile>())
                .Where(tile => !tile.RegionId.HasValue);

        /// <summary>在指定位面内按固定边界、地区或基础填充来源筛选。</summary>
        public IEnumerable<Tile> GetBySource(int planeId, TileGenerationSource source)
            => (PlaneMgr.Instance?.GetPlaneById(planeId)?.Tiles.Values ?? Enumerable.Empty<Tile>())
                .Where(tile => tile.Source == source);


        // ── IDomainMgrBase 接口 ───────────────────────────

        /// <summary>保留世界启动回调</summary>
        public void Begin()  { }
        /// <summary>保留时步回调</summary>
        public void Tick(float deltaTime) { }
        /// <summary>保留帧更新回调</summary>
        public void Update() { }
        /// <summary>保留渲染回调</summary>
        public void Render(float dt) { }

        /// <summary>释放当前地块管理器实例</summary>
        public void End()
        {
            Instance = null;
        }

        /// <summary>结束存档流程，当前无写入</summary>
        public IEnumerator Save() { yield break; }
        /// <summary>结束读档流程，当前无读取</summary>
        public IEnumerator Load() { yield break; }

        /// <summary>管理器名称</summary>
        public string Name => "TileMgr";
        /// <summary>地块管理器职责说明</summary>
        public string Desc => "地块管理器（跨位面查询）";

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[{0}] {1}", Name, Desc);
        }
    }
}
