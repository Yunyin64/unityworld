using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityWorld.Core;
using Xianxia.Client.Battle;
using Xianxia.Client.UI.Templates;
using File = System.IO.File;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace Xianxia.Client.UI;

/// <summary>用正式外观选择和 Godot 绘制路径输出可复现的 NPC 验收图。</summary>
internal static class NpcAppearanceVisualCheck
{
    private sealed record Pose(string Clip, float Seconds, bool Mirror = false);
    private sealed record Frame(Pose Pose, int[] AlphaBounds, DuelSpineActor.AttachmentGeometry[] Slots);
    private sealed record Sample(int Index, int Seed, BattleActorLook Look, string Image, Frame[] Frames, string[] Failures);

    private const int Width = 448;
    private const int Height = 388;
    private const int Columns = 7;
    private static readonly Vector2 Origin = new(224, 328);
    private static readonly Pose[] Poses =
    [
        new("Ready", 0), new("Ready", .6f), new("Ready", 1.2f), new("Ready", 2.399f),
        new("Ready", .6f, true), new("Windup", .05f), new("Windup", .10f),
        new("Slash", .05f), new("Slash", .074f), new("Slash", .075f), new("Slash", .10f), new("Slash", .16f),
        new("Slash", .24f), new("Slash", .10f, true),
        new("Strike", .05f), new("Strike", .10f), new("Smash", .045f), new("Smash", .10f),
        new("Shot", .10f), new("Cast", .24f), new("Cast", .43f),
        new("FollowThrough", 0), new("FollowThrough", .084f), new("FollowThrough", .085f), new("FollowThrough", .23f),
        new("Guard", .11f), new("Recoil", .07f), new("Brace", .11f),
        new("Advance", .08f), new("Advance", .24f), new("Withdraw", .12f), new("Evade", .125f),
        new("Land", .04f), new("Land", .16f), new("Kneel", .36f),
        new("Ready", 1.78f), new("Ready", 1.84f), new("Ready", 1.91f),
        new("Strike", .23f), new("Smash", .24f), new("Land", 0), new("Land", .10f)
    ];

    internal static async Task Run(SceneTree tree, string output)
    {
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Directory.CreateDirectory(output);
            string? requestedSeed = ObserverSessionOutput.Argument("--appearance-seed=");
            int seed = requestedSeed == null ? RandomNumberGenerator.GetInt32(int.MaxValue) : int.Parse(requestedSeed);
            var rng = new Rng(seed);
            // Shuffle a balanced set before drawing every outfit, with no retry for a bad appearance.
            string[] rigs = ["male", "female", "male", "female", "male", "female", "male", "female", "male", "female"];
            for (int i = rigs.Length - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (rigs[i], rigs[j]) = (rigs[j], rigs[i]);
            }

            var results = new List<Sample>();
            for (int i = 0; i < rigs.Length; i++)
            {
                int sampleSeed = rng.Range(0, int.MaxValue);
                var look = DuelWardrobe.Generate(rigs[i], sampleSeed) ?? throw new InvalidOperationException("随机装配失败。");
                var result = await Capture(tree, output, i + 1, sampleSeed, look);
                results.Add(result);
                GD.Print($"NPC_APPEARANCE_VISUAL_SAMPLE {result.Index:00} {look.Rig} {sampleSeed} failures={result.Failures.Length}");
            }

            File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(new
            {
                seed, width = Width, height = Height, columns = Columns, origin = new[] { Origin.X, Origin.Y }, samples = results
            }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            int failures = results.Sum(sample => sample.Failures.Length);
            GD.Print($"NPC_APPEARANCE_VISUAL seed={seed} cases={results.Count} frames={results.Count * Poses.Length} failures={failures}");
            tree.Quit(failures == 0 ? 0 : 1);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            tree.Quit(1);
        }
    }

    private static async Task<Sample> Capture(SceneTree tree, string output, int index, int seed, BattleActorLook look)
    {
        using var lease = DuelSpineActor.Rent(look.Rig) ?? throw new InvalidOperationException($"缺少骨架 {look.Rig}。");
        var actor = lease.Instance;
        actor.Equip(look);
        var layers = new DuelSpineActor.AttachmentGeometry[Poses.Length][];
        string imageName = $"{index:00}-{look.Rig}-{seed}.png";
        var viewport = new SubViewport
        {
            Size = new(Width * Columns, Height * ((Poses.Length + Columns - 1) / Columns)),
            TransparentBg = true,
            Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once
        };
        var canvas = new Node2D { TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps };
        canvas.Draw += () =>
        {
            for (int i = 0; i < Poses.Length; i++)
            {
                var pose = Poses[i];
                canvas.DrawSetTransform(new Vector2(i % Columns * Width, i / Columns * Height) + Origin);
                actor.Sample(pose.Clip, pose.Seconds);
                layers[i] = actor.InspectAttachments();
                actor.Draw(canvas, 234, pose.Mirror, Colors.White);
            }
        };
        viewport.AddChild(canvas);
        tree.Root.AddChild(viewport);
        try
        {
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = viewport.GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(output, imageName)) != Error.Ok)
                throw new IOException($"无法保存真实渲染截图 {imageName}。请使用图形渲染驱动运行。");

            var failures = new List<string>();
            var frames = new Frame[Poses.Length];
            for (int i = 0; i < Poses.Length; i++)
            {
                var pose = Poses[i];
                using var tile = image.GetRegion(new(i % Columns * Width, i / Columns * Height, Width, Height));
                var bounds = tile.GetUsedRect();
                frames[i] = new(pose, [bounds.Position.X, bounds.Position.Y, bounds.Size.X, bounds.Size.Y], layers[i]);
                CheckFrame(look, pose, bounds, layers[i], failures);
            }
            return new(index, seed, look, imageName, frames, failures.ToArray());
        }
        finally { viewport.QueueFree(); }
    }

    private static void CheckFrame(BattleActorLook look, Pose pose, Rect2I bounds, DuelSpineActor.AttachmentGeometry[] layers,
        List<string> failures)
    {
        void Require(bool pass, string message)
        {
            if (!pass) failures.Add($"{pose.Clip} {pose.Seconds:0.000}: {message}");
        }
        var order = layers.Select((part, i) => (part.Slot, i)).ToDictionary(pair => pair.Slot, pair => pair.i);
        bool Behind(string back, string front) => order.TryGetValue(back, out int b) && order.TryGetValue(front, out int f) && b < f;
        Require(bounds.Size.Y is >= 110 and <= 320 && bounds.Size.X >= 65 && bounds.Position.X > 0 && bounds.Position.Y > 0 &&
            bounds.End.X < Width && bounds.End.Y < Height, $"角色包围框异常 {bounds}");
        foreach (var area in new[] { new Vector2(170, 260), new Vector2(280, 204) })
        {
            var (height, foot) = TemplateSpinePreview.Framing(area);
            var corner = foot + ((Vector2)bounds.Position - Origin) * (height / 234);
            var end = foot + ((Vector2)bounds.End - Origin) * (height / 234);
            Require(corner.X > 0 && corner.Y > 0 && end.X < area.X && end.Y < area.Y, $"预览区 {area} 会截断人物");
        }
        if (pose.Clip is "Ready" or "Land")
            Require(Math.Abs(bounds.End.Y - Origin.Y) <= 5, "站立或落地的脚底离开地线");
        foreach (string slot in new[] { "face", "eyes", "brows", "mouth", "hairRear", "hairFront", "robe", "robeTails",
            "armBack", "elbowBack", "handBack", "armFront", "elbowFront", "handFront", "pantsBack", "shinBack", "bootBack",
            "pantsFront", "shinFront", "bootFront" })
            Require(order.ContainsKey(slot), $"缺失 {slot}");
        Require(Behind("hairRear", "face") && Behind("face", "eyes") && Behind("face", "hairFront") &&
            Behind("robeTails", "robe") && Behind("robe", "armFront") &&
            Behind("shinBack", "pantsBack") && Behind("shinFront", "pantsFront"), "身体、头部或膝部的前后关系错误");
        bool crossed = (pose.Clip is "Slash" or "Strike" or "Smash" && pose.Seconds >= .075f) ||
            (pose.Clip == "FollowThrough" && pose.Seconds < .085f);
        Require(crossed ? Behind("robe", "armBack") : Behind("armBack", "robe"), "持物臂未按动作切换前后关系");
        Require(!order.ContainsKey("weapon") || Behind("weapon", "handBack"), "武器遮住持握手");
        foreach (string slot in new[] { "weapon", "accessory" })
            if (look.Parts.TryGetValue(slot, out var part))
                Require(order.ContainsKey(slot) != (part.Id == slot + "/none"), $"{slot} 与换装选择不符");
    }
}
