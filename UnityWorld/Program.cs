using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.Domain.Tag;
using UnityWorld.Game.World;

// ═══════════════════════════════════════════════════════════
//  UnityWorld —— 核心测试入口
//  旧秒 Tick 与游戏周历独立；测试入口不隐式换算秒、月或周。
// ═══════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 基准批次先用正式定义加载器解析模板和卡牌显示名，包括 Mod 提供的配置。
if (args.Length is 2 or 3 && args[0] == "--combat-benchmark-catalog"
    && (args.Length == 2 || args[2] == "--card-names"))
{
    UnityWorld.Core.LogMgr.MinLevel = UnityWorld.Core.LogLevel.Error;
    var logger = new UnityWorld.Core.LogMgr(0);
    logger.Init();
    var dataDir = Path.Combine(args[1], "Data");
    var npcDefines = new NpcDefineMgr(Path.Combine(dataDir, "Npc"));
    npcDefines.Load();
    var templates = npcDefines.GetAll().Select(npc => new { npc.ID, npc.DisplayName });
    if (args.Length == 3)
    {
        var cards = new CardDefineMgr(Path.Combine(dataDir, "Card"));
        cards.Load();
        var cardNames = cards.GetAll().ToDictionary(card => card.ID, card => card.DisplayName);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { templates, cardNames }));
    }
    else
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(templates));
    }
    Environment.ExitCode = UnityWorld.Core.LogMgr.History.Count == 0 ? 0 : 1;
    logger.End();
    return;
}

if (args.FirstOrDefault() == "--combat-benchmark-sample")
{
    Environment.ExitCode = UnityWorld.ConsoleHost.CombatBenchmark.Run(args);
    return;
}

if (args.Length == 2 && args[0] == "--combat-benchmark-checks")
{
    UnityWorld.ConsoleHost.CombatBenchmarkChecks.Run(args[1]);
    return;
}

// 移动的测试／演示有自己的生命周期，不能落入默认战斗或逐帧秒更新。
if (args.Contains("--npc-movement-demo"))
{
    Environment.ExitCode = UnityWorld.ConsoleHost.NpcMovementConsole.Run(args);
    return;
}
if (args.Contains("--npc-movement-tests"))
{
    try
    {
        if (args.Length != 1) throw new ArgumentException("--npc-movement-tests 必须单独使用。");
        UnityWorld.Tests.NpcMovementTests.Run();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("NPC_MOVEMENT_FAILED " + ex);
        Environment.ExitCode = 1;
    }
    return;
}

// 纯历法测试必须先分派，避免为时间换算加载 Lua 或生成整个世界。
if (args.Contains("--world-calendar-tests"))
{
    try
    {
        if (args.Length != 1) throw new ArgumentException("--world-calendar-tests 必须单独使用。");
        UnityWorld.Tests.WorldCalendarTests.Run();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("WORLD_CALENDAR_FAILED " + ex);
        Environment.ExitCode = 1;
    }
    return;
}

// 地图诊断先解析参数，仅在明确的运行时验证分支启动整个世界。
if (args.Any(a => a.StartsWith("--map-", StringComparison.Ordinal)))
{
    Environment.ExitCode = UnityWorld.ConsoleHost.MapConsole.Run(args);
    return;
}

bool smokeTest = args.Contains("--core-smoke-test");
bool backpackModelTests = args.Contains("--backpack-model-tests");
bool backpackTests = args.Contains("--backpack-tests");
bool timingTests = args.Contains("--backpack-timing-tests");
bool effectTests = args.Contains("--effect-tests");
bool activationTests = args.Contains("--activation-migration-tests");
bool templateTests = args.Contains("--template-tests");
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
    else if (activationTests)
    {
        UnityWorld.Tests.ActivationMigrationTests.Run();
    }
    else if (templateTests)
    {
        UnityWorld.Tests.TemplateBackpackTests.Run();
    }
    else if (effectTests)
    {
        UnityWorld.Tests.EffectMigrationTests.Run();
    }
    else if (args.Contains("--tca-tests"))
    {
        UnityWorld.Tests.TcaAlignmentTests.Run();
    }
    else if (timingTests)
    {
        UnityWorld.Tests.BackpackTimingTests.Run();
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
