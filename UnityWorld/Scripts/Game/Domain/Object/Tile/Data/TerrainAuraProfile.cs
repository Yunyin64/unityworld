namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地形基础五行浓度映射表。
    /// 提供每种 <see cref="TerrainType"/> 对应的初始 <see cref="TileAura"/> 偏向值，
    /// 供世界生成 Step2（TerrainGen）写入地块基础元气时使用。
    /// 预缓存模式：避免热路径 new TileAura() 产生 GC。
    /// </summary>
    public static class TerrainAuraProfile
    {
        // ── 预缓存的只读实例 ─────────────────────────────────────
        private static readonly TileAura PlainProfile     = new TileAura { Jin = 1.0f, Mu = 1.0f, Shui = 1.0f, Huo = 1.0f, Tu = 1.0f };
        private static readonly TileAura HillProfile      = new TileAura { Jin = 1.0f, Mu = 0.8f, Shui = 0.8f, Huo = 0.8f, Tu = 1.5f };
        private static readonly TileAura MountainProfile  = new TileAura { Jin = 2.0f, Mu = 0.5f, Shui = 0.8f, Huo = 0.8f, Tu = 1.2f };
        private static readonly TileAura RiverLakeProfile = new TileAura { Jin = 0.8f, Mu = 1.2f, Shui = 2.5f, Huo = 0.5f, Tu = 0.8f };
        private static readonly TileAura OceanProfile     = new TileAura { Jin = 0.5f, Mu = 0.8f, Shui = 4.0f, Huo = 0.3f, Tu = 0.5f };
        private static readonly TileAura DesertProfile    = new TileAura { Jin = 1.0f, Mu = 0.3f, Shui = 0.3f, Huo = 2.5f, Tu = 1.5f };
        private static readonly TileAura ForestProfile    = new TileAura { Jin = 0.8f, Mu = 2.5f, Shui = 1.2f, Huo = 0.5f, Tu = 0.8f };
        private static readonly TileAura DefaultProfile   = new TileAura { Jin = 1.0f, Mu = 1.0f, Shui = 1.0f, Huo = 1.0f, Tu = 1.0f };

        /// <summary>
        /// 获取指定地形类型的基础五行浓度偏向值（返回预缓存实例的引用，只读不可修改）。
        /// 如需获取可修改副本，请使用 <see cref="GetCopy"/>。
        /// </summary>
        public static TileAura Get(TerrainType terrain) => terrain switch
        {
            TerrainType.Plain     => PlainProfile,
            TerrainType.Hill      => HillProfile,
            TerrainType.Mountain  => MountainProfile,
            TerrainType.RiverLake => RiverLakeProfile,
            TerrainType.Ocean     => OceanProfile,
            TerrainType.Desert    => DesertProfile,
            TerrainType.Forest    => ForestProfile,
            _                     => DefaultProfile,
        };

        /// <summary>
        /// 获取指定地形类型的基础五行浓度偏向值的深拷贝副本（可安全修改）。
        /// </summary>
        public static TileAura GetCopy(TerrainType terrain)
        {
            var source = Get(terrain);
            return new TileAura
            {
                Jin = source.Jin,
                Mu   = source.Mu,
                Shui = source.Shui,
                Huo  = source.Huo,
                Tu   = source.Tu,
            };
        }
    }
}
