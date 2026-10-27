using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>从正式 NPC JSON 装配真实卡牌和 Lua 的背包迁移演示。</summary>
    public static class CombatTestRunner
    {
        public static void RunBasicTest()
        {
            var a = NpcMgr.Instance.Assemble(NpcDefineMgr.Instance.Get("test_npc_a"));
            var b = NpcMgr.Instance.Assemble(NpcDefineMgr.Instance.Get("test_npc_b"));
            LogMgr.Instance.Dbg("真实模板背包战斗：test_npc_a / test_npc_b；主功法与位置直接来自JSON，种子12345。");
            var scene = new CombatScene();
            try
            {
                scene.Init(12345, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 5000);
                var result = scene.Run();
                if (result.GetValue<CombatEndReason>("EndReason") != CombatEndReason.AllDefeated ||
                    result.GetValue<CombatTeam>("WinnerTeam") == CombatTeam.None)
                    throw new InvalidOperationException("真实模板演示未完成明确胜负，检查模板资源与数值。");
                Console.WriteLine($"TEMPLATE_BATTLE_OK reason={result.GetValue<CombatEndReason>("EndReason")} winner={result.GetValue<CombatTeam>("WinnerTeam")} ticks={result.GetValue<int>("TotalTicks")}");
            }
            finally { scene.Cleanup(); }
        }
    }
}
