using System.Collections.Generic;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 创建位面时的配置参数，传给 PlaneMgr.CreatePlane 使用
    /// </summary>
    public class PlaneConfig
    {
        /// <summary>位面名称（必填）</summary>
        public string Name { get; set; } = "未命名位面";

        /// <summary>位面类型，默认小世界</summary>
        public PlaneTypes.PlaneKind Kind { get; set; } = PlaneTypes.PlaneKind.SubPlane;

        /// <summary>
        /// 地图宽度（列数）。由 PlaneDefine 填入，0 表示未指定（空位面）。
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// 地图高度（行数）。由 PlaneDefine 填入，0 表示未指定（空位面）。
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>时间流速倍率，默认 1.0（与主世界同步）</summary>
        public float TimeFlowRate { get; set; } = 1f;

        /// <summary>位面级五行元气加成，默认全 0</summary>
        public TileAura? AuraBonus { get; set; } = null;

        /// <summary>
        /// 预填地块列表（可选）。
        /// 传入后会在位面创建时一次性写入，适合从模板/存档加载。
        /// </summary>
        public IEnumerable<Tile>? InitialTiles { get; set; } = null;

        // ── 快捷工厂方法 ────────────────────────────────────

        /// <summary>创建主世界配置</summary>
        public static PlaneConfig ForMainPlane(string name = "主世界") => new()
        {
            Name = name,
            Kind = PlaneTypes.PlaneKind.MainPlane,
            TimeFlowRate = 1f,
        };

        /// <summary>创建小世界配置</summary>
        public static PlaneConfig ForSubPlane(string name, float timeFlowRate = 1f, TileAura? auraBonus = null) => new()
        {
            Name         = name,
            Kind         = PlaneTypes.PlaneKind.SubPlane,
            TimeFlowRate = timeFlowRate,
            AuraBonus    = auraBonus,
        };
    }

    /// <summary>
    /// 位面核心实体：代表世界中一张独立的空间地图。
    /// 由六边形地块组成，持有属于本位面的NPC引用列表，坐标空间独立。
    /// </summary>
    public class Plane
    {
        // ── 基本信息 ──────────────────────────────────────

        /// <summary>唯一ID</summary>
        public PlaneId Id { get; }

        /// <summary>位面名称（如"剑冢秘境"、"东荒大陆"）</summary>
        public string Name { get; set; }

        /// <summary>位面种类</summary>
        public PlaneTypes.PlaneKind Kind { get; }

        /// <summary>位面当前状态</summary>
        public PlaneTypes.PlaneState State { get; private set; } = PlaneTypes.PlaneState.Active;

        // ── 地图尺寸 ──────────────────────────────────────

        /// <summary>
        /// 地图宽度（列数，即 Offset 坐标的 col 范围 [0, Width-1]）。
        /// 0 表示未定义（空位面，不限制坐标范围）。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 地图高度（行数，即 Offset 坐标的 row 范围 [0, Height-1]）。
        /// 0 表示未定义（空位面，不限制坐标范围）。
        /// </summary>
        public int Height { get; }

        /// <summary>地块总格数（Width × Height，未定义时为 0）</summary>
        public int Capacity => Width * Height;

        // ── 时间 ──────────────────────────────────────────

        /// <summary>
        /// 本位面时间流速倍率（相对于世界主时间）。
        /// 默认 1.0；小世界可设为 10.0 表示内部时间快10倍。
        /// </summary>
        public float TimeFlowRate { get; set; } = 1f;

        // ── 位面级元气加成 ─────────────────────────────────

        /// <summary>
        /// 位面对所有地块的五行元气加成（叠加到地块 BaseAura 上）。
        /// 默认全为 0。
        /// </summary>
        public TileAura PlaneAuraBonus { get; } = new()
        {
            Metal = 0f, Wood = 0f, Water = 0f, Fire = 0f, Earth = 0f
        };

        // ── 地块 ──────────────────────────────────────────

        private readonly Dictionary<TileId, Tile> _tiles = new();

        /// <summary>本位面所有地块（只读视图）</summary>
        public IReadOnlyDictionary<TileId, Tile> Tiles => _tiles;

        /// <summary>当前地块数量</summary>
        public int TileCount => _tiles.Count;

        // ── NPC列表 ───────────────────────────────────────

        private readonly HashSet<NpcId> _npcIds = new();

        /// <summary>本位面所有NPC的ID（只读快照）</summary>
        public IReadOnlyCollection<NpcId> NpcIds => _npcIds;

        /// <summary>本位面当前NPC数量</summary>
        public int NpcCount => _npcIds.Count;

        // ── 构造 ──────────────────────────────────────────

        /// <summary>通过 PlaneConfig 构造（推荐通过 PlaneMgr.CreatePlane 创建）</summary>
        internal Plane(PlaneId id, PlaneConfig config)
        {
            Id           = id;
            Name         = config.Name;
            Kind         = config.Kind;
            Width        = config.Width;
            Height       = config.Height;
            TimeFlowRate = config.TimeFlowRate;

            if (config.AuraBonus != null)
            {
                PlaneAuraBonus.Metal = config.AuraBonus.Metal;
                PlaneAuraBonus.Wood  = config.AuraBonus.Wood;
                PlaneAuraBonus.Water = config.AuraBonus.Water;
                PlaneAuraBonus.Fire  = config.AuraBonus.Fire;
                PlaneAuraBonus.Earth = config.AuraBonus.Earth;
            }

            if (config.InitialTiles != null)
                foreach (var tile in config.InitialTiles)
                    _tiles[tile.Id] = tile;
        }

        // ── 坐标工具 ──────────────────────────────────────

        /// <summary>
        /// 将 Offset 行列坐标（视觉长方形，奇偶行交错）转换为 Axial(q, r) 坐标。
        /// 采用「奇数行右移」（odd-r）布局：偶数行对齐左侧，奇数行向右偏移半格。
        /// </summary>
        /// <param name="col">列下标 [0, Width-1]</param>
        /// <param name="row">行下标 [0, Height-1]</param>
        public static TileId OffsetToAxial(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return new TileId(q, r);
        }

        /// <summary>
        /// 将 Axial(q, r) 坐标反转换为 Offset 行列坐标。
        /// </summary>
        public static (int col, int row) AxialToOffset(TileId id)
        {
            int row = id.R;
            int col = id.Q + (id.R - (id.R & 1)) / 2;
            return (col, row);
        }

        /// <summary>
        /// 判断 Axial 坐标是否在本位面的 Offset 长方形范围内。
        /// 若 Width/Height 为 0（未定义），始终返回 true。
        /// </summary>
        public bool IsInBounds(TileId id)
        {
            if (Width == 0 || Height == 0) return true;
            var (col, row) = AxialToOffset(id);
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }

        // ── 地块管理 ──────────────────────────────────────

        /// <summary>添加或覆盖一个地块</summary>
        public void SetTile(Tile tile) => _tiles[tile.Id] = tile;

        /// <summary>获取指定坐标的地块，不存在则返回 null</summary>
        public Tile? GetTile(TileId id) => _tiles.TryGetValue(id, out var t) ? t : null;

        /// <summary>移除指定坐标的地块</summary>
        public bool RemoveTile(TileId id) => _tiles.Remove(id);

        /// <summary>判断指定坐标是否有地块</summary>
        public bool HasTile(TileId id) => _tiles.ContainsKey(id);

        /// <summary>
        /// 计算某地块的有效元气（地块基础值 + 位面加成）
        /// </summary>
        public TileAura GetEffectiveAura(TileId id)
        {
            var effective = new TileAura();
            if (_tiles.TryGetValue(id, out var tile))
            {
                effective.Metal = tile.BaseAura.Metal + PlaneAuraBonus.Metal;
                effective.Wood  = tile.BaseAura.Wood  + PlaneAuraBonus.Wood;
                effective.Water = tile.BaseAura.Water + PlaneAuraBonus.Water;
                effective.Fire  = tile.BaseAura.Fire  + PlaneAuraBonus.Fire;
                effective.Earth = tile.BaseAura.Earth + PlaneAuraBonus.Earth;
            }
            return effective;
        }

        // ── NPC管理 ───────────────────────────────────────

        /// <summary>将NPC加入本位面</summary>
        public bool AddNpc(NpcId npcId) => _npcIds.Add(npcId);

        /// <summary>将NPC从本位面移除</summary>
        public bool RemoveNpc(NpcId npcId) => _npcIds.Remove(npcId);

        /// <summary>判断NPC是否在本位面</summary>
        public bool ContainsNpc(NpcId npcId) => _npcIds.Contains(npcId);

        // ── 状态控制 ──────────────────────────────────────

        /// <summary>封印位面（暂停，保留数据）</summary>
        public void Seal()
        {
            if (State == PlaneTypes.PlaneState.Active)
                State = PlaneTypes.PlaneState.Sealed;
        }

        /// <summary>解封位面</summary>
        public void Unseal()
        {
            if (State == PlaneTypes.PlaneState.Sealed)
                State = PlaneTypes.PlaneState.Active;
        }

        /// <summary>标记为已销毁</summary>
        internal void MarkDestroyed() => State = PlaneTypes.PlaneState.Destroyed;

        // ── 工具 ──────────────────────────────────────────

        public override string ToString()
            => $"Plane({Id.Value}, \"{Name}\", {Kind}, x{TimeFlowRate}, tiles={TileCount}, npcs={NpcCount})";
    }
}