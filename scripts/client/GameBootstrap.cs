using Godot;
using System;
using System.IO;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace Xianxia.Client;

/// <summary>Godot 宿主：加载核心 DLL，驱动世界并在退出时释放资源。</summary>
public partial class GameBootstrap : Node
{
    private bool _running;

    /// <summary>由 Godot 宿主提供数据目录，核心不依赖引擎的程序集加载方式。</summary>
    public override void _Ready()
    {
        try
        {
            // Godot 可以从内存加载 DLL，此时 Assembly.Location 为空。
            string baseDir = OS.HasFeature("editor")
                ? ProjectSettings.GlobalizePath("res://UnityWorld")
                : Path.GetDirectoryName(OS.GetExecutablePath())
                    ?? throw new InvalidOperationException("无法定位游戏数据根目录。");
            LogMgr.LogFilePath = ProjectSettings.GlobalizePath("user://Logs/game.log");
            WorldMgr.Initialize(12345, baseDir);
            WorldMgr.Start();
            _running = true;
            GD.Print($"[GameBootstrap] Core loaded: {typeof(WorldMgr).Assembly.GetName().Name}; data={baseDir}");

            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--core-smoke-test") >= 0)
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
        WorldMgr.End();
    }
}
