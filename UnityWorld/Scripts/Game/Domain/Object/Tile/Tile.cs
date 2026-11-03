using System.Collections.Generic;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块实体：六边形网格中的最小空间单元
    /// 持有实时五行元气（CurrentAura）和作用于本地块的修正源列表（Modifiers）
    /// </summary>
    public class Tile
    {
        // ── 基本信息 ──────────────────────────────────────

        /// <summary>地块唯一坐标ID（Axial 坐标）</summary>
        public TileId Id { get; }

        /// <summary>独立地域类型 ID，与共同筛选标签分别表达。</summary>
        public string TypeId { get; internal set; } = "plain";
        /// <summary>唯一生成来源，固定边界不归入无地区的基础填充。</summary>
        public TileGenerationSource Source { get; internal set; } = TileGenerationSource.Fill;
        public int? RegionId { get; internal set; }
        public int? LandMarkId { get; internal set; }
        public bool IsFixed { get; internal set; }
        public bool HasForcedLandmark { get; internal set; }
        public MapEnvironment Environment { get; internal set; } = new();
        private MapWater _water = new();
        /// <summary>Detached read-only projection: callers may inspect/copy it without mutating the world.</summary>
        public MapWater Water { get => _water.Copy(); internal set => _water = value.Copy(); }
        public bool BlockAllMovement { get; internal set; }
        public IReadOnlyList<string> AllowedMovement { get; internal set; } = Array.Empty<string>();

        // ── 五行元气 ──────────────────────────────────────

        /// <summary>地块当前五行元气浓度（实时值，由 TileSystemAura 每 Tick 根据修正源累积更新）</summary>
        public TileAura CurrentAura { get; } = new();

        // ── 修正源 ────────────────────────────────────────

        /// <summary>作用于本地块的修正源列表（地标、NPC 影响、事件等）</summary>
        public List<TileModifier> Modifiers { get; } = new();


        // ── 构造 ──────────────────────────────────────────

        public Tile(TileId id)
        {
            Id = id;
        }

        public Tile(TileId id, TileAura currentAura)
        {
            Id = id;
            CurrentAura.Jin  = currentAura.Jin;
            CurrentAura.Mu    = currentAura.Mu;
            CurrentAura.Shui  = currentAura.Shui;
            CurrentAura.Huo   = currentAura.Huo;
            CurrentAura.Tu    = currentAura.Tu;
        }

        // ── 修正源管理 ────────────────────────────────────

        /// <summary>向本地块添加一个修正源</summary>
        public void AddModifier(TileModifier modifier)
        {
            Modifiers.Add(modifier);
        }

        /// <summary>从本地块移除一个修正源</summary>
        public void RemoveModifier(TileModifier modifier)
        {
            Modifiers.Remove(modifier);
        }

        /// <summary>移除指定来源的全部未来速率，不撤销已经累积的元气。</summary>
        public int RemoveModifiersFromSource(string sourceId)
            => Modifiers.RemoveAll(modifier => modifier.SourceId == sourceId);

        // ── 工具 ──────────────────────────────────────────

        public override string ToString() => $"Tile({Id}, {CurrentAura})";
    }
}
