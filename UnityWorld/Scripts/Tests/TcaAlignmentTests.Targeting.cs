using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifySelectors(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var ctx = new APIContext { Scene = owner.Scene, Caster = owner, SourceCard = owner.GetField()[0] };
        using var select = lua.GetFunction("SelectCards");
        var all = (List<CombatCard>)select.Call(ctx, "All")[0];
        Check(all.SequenceEqual(owner.GetField()), "选中全部己方场上卡");
        all.Clear();
        Check(owner.GetField().Count > 1, "修改结果不会修改场上列表");
        var adjacent = (List<CombatCard>)select.Call(ctx, "Adjacent")[0];
        Check(adjacent.SequenceEqual(owner.GetField().Skip(1).Take(1)), "场上顺序首位只返回后一张");
        ctx.SourceCard = owner.GetField()[1];
        adjacent = (List<CombatCard>)select.Call(ctx, "Adjacent")[0];
        Check(adjacent.SequenceEqual([owner.GetField()[0], owner.GetField()[2]]), "场上顺序中间返回前后卡");
        var enemies = (List<CombatCard>)select.Call(ctx, "TargetAll")[0];
        Check(enemies.SequenceEqual(opponent.GetField()), "对手全选只返回该对手场上卡");
        var random = (List<CombatCard>)select.Call(ctx, "TargetRandom")[0];
        Check(random.Count == 1 && enemies.Contains(random[0]), "随机选取一张真实对手卡");
        var picked = random[0];
        random.Clear();
        Check(APIMgr.Instance.GetTargetCard("Selected", ctx).SequenceEqual([picked]),
            "选中的随机卡交给动作使用，无需再随机且不受返回副本影响");
        ctx.Caster = null;
        Check(((List<CombatCard>)select.Call(ctx, "TargetAll")[0]).Count == 0
            && APIMgr.Instance.GetTargetCard("Selected", ctx).Count == 0, "空目标清除之前的选中目标");
    }

    private static void VerifyWoundSelection(Lua lua, CombatNpc owner)
    {
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        using var remove = lua.GetFunction("RemoveRandomWound");
        var small = owner.AddWound("bp_wound_small");
        owner.DealFieldChange();
        remove.Call(ctx, "Self", 2, true);
        owner.DealFieldChange();
        Check(owner.GetField().Contains(small), "精确移除 2 格伤口时保留 1 格伤口");
        remove.Call(ctx, "Self", 2, false);
        owner.DealFieldChange();
        Check(!owner.GetField().Contains(small), "至多 2 格的移除可选择 1 格伤口");
    }
}
