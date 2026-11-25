using System.Globalization;
using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace UnityWorld.ConsoleHost;

/// <summary>在独立世界中跑一局，按配置把行读数交给本局 LogMgr。</summary>
internal sealed class CombatBenchmark : IEventListener, IDisposable
{
    private readonly CombatScene scene;
    private readonly CombatNpc subject;
    private readonly CombatNpc opponent;
    private readonly JsonElement[] plan;
    private readonly Dictionary<int, Dictionary<string, long>> totals = new();
    private readonly Dictionary<int, Dictionary<string, long>> paidElements = new();
    private int presentationCount;

    internal CombatBenchmark(CombatScene scene, int subjectId, int opponentId, JsonElement[] plan)
    {
        this.scene = scene;
        this.plan = plan;
        subject = scene.GetCombatants()[subjectId];
        opponent = scene.GetCombatants()[opponentId];
        foreach (var npc in new[] { subject, opponent })
        {
            totals.Add(npc.Id, new(StringComparer.Ordinal));
            paidElements.Add(npc.Id, new(StringComparer.Ordinal));
        }
        EventMgr.Instance.RegisterEvent("combat-benchmark", "OnRoundEnd", ScopeKey.Global, this);
        EventMgr.Instance.RegisterEvent("combat-benchmark", "OnMPChanged", ScopeKey.Global, this);
    }

    /// <summary>轮末与 MP 实付已有逐笔事件，包括开场和同 Tick 的多次变动。</summary>
    public void OnEvent(string eventId, ScopeKey scope, object args)
    {
        if (args is not APIContext context || context.Scene != scene || context.Caster == null) return;
        var values = totals[context.Caster.Id];
        if (eventId == "OnRoundEnd")
            values["roundsCompleted"] = values.GetValueOrDefault("roundsCompleted") + 1;
        else
        {
            var delta = context.GetValue<int>("Delta");
            var key = delta < 0 ? "mpSpent" : "mpRecovered";
            values[key] = values.GetValueOrDefault(key) + Math.Abs((long)delta);
        }
    }

    /// <summary>释放只属于本场景的两个监听。</summary>
    public void Dispose()
    {
        EventMgr.Instance.RemoveEvent("OnRoundEnd", ScopeKey.Global, this);
        EventMgr.Instance.RemoveEvent("OnMPChanged", ScopeKey.Global, this);
    }

    internal void Capture()
    {
        // 读增量而不取走事件；旧战报可以继续读取同一条完整表现流。
        foreach (var entry in scene.ReadPresentationEvents(presentationCount))
        {
            presentationCount++;
            var values = totals[entry.NpcId];
            if (entry.Damage is { } damage)
            {
                // 承伤按目标入账，包括自伤和无来源伤害。对敌输出另按来源入账。
                if (damage.HpDamage > 0)
                {
                    values.TryAdd("firstHpLossTick", entry.Tick);
                    values["damageInHp"] = values.GetValueOrDefault("damageInHp") + damage.HpDamage;
                }
                if (damage.SourceNpcId is int source && source != damage.TargetNpcId && totals.TryGetValue(source, out var output))
                {
                    output["damageOutHp"] = output.GetValueOrDefault("damageOutHp") + damage.HpDamage;
                    output["damageOutShield"] = output.GetValueOrDefault("damageOutShield") + damage.ShieldAbsorbed;
                    if (damage.HpDamage > 0)
                    {
                        output["hpHitsOut"] = output.GetValueOrDefault("hpHitsOut") + 1;
                        output["maxHitOutHp"] = Math.Max(output.GetValueOrDefault("maxHitOutHp"), damage.HpDamage);
                    }
                }
            }
            else if (entry.CardUsed is { } payment)
            {
                // 一次扫描提交算一次发动，零费用卡与法宝续期也计入。
                values.TryAdd("firstCardUseTick", entry.Tick);
                values["cardUses"] = values.GetValueOrDefault("cardUses") + 1;
                values["manaSpent"] = values.GetValueOrDefault("manaSpent") + payment.Before.Count;
                foreach (var element in payment.Before)
                    paidElements[entry.NpcId][element] = paidElements[entry.NpcId].GetValueOrDefault(element) + 1;
            }
            else if (entry.CardAdded != null && entry.Card.Keywords.Contains("Wound"))
            {
                values.TryAdd("firstWoundTick", entry.Tick);
                values["woundsAdded"] = values.GetValueOrDefault("woundsAdded") + 1;
            }
        }

        foreach (var item in plan)
        {
            if (item.TryGetProperty("at", out var at))
            {
                if (at.ValueKind == JsonValueKind.Number && at.GetInt32() != scene.CurrentTick) continue;
                if (at.ValueKind == JsonValueKind.String && !scene.IsFinished) continue;
            }
            var npc = item.GetProperty("side").GetString() == "subject" ? subject : opponent;
            var metric = item.GetProperty("metric").GetString();
            var values = totals[npc.Id];
            LogMgr.Instance.OfflineSnapshot[item.GetProperty("id").GetString()] = metric switch
            {
                "hp" => npc.GetHp().ToString(CultureInfo.InvariantCulture),
                "mp" => npc.GetMp().ToString(CultureInfo.InvariantCulture),
                "shield" => npc.ShieldValue.ToString(CultureInfo.InvariantCulture),
                "spUsed" => npc.GetSp().ToString(CultureInfo.InvariantCulture),
                "manaSpentByElement" => JsonSerializer.Serialize(paidElements[npc.Id]),
                "firstHpLossTick" or "firstWoundTick" or "firstCardUseTick" when !values.ContainsKey(metric) => null,
                _ => values.GetValueOrDefault(metric).ToString(CultureInfo.InvariantCulture)
            };
        }
    }

    /// <summary>批次入口为每个种子启动一次此命令。正常或可控异常都尝试导出本局两列表。</summary>
    internal static int Run(string[] args)
    {
        if (args.Length != 6) throw new ArgumentException("需要项目路径、清单、木桩模板、种子和 Markdown 路径。");
        using var manifest = JsonDocument.Parse(File.ReadAllText(args[2]));
        var config = manifest.RootElement;
        var seed = int.Parse(args[4], CultureInfo.InvariantCulture);
        var labels = config.TryGetProperty("labels", out var descriptions)
            ? descriptions.Deserialize<Dictionary<string, string>>() : null;
        string a = config.GetProperty("subject").GetString(), b = config.GetProperty("opponent").GetString();
        var plan = config.GetProperty("plan").EnumerateArray().ToArray();
        var scene = new CombatScene();
        var errors = new List<string>();
        void CollectError(LogEntry entry)
        {
            if (entry.Level == LogLevel.Error) errors.Add(entry.Message);
        }

        LogMgr.MinLevel = LogLevel.Error;
        LogMgr.OnLogEmitted += CollectError;
        try
        {
            WorldMgr.Initialize(config.GetProperty("worldSeed").GetInt32(), args[1]);
            WorldMgr.Start();
            var snapshot = LogMgr.Instance.OfflineSnapshot;
            snapshot.Add("seed", args[4]);
            snapshot.Add("NpcA", a);
            snapshot.Add("NpcB", b);
            foreach (var item in plan) snapshot.Add(item.GetProperty("id").GetString(), null);
            string reason = "Error";
            try
            {
                var attacker = NpcMgr.Instance.Assemble(NpcDefineMgr.Instance.Get(a));
                var defender = b == "training_dummy"
                    ? NpcMgr.Instance.Assemble(JsonSerializer.Deserialize<NpcDefine>(File.ReadAllText(args[3])))
                    : NpcMgr.Instance.Assemble(NpcDefineMgr.Instance.Get(b));
                if (b == "training_dummy")
                    defender.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "training_dummy"));

                scene.Init(seed, [(attacker, CombatTeam.TeamA), (defender, CombatTeam.TeamB)], config.GetProperty("limit").GetInt32());
                scene.EnablePresentationRecording(true);
                using var observation = new CombatBenchmark(scene, attacker.Id, defender.Id, plan);
                scene.PreStart();
                scene.Start();
                observation.Capture();
                while (!scene.IsFinished)
                {
                    scene.Tick();
                    observation.Capture();
                }
                if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                reason = scene.GetResult().GetObject("EndReason").ToString();
            }
            finally
            {
                snapshot.Add("FinsihTick", scene.CurrentTick.ToString(CultureInfo.InvariantCulture));
                snapshot.Add("Finishreson", reason);
                LogMgr.Instance.ExportOfflineSnapshot(args[5], labels);
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            LogMgr.OnLogEmitted -= CollectError;
            scene.Cleanup();
            WorldMgr.End();
        }
    }
}
