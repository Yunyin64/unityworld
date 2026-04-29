using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
public class APIContext : ContextBase
{
    public CombatNpc Caster { get; set; }
    public CombatCard SourceCard { get; set; }
    public CombatScene Scene { get; set; }

    public APIContext CreateContextFromCard(CombatCard SourceCard,CombatScene scene) { 
        return new APIContext
        {
            SourceCard = SourceCard,
            Caster = SourceCard.Owner,
            Scene = scene,
        };
    }
}
    
}
