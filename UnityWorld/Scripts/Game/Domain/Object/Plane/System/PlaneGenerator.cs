using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面地块生成器：根据位面的 Width × Height 填充初始地块。
    /// 坐标使用「奇数行右移（odd-r）」Offset 布局，生成后转换为 Axial 存入 Plane。
    /// </summary>
    public static class PlaneGenerator
    {
        internal sealed class PreparedContent
        {
            /// <summary>准备发布的地区实例</summary>
            public List<Region> Regions { get; } = new();
            /// <summary>准备发布的地标实例</summary>
            public List<LandMark> LandMarks { get; } = new();
            /// <summary>添加人工地标前的元气快照</summary>
            public Dictionary<TileId, TileAura> NaturalSnapshot { get; set; }
        }

        /// <summary>地块类型与基础元气 → 自然地标 → 原初快照 → 人工地标；绝不推进 Tick。</summary>
        internal static PreparedContent BuildContinent(Plane plane, MapCatalog catalog, GeneratedWorld world,
            RegionMgr regions, LandMarkMgr landMarks)
        {
            if (world.Width != plane.Width || world.Height != plane.Height || world.Cells.Length != plane.Capacity)
                throw new InvalidDataException("候选地图尺寸不完整。");
            var prepared = new PreparedContent();
            var regionByIndex = new Dictionary<int, Region>();
            foreach (var plan in world.Regions.OrderBy(r => r.Index))
            {
                var region = regions.Prepare(plane.Id, plan, world);
                if (!regionByIndex.TryAdd(plan.Index, region)) throw new InvalidDataException("重复地区生成序号。");
                prepared.Regions.Add(region);
            }
            foreach (var cell in world.Cells)
            {
                var definition = catalog.Tiles[cell.TileDefineId];
                var tile = new Tile(cell.Id, definition.InitialAura)
                {
                    TypeId = cell.TileDefineId, Source = cell.Source,
                    IsFixed = cell.IsFixed, HasForcedLandmark = cell.HasForcedLandmark,
                    Environment = cell.Environment.Copy(), Water = cell.Water.Copy(),
                    BlockAllMovement = cell.BlockAllMovement || definition.BlocksAllMovement,
                    AllowedMovement = (cell.AllowedMovement ?? definition.MovementModes).ToArray(),
                };
                if (cell.RegionIndex.HasValue)
                {
                    var region = regionByIndex[cell.RegionIndex.Value];
                    if (!region.OwnedTiles.Contains(cell.Id)) throw new InvalidDataException("地区格子归属不一致。");
                    tile.RegionId = region.Id;
                    tile.CurrentAura.AddFrom(catalog.Regions[region.DefineId].AuraProfile);
                }
                if (plane.HasTile(tile.Id) || !plane.IsInBounds(tile.Id)) throw new InvalidDataException("候选地块重复或越界。");
                plane.SetTile(tile);
            }

            var singletons = new HashSet<string>(landMarks.ReservedSingletons, StringComparer.Ordinal);
            var authoredOrder = new Dictionary<TileId, int[]>();
            // 按天然或人工阶段准备地标并检查单例冲突
            void PrepareLandmarks(bool isNatural)
            {
                // 地区附带和独立散布都只提供位置计划，按同一个 IsNatural 阶段创建实际实体。
                foreach (var cell in world.Cells.Where(c => c.LandMarkDefineId != null))
                {
                    bool hasGroup = cell.AdditionalLandMarkDefineIds.Count > 0;
                    if (hasGroup && (!cell.RegionIndex.HasValue || cell.HasForcedLandmark))
                        throw new InvalidDataException($"地块 {cell.Id} 的多地标必须来自地区模板。");
                    var tile = plane.GetTile(cell.Id);
                    int index = 0;
                    foreach (string id in cell.PlannedLandMarks())
                    {
                        int slot = index++;
                        var define = catalog.LandMarks[id];
                        if (define.IsNatural != isNatural) continue;
                        if (define.IsSingleton && !singletons.Add(define.ID)) throw new InvalidDataException($"重复单例地标 {define.ID}");
                        var phase = isNatural ? LandMarkPlacementPhase.CreationNatural : LandMarkPlacementPhase.Dynamic;
                        var landmark = landMarks.Prepare(plane, tile, define.ID, phase, fromLandMarkGroup: hasGroup);
                        prepared.LandMarks.Add(landmark);
                        if (cell.RegionIndex.HasValue) regionByIndex[cell.RegionIndex.Value].AddLandMark(landmark.Id);
                        if (!hasGroup) continue;
                        if (!authoredOrder.TryGetValue(cell.Id, out var ids))
                            authoredOrder.Add(cell.Id, ids = new int[1 + cell.AdditionalLandMarkDefineIds.Count]);
                        ids[slot] = landmark.Id;
                    }
                }
            }
            PrepareLandmarks(isNatural: true);
            prepared.NaturalSnapshot = AuraDaoMgr.PrepareSnapshot(plane);
            PrepareLandmarks(isNatural: false);
            foreach (var (cell, order) in authoredOrder) plane.GetTile(cell).OrderLandMarks(order);
            plane.GenerationResult = world;
            return prepared;
        }

        /// <summary>
        /// 为指定位面填充所有地块（Width × Height 格）。
        /// 若位面 Width 或 Height 为 0，则跳过生成（空位面）。
        /// 已存在的地块不会被覆盖（支持存档恢复后补全）。
        /// 
        /// 地块全部生成后若传入了 auraDaoMgr，则立即拍摄原初元气快照（在任何后天 Modifier 加入之前）。
        /// </summary>
        /// <param name="plane">目标位面</param>
        /// <param name="auraDaoMgr">天道管理器（可选），传入后在生成完毕时自动拍摄元气快照</param>
        public static void Fill(Plane plane, AuraDaoMgr? auraDaoMgr = null)
        {
            if (plane.Width <= 0 || plane.Height <= 0) return;

            for (int row = 0; row < plane.Height; row++)
            {
                for (int col = 0; col < plane.Width; col++)
                {
                    var axial = Plane.OffsetToAxial(col, row);
                    if (!plane.HasTile(axial))
                        plane.SetTile(new Tile(axial));
                }
            }

            // 所有自然地块生成完毕后，拍摄原初元气快照（此时尚无任何后天 Modifier）
            auraDaoMgr?.TakeSnapshot(plane);
        }
    }
}
