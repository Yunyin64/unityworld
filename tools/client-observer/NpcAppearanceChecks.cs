using System.Diagnostics;
using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Battle;

/// <summary>以正式案例验证出生选择、冻结重开与玩法序列隔离。</summary>
public static class NpcAppearanceChecks
{
    private sealed record Observation(BattleCase Case, List<string> Frames, float[] Heights);

    public static void Run(string root)
    {
        var input = BattleCaseCatalog.Load(Path.Combine(root, "assets/testing/battle_cases.json")) with { MaxTicks = 650 };
        var custom = input.Participants.ToArray();
        custom[0] = custom[0] with { Appearance = new("male", new()
        {
            ["hairFront"] = new("hair/front/jade", "#555555", "game"),
            ["hairRear"] = new("hair/rear/braid", SourceId: "game")
        }) };

        var reportDirectory = Directory.CreateTempSubdirectory("xianxia-appearance-regression-").FullName;
        var baseline = Observe(root, reportDirectory, "baseline", input);
        var changed = Observe(root, reportDirectory, "appearance", input with { Participants = custom });
        Check(baseline.Frames.SequenceEqual(changed.Frames), "改外观影响了战斗快照、随机序列或结算。");
        Check(baseline.Heights.SequenceEqual(changed.Heights), "改外观影响了身高生成。");
        var selected = changed.Case.Participants[0].Appearance!;
        Check(selected.Parts["hairFront"].Dye == "#555555" && selected.Parts["hairRear"].Id == "hair/rear/braid",
            "试战未读到逐槽显式外观。");
        var reopened = Observe(root, reportDirectory, "reopen", changed.Case);
        Check(reopened.Frames.SequenceEqual(changed.Frames) &&
            reopened.Case.Participants[0].Appearance!.Parts.SequenceEqual(selected.Parts), "重开未保留外观或玩法。");

        var wardrobe = NpcAppearanceCatalog.Default;
        var issues = new List<NpcAppearanceCatalog.Issue>();
        var data = wardrobe.Resolve("male", new Dictionary<string, string>
        {
            ["Face"] = "src/face/angular[]",
            ["HairFront"] = "missing/hair"
        }, 25, issues.Add);
        Check(issues is [{ Error: false, Slot: "HairFront" }] && data.Parts["face"].Dye == null &&
            data.Parts.ContainsKey("hairFront"), "缺图回退或默认色异常。");
        var copy = data.Clone();
        copy.Parts.Remove("face");
        Check(data.Parts.ContainsKey("face"), "复制的插槽字典共享了原对象。");
        var again = wardrobe.Resolve("male", new Dictionary<string, string>
        {
            ["Face"] = "face/angular",
            ["HairFront"] = "missing/hair"
        }, 25);
        Check(data.Parts.SequenceEqual(again.Parts), "相同种子与规范地址未复现外观。");
        var omitted = wardrobe.Resolve("male", null, 25);
        var blank = wardrobe.Resolve("male", new Dictionary<string, string> { ["face"] = "" }, 25);
        var other = wardrobe.Resolve("male", null, 26);
        Check(omitted.Parts.SequenceEqual(blank.Parts) && !omitted.Parts.SequenceEqual(other.Parts),
            "留空与缺省必须一致，并随 NPC 的独立种子选取不同组合。");
        string animalRig = NpcAppearanceCatalog.RigFor(NpcTypes.NpcType.Animal, NpcTypes.Gender.Male, "wolf", 25);
        Check(BattleActorLook.FromData(wardrobe.Resolve(animalRig, null, 25)) == null, "非人形误用人形外观。");

        Console.WriteLine($"NPC_APPEARANCE_OK frames={changed.Frames.Count} slots={selected.Parts.Count}");
    }

    private static Observation Observe(string root, string directory, string name, BattleCase battle)
    {
        string input = Path.Combine(directory, name + ".case.json");
        string report = Path.Combine(directory, name + ".report.json");
        File.WriteAllText(input, battle.ToJson());
        // Rng keeps unique IDs for the whole process. Like actual replay, each run uses a fresh process.
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(NpcAppearanceChecks).Assembly.Location);
        start.ArgumentList.Add("--root=" + root);
        start.ArgumentList.Add("--npc-appearance-check-input=" + input);
        start.ArgumentList.Add("--npc-appearance-check-report=" + report);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动外观对照进程。");
        process.WaitForExit();
        Check(process.ExitCode == 0, "外观对照进程退出失败：" + name);
        return JsonSerializer.Deserialize<Observation>(File.ReadAllText(report))!;
    }

    public static void WriteObservation(string root, string input, string report)
    {
        using var session = new BattleObserverSession();
        var frames = new List<string>();
        session.FrameCommitted += frame => frames.Add(BattleJson.SerializeFrame(frame));
        session.Initialize(BattleCase.FromJson(File.ReadAllText(input)), Path.Combine(root, "UnityWorld"));
        Check(session.Diagnostic == null, session.Diagnostic ?? "初始化失败。");
        float[] heights = session.CurrentSnapshot.Participants
            .Select(npc => NpcMgr.Instance.BioSystem.GetData(npc.Id).AppearanceData.Height).ToArray();
        Check(heights.Length == 2, "未取得装配角色的身高。");
        while (session.CanAdvance) Check(session.Step(), session.Diagnostic ?? "试战未推进。");
        Check(session.CurrentSnapshot.Result != null, "缺少对照结算。");
        File.WriteAllText(report, JsonSerializer.Serialize(new Observation(session.Case.Copy(), frames, heights)));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
