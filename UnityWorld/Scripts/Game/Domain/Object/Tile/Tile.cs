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

        // ── 五行元气 ──────────────────────────────────────

        /// <summary>地块当前五行元气浓度（实时值，由 TileSystemAura 每 Tick 根据修正源累积更新）</summary>
        public TileAura CurrentAura { get; } = new();

        // ── 修正源 ────────────────────────────────────────

        /// <summary>作用于本地块的修正源列表（地标、NPC 影响、事件等）</summary>
        public List<TileModifier> Modifiers { get; } = new();

        // ── 预留扩展区 ────────────────────────────────────
        // TODO: 地形类型（平原、山地、河流、海洋、林地等）
        // TODO: 建筑/地标（城镇、宗门、洞府等）
        // TODO: 资源节点（矿脉、灵草地等）
        // TODO: 是否可通行

        // ── 构造 ──────────────────────────────────────────

        public Tile(TileId id)
        {
            Id = id;
        }

        public Tile(TileId id, TileAura currentAura)
        {
            Id = id;
            CurrentAura.Jing  = currentAura.Jing;
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

        // ── 工具 ──────────────────────────────────────────

        public override string ToString() => $"Tile({Id}, {CurrentAura})";
    }
}