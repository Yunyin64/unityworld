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
    public class AuraDaoMgr
    {
        // ── 原初快照 ─────────────────────────────────────

        /// <summary>原初元气快照：Key=TileId，Value=自然态 TileAura 深拷贝</summary>
        private readonly Dictionary<TileId, TileAura> _originSnapshot = new();

        // ── 已注册位面 ────────────────────────────────────

        /// <summary>已注册进行天道感知的位面列表</summary>
        private readonly List<Plane> _registeredPlanes = new();

        // ── 失衡权重缓存（本 Tick 计算结果）─────────────────

        private readonly Dictionary<BaseElementType, float> _imbalanceWeightCache = new();
        private bool _cacheValid = false;

        // ── 快照 ──────────────────────────────────────────

        /// <summary>
        /// 为指定位面拍摄元气原初快照（应在自然地标生成完毕后、任何后天 Modifier 添加前调用）
        /// </summary>
        public void TakeSnapshot(Plane plane)
        {
            foreach (var (id, tile) in plane.Tiles)
            {
                var snapshot = new TileAura
                {
                    Jing = tile.CurrentAura.Jing,
                    Mu   = tile.CurrentAura.Mu,
                    Shui = tile.CurrentAura.Shui,
                    Huo  = tile.CurrentAura.Huo,
                    Tu   = tile.CurrentAura.Tu,
                };
                _originSnapshot[id] = snapshot;
            }

            if (!_registeredPlanes.Contains(plane))
                _registeredPlanes.Add(plane);

            _cacheValid = false;
        }

        /// <summary>
        /// 获取指定地块的原初元气快照；不存在时返回 null
        /// </summary>
        public TileAura? GetOrigin(TileId id)
            => _originSnapshot.TryGetValue(id, out var aura) ? aura : null;

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
                [BaseElementType.Jing] = 0f,
                [BaseElementType.Mu]   = 0f,
                [BaseElementType.Shui] = 0f,
                [BaseElementType.Huo]  = 0f,
                [BaseElementType.Tu]   = 0f,
            };

            foreach (var plane in _registeredPlanes)
            {
                foreach (var (id, tile) in plane.Tiles)
                {
                    if (!_originSnapshot.TryGetValue(id, out var origin)) continue;

                    balance[BaseElementType.Jing] += tile.CurrentAura.Jing - origin.Jing;
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
    }
}
