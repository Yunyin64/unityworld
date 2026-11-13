using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Xianxia.Client.Battle;
using Xianxia.Client.UI;

namespace Xianxia.Client.BattleWindows;

public enum BattleWindowMode { CurrentWindow, NewWindow }
public sealed record BattleLaunchResult(int ProcessId, string InputPath);

/// <summary>窗口方式只决定观察界面的宿主，不进入案例、种子或数据指纹。</summary>
public static class BattleLauncher
{
    public const string ObserverScene = "res://scenes/battle/battle_observer.tscn";

    public static BattleLaunchResult OpenBattle(BattleCase input, BattleWindowMode mode) =>
        Open(input, mode, GameBootstrap.ResolveDataRoot(), true);

    internal static BattleLaunchResult Open(BattleCase input, BattleWindowMode mode, string dataRoot, bool animations)
    {
        var frozen = input.Copy();
        BattleCaseFactory.Validate(frozen);
        frozen = BattleCaseFactory.VerifyVersion(frozen, BattleCaseFactory.Fingerprint(dataRoot).Sha256);
        var path = BattleCaseInput.Write(frozen, ProjectSettings.GlobalizePath("user://BattleInputs"));
        if (mode == BattleWindowMode.NewWindow)
        {
            var args = ObserverArguments(path, dataRoot);
            if (!animations) args.Add("--observer-no-animations");
            int pid = OS.CreateProcess(OS.GetExecutablePath(), args.ToArray());
            if (pid <= 0) throw new IOException($"无法打开独立战斗窗口，输入位于 {path}。");
            return new(pid, path);
        }
        if (mode != BattleWindowMode.CurrentWindow) throw new ArgumentOutOfRangeException(nameof(mode));
        var tree = (SceneTree)Engine.GetMainLoop();
        var source = tree.CurrentScene ?? throw new InvalidOperationException("当前窗口没有可保留的调用页面。");
        if (source.GetParent().GetNodeOrNull<BattleObserverView>("EmbeddedBattle") != null)
            throw new InvalidOperationException("当前窗口已经打开战斗。");
        // 保留原节点和焦点。移除节点再挂回会触发页面自己的 _ExitTree，销毁原现场。
        var view = GD.Load<PackedScene>(ObserverScene).Instantiate<BattleObserverView>();
        view.Name = "EmbeddedBattle";
        view.ConfigureEmbedded(source, path, dataRoot, animations);
        source.GetParent().AddChild(view);
        if (!view.IsInsideTree())
        {
            view.Free();
            throw new InvalidOperationException("调用页面还在切换，未能挂载战斗界面。");
        }
        return new(System.Environment.ProcessId, path);
    }

    internal static List<string> ProcessArguments(string scene, string inputPath, bool headless = false)
    {
        var args = new List<string>();
        if (headless) args.Add("--headless");
        if (OS.HasFeature("editor")) args.AddRange(new[] { "--path", ProjectSettings.GlobalizePath("res://") });
        // Godot 默认滚动同一个 godot.log。每次启动有自己的输入目录，也用来隔离引擎日志。
        args.Add("--log-file");
        args.Add(Path.Combine(Path.GetDirectoryName(inputPath)!, "observer-engine.log"));
        args.Add(scene);
        args.Add("--");
        return args;
    }

    public static List<string> ObserverArguments(string path, string dataRoot)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("战斗输入需要绝对路径。", nameof(path));
        var args = ProcessArguments(ObserverScene, path);
        args.Add("--battle-observer");
        args.Add("--observer-case-input=" + path);
        args.Add("--observer-data-root=" + Path.GetFullPath(dataRoot));
        return args;
    }
}
