using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地标管理器：负责地标的生成、销毁、查询。
    /// 生成时自动将 <see cref="TileModifierDefine"/> 实例化为 <see cref="TileModifier"/> 挂载到地块，
    /// 销毁时自动清理对应 Modifier。
    /// </summary>
    public class LandMarkMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────
        public static LandMarkMgr? Instance { get; private set; }

        // ── 数据 ─────────────────────────────────────────
        private readonly Dictionary<int, LandMark> _allLandMarks = new();
        private Rng _rng = new();

        // ── 生命周期 ──────────────────────────────────────

        /// <summary>初始化</summary>
        public void Init()
        {
            Instance = this;
        }

        /// <summary>注入随机数生成器（由 WorldMgr 初始化时调用）</summary>
        public void SetRng(Rng rng) => _rng = rng;

        // ── 生成 ─────────────────────────────────────────

        /// <summary>
        /// 在指定地块上生成一个地标实例。
        /// 若 Define 不存在、地块已有地标，或 Singleton 约束触发，则返回 null。
        /// </summary>
        /// <param name="plane">目标位面</param>
        /// <param name="tile">目标地块</param>
        /// <param name="defineId">地标 Define ID</param>
        public LandMark? Place(Plane plane, Tile tile, string defineId)
        {
            var define = LandMarkDefineMgr.Instance?.Get(defineId);
            if (define == null)
            {
                LogMgr.Instance.Warn("[LandMarkMgr] 找不到 LandMarkDefine: {0}", defineId);
                return null;
            }

            // Singleton 约束
            if (define.IsSingleton && HasInstance(defineId))
            {
                LogMgr.Instance.Warn("[LandMarkMgr] Singleton 地标 {0} 已存在，跳过", defineId);
                return null;
            }


            // 创建实例
            var id       = _rng.NewId();
            var landmark = new LandMark(id, defineId, tile.Id, plane.Id);


            // 挂载 Modifier
            AttachModifiers(tile, define, id);

            _allLandMarks[id] = landmark;
            return landmark;
        }

        /// <summary>
        /// 销毁指定地标：清理 Tile 上对应 Modifier，重置 Tile.LandMarkId，置 IsActive = false。
        /// </summary>
        public bool Remove(int id)
        {
            if (!_allLandMarks.TryGetValue(id, out var landmark)) return false;

            // 找到对应 Tile 并清理 Modifier
            var plane = PlaneMgr.Instance?.GetPlaneById(landmark.PlaneId);
            var tile  = plane?.GetTile(landmark.Position);
            if (tile != null)
            {
                var sourceIdStr = id.ToString();
                var toRemove = tile.Modifiers.Where(m => m.SourceId == sourceIdStr).ToList();
                foreach (var mod in toRemove)
                    tile.RemoveModifier(mod);

            }

            landmark.IsActive = false;
            _allLandMarks.Remove(id);
            return true;
        }

        // ── 查询 ─────────────────────────────────────────

        /// <summary>按 ID 查询地标</summary>
        public LandMark? GetById(int id)
            => _allLandMarks.TryGetValue(id, out var lm) ? lm : null;

        /// <summary>获取某位面的所有地标</summary>
        public IEnumerable<LandMark> GetByPlane(int planeId)
            => _allLandMarks.Values.Where(lm => lm.PlaneId == planeId);

        /// <summary>获取某位面所有 IsNatural=true 的地标（用于原初快照）</summary>
        public IEnumerable<LandMark> GetNaturalLandMarks(int planeId)
        {
            foreach (var lm in _allLandMarks.Values)
            {
                if (lm.PlaneId != planeId) continue;
                var define = LandMarkDefineMgr.Instance?.Get(lm.DefineId);
                if (define?.IsNatural == true) yield return lm;
            }
        }

        /// <summary>获取某位面所有 IsNatural=false 的地标（后天建筑，用于过滤原初快照）</summary>
        public IEnumerable<LandMark> GetArtificialLandMarks(int planeId)
        {
            foreach (var lm in _allLandMarks.Values)
            {
                if (lm.PlaneId != planeId) continue;
                var define = LandMarkDefineMgr.Instance?.Get(lm.DefineId);
                if (define?.IsNatural == false) yield return lm;
            }
        }

        // ── 内部工具 ──────────────────────────────────────

        private bool HasInstance(string defineId)
            => _allLandMarks.Values.Any(lm => lm.DefineId == defineId);

        private static void AttachModifiers(Tile tile, LandMarkDefine define, int id)
        {
            var modMgr = TileModifierDefineMgr.Instance;
            if (modMgr == null) return;

            var sourceId = id.ToString();
            foreach (var modDefineId in define.ModifierDefineIds)
            {
                var modDefine = modMgr.Get(modDefineId);
                if (modDefine == null)
                {
                    LogMgr.Instance.Warn("[LandMarkMgr] 找不到 TileModifierDefine: {0}", modDefineId);
                    continue;
                }
                tile.AddModifier(modDefine.CreateModifier(sourceId));
            }
        }

        // ── IDomainMgrBase 接口 ───────────────────────────

        public void Begin()   { }
        public void Tick(float deltaTime) { }
        public void Update()  { }
        public void Render(float dt) { }

        public void End()
        {
            _allLandMarks.Clear();
            Instance = null;
        }

        public IEnumerator Save()  { yield break; }
        public IEnumerator Load()  { yield break; }

        public string Name => "LandMarkMgr";
        public string Desc => "地标管理器";

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[{0}] {1} | LandMarks={2}", Name, Desc, _allLandMarks.Count);
        }
    }
}
