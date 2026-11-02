using System.Collections.Generic;
using System.Linq;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 元气天道管理器（领域层）：
    /// - 持有全位面原初元气快照（世界自然态基准）
    /// - 计算全局五行收支偏差
    /// - 暴露失衡权重接口供外部系统（如事件系统）查询
    /// 
    /// 天道只感知、不主动干预——它计算偏差并暴露权重，具体事件触发由其他系统决定。
    /// </summary>
    public class AuraDaoMgr:IGameplayMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        /// <summary>全局单例</summary>
        public static AuraDaoMgr Instance { get; private set; }
        // ── 原初快照 ─────────────────────────────────────

        /// <summary>原初值以位面和坐标复合定位，内部值从不向查询方直接暴露。</summary>
        private readonly Dictionary<(PlaneId Plane, TileId Tile), TileAura> _originSnapshot = new();

        // ── 已注册位面 ────────────────────────────────────

        /// <summary>已注册进行天道感知的位面列表</summary>
        private readonly List<Plane> _registeredPlanes = new();

        // ── 失衡权重缓存（本 Tick 计算结果）─────────────────

        private readonly Dictionary<BaseElementType, float> _imbalanceWeightCache = new();
        private bool _cacheValid = false;

        public string Name => "AuraDaoMgr";

        public string Desc => "";

        public AuraDaoMgr(int seed = 12345)
        {
            Instance = this;
        }

        // ── 快照 ──────────────────────────────────────────

        /// <summary>
        /// 为指定位面拍摄元气原初快照（创世自然地标生成完毕、人工地标尚未生成时调用）
        /// </summary>
        public void TakeSnapshot(Plane plane)
        {
            RegisterSnapshot(plane, PrepareSnapshot(plane));
        }

        /// <summary>仅准备自然值的深拷贝，不登记位面或任何公开快照。</summary>
        public static Dictionary<TileId, TileAura> PrepareSnapshot(Plane plane)
            => plane.Tiles.ToDictionary(pair => pair.Key, pair => Copy(pair.Value.CurrentAura));

        /// <summary>发布候选的自然快照；先完整校验和复制，再写入任何公开记录。</summary>
        public void RegisterSnapshot(Plane plane, IReadOnlyDictionary<TileId, TileAura> values)
        {
            if (_registeredPlanes.Any(p => p.Id == plane.Id))
                throw new InvalidOperationException($"位面 {plane.Id} 已登记原初快照。");
            if (values.Count != plane.TileCount || values.Keys.Any(id => !plane.HasTile(id)))
                throw new InvalidOperationException("自然快照必须与位面实际地块完全对应。");
            var prepared = values.ToDictionary(pair => pair.Key, pair => Copy(pair.Value));
            foreach (var pair in prepared) _originSnapshot.Add((plane.Id, pair.Key), pair.Value);
            _registeredPlanes.Add(plane);
            _cacheValid = false;
        }

        /// <summary>是否已登记该位面，包括明确允许的空位面。</summary>
        public bool HasSnapshot(PlaneId planeId) => _registeredPlanes.Any(p => p.Id == planeId);

        /// <summary>同时注销原初值与收支遍历引用，其他位面不受影响。</summary>
        public void UnregisterPlane(PlaneId planeId)
        {
            foreach (var key in _originSnapshot.Keys.Where(key => key.Plane == planeId).ToArray())
                _originSnapshot.Remove(key);
            _registeredPlanes.RemoveAll(plane => plane.Id == planeId);
            _cacheValid = false;
        }

        private static TileAura Copy(TileAura value)
        {
            if (value == null || !float.IsFinite(value.Jin) || !float.IsFinite(value.Mu)
                || !float.IsFinite(value.Shui) || !float.IsFinite(value.Huo) || !float.IsFinite(value.Tu))
                throw new InvalidDataException("元气快照必须包含有限的五行数值。");
            return new TileAura { Jin = value.Jin, Mu = value.Mu, Shui = value.Shui, Huo = value.Huo, Tu = value.Tu };
        }

        /// <summary>
        /// 获取指定地块的原初元气快照；不存在时返回 null
        /// </summary>
        public TileAura? GetOrigin(TileId id)
            => GetOrigin(0, id);

        /// <summary>跨位面查询原初值；返回可独立修改的副本，不存在时返回 null。</summary>
        public TileAura? GetOrigin(PlaneId planeId, TileId id)
            => _originSnapshot.TryGetValue((planeId, id), out var aura) ? Copy(aura) : null;

        // ── 收支计算 ──────────────────────────────────────

        /// <summary>
        /// 计算全局五行净偏差：遍历所有已注册位面中的 Tile，
        /// 累加 (CurrentAura - OriginAura)，返回各元素净偏差值。
        /// 正值表示该元素当前偏多，负值表示偏少。
        /// </summary>
        public Dictionary<BaseElementType, float> CalculateBalance()
        {
            var balance = new Dictionary<BaseElementType, float>
            {
                [BaseElementType.Jin] = 0f,
                [BaseElementType.Mu]   = 0f,
                [BaseElementType.Shui] = 0f,
                [BaseElementType.Huo]  = 0f,
                [BaseElementType.Tu]   = 0f,
            };

            foreach (var plane in _registeredPlanes)
            {
                foreach (var (id, tile) in plane.Tiles)
                {
                    if (!_originSnapshot.TryGetValue((plane.Id, id), out var origin)) continue;

                    balance[BaseElementType.Jin] += tile.CurrentAura.Jin - origin.Jin;
                    balance[BaseElementType.Mu]   += tile.CurrentAura.Mu   - origin.Mu;
                    balance[BaseElementType.Shui] += tile.CurrentAura.Shui - origin.Shui;
                    balance[BaseElementType.Huo]  += tile.CurrentAura.Huo  - origin.Huo;
                    balance[BaseElementType.Tu]   += tile.CurrentAura.Tu   - origin.Tu;
                }
            }

            return balance;
        }

        /// <summary>
        /// 获取指定元素的失衡程度（绝对偏差值，越大表示越失衡）。
        /// 结果来自本 Tick 的缓存计算，OnTick 时自动刷新。
        /// </summary>
        public float GetImbalanceWeight(BaseElementType element)
        {
            EnsureCache();
            return _imbalanceWeightCache.TryGetValue(element, out var w) ? w : 0f;
        }

        // ── Tick 驱动 ─────────────────────────────────────

        /// <summary>
        /// 每 Tick 调用：刷新失衡权重缓存
        /// </summary>
        public void OnTick(float deltaTime)
        {
            _cacheValid = false;
        }

        // ── 内部工具 ──────────────────────────────────────

        private void EnsureCache()
        {
            if (_cacheValid) return;

            var balance = CalculateBalance();
            foreach (var (element, delta) in balance)
                _imbalanceWeightCache[element] = System.MathF.Abs(delta);

            _cacheValid = true;
        }

        public void Init()
        {
             
        }

        public void Begin()
        {
             
        }

        public void Tick(float deltaTime)
        {
             
        }

        public void Update()
        {
             
        }

        public void Render(float dt)
        {
             
        }

        public void End()
        {
            _originSnapshot.Clear();
            _registeredPlanes.Clear();
            _imbalanceWeightCache.Clear();
            _cacheValid = false;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        public void Log()
        {
             
        }
    }
}
