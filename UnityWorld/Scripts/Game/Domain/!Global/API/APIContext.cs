using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
public class APIContext : ContextBase
{
    public CombatNpc Caster { get; set; }
    public CombatCard SourceCard { get; set; }
    public CombatNpcModifier SourceBuff { get; set; }
    public CombatScene Scene { get; set; }
    /// <summary>同一次实际事件在卡、Keyword、EventMgr 通道共享身份。</summary>
    public long EventId { get; internal set; }
    public long ChainId { get; internal set; }
    public string EventName { get; internal set; } = "";
    /// <summary>主动效果的直接触发事件；Caster/SourceCard 仍属于本次发动卡。</summary>
    public APIContext TriggerEvent { get; internal set; }
    internal HashSet<int> SuccessfulEventCards { get; } = new();
    internal List<int> ActivationPath { get; set; } = new();
    internal IReadOnlyDictionary<int, IReadOnlyList<BackpackCell>> EventPlacements { get; set; }

    internal APIContext CopyForEvent()
    {
        var copy = new APIContext
        {
            Caster = Caster, SourceCard = SourceCard, SourceBuff = SourceBuff, Scene = Scene,
            TriggerEvent = TriggerEvent, NpcTargets = [.. NpcTargets], CardTargets = [.. CardTargets], EventPlacements = EventPlacements
        };
        foreach (var pair in _causes) copy.Set(pair.Key, pair.Value);
        return copy;
    }

    internal bool HasSameEventPayload(APIContext other)
    {
        if (!NpcTargets.SequenceEqual(other.NpcTargets) || !CardTargets.SequenceEqual(other.CardTargets) ||
            _causes.Count != other._causes.Count ||
            _causes.Any(pair => !other._causes.TryGetValue(pair.Key, out var value) || !Equals(pair.Value, value)))
            return false;

        if (ReferenceEquals(EventPlacements, other.EventPlacements)) return true;
        return EventPlacements != null && other.EventPlacements != null && EventPlacements.Count == other.EventPlacements.Count &&
            EventPlacements.All(pair => other.EventPlacements.TryGetValue(pair.Key, out var cells) && pair.Value.SequenceEqual(cells));
    }

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
