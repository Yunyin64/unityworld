namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC核心实体（轻量句柄，真实数据由各子系统管理）
    /// </summary>
    public class Npc
    {
        /// <summary>唯一ID</summary>
        public NpcId Id { get; }

        /// <summary>属性集合（所有数值属性统一在此管理�?/summary>
        public StatBlock Stats { get; } = new();

        public Npc(NpcId id)
        {
            Id = id;
        }

        public NpcBioData? Bio           => NpcMgr.Instance?.BioSystem.GetBio(Id);
        public string? Name              => NpcMgr.Instance?.NameSystem.GetName(Id);
        public IEnumerable<string>? Roles => NpcMgr.Instance?.RoleSystem.GetRoles(Id);
        public NpcPositionData? Position => NpcMgr.Instance?.PositionSystem.GetPosition(Id);

        /// <summary>当前持有的所有 Trait 实例（只读快照）</summary>
        public IReadOnlyCollection<Trait>? Traits => NpcMgr.Instance?.TraitSystem.GetTraits(Id);

        /// <summary>获取指定 Trait 实例（不持有返回 null）</summary>
        public Trait? GetTrait(TraitId traitId) => NpcMgr.Instance?.TraitSystem.GetTrait(Id, traitId);

        /// <summary>是否持有指定 Trait</summary>
        public bool HasTrait(TraitId traitId) => NpcMgr.Instance?.TraitSystem.HasTrait(Id, traitId) ?? false;

        // ── 移动 ─────────────────────────────────────────

        /// <summary>
        /// 朝六边形地图的指定方向走一步（0=右，顺时针 0~5）。
        /// 会校验目标格是否在位面边界内。
        /// </summary>
        /// <param name="direction">方向 0~5</param>
        /// <returns>移动结果（Success / OutOfBounds / NotFound）</returns>
        public NpcTypes.MoveResult Move(int direction)
            => NpcMgr.Instance?.PositionSystem.TryMove(Id, direction) ?? NpcTypes.MoveResult.NotFound;

        public override bool Equals(object? obj) => obj is Npc npc && Id.Equals(npc.Id);
        public override int GetHashCode() => Id.GetHashCode();
        
        public override string ToString() => $"Npc({Id.Value})";
    }
}