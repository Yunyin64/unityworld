using System.Collections;
using UnityWorld.Core;

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


        // ── IDomainMgrBase 接口 ───────────────────────────

        public void Begin()  { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }

        public void End()
        {
            Instance = null;
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        public string Name => "TileMgr";
        public string Desc => "地块管理器（跨位面查询）";

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[{0}] {1}", Name, Desc);
        }
    }
}
