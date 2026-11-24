using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
public class APIContext : ContextBase
{
    /// <summary>本次效果的施术者</summary>
    public CombatNpc Caster { get; set; }
    /// <summary>本次效果的来源卡</summary>
    public CombatCard SourceCard { get; set; }
    /// <summary>本次效果的来源状态</summary>
    public CombatNpcModifier SourceBuff { get; set; }
    /// <summary>当前战斗场景</summary>
    public CombatScene Scene { get; set; }
    /// <summary>同一次实际事件在卡、Keyword、EventMgr 通道共享身份。</summary>
    public long EventId { get; internal set; }
    /// <summary>所属事件链编号</summary>
    public long ChainId { get; internal set; }
    /// <summary>当前分发的事件名</summary>
    public string EventName { get; internal set; } = "";
    /// <summary>主动效果的直接触发事件；Caster/SourceCard 仍属于本次发动卡。</summary>
    public APIContext TriggerEvent { get; internal set; }
    /// <summary>已在本事件成功响应的卡牌编号</summary>
    internal HashSet<int> SuccessfulEventCards { get; } = new();
    /// <summary>本条发动路径经过的卡牌编号</summary>
    internal List<int> ActivationPath { get; set; } = new();
    /// <summary>事件发生时各卡牌的占格</summary>
    internal IReadOnlyDictionary<int, IReadOnlyList<BackpackCell>> EventPlacements { get; set; }

    /// <summary>复制当前上下文，供事件分发使用</summary>
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

    /// <summary>比较事件的目标、触发参数及占格快照</summary>
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
    /// <summary>本次效果选中的卡牌</summary>
    public List<CombatCard> CardTargets { get; set; } = new();

    /// <summary>用来源卡及其持有者建立上下文</summary>
    public APIContext CreateContextFromCard(CombatCard SourceCard,CombatScene scene) { 
        return new APIContext
        {
            SourceCard = SourceCard,
            Caster = SourceCard.Owner,
            Scene = scene,
        };
    }
    /// <summary>输出上下文的参数与目标信息</summary>
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
