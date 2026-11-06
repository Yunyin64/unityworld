using Godot;
using System;
using System.IO;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace Xianxia.Client;

public enum ClientHostMode { World, Observer, TemplateEditor, CoreSmokeTest }

/// <summary>Godot 宿主：加载核心 DLL，驱动世界并在退出时释放资源。</summary>
public partial class GameBootstrap : Node
{
    private bool _running;
    private bool _ownsWorld;
    public static ClientHostMode CurrentMode { get; private set; }

    /// <summary>Explicit mode first, actual requested scene next, project default only as the final fallback.</summary>
    public static ClientHostMode ResolveHostMode(string[] userArgs, string[] engineArgs, string? currentScene, string? defaultScene)
    {
        if (userArgs.Contains("--core-smoke-test")) return ClientHostMode.CoreSmokeTest;
        bool editor = userArgs.Contains("--template-editor") || userArgs.Contains("--character-template-editor");
        bool observer = userArgs.Contains("--battle-observer") || userArgs.Any(a =>
            a.StartsWith("--observer-case=", StringComparison.Ordinal) || a.StartsWith("--observer-case-input=", StringComparison.Ordinal));
        bool world = userArgs.Contains("--world");
        if ((editor ? 1 : 0) + (observer ? 1 : 0) + (world ? 1 : 0) > 1)
            throw new InvalidOperationException("不能同时指定编辑器、观察器或世界宿主模式。");
        if (editor) return ClientHostMode.TemplateEditor;
        if (observer) return ClientHostMode.Observer;
        if (world) return ClientHostMode.World;
        string? requestedScene = engineArgs.LastOrDefault(a => a.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase));
        string? scene = !string.IsNullOrWhiteSpace(requestedScene) ? requestedScene
            : !string.IsNullOrWhiteSpace(currentScene) ? currentScene : defaultScene;
        if (scene?.EndsWith("character_template_editor.tscn", StringComparison.OrdinalIgnoreCase) == true)
            return ClientHostMode.TemplateEditor;
        if (scene?.EndsWith("battle_observer.tscn", StringComparison.OrdinalIgnoreCase) == true)
            return ClientHostMode.Observer;
        return ClientHostMode.World;
    }

    /// <summary>由 Godot 宿主提供数据目录，核心不依赖引擎的程序集加载方式。</summary>
    public override void _Ready()
    {
        try
        {
            CurrentMode = ResolveHostMode(OS.GetCmdlineUserArgs(), OS.GetCmdlineArgs(),
                GetTree().CurrentScene?.SceneFilePath, ProjectSettings.GetSetting("application/run/main_scene").AsString());
            // The editor reads definitions only; observer owns its separate world in BattleObserverSession.
            if (CurrentMode is ClientHostMode.TemplateEditor or ClientHostMode.Observer)
            {
                SetPhysicsProcess(false);
                return;
            }
            // Godot 可以从内存加载 DLL，此时 Assembly.Location 为空。
            string baseDir = OS.HasFeature("editor")
                ? ProjectSettings.GlobalizePath("res://UnityWorld")
                : Path.GetDirectoryName(OS.GetExecutablePath())
                    ?? throw new InvalidOperationException("无法定位游戏数据根目录。");
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

    /// <summary>使用物理帧步长推进引擎无关的世界逻辑。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_running) WorldMgr.Tick((float)delta);
    }

    /// <summary>释放当前宿主持有的核心状态。</summary>
    public override void _ExitTree()
    {
        _running = false;
        if (_ownsWorld) WorldMgr.End();
        _ownsWorld = false;
    }
}
