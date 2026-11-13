using System.Collections.Generic;
using Xianxia.Client.Battle;
using Xianxia.Client.BattleWindows;

namespace Xianxia.Client.Templates;

public sealed record TemplateTrialLaunchResult(int ProcessId, string InputPath);

/// <summary>Godot process boundary; the caller's editor tree and draft remain untouched.</summary>
public static class TemplateTrialLauncher
{
    public const string ObserverScene = BattleLauncher.ObserverScene;

    public static TemplateTrialLaunchResult Launch(BattleCase input, string dataRoot, bool animations = true)
    {
        var result = BattleLauncher.Open(input, BattleWindowMode.NewWindow, dataRoot, animations);
        return new(result.ProcessId, result.InputPath);
    }

    /// <summary>Includes an explicit scene so exported builds do not return to the editor main scene.</summary>
    public static List<string> ObserverArguments(string inputPath, string dataRoot)
    {
        return BattleLauncher.ObserverArguments(inputPath, dataRoot);
    }
}
