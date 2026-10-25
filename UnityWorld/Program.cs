using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.Domain.Tag;
using UnityWorld.Game.World;

// ═══════════════════════════════════════════════════════════
//  UnityWorld —— NPC系统实时Tick测试
//  规则：现实时间 1秒 = 游戏世界 1个月
//        每过1游戏年（12个月）打印一次状态
// ═══════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool smokeTest = args.Contains("--core-smoke-test");
bool backpackModelTests = args.Contains("--backpack-model-tests");
bool backpackTests = args.Contains("--backpack-tests");
try
{
    WorldMgr.Initialize(12345);
    WorldMgr.Start();
    if (smokeTest)
    {
        for (int i = 0; i < 3; i++) WorldMgr.Tick(1f / 60f);
        if (NpcDefineMgr.Instance.Get("test_npc_a") == null || PlaneMgr.Instance.MainPlane.Tiles.Count == 0)
            throw new InvalidOperationException($"核心数据未成功加载：NPC={NpcDefineMgr.Instance.Get("test_npc_a") != null}, tiles={PlaneMgr.Instance.MainPlane.Tiles.Count}");
        Console.WriteLine($"CORE_SMOKE_OK seed={WorldTime.RandomSeed} ticks={WorldTime.CurrentTick} tiles={PlaneMgr.Instance.MainPlane.Tiles.Count}");
    }
    else if (backpackModelTests || backpackTests)
    {
        UnityWorld.Tests.BackpackModelTests.Run();
        if (backpackTests) UnityWorld.Tests.BackpackCombatTests.Run();
    }
    else
    {
        CombatTestRunner.RunBasicTest();
    }
}
finally
{
    WorldMgr.End();
}
