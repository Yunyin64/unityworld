using System.Collections.Generic;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>一次完整逻辑步的值包。空事件步也提交；显示帧率不参与编号。</summary>
public sealed record BattlePresentationFrame(int SchemaVersion, string SessionId, int Tick, bool IsInitialization,
    BattleSnapshot Snapshot, IReadOnlyList<CombatPresentationEvent> Events, long FirstSequence, long LastSequence)
{
    public const int CurrentSchemaVersion = 1;
}
