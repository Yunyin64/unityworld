using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace UnityWorld.ConsoleHost;

/// <summary>以确定的输入检查木桩、计量边界及离线导出。由控制台验收选项调用。</summary>
internal static class CombatBenchmarkChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Run(string root)
    {
        LogMgr.MinLevel = LogLevel.Error;
        WorldMgr.Initialize(12345, root);
        WorldMgr.Start();
        try
        {
            var dummy = JsonSerializer.Deserialize<NpcDefine>(File.ReadAllText(Path.Combine(root, "tools/combat_benchmark/training_dummy.json")));
            Check(NpcDefineMgr.Instance.Get(dummy.ID) == null, "木桩误入世界模板库");
            var actor = NpcMgr.Instance.Assemble(dummy);
            var target = NpcMgr.Instance.Assemble(dummy);
            actor.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, dummy.ID));
            target.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, dummy.ID));
            var scene = new CombatScene();
            scene.Init(1, [(actor, CombatTeam.TeamA), (target, CombatTeam.TeamB)], 200);
            var a = scene.GetCombatants()[actor.Id];
            var b = scene.GetCombatants()[target.Id];
            var plan = JsonSerializer.Deserialize<JsonElement[]>("""
                [
                    {"id":"hpLoss","side":"opponent","metric":"firstHpLossTick"},
                    {"id":"wound","side":"opponent","metric":"firstWoundTick"},
                    {"id":"selfWound","side":"subject","metric":"firstWoundTick"},
                    {"id":"paid","side":"subject","metric":"mpSpent","at":1},
                    {"id":"back","side":"subject","metric":"mpRecovered","at":1},
                    {"id":"rounds","side":"subject","metric":"roundsCompleted","at":1},
                    {"id":"damage","side":"subject","metric":"damageOutHp","at":75},
                    {"id":"targetLoss","side":"opponent","metric":"damageInHp","at":75},
                    {"id":"hits","side":"subject","metric":"hpHitsOut","at":75},
                    {"id":"maxHit","side":"subject","metric":"maxHitOutHp","at":75},
                    {"id":"wounds","side":"opponent","metric":"woundsAdded","at":75},
                    {"id":"dummyFirstUse","side":"opponent","metric":"firstCardUseTick"},
                    {"id":"absorbed","side":"subject","metric":"damageOutShield","at":75}
                ]
                """);
            try
            {
                var snapshot = LogMgr.Instance.OfflineSnapshot;
                using var observation = new CombatBenchmark(scene, a.Id, b.Id, plan);
                scene.EnablePresentationRecording(true);
                scene.PreStart();
                scene.Start();
                Check(b.GetHp() == 20 && b.GetMp() == 30 && b.GetSp() == 1 && b.GetSpMax() == 10, "木桩开场数值");
                var main = b.GetField().Single();
                Check(main.DefineId == dummy.MainGongFaId && main.LuaHooks.Count == 0 &&
                    b.Backpack.GetCardPosition(main.Id) == new BackpackPosition(3, 3), "白板主功法或位置错误");
                a.AddWound("card_wound_mana_exhaustion");
                a.DealFieldChange();
                a.ReduceMP(5);
                a.RecoverMP(2);
                a.Backpack.Charge(100);
                b.ChangeShield(3);
                b.ApplyDamage(new DamageInfo(a, null, b, 3));
                observation.Capture();
                Check(snapshot["hpLoss"] == null && snapshot["selfWound"] == "0", "全盾吸收及开场直接伤口");
                scene.Tick();
                observation.Capture();
                Check(a.GetMp() == 27 && snapshot["paid"] == "5" && snapshot["back"] == "2" &&
                    snapshot["rounds"] == "2", "同 Tick 支付回复与多轮累加");
                while (scene.CurrentTick < 50) scene.Tick();
                Check(b.GetMp() == 30 && b.Backpack.Mana.Count == 0 && main.UseCount == 0, "木桩轮首不补灵");
                b.ApplyDamage(new DamageInfo(a, null, b, 10));
                observation.Capture();
                Check(b.GetHp() == 10 && snapshot["hpLoss"] == "50" && snapshot["wound"] == null, "首击扣血");
                while (scene.CurrentTick < 75) scene.Tick();
                b.ApplyDamage(new DamageInfo(a, null, b, 10));
                b.DealFieldChange();
                observation.Capture();
                Check(snapshot["damage"] == "20" && snapshot["absorbed"] == "3" && snapshot["wound"] == "75" &&
                    b.GetSp() == 2 && b.GetHp() == 10, "第二击清零回半血及首次轻伤");
                b.ApplyDamage(new DamageInfo(b, null, b, 2));
                b.ApplyDamage(new DamageInfo(null, null, b, 1));
                observation.Capture();
                Check(snapshot["damage"] == "20", "自伤与无来源扣血不能算作对敌输出");
                Check(snapshot["hits"] == "2" && snapshot["maxHit"] == "10" && snapshot["targetLoss"] == "23" &&
                    snapshot["wounds"] == "1" && snapshot["dummyFirstUse"] == null, "实际扣血笔数、受伤量或木桩发动读数错误");
                while (scene.CurrentTick < 90) scene.Tick();
                for (int i = b.GetSp(); i <= b.GetSpMax(); i++) b.AddWound("card_wound_mana_exhaustion");
                b.DealFieldChange();
                scene.Tick();
                observation.Capture();
                Check(snapshot["wound"] == "75" && scene.IsFinished && !b.IsActive, "首次值被改写或木桩未正常败退");
                Check(scene.ReadPresentationEvents().Count(e => e.Damage != null) >= 5, "观察破坏了原有战报");
            }
            finally { scene.Cleanup(); }
            ArtifactPayments(dummy);
            OfflineExport(dummy.ID);
            Console.WriteLine("COMBAT_BENCHMARK_CHECKS_OK");
        }
        finally { WorldMgr.End(); }
    }

    // 以相同八行输入分别开关诊断链；输出必须逐字节一致。
    private static void OfflineExport(string opponentId)
    {
        var folder = Directory.CreateTempSubdirectory("combat-snapshot-check-").FullName;
        var logger = LogMgr.Instance;
        var originalConsole = Console.Out;
        using var output = new StringWriter();
        var callbacks = new List<string>();
        void Watch(LogEntry item) => callbacks.Add(item.Message);
        LogMgr.OnLogEmitted += Watch;
        try
        {
            var rows = logger.OfflineSnapshot;
            rows.Clear();
            rows.Add("seed", "1");
            var subjectId = nameof(CombatBenchmarkChecks);
            rows.Add("NpcA", subjectId);
            rows.Add("NpcB", opponentId);
            rows.Add("damageOut_50", "12");
            rows.Add("damageOut_100", null);
            rows.Add("firstWoundTick", "75");
            rows.Add("FinsihTick", "75");
            rows.Add("Finishreson", "AllDefeated");
            Console.SetOut(output);
            foreach (var level in new[] { LogLevel.Debug, LogLevel.Off })
            {
                LogMgr.MinLevel = level;
                logger.Dbg("[BenchmarkChecks] ordinary-diagnostic");
                var historyCount = LogMgr.History.Count;
                var callbackCount = callbacks.Count;
                var path = Path.Combine(folder, level + ".md");
                logger.ExportOfflineSnapshot(path);
                Check(LogMgr.History.Count == historyCount && callbacks.Count == callbackCount, "快照进入诊断通道");
                Check(File.ReadAllLines(path).SequenceEqual(new[] {
                    "| 条目ID | 值 |", "| --- | --- |", "| seed | 1 |", $"| NpcA | {subjectId} |",
                    $"| NpcB | {opponentId} |", "| damageOut_50 | 12 |", "| damageOut_100 | null |",
                    "| firstWoundTick | 75 |", "| FinsihTick | 75 |", "| Finishreson | AllDefeated |" }), "八行报告格式");
            }
            Check(output.ToString().Contains("ordinary-diagnostic") && !output.ToString().Contains("damageOut_50"),
                "控制台与离线读数串流");
            try
            {
                logger.ExportOfflineSnapshot(Path.Combine(folder, "Debug.md"));
                throw new InvalidOperationException("已存在报告被覆盖");
            }
            catch (IOException) { }
            try
            {
                logger.ExportOfflineSnapshot(Path.Combine(folder, "missing", "bad.md"));
                throw new InvalidOperationException("导出错误未暴露");
            }
            catch (DirectoryNotFoundException) { }
        }
        finally
        {
            Console.SetOut(originalConsole);
            LogMgr.OnLogEmitted -= Watch;
            LogMgr.MinLevel = LogLevel.Error;
            File.Delete(Path.Combine(folder, "Debug.md"));
            File.Delete(Path.Combine(folder, "Off.md"));
            Directory.Delete(folder);
        }
    }

    private static void ArtifactPayments(NpcDefine dummy)
    {
        var b = NpcMgr.Instance.Assemble(dummy);
        var a = NpcMgr.Instance.Assemble(dummy);
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "check"));
        // 自检法宝只需要法宝规则和一枚金灵，直接构造并摆入背包。
        var artifactCard = CardMgr.Instance.InstantiateFromDefine(new CardDefine
        {
            ID = nameof(CombatBenchmarkChecks) + "_" + nameof(ArtifactPayments),
            Size = 1,
            Keywords = ["FaBao"],
            ManaCost = new() { [nameof(ElementType.Jin)] = 1 },
            ProducesContest = false
        });
        a.GainCard(artifactCard);
        a.PlaceBackpackCard(artifactCard.Id, 1, 1);
        var scene = new CombatScene();
        scene.Init(2, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 60);
        try
        {
            var plan = JsonSerializer.Deserialize<JsonElement[]>("""
                [
                    {"id":"before","side":"subject","metric":"manaSpent","at":0},
                    {"id":"paid","side":"subject","metric":"manaSpent","at":60},
                    {"id":"firstUse","side":"subject","metric":"firstCardUseTick"},
                    {"id":"uses","side":"subject","metric":"cardUses","at":60},
                    {"id":"elements","side":"subject","metric":"manaSpentByElement","at":60}
                ]
                """);
            using var observation = new CombatBenchmark(scene, a.Id, b.Id, plan);
            scene.EnablePresentationRecording(true);
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var artifact = owner.GetField().Single(card => card.Id == artifactCard.Id);
            for (int charge = 0; charge < 2; charge++)
            {
                var cell = owner.Backpack.EmptyCells()[0];
                owner.Backpack.PlaceMana(cell, ElementType.Jin);
                Check(owner.Backpack.TryAssign(cell, artifact), "法宝蓄灵");
                observation.Capture();
                while (scene.CurrentTick < (charge == 0 ? 10 : 60)) scene.Tick();
            }
            observation.Capture();
            var results = LogMgr.Instance.OfflineSnapshot;
            Check(results["before"] == "0" && results["paid"] == "2" && artifact.UseCount == 2 &&
                JsonSerializer.Deserialize<Dictionary<string, long>>(results["elements"])["Jin"] == 2, "激活与续期各实付一金");
            var firstUse = scene.ReadPresentationEvents().First(e => e.NpcId == a.Id && e.CardUsed != null);
            Check(int.Parse(results["firstUse"]) == firstUse.Tick && results["uses"] == "2", "首次发动或续期计数错误");
        }
        finally { scene.Cleanup(); }
    }
}
