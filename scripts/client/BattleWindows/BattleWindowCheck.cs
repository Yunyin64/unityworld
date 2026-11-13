using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xianxia.Client.Battle;
using Xianxia.Client.UI;

namespace Xianxia.Client.BattleWindows;

/// <summary>开发场景直接调用公开入口。来源实例、窗口、独立进程和真实落盘战报都参与断言。</summary>
public partial class BattleWindowCheck : Control
{
    private int _callerUpdates;
    private bool _closeReceived;
    public override void _Process(double delta) => _callerUpdates++;
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && IsVisibleInTree()) _closeReceived = true;
    }
    private string _results = "";
    private BattleObserverView View => GetParent().GetNode<BattleObserverView>("EmbeddedBattle");
    private BattleCase _case = null!;

    public override async void _Ready()
    {
        var path = ObserverSessionOutput.Argument("--battle-window-check=");
        if (path == null) return;
        try
        {
            _results = Path.GetFullPath(path);
            Directory.CreateDirectory(_results);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _case = BattleCaseCatalog.Load(ProjectSettings.GlobalizePath("res://assets/testing/battle_cases.json")) with
            {
                Id = "window-nondefault", Name = "双窗口验收", WorldSeed = 4521,
                CombatSeed = 7324, MaxTicks = 16
            };
            _case = BattleCaseFactory.VerifyVersion(_case, BattleCaseFactory.Fingerprint(GameBootstrap.ResolveDataRoot()).Sha256);
            File.WriteAllText(Path.Combine(_results, "case.json"), _case.ToJson());
            var caller = GetNode<Button>("Caller");
            caller.ProcessMode = ProcessModeEnum.Always;
            caller.GrabFocus();
            Open(_case);
            await ReadyWorker();
            int originalPid = View.WorkerPid;
            Check(caller.ProcessMode == ProcessModeEnum.Disabled, "调用页的独立输入控件仍在处理。");
            if (OS.GetName() == "Windows" && DisplayServer.GetName() != "headless")
            {
                using var observer = Process.GetProcessById(originalPid);
                using var host = Process.GetCurrentProcess();
                Check(observer.MainWindowHandle == IntPtr.Zero && host.MainWindowHandle != IntPtr.Zero,
                    "同窗战斗的可见窗口数量异常。");
            }
            File.WriteAllText(Path.Combine(_results, "worker.pid"), originalPid.ToString());
            File.WriteAllText(Path.Combine(_results, "host.pid"), System.Environment.ProcessId.ToString());
            if (OS.GetCmdlineUserArgs().Contains("--battle-window-parent-exit-check"))
            {
                GD.Print($"WINDOW_CHECK_PARENT_READY child={originalPid}");
                return;
            }
            var initial = BattleJson.SerializeSnapshot(View.DisplayedSession!.CurrentSnapshot);
            var callerCount = _callerUpdates;
            await ToSignal(GetTree().CreateTimer(0.22), SceneTreeTimer.SignalName.Timeout);
            Check(_callerUpdates == callerCount && initial == BattleJson.SerializeSnapshot(View.DisplayedSession.CurrentSnapshot),
                "暂停中调用页或战斗仍在推进。");
            await CompleteBattle(false);
            SaveRun("current-step");
            string terminal = BattleJson.SerializeSnapshot(View.DisplayedSession.CurrentSnapshot);
            for (int i = 0; i < 2; i++)
            {
                int oldPid = View.WorkerPid;
                ObserverVerificationDriver.Click(View.Layout!.Restart);
                await Wait(() => View.WorkerPid != oldPid && View.DisplayedSession?.CurrentSnapshot.Tick == 0);
                Check(BattleJson.SerializeSnapshot(View.DisplayedSession!.CurrentSnapshot) == initial, "重开后的 Tick 0 有差异。");
                await Wait(() => !IsRunning(oldPid));
            }
            await CompleteBattle(true);
            Check(terminal == BattleJson.SerializeSnapshot(View.DisplayedSession!.CurrentSnapshot), "自动播放结果与单步有差异。");
            SaveRun("current-play");
            CompareRuns(Path.Combine(_results, "current-step"), Path.Combine(_results, "current-play"));
            int disconnected = View.WorkerPid;
            using (var child = Process.GetProcessById(disconnected)) child.Kill();
            await Wait(() => View.Failure != null);
            Check(terminal == BattleJson.SerializeSnapshot(View.DisplayedSession.CurrentSnapshot), "断线丢失最后确认状态。");
            await ReturnToCaller();

            // 结构合法但无法装配的模板必须由真实观察进程报错。
            var broken = _case with { Participants = new[] { _case.Participants[0], new BattleParticipant("missing-window-check-npc", "TeamB") } };
            Open(broken);
            await Wait(() => View.Failure != null);
            Check(View.Failure!.Contains("missing-window-check-npc"), "装配错误原因丢失。");
            await ReturnToCaller();
            await CheckDataDrift();
            GetTree().AutoAcceptQuit = false;
            Open(_case);
            await ReadyWorker();
            int closePid = View.WorkerPid;
            GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
            await Wait(() => _closeReceived && Visible && !IsRunning(closePid));
            GetTree().AutoAcceptQuit = true;
            await CheckSeparateWindow();
            string summary = $"BATTLE_WINDOW_OK caller={System.Environment.ProcessId} mode=CurrentWindow,NewWindow ticks={_case.MaxTicks} restarts=2 results={_results}";
            GD.Print(summary);
            File.WriteAllText(Path.Combine(_results, "result.txt"), summary);
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError("[BattleWindowCheck] " + error);
            if (_results.Length != 0) File.WriteAllText(Path.Combine(_results, "failure.txt"), error.ToString());
            GetTree().Quit(1);
        }
    }

    private void Open(BattleCase input)
    {
        BattleLauncher.OpenBattle(input, BattleWindowMode.CurrentWindow);
        Check(!Visible && ProcessMode == ProcessModeEnum.Disabled && GetTree().CurrentScene == this,
            "同窗调用未保留来源现场。");
    }

    private Task ReadyWorker() => Wait(() => View.DisplayedSession?.CanAdvance == true || View.Failure != null, true);

    private async Task CompleteBattle(bool play)
    {
        if (play) ObserverVerificationDriver.Click(View.Layout!.Play);
        while (View.DisplayedSession?.CurrentSnapshot.IsFinished != true)
        {
            if (!play)
            {
                int tick = View.DisplayedSession!.CurrentSnapshot.Tick;
                ObserverVerificationDriver.Click(View.Layout!.Step);
                await Wait(() => View.DisplayedSession.CurrentSnapshot.Tick == tick + 1 &&
                    (View.DisplayedSession.CanAdvance || View.DisplayedSession.CurrentSnapshot.IsFinished), true);
            }
            else await Wait(() => View.DisplayedSession!.CurrentSnapshot.IsFinished, true);
        }
    }

    private async Task ReturnToCaller()
    {
        int pid = View.WorkerPid;
        ObserverVerificationDriver.Click(View.Layout!.Return);
        await Wait(() => Visible && GetParent().GetNodeOrNull("EmbeddedBattle") == null && !IsRunning(pid));
        Check(ProcessMode != ProcessModeEnum.Disabled && GetViewport().GuiGetFocusOwner() == GetNode<Button>("Caller"), "返回时来源或焦点没有恢复。");
        Check(GetNode<Button>("Caller").ProcessMode == ProcessModeEnum.Always, "独立处理的控件未恢复。");
    }

    private void SaveRun(string label)
    {
        string target = Path.Combine(_results, label);
        Directory.CreateDirectory(target);
        foreach (string file in new[] { "presentation-v1.jsonl", "records.jsonl", "input.json" })
            File.Copy(Path.Combine(View.WorkerLogPath!, file), Path.Combine(target, file), true);
        File.WriteAllText(Path.Combine(target, "mode.txt"), "CurrentWindow");
    }

    private async Task CheckDataDrift()
    {
        // 改动验收目录里的副本，仓库数据和第一次成功装配的冻结输入均保留。
        string root = Path.Combine(_results, "data-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(GameBootstrap.ResolveDataRoot(), "Data");
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(root, "Data", Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
        BattleLauncher.Open(_case, BattleWindowMode.CurrentWindow, root, false);
        await ReadyWorker();
        string before = BattleJson.SerializeSnapshot(View.DisplayedSession!.CurrentSnapshot);
        var example = Directory.EnumerateFiles(Path.Combine(root, "Data"), "*.json", SearchOption.AllDirectories).First();
        File.AppendAllText(example, " ");
        ObserverVerificationDriver.Click(View.Layout!.Restart);
        await Wait(() => View.Failure != null);
        Check(View.Failure!.Contains("版本已变化") && before == BattleJson.SerializeSnapshot(View.DisplayedSession.CurrentSnapshot),
            "数据漂移时未保留已确认的状态或诊断。");
        await ReturnToCaller();
    }

    private async Task CheckSeparateWindow()
    {
        var result = BattleLauncher.OpenBattle(_case, BattleWindowMode.NewWindow);
        using var child = Process.GetProcessById(result.ProcessId);
        string? directory = null;
        try
        {
            string logs = ProjectSettings.GlobalizePath("user://Logs/observer");
            await Wait(() =>
            {
                directory = Directory.EnumerateDirectories(logs, "*-" + result.ProcessId)
                    .FirstOrDefault(dir => File.Exists(Path.Combine(dir, "input.json")));
                return directory != null;
            });
            Check(Visible && GetTree().CurrentScene == this && result.ProcessId != System.Environment.ProcessId, "新窗调用改变来源页。");
            if (OS.GetName() == "Windows") { child.Refresh(); Check(child.MainWindowHandle != IntPtr.Zero, "新观察窗口不可见。"); }
            Check(File.ReadAllText(Path.Combine(directory!, "input.json")) == _case.ToJson(), "新窗口案例不一致。");
        }
        finally
        {
            if (!child.HasExited && !child.CloseMainWindow()) child.Kill();
            await Wait(() => child.HasExited);
        }
        var first = JsonNode.Parse(File.ReadLines(Path.Combine(directory!, "presentation-v1.jsonl")).First())!;
        var other = JsonNode.Parse(File.ReadLines(Path.Combine(_results, "current-step", "presentation-v1.jsonl")).First())!;
        first.AsObject().Remove("sessionId");
        other.AsObject().Remove("sessionId");
        Check(JsonNode.DeepEquals(first, other), "两种窗口模式初始状态不同。");
    }

    private async Task Wait(Func<bool> condition, bool requireHealthy = false)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            if (requireHealthy && View.Failure != null) throw new InvalidOperationException(View.Failure);
            if (condition()) return;
            if (watch.Elapsed.TotalSeconds > 40) throw new TimeoutException("观察验收等待超时。");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void CompareRuns(string left, string right)
    {
        var lines = File.ReadAllLines(Path.Combine(left, "presentation-v1.jsonl"));
        var other = File.ReadAllLines(Path.Combine(right, "presentation-v1.jsonl"));
        Check(lines.Length == other.Length, "两次模拟帧数不同。");
        for (int i = 0; i < lines.Length; i++)
        {
            var a = JsonNode.Parse(lines[i])!.AsObject();
            var b = JsonNode.Parse(other[i])!.AsObject();
            a.Remove("sessionId"); b.Remove("sessionId");
            Check(JsonNode.DeepEquals(a, b), $"两次模拟在 Tick {i} 有差异。");
        }
        Check(File.ReadLines(Path.Combine(left, "records.jsonl")).SequenceEqual(File.ReadLines(Path.Combine(right, "records.jsonl"))),
            "两次模拟增量战报不同。");
    }

    private static bool IsRunning(int pid)
    {
        if (pid <= 0) return false;
        try { using var process = Process.GetProcessById(pid); return !process.HasExited; }
        catch (ArgumentException) { return false; }
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
