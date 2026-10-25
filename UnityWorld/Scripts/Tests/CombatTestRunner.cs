using UnityWorld.Core;
using UnityWorld.Tests;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>固定种子、固定布局演示两种主功法各自的扫描节奏。</summary>
    public static class CombatTestRunner
    {
        public static void RunBasicTest()
        {
            var a = BackpackModelTests.CreateNpc("bp_main_6x5");
            var b = BackpackModelTests.CreateNpc("bp_main_8x3");
            BackpackModelTests.Place(a, "bp_attack", 2, 0);
            BackpackModelTests.Place(a, "bp_charge", 4, 1);
            BackpackModelTests.Place(b, "bp_guard", 2, 0);
            BackpackModelTests.Place(b, "bp_focus", 4, 1);
            foreach (var npc in new[] { a, b })
            {
                npc.Stats.SetBase("AffinityJin", 100);
                foreach (var name in new[] { "Mu", "Shui", "Huo", "Tu" }) npc.Stats.SetBase("Affinity" + name, 0);
            }
            LogMgr.Instance.Dbg("背包扫描演示：6×5每50Tick一轮 / 8×3每30Tick一轮，每10Tick逐行扫描；种子12345。");
            var scene = new CombatScene();
            scene.Init(12345, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 150);
            var result = scene.Run();
            LogMgr.Instance.Dbg($"结束原因={result.GetValue<CombatEndReason>("EndReason")} 胜方={result.GetValue<CombatTeam>("WinnerTeam")} Tick={result.GetValue<int>("TotalTicks")}");
        }
    }
}
