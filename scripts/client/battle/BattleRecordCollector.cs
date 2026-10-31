using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace Xianxia.Client.Battle;

/// <summary>旧玩法事件和原始战报的值副本；仅用于记录，不驱动表现动画。</summary>
internal sealed class BattleRecordCollector : IDisposable
{
    private static readonly string[] EventNames =
    {
        "OnUse", "OnApply", "OnStraight", "OnAttack", "OnDefend", "OnDamage", "OnHeal",
        "OnBaseManaDraw", "OnManaDraw", "OnMPChanged", "OnContestOverflow"
    };
    private static readonly IReadOnlyDictionary<string, string> EmptyFields =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    private readonly List<BattleRecord> _records = new();
    private readonly CombatScene _scene;
    private DelegateEventListener? _listener;
    private EventMgr? _eventManager;
    private int _traceOffset;
    private long _sequence;
    public string? Error { get; private set; }
    public IReadOnlyList<BattleRecord> Records { get; }
    public BattleRecordCollector(CombatScene scene)
    {
        _scene = scene;
        Records = _records.AsReadOnly();
    }
    public void AddDiagnostic(string message) => _records.Add(new(++_sequence, _scene.CurrentTick,
        "diagnostic", "Error", message, null, null, null, null, EmptyFields));
    public void Subscribe()
    {
        _eventManager = EventMgr.Instance;
        _listener = new DelegateEventListener(OnCoreEvent);
        foreach (var name in EventNames)
            _eventManager.RegisterEvent("ClientBattleObserver", name, ScopeKey.Global, _listener);
    }

    private void OnCoreEvent(string eventName, ScopeKey scope, object args)
    {
        if (args is not APIContext ctx || !ReferenceEquals(ctx.Scene, _scene)) return;
        try
        {
            FlushTrace();
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            if (ctx.Caster != null) fields["casterName"] = ctx.Caster.GetName();
            if (ctx.SourceCard != null)
            {
                fields["sourceDefineId"] = ctx.SourceCard.DefineId;
                fields["sourceName"] = ctx.SourceCard.DisplayName;
            }
            // Copy only payloads the core actually supplied. No formula, inferred damage, or guessed pairing.
            foreach (var key in new[] { "Winner", "Loser", "Overflow", "WinnerType", "LoserType", "WinnerCard", "LoserCard" })
            {
                var value = ctx.GetObject(key);
                var copied = value switch
                {
                    CombatNpc npc => npc.Id.ToString(CultureInfo.InvariantCulture),
                    CombatCard card => card.Id.ToString(CultureInfo.InvariantCulture),
                    Enum e => e.ToString(),
                    int n => n.ToString(CultureInfo.InvariantCulture),
                    float n => n.ToString(CultureInfo.InvariantCulture),
                    double n => n.ToString(CultureInfo.InvariantCulture),
                    string s => s,
                    _ => null
                };
                if (copied != null) fields[key] = copied;
            }
            var text = eventName + (fields.TryGetValue("casterName", out var caster) ? " · " + caster : "")
                + (fields.TryGetValue("sourceName", out var source) ? " · " + source : "");
            _records.Add(new(++_sequence, _scene!.CurrentTick, "event", eventName, text,
                ctx.EventId, ctx.ChainId, ctx.Caster?.Id, ctx.SourceCard?.Id,
                new ReadOnlyDictionary<string, string>(fields)));
        }
        catch (Exception ex) { Error ??= "观察事件复制失败：" + ex; }
    }

    public void FlushTrace()
    {
        if (_scene == null) return;
        var trace = _scene.TraceLog;
        while (_traceOffset < trace.Count)
            _records.Add(new(++_sequence, _scene.CurrentTick, "trace", "TraceLog", trace[_traceOffset++],
                null, null, null, null, EmptyFields));
    }

    public void Dispose()
    {
        if (_eventManager != null && _listener != null)
            foreach (var name in EventNames) _eventManager.RemoveEvent(name, ScopeKey.Global, _listener);
        _listener = null;
    }
}
