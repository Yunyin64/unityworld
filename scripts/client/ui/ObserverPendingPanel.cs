using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>把两人的实际待发牌与飞行事件交给舞台层和表现层。</summary>
internal sealed class ObserverPendingPanel
{
    private readonly (PendingSnapshot? Card, PresentationCue? Flight)[] _entries =
        new (PendingSnapshot?, PresentationCue?)[2];

    internal PendingSnapshot? Card(int side) => _entries[side].Card;
    internal PresentationCue? Flight(int side) => _entries[side].Flight;

    internal void Submit(IReadOnlyList<NpcSnapshot> participants, IReadOnlyList<PresentationCue> cues)
    {
        for (int side = 0; side < _entries.Length; side++)
        {
            _entries[side] = default;
            if (side >= participants.Count || participants[side].Pending is not { } card) continue;
            int npcId = participants[side].Id;
            var flight = cues.FirstOrDefault(c => c.Event.NpcId == npcId && c.Event.Pending is { Entered: true } &&
                c.Event.Card is { Cells.Count: > 0 } source && source.Id == card.CardId);
            _entries[side] = (card, flight);
        }
    }
}
