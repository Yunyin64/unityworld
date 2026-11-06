using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Xianxia.Client.Battle;

namespace Xianxia.Client.Templates;

public sealed record TemplateTrialLaunchResult(int ProcessId, string InputPath);

/// <summary>Godot process boundary; the caller's editor tree and draft remain untouched.</summary>
public static class TemplateTrialLauncher
{
    public const string ObserverScene = "res://scenes/battle/battle_observer.tscn";

    public static TemplateTrialLaunchResult Launch(BattleCase input, string dataRoot, bool animations = true)
    {
        string path = TemplateTrialService.WriteInput(input,
            ProjectSettings.GlobalizePath("user://TemplateEditor/trials"), dataRoot);
        var args = ObserverArguments(path, dataRoot);
        if (!animations) args.Add("--observer-no-animations");
        int pid = OS.CreateProcess(OS.GetExecutablePath(), args.ToArray());
        if (pid <= 0)
            throw new InvalidOperationException($"试战进程启动失败，编辑内容保留；输入位于 {path}。");
        return new(pid, path);
    }

    /// <summary>Includes an explicit scene so exported builds do not return to the editor main scene.</summary>
    public static List<string> ObserverArguments(string inputPath, string dataRoot)
    {
        if (!Path.IsPathFullyQualified(inputPath)) throw new ArgumentException("试战输入必须是绝对路径。", nameof(inputPath));
        var args = new List<string>();
        if (OS.HasFeature("editor")) args.AddRange(new[] { "--path", ProjectSettings.GlobalizePath("res://") });
        args.Add(ObserverScene);
        args.Add("--");
        args.Add("--battle-observer");
        args.Add("--observer-case-input=" + inputPath);
        args.Add("--observer-data-root=" + Path.GetFullPath(dataRoot));
        return args;
    }
}
