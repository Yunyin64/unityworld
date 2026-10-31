using Godot;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityWorld.Core;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>会话文件输出。完整事实在接收时写盘，不依赖动画是否被压缩或跳过。</summary>
internal sealed class ObserverSessionOutput : IDisposable
{
    private readonly StreamWriter _records, _presentation;
    private readonly StreamWriter? _frames;
    private int _cursor;
    public string SessionDirectory { get; }
    public string VerifyDirectory { get; }
    public string VerifyMode { get; }
    public int RunIndex { get; }
    public int Restarts { get; }

    public ObserverSessionOutput()
    {
        VerifyDirectory = Argument("--observer-verify=") ?? "";
        VerifyMode = Argument("--observer-mode=") ?? "step";
        RunIndex = int.Parse(Argument("--observer-run=") ?? "0");
        Restarts = int.Parse(Argument("--observer-restarts=") ?? "0");
        var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + System.Environment.ProcessId;
        SessionDirectory = Path.Combine(ProjectSettings.GlobalizePath("user://Logs/observer"), id);
        Directory.CreateDirectory(SessionDirectory);
        LogMgr.LogFilePath = Path.Combine(SessionDirectory, "core.log");
        LogMgr.FileWriteEnabled = true;
        _records = Writer(Path.Combine(SessionDirectory, "records.jsonl"));
        _presentation = Writer(Path.Combine(SessionDirectory, "presentation-v1.jsonl"));
        if (VerifyDirectory.Length > 0)
        {
            Directory.CreateDirectory(VerifyDirectory);
            _frames = Writer(Path.Combine(VerifyDirectory, $"godot-{VerifyMode}-{RunIndex}.jsonl"));
        }
    }

    public void Attach(BattleObserverSession session)
    {
        session.FrameCommitted += frame => _frames?.WriteLine(BattleJson.SerializeFrame(frame));
        session.PresentationCommitted += frame => _presentation.WriteLine(JsonSerializer.Serialize(frame, BattleJson.Options));
    }

    public void SaveInput(BattleCase battleCase)
    {
        File.WriteAllText(Path.Combine(SessionDirectory, "input.json"), battleCase.ToJson());
        if (VerifyDirectory.Length > 0)
            File.WriteAllText(Path.Combine(VerifyDirectory, $"godot-{VerifyMode}-{RunIndex}.jsonl.case.json"), battleCase.ToJson());
    }

    public void AppendRecords(BattleObserverSession session)
    {
        while (_cursor < session.Records.Count)
            _records.WriteLine(JsonSerializer.Serialize(session.Records[_cursor++], BattleJson.Options));
    }

    public void Flush() { _frames?.Flush(); _records.Flush(); _presentation.Flush(); }
    public void Dispose() { _frames?.Dispose(); _records.Dispose(); _presentation.Dispose(); }
    public static string? Argument(string prefix) => System.Linq.Enumerable.FirstOrDefault(OS.GetCmdlineUserArgs(), a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
    private static StreamWriter Writer(string path) => new(path, false, new UTF8Encoding(false)) { AutoFlush = true };
}
