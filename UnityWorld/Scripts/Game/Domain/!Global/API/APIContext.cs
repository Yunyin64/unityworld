using UnityWorld.Core;
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
    public override string LogAllInfo()
        {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("APIContext:");
                foreach (var kvp in _causes)
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
                    sb.AppendLine($"  Caster: {Caster}");
                    sb.AppendLine($"  SourceCard: {SourceCard}");
                LogMgr.Dbg(sb.ToString());
                return sb.ToString();
        }
}
    
}
