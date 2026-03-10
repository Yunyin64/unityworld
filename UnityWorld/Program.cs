using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

// ═══════════════════════════════════════════════════════════
//  UnityWorld —— NPC系统实时Tick测试
//  规则：现实时间 1秒 = 游戏世界 1个月
//        每过1游戏年（12个月）打印一次状态
// ═══════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ① 初始化世界
WorldMgr.Initialize(seed: 42);
WorldMgr.Start();
Console.WriteLine("=== 世界初始化完成 ===\n");

// ② 创建NPC
var farmerDefine = NpcDefine.HumanFarmer();
var cultivatorDefine = NpcDefine.CultivatorWanderer();

var npc1 = NpcMgr.Instance.Create(farmerDefine, x: 10, y: 20);
var npc2 = NpcMgr.Instance.Create(farmerDefine, x: 15, y: 22);
var npc3 = NpcMgr.Instance.Create(cultivatorDefine, x: 100, y: 200);

Console.WriteLine($"=== 创建了 {NpcMgr.Instance.Count} 个NPC ===\n");
PrintAllNpcs(year: 0);

// ③ 给修士加"洞天秘境"加速效果（时间流速 ×2）
Console.WriteLine("\n=== 修士进入洞天秘境（时间流速 ×2）===\n");
npc3.Stats.AddModifier(StatId.TimeFlowRate, StatModifier.Flat(1f, sourceId: "secret_realm"));

// ── 时间参数 ────────────────────────────────────────────────
// 现实 1秒 = 游戏 1个月 = 游戏 (1/12) 年
// 每月一个Tick，dt = 1/12 年/Tick
const float dtPerTick = 1f / 12f;          // 游戏时间步长（年）
const int ticksPerYear = 12;               // 每游戏年12个Tick
const int intervalMs = 1000 / ticksPerYear; // 每Tick对应的现实毫秒数（≈83ms）

int monthAccumulator = 0;                  // 已走过的游戏月数
int gameYear = 0;                          // 当前游戏年

Console.WriteLine("=== 开始实时模拟（Ctrl+C 退出）===\n");

// ── 实时Tick循环 ─────────────────────────────────────────────
while (monthAccumulator < 360) // 模拟30年（360个月）
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    WorldMgr.Tick(dtPerTick);
    monthAccumulator++;

    // 每12个月（1年）打印一次
    if (monthAccumulator % ticksPerYear == 0)
    {
        gameYear++;
        PrintAllNpcs(gameYear);

        // 30年后修士离开秘境（演示modifier移除）
        if (gameYear == 30)
        {
            Console.WriteLine("\n  >>> 修士离开洞天秘境，时间流速恢复 ×1.0\n");
            npc3.Stats.RemoveModifiersBySource("secret_realm");
        }
    }

    // 精确等待到下一Tick（补偿执行时间）
    sw.Stop();
    int sleepMs = intervalMs - (int)sw.ElapsedMilliseconds;
    if (sleepMs > 0)
        await Task.Delay(sleepMs);
}

// ───────────────────────────────────────────────────────────
void PrintAllNpcs(int year)
{
    Console.WriteLine($"── 第 {year} 年 [Tick={WorldTime.CurrentTick}] ──────────────────");
    foreach (var npc in NpcMgr.Instance.GetAll())
    {
        var bio = NpcMgr.Instance.BioSystem.GetBio(npc.Id);
        var pos = NpcMgr.Instance.PositionSystem.GetPosition(npc.Id);
        if (bio == null || pos == null) continue;

        float age = NpcMgr.Instance.BioSystem.GetAge(npc);
        float lifespanMax = npc.Stats.Get(StatId.LifespanMax);
        float lifespanRatio = NpcMgr.Instance.BioSystem.GetLifespanRatio(npc);
        float timeRate = npc.Stats.Get(StatId.TimeFlowRate, 1f);
        bool isDead = NpcMgr.Instance.BioSystem.IsLifespanExhausted(npc);
        string status = isDead ? "💀寿终" : "✅存活";

        float cultivation = npc.Stats.Get(StatId.CultivationLevel);
        string cultivationTag = cultivation > 0 ? $"修为Lv.{(int)cultivation}" : "凡人";
        Console.WriteLine(
            $"  {status} [{bio.Name}] " +
            $"{bio.Gender} [{string.Join(",", NpcMgr.Instance.RoleSystem.GetRoles(npc.Id))}] {cultivationTag} | " +
            $"年龄 {age:F1}/{lifespanMax:F0}岁 ({lifespanRatio:P0}) | " +
            $"时间流速 ×{timeRate:F1} | " +
            $"({pos.TileId.Q:F0},{pos.TileId.R:F0}) {pos.PlaneId}"
        );
    }
    Console.WriteLine();
}