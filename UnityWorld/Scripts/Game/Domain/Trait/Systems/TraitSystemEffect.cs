namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait 效果系统：负责在 Trait 到期时从 NpcSystemTrait 中移除该 Trait，
    /// 并同步撤销对 StatBlock 的修改
    /// </summary>
    public class TraitSystemEffect : TraitSystemBase
    {

        public override void OnTick(Trait trait, float deltaTime)
        {
            if (NpcMgr.Instance == null) return;
            if (!trait.IsExpired) return;

            // Trait 已到期，从 NPC 身上移除
            var npc = NpcMgr.Instance.GetById(trait.OwnerId);
            if (npc != null)
                NpcMgr.Instance.TraitSystem.RemoveTrait(npc, trait.Id);
        }
    }
}
