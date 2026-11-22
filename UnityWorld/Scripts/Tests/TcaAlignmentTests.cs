using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static int _assertions;

    // 用例按被验证的规则放进对应分文件，入口负责初始化、执行顺序和清理。
    public static void Run()
    {
        _assertions = 0;
        var dataDir = Path.GetDirectoryName(CardDefineMgr.Instance.DataDir)!;
        using var lua = new Lua();
        lua["API"] = APIMgr.Instance;
        lua["templateCard"] = new CombatCard();
        lua["templateNpc"] = new CombatNpc(0);
        foreach (var file in new[] { "CombatCard_Condition", "CombatCard_Action", "CombatCard_Aura", "CombatNpc_Action", "CombatNpc_Contest" })
            lua.DoString(File.ReadAllText(Path.Combine(dataDir, "LuaScripts", file + ".lua")));

        VerifyCatalog(lua, dataDir);
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(2, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        BackpackModelTests.Place(a, "bp_attack", 1, 0);
        BackpackModelTests.Place(a, "bp_focus", 2, 0);
        BackpackModelTests.Place(b, "bp_attack", 1, 0);
        var scene = new CombatScene();
        scene.Init(7351, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var opponent = scene.GetCombatants()[b.Id];
            VerifySelectors(lua, owner, opponent);
            VerifyResourceSettlement(lua, owner, opponent);
            VerifyContestEvents(owner, opponent);
            VerifyContestPair(lua, owner, opponent);
            VerifyDamageDefense(lua, owner, opponent);
            VerifyDeflection(lua, owner, opponent);
            VerifyContestBuffs(lua, owner);
            VerifyWoundSelection(lua, owner);
        }
        finally
        {
            scene.Cleanup();
        }
        VerifyNextStabOnScan(lua);
        VerifyRoundEffects(lua);
        VerifySimultaneousRoundEffects(lua);
        VerifyEquipmentAndAffinity(lua);
        VerifyArtifactPower(lua);
        VerifySpatialEffects(lua);
        VerifyRandomSlide(lua);
        VerifyAmmoInstances();
        VerifyAmmoScan(lua, flexible: false);
        VerifyAmmoScan(lua, flexible: true);
        VerifyRemovalOrder(lua);
        VerifyDefeatedPending();
        VerifyLastArtifactAtTimeout();
        VerifyConsumables(lua);
        VerifyTcaExtension(lua);
        Console.WriteLine($"TCA_ALIGNMENT_TESTS_OK assertions={_assertions}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TCA 对齐断言失败：" + message);
        _assertions++;
    }
}
