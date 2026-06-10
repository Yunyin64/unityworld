using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
public class APIContext : ContextBase
{
    public CombatNpc Caster { get; set; }
    public CombatCard SourceCard { get; set; }
    public CombatNpcModifier SourceBuff { get; set; }
    public CombatScene Scene { get; set; }
    /// <summary>
    /// 要通讯的目标
    /// </summary>
    public List<CombatNpc> NpcTargets { get; set; } = new();
    public List<CombatCard> CardTargets { get; set; } = new();

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
                LogMgr.Instance.Dbg(sb.ToString());
                return sb.ToString();
        }
}
    
}
