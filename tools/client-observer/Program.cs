using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xianxia.Client.Battle;

Console.OutputEncoding = Encoding.UTF8;
try
{
    if (args.Length >= 3 && args[0] is "compare" or "compare-legacy") return Compare(args[1], args[2], args[0] == "compare-legacy");
    if (args.Contains("--help"))
    {
        Console.WriteLine("ClientObserver --root=<repo> --output=<frames.jsonl> [--observe-repeat=25] [--mode=play|step] [--no-event-observer] [--case-input=<full-case.json>] [--data-root=<directory-containing-Data>] [--stop-tick=N]\nClientObserver compare <baseline.jsonl> <candidate.jsonl>");
        return 0;
    }
    string Option(string name, string fallback = "") => args.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..] ?? fallback;
    var root = Path.GetFullPath(Option("--root", Path.Combine(AppContext.BaseDirectory, "../../../../../")));
    var inputPath = Option("--case-input");
    var input = inputPath.Length == 0
        ? BattleCaseCatalog.Load(Path.Combine(root, "assets/testing/battle_cases.json"), Option("--case-id", "console-basic-template"))
        : BattleCase.FromJson(File.ReadAllText(inputPath));
    var expectedFingerprint = Option("--expected-fingerprint");
    if (expectedFingerprint.Length != 0) input = input with { DataFingerprint = expectedFingerprint };
    var outputPath = Path.GetFullPath(Option("--output", Path.Combine(root, ".local-verification/battle-observer/frames.jsonl")));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    var dataRoot = Path.GetFullPath(Option("--data-root", Path.Combine(root, "UnityWorld")));
    // 每组核心验证独占新进程，不能与默认战斗共享静态 ID/监听。
    var check = args.Contains("--verify-stat-projection") ? "StatProjectionChecks"
        : args.Contains("--check-presentation-core") ? "PresentationCoreChecks"
        : args.Contains("--check-presentation-player") ? "PresentationPlayerChecks" : null;
    if (check != null)
    {
        var type = typeof(BattleObserverSession).Assembly.GetType("Xianxia.Client.Battle." + check)
            ?? throw new InvalidOperationException("未包含验证入口：" + check);
        var run = type.GetMethod("Run")!;
        run.Invoke(null, run.GetParameters().Length == 0 ? null : new object[] { check == "PresentationCoreChecks" ? root : dataRoot });
        return 0;
    }
    var repeat = int.Parse(Option("--observe-repeat", "0"));
    var stopTick = int.Parse(Option("--stop-tick", input.MaxTicks.ToString()));
    var mode = Option("--mode", "step");
    if (repeat < 0 || stopTick < 0 || mode is not ("step" or "play")) throw new ArgumentException("无效的观察次数、结束步数或播放模式。");
    using var session = new BattleObserverSession(!args.Contains("--no-event-observer"), !args.Contains("--no-presentation"));
    using var presentationOutput = new StreamWriter(outputPath + ".presentation.jsonl", false, new UTF8Encoding(false));
    var presentationFrames = 0;
    session.PresentationCommitted += batch =>
    {
        if (batch.Tick != presentationFrames++) throw new InvalidOperationException("表现批次缺帧。");
        presentationOutput.WriteLine(JsonSerializer.Serialize(batch, BattleJson.Options));
    };
    session.Initialize(input, dataRoot);
    File.WriteAllText(outputPath + ".case.json", session.Case.ToJson(), new UTF8Encoding(false));
    File.WriteAllText(outputPath + ".identity.json", JsonSerializer.Serialize(new
    {
        session.Case, session.DataIdentity, mode, observeRepeat = repeat, events = !args.Contains("--no-event-observer")
    }, BattleJson.Options), new UTF8Encoding(false));
    using var output = new StreamWriter(outputPath, false, new UTF8Encoding(false));
    if (args.Contains("--expect-initialization-error"))
    {
        if (session.Diagnostic == null || session.CanAdvance || session.Play() || session.Step())
            throw new InvalidOperationException("初始化异常没有正确隔离推进。");
        output.WriteLine(BattleJson.SerializeFrame(session.CaptureVerificationFrame()));
        Console.WriteLine("OBSERVER_EXPECTED_ERROR_OK " + session.Diagnostic.Split('\n')[0]);
        return 0;
    }
    if (session.Diagnostic != null) throw new InvalidOperationException(session.Diagnostic);
    if (args.Contains("--expect-sink-error"))
    {
        session.FrameCommitted += _ => throw new InvalidOperationException("deliberate-client-sink-verification-error");
        session.Step();
        if (session.Diagnostic == null || !session.Diagnostic.Contains("deliberate-client-sink-verification-error") ||
            session.CurrentSnapshot.Tick != 1 || session.CurrentSnapshot.Result != null || session.Play() || session.Step())
            throw new InvalidOperationException("观察帧错误未正确保留原诊断/停止推进。");
        output.WriteLine(BattleJson.SerializeFrame(session.CaptureVerificationFrame()));
        Console.WriteLine("OBSERVER_SINK_ERROR_OK tick=1 result=null stopped=true");
        return 0;
    }
    var initial = BattleJson.SerializeSnapshot(session.CurrentSnapshot);
    session.Update(60);
    session.Play(); session.Update(0.09); session.Pause();
    session.Update(30);
    session.Play(); session.Update(0.01); session.Pause();
    if (initial != BattleJson.SerializeSnapshot(session.CurrentSnapshot))
        throw new InvalidOperationException("暂停/恢复消耗了等待时间，Tick 0 状态改变。");
    var samples = 0;
    var largestRowAdvance = 0;
    BattleSnapshot? previous = null;
    void WriteFrame()
    {
        if (session.CurrentSnapshot.Tick != samples) throw new InvalidOperationException($"逐Tick订阅缺帧：expected={samples} actual={session.CurrentSnapshot.Tick}");
        var canonical = BattleJson.SerializeSnapshot(session.CurrentSnapshot);
        for (var i = 0; i < repeat; i++)
        {
            var observed = BattleJson.SerializeSnapshot(session.CaptureSnapshot());
            if (canonical != observed)
                throw new InvalidOperationException($"重复读取改变快照 Tick={session.CurrentSnapshot.Tick} repeat={i}。");
        }
        if (session.CurrentSnapshot.WorldTick != 0) throw new InvalidOperationException("观察模式推进了背景世界。");
        if (previous != null)
            foreach (var npc in session.CurrentSnapshot.Participants)
            {
                var prior = previous.Participants.Single(n => n.Id == npc.Id).Backpack;
                var current = npc.Backpack;
                largestRowAdvance = Math.Max(largestRowAdvance,
                    (current.Round - prior.Round) * current.Height + current.Row - prior.Row);
            }
        previous = session.CurrentSnapshot;
        output.WriteLine(BattleJson.SerializeFrame(session.CaptureVerificationFrame()));
        samples++;
    }
    WriteFrame();
    session.FrameCommitted += _ => WriteFrame();
    if (mode == "play") session.Play();
    var displayFrame = 0;
    var deltas = new[] { 0.017, 0.003, 0.041, 0.27, 1.3, 0.060 };
    while (session.CanAdvance && session.CurrentSnapshot.Tick < stopTick)
    {
        var before = session.CurrentSnapshot.Tick;
        if (mode == "play") session.Update(Math.Min(deltas[displayFrame++ % deltas.Length], (stopTick - before) / BattleObserverSession.TicksPerSecond));
        else if (!session.Step()) throw new InvalidOperationException(session.Diagnostic ?? "单步未推进。");
        var after = session.CurrentSnapshot.Tick;
        if (mode == "step" && after != before + 1) throw new InvalidOperationException("单步不是恰好一个 Tick。");
    }
    session.Pause();
    var terminal = BattleJson.SerializeSnapshot(session.CurrentSnapshot);
    session.Update(10);
    if (terminal != BattleJson.SerializeSnapshot(session.CurrentSnapshot)) throw new InvalidOperationException("暂停后仍推进。");
    if (session.CurrentSnapshot.IsFinished && (session.Play() || session.Step())) throw new InvalidOperationException("结束后仍可推进。");
    if (session.Diagnostic != null) throw new InvalidOperationException(session.Diagnostic);
    File.WriteAllLines(outputPath + ".records.jsonl", session.Records.Select(r => JsonSerializer.Serialize(r, BattleJson.Options)), new UTF8Encoding(false));
    var result = session.CurrentSnapshot.Result;
    Console.WriteLine($"OBSERVER_OK samples={samples} tick={session.CurrentSnapshot.Tick} worldTick={session.CurrentSnapshot.WorldTick} maxRowsPerTick={largestRowAdvance} winner={result?.WinnerTeam ?? "pending"} reason={result?.Reason ?? "pending"} fingerprint={session.DataFingerprint}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine("OBSERVER_FAILED " + error);
    return 1;
}

static int Compare(string baseline, string candidate, bool legacy = false)
{
    if (!File.Exists(baseline + ".case.json") || !File.Exists(candidate + ".case.json"))
    {
        Console.Error.WriteLine("OBSERVER_DIFF required complete-input sidecar missing: both files need .case.json");
        return 2;
    }
    else
    {
        using var leftCase = JsonDocument.Parse(Normalize(File.ReadAllText(baseline + ".case.json"), legacy, true));
        using var rightCase = JsonDocument.Parse(Normalize(File.ReadAllText(candidate + ".case.json"), legacy, true));
        var identityDifference = FirstDifference(leftCase.RootElement, rightCase.RootElement, "case");
        if (identityDifference != null) { Console.Error.WriteLine("OBSERVER_DIFF " + identityDifference); return 2; }
    }
    using var left = File.ReadLines(baseline).GetEnumerator();
    using var right = File.ReadLines(candidate).GetEnumerator();
    var line = 0;
    while (true)
    {
        var hasLeft = left.MoveNext(); var hasRight = right.MoveNext();
        if (!hasLeft && !hasRight) { Console.WriteLine($"OBSERVER_MATCH frames={line}"); return 0; }
        line++;
        if (hasLeft != hasRight) { Console.Error.WriteLine($"OBSERVER_DIFF line={line} frame count baseline={hasLeft} candidate={hasRight}"); return 2; }
        using var leftJson = JsonDocument.Parse(Normalize(left.Current, legacy, false));
        using var rightJson = JsonDocument.Parse(Normalize(right.Current, legacy, false));
        var difference = FirstDifference(leftJson.RootElement, rightJson.RootElement, "$");
        if (difference != null) { Console.Error.WriteLine($"OBSERVER_DIFF line={line} {difference}"); return 2; }
    }
}

// 迁移对照只忽略明确新增的展示字段与已知改变的程序集身份；其它输入/玩法/Trace 严格比较。
static string Normalize(string json, bool legacy, bool input)
{
    if (!legacy) return json;
    var node = JsonNode.Parse(json)!;
    if (input) node.AsObject().Remove("coreVersion");
    else if (node["snapshot"]?["participants"] is JsonArray participants)
        foreach (var npc in participants)
            foreach (var key in new[] { "hpMax", "mpMax", "spMax", "statReadError" }) npc!.AsObject().Remove(key);
    return node.ToJsonString();
}

static string? FirstDifference(JsonElement a, JsonElement b, string path)
{
    if (a.ValueKind != b.ValueKind) return $"{path}: {a.ValueKind} != {b.ValueKind}";
    if (a.ValueKind == JsonValueKind.Object)
    {
        foreach (var item in a.EnumerateObject())
        {
            if (!b.TryGetProperty(item.Name, out var other)) return $"{path}.{item.Name}: candidate field missing";
            var diff = FirstDifference(item.Value, other, path + "." + item.Name);
            if (diff != null) return diff;
        }
        foreach (var item in b.EnumerateObject())
            if (!a.TryGetProperty(item.Name, out _)) return $"{path}.{item.Name}: unexpected candidate field";
        return null;
    }
    if (a.ValueKind == JsonValueKind.Array)
    {
        if (a.GetArrayLength() != b.GetArrayLength()) return $"{path}.length: {a.GetArrayLength()} != {b.GetArrayLength()}";
        for (var i = 0; i < a.GetArrayLength(); i++)
        {
            var diff = FirstDifference(a[i], b[i], $"{path}[{i}]");
            if (diff != null) return diff;
        }
        return null;
    }
    if (a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var an) && b.TryGetDecimal(out var bn))
        return an == bn ? null : $"{path}: {an} != {bn}";
    if (a.ValueKind == JsonValueKind.String)
        return a.GetString() == b.GetString() ? null : $"{path}: {a.GetString()} != {b.GetString()}";
    return a.GetRawText() == b.GetRawText() ? null : $"{path}: {a.GetRawText()} != {b.GetRawText()}";
}
