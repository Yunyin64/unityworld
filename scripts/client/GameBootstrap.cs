using Godot;
using System;
using System.IO;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace Xianxia.Client;

public enum ClientHostMode { World, Observer, TemplateEditor, CoreSmokeTest, Exploration, TestHome, ObserverWorker }

/// <summary>Godot 宿主：加载核心 DLL，驱动世界并在退出时释放资源。</summary>
public partial class GameBootstrap : Node
{
    public const string ExplorationScene = "res://scenes/exploration/exploration_view.tscn";
    public const string HomeScene = "res://scenes/home/test_home.tscn";
    public const string TemplateScene = "res://scenes/templates/character_template_editor.tscn";
    private static GameBootstrap? _instance;
    private bool _changingScene;
    private bool _running;
    private bool _ownsWorld;
    public static ClientHostMode CurrentMode { get; private set; }

    // 各世界宿主共用数据根目录规则；Godot 可从内存加载程序集，无法依赖 Assembly.Location。
    internal static string ResolveDataRoot() => OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("res://UnityWorld")
        : Path.GetDirectoryName(OS.GetExecutablePath()) ?? throw new InvalidOperationException("无法定位游戏数据根目录。");

    /// <summary>Explicit mode first, actual requested scene next, project default only as the final fallback.</summary>
    public static ClientHostMode ResolveHostMode(string[] userArgs, string[] engineArgs, string? currentScene, string? defaultScene)
    {
        if (userArgs.Contains("--core-smoke-test")) return ClientHostMode.CoreSmokeTest;
        if (userArgs.Any(a => a.StartsWith("--observer-worker=", StringComparison.Ordinal))) return ClientHostMode.ObserverWorker;
        bool editor = userArgs.Contains("--template-editor") || userArgs.Contains("--character-template-editor");
        bool observer = userArgs.Contains("--battle-observer") || userArgs.Any(a =>
            a.StartsWith("--observer-case=", StringComparison.Ordinal) || a.StartsWith("--observer-case-input=", StringComparison.Ordinal));
        bool world = userArgs.Contains("--world");
        bool exploration = userArgs.Contains("--exploration");
        // 显式模式只允许一个世界所有者；原有烟测始终优先处理。
        if ((editor ? 1 : 0) + (observer ? 1 : 0) + (world ? 1 : 0) + (exploration ? 1 : 0) > 1)
            throw new InvalidOperationException("不能同时指定编辑器、观察器、探索或世界宿主模式。");
        if (editor) return ClientHostMode.TemplateEditor;
        if (observer) return ClientHostMode.Observer;
        if (exploration) return ClientHostMode.Exploration;
        if (world) return ClientHostMode.World;
        string? requestedScene = engineArgs.LastOrDefault(a => a.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase));
        string? scene = !string.IsNullOrWhiteSpace(requestedScene) ? requestedScene
            : !string.IsNullOrWhiteSpace(currentScene) ? currentScene : defaultScene;
        if (scene?.EndsWith("character_template_editor.tscn", StringComparison.OrdinalIgnoreCase) == true)
            return ClientHostMode.TemplateEditor;
        if (scene?.EndsWith("battle_observer.tscn", StringComparison.OrdinalIgnoreCase) == true)
            return ClientHostMode.Observer;
        if (string.Equals(Path.GetFileName(scene), "exploration_view.tscn", StringComparison.OrdinalIgnoreCase))
            return ClientHostMode.Exploration;
        if (Path.GetFileName(scene) is "test_home.tscn" or "battle_window_check.tscn") return ClientHostMode.TestHome;
        return ClientHostMode.World;
    }

    /// <summary>由 Godot 宿主提供数据目录，核心不依赖引擎的程序集加载方式。</summary>
    public override void _Ready()
    {
        _instance = this;
        GetTree().SceneChanged += FinishNavigation;
        try
        {
            CurrentMode = ResolveHostMode(OS.GetCmdlineUserArgs(), OS.GetCmdlineArgs(),
                GetTree().CurrentScene?.SceneFilePath, ProjectSettings.GetSetting("application/run/main_scene").AsString());
            if (CurrentMode != ClientHostMode.World && CurrentMode != ClientHostMode.CoreSmokeTest)
            {
                SetPhysicsProcess(false);
                if (CurrentMode == ClientHostMode.ObserverWorker)
                {
                    UI.ObserverWorker.Start(this);
                    return;
                }
                // 显式模式允许从默认首页直达；F6 的场景路径已由上面的判定识别。
                if (CurrentMode != ClientHostMode.TestHome)
                    Callable.From(() => OpenTool(CurrentMode)).CallDeferred();
                return;
            }
            string baseDir = ResolveDataRoot();
            LogMgr.LogFilePath = ProjectSettings.GlobalizePath("user://Logs/game.log");
            WorldMgr.Initialize(12345, baseDir);
            _ownsWorld = true;
            WorldMgr.Start();
            _running = true;
            GD.Print($"[GameBootstrap] Core loaded: {typeof(WorldMgr).Assembly.GetName().Name}; data={baseDir}");

            if (CurrentMode == ClientHostMode.CoreSmokeTest)
            {
                for (int i = 0; i < 3; i++) WorldMgr.Tick(1f / 60f);
                if (NpcDefineMgr.Instance.Get("test_npc_a") == null || PlaneMgr.Instance.MainPlane.Tiles.Count == 0)
                    throw new InvalidOperationException($"核心数据未成功加载：NPC={NpcDefineMgr.Instance.Get("test_npc_a") != null}, tiles={PlaneMgr.Instance.MainPlane.Tiles.Count}");
                GD.Print($"CORE_SMOKE_OK seed={WorldTime.RandomSeed} ticks={WorldTime.CurrentTick} tiles={PlaneMgr.Instance.MainPlane.Tiles.Count}");
                SetPhysicsProcess(false);
                GetTree().Quit();
            }
        }
        catch (Exception exception)
        {
            _running = false;
            GD.PushError($"[GameBootstrap] {exception}");
            GetTree().Quit(1);
        }
    }

    public static void OpenHome() => OpenTool(ClientHostMode.TestHome);

    /// <summary>切换前先交给目标宿主模式，旧页面的退出回调仍然只释放自己的资源。</summary>
    public static void OpenTool(ClientHostMode target) => _instance?.Navigate(target);

    private void Navigate(ClientHostMode target)
    {
        string scene = target switch
        {
            ClientHostMode.TestHome => HomeScene,
            ClientHostMode.Exploration => ExplorationScene,
            ClientHostMode.TemplateEditor => TemplateScene,
            ClientHostMode.Observer => BattleWindows.BattleLauncher.ObserverScene,
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        var tree = GetTree();
        if (_changingScene || tree.CurrentScene?.SceneFilePath == scene) return;
        var previous = CurrentMode;
        CurrentMode = target;
        _changingScene = true;
        var error = tree.ChangeSceneToFile(scene);
        if (error != Error.Ok)
        {
            CurrentMode = previous;
            _changingScene = false;
            GD.PushError($"无法切换到场景 {scene}：{error}");
        }
    }

    private void FinishNavigation() => _changingScene = false;

    /// <summary>使用物理帧步长推进引擎无关的世界逻辑。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_running) WorldMgr.Tick((float)delta);
    }

    /// <summary>释放当前宿主持有的核心状态。</summary>
    public override void _ExitTree()
    {
        GetTree().SceneChanged -= FinishNavigation;
        _instance = null;
        _running = false;
        if (_ownsWorld) WorldMgr.End();
        _ownsWorld = false;
    }
}
