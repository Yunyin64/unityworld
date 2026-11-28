using Godot;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Xianxia.Client.Battle;
using UnityWorld.Game.Data;
using Xianxia.Client.UI.Templates;

namespace Xianxia.Client.UI;

/// <summary>在真实卡库和两个场景之间验证资源共享及实例借还。</summary>
internal static class AssetPipelineCheck
{
    internal static async Task Run(SceneTree tree)
    {
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Check(GameDataMgr.Instance == null, "资源验证启动时不应需要世界数据。");
            var assets = GameBootstrap.Assets.Catalog;
            Check(assets.Find("spine/wardrobe.json").FirstOrDefault()?.SourceId == "game", "未挂载随包资源。");
            Check(ReferenceEquals(assets.Find("cards/jin_fa.png"), assets.Find("src/cards/jin_fa.png")), "src 别名没有命中同一目录。");
            string expected = ObserverSessionOutput.Argument("--expect-card-source=") ?? "local";
            Check(assets.Find("cards/jin_fa.png").FirstOrDefault()?.SourceId == expected, $"卡图来源不是 {expected}。");

            using var mapping = JsonDocument.Parse(FileAccess.GetFileAsString("res://assets/presentation/card_visuals.json"));
            var catalog = new ObserverArtCatalog();
            var other = new ObserverArtCatalog();
            int count = 0;
            foreach (var card in mapping.RootElement.GetProperty("cards").EnumerateObject())
            {
                var texture = catalog.Card(card.Name);
                Check(texture != null && ReferenceEquals(texture, other.Card(card.Name)), $"卡图缺失或未共享：{card.Name}");
                count++;
            }
            Check(count == 11 && catalog.Card("no-such-card") == null, "正式卡图数量或缺图回退异常。");
            string sampleHash = Convert.ToHexString(SHA256.HashData(assets.Find("cards/jin_fa.png")[0].Bytes.Span));
            var wardrobe = GameBootstrap.Assets.Appearance(GameBootstrap.ResolveDataRoot());
            var initialLook = wardrobe.Resolve("male", new System.Collections.Generic.Dictionary<string, string>
            {
                ["HairFront"] = "hair/front/jade[#555555]",
                ["hairRear"] = "hair/rear/braid"
            }, 12345);
            Check(initialLook.Parts["hairFront"].Dye == "#555555" && initialLook.Parts["hairRear"].Dye == null,
                "前发与后发的染色未独立。");
            var actor = DuelSpineActor.Rent("male") ?? throw new InvalidOperationException("未加载 Spine 骨架。");
            var firstActor = actor.Instance;
            firstActor.Equip(BattleActorLook.FromData(initialLook)!, wardrobe);
            firstActor.Sample("Ready", 0);
            Check(firstActor.SlotArtwork("hairFront") != null && firstActor.SlotArtwork("hairRear") != null,
                "逐槽皮肤未在 Spine 挂载。");
            actor.Dispose();
            using (var reused = DuelSpineActor.Rent("male"))
                Check(reused != null && ReferenceEquals(firstActor, reused.Instance) &&
                    reused.Instance.SlotArtwork("hairFront") == null, "Spine 实例未复用或携带旧服饰。");

            var outfit = wardrobe.Resolve("male", new System.Collections.Generic.Dictionary<string, string>
            {
                ["robe"] = "robe/violet", ["neck"] = "robe/neck/jade",
                ["elbowFront"] = "robe/elbowFront/jade", ["cape"] = "robe/cape/jade"
            }, 25);
            Check(outfit.Parts["armBack"].Address == "robe/armBack/violet" &&
                outfit.Parts["elbowBack"].Address == "robe/elbowBack/violet" &&
                outfit.Parts["elbowFront"].Address == "robe/elbowFront/jade", "随机配袖或单槽显式选择异常。");
            Check(!outfit.Parts.ContainsKey("neck"), "旧脖颈配置重新生成了独立槽位。");
            using (var dressed = DuelSpineActor.Rent("male"))
            {
                dressed!.Instance.Equip(BattleActorLook.FromData(outfit)!, wardrobe);
                foreach (string pose in new[] { "Ready", "Strike" })
                {
                    dressed.Instance.Sample(pose, .10f);
                    Check(dressed.Instance.SlotArtwork("robe") == "paint_robe_violet" &&
                        dressed.Instance.SlotArtwork("armBack") == "paint_armBack_violet" &&
                        dressed.Instance.SlotArtwork("elbowBack") == "paint_elbowBack_violet" &&
                        dressed.Instance.SlotArtwork("elbowFront") == "paint_elbowFront_jade",
                        $"{pose} 的衣身、远侧袖和近侧小臂贴图未独立挂载。");
                }
                var body = dressed.Instance;
                body.Sample("Ready", .10f);
                Check(body.LayerOrder("cape") >= 0 && body.LayerOrder("cape") < body.LayerOrder("armBack") &&
                    body.LayerOrder("armBack") < body.LayerOrder("robe") &&
                    body.LayerOrder("robe") < body.LayerOrder("armFront") &&
                    body.LayerOrder("shinFront") < body.LayerOrder("pantsFront"), "待机时的衣装遮挡顺序错误。");
                body.Sample("Slash", .13f);
                Check(body.LayerOrder("robe") < body.LayerOrder("armBack") &&
                    body.LayerOrder("armBack") < body.LayerOrder("armFront"), "斩击时右手没有切到衣身前。");
                body.Sample("Ready", .10f);
                Check(body.LayerOrder("armBack") < body.LayerOrder("robe"), "斩击结束后未恢复待机深度。");
            }

            foreach (string rig in new[] { "male", "female" })
            {
                using var lease = DuelSpineActor.Rent(rig);
                var actorForPose = lease!.Instance;
                var poseLook = wardrobe.Resolve(rig, null, 25);
                actorForPose.Equip(BattleActorLook.FromData(poseLook)!, wardrobe);
                Check(actorForPose.JointPosition("Neck") != null && actorForPose.SlotArtwork("neck") == null,
                    $"{rig} 的颈部应只保留骨骼。");
                foreach (string pose in new[] { "Slash", "Strike", "Smash" })
                {
                    actorForPose.Sample(pose, .10f);
                    var head = actorForPose.JointPosition("Head")!.Value;
                    var hand = actorForPose.JointPosition("Hand_R")!.Value;
                    var grip = actorForPose.JointPosition("weapon")!.Value;
                    Check(head.Y - hand.Y >= 22 && grip.X - head.X >= 18 && head.Y - grip.Y >= 16 && grip.DistanceTo(hand) < 12,
                        $"{rig} 的 {pose} 出招帧没有把脸、持握手和武器拉开。");
                }
            }

            BattleActorLook? externalLook = null;
            if (ObserverSessionOutput.Argument("--expect-external-hair=") is { } externalHair)
            {
                string rearAddress = ObserverSessionOutput.Argument("--expect-external-rear=") ?? "hair/rear/braid";
                var configuration = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["hairFront"] = externalHair + "[#557755]", ["hairRear"] = rearAddress + "[#555577]"
                };
                string? animatedEyes = ObserverSessionOutput.Argument("--expect-animated-eyes=");
                if (animatedEyes != null) configuration["eyes"] = animatedEyes;
                var hair = wardrobe.Resolve("male", configuration, 12345);
                var front = wardrobe.Find("male", externalHair);
                var rear = wardrobe.Find("male", rearAddress);
                Check(front is { Slot: "hairFront", Attachments.Count: > 0 } && rear?.Slot == "hairRear" &&
                    hair.Parts["hairFront"].Dye == "#557755" && hair.Parts["hairRear"].Dye == "#555577",
                    "独立前发、后发的候选或染色异常。");
                externalLook = BattleActorLook.FromData(hair)!;
                using var external = DuelSpineActor.Rent("male");
                external!.Instance.Equip(externalLook, wardrobe);
                external.Instance.Sample("Strike", 0.1f);
                Check(front!.Attachments.Any(part => part.Image == external.Instance.SlotArtwork("hairFront")) &&
                    external.Instance.SlotArtwork("hairRear") is { } rearArtwork &&
                    (rear!.Attachments.Count == 0 || rear.Attachments.Any(part => part.Image == rearArtwork)),
                    "Spine 未在正常动作中挂载前发和后发。");
                if (animatedEyes != null)
                {
                    var expressions = wardrobe.Find("male", animatedEyes)!.Attachments;
                    Check(external.Instance.SlotArtwork("eyes") == expressions.Single(part => part.Key == "angry").Image,
                        "出招时未切换同一插槽的表情附件。");
                    external.Instance.Sample("Ready", 0);
                    Check(external.Instance.SlotArtwork("eyes") == expressions.Single(part => part.Key == "base").Image,
                        "待机时未恢复同一插槽的基础附件。");
                }
                if (OS.GetCmdlineUserArgs().Contains("--check-front-hair-depth"))
                    Check(external.Instance.LayerOrder("hairFront") < external.Instance.LayerOrder("robe"),
                        "外部素材的深度覆盖未应用。");
            }

            var pool = GameBootstrap.AssetInstances;
            const string key = "verification/idle-cap";
            pool.SetRetentionLimit<object>(key, 12);
            int discarded = 0;
            var first = Enumerable.Range(0, 20)
                .Select(_ => pool.Rent(key, static () => new object(), discard: _ => discarded++)!).ToArray();
            var originals = first.Select(lease => lease.Instance).ToHashSet();
            Check(originals.Count == 20 && discarded == 0, "借出实例并不独立。");
            foreach (var lease in first) lease.Dispose();
            Check(discarded == 8, "空闲实例未按 12 个裁减。");
            var second = Enumerable.Range(0, 13)
                .Select(_ => pool.Rent("src/" + key, static () => new object(), discard: _ => discarded++)!).ToArray();
            Check(second.Count(lease => originals.Contains(lease.Instance)) == 12, "对象池未复用保留的实例。");
            foreach (var lease in second) lease.Dispose();
            pool.SetRetentionLimit<object>(key, 0);
            Check(discarded == 21, "降低上限后仍有空闲对象。");

            await Navigate(tree, ClientHostMode.TemplateEditor);
            if (externalLook != null)
            {
                var preview = tree.CurrentScene.FindChild("SpinePreview", true, false) as TemplateSpinePreview;
                Check(preview?.SetLook(externalLook) == true, "模板编辑器无法预览外部组件。");
            }
            var originalTiles = Tiles(tree);
            Check(originalTiles.Length >= 20, "卡库未创建足够的卡面。");
            await Navigate(tree, ClientHostMode.TestHome);
            await Navigate(tree, ClientHostMode.TemplateEditor);
            Check(originalTiles.ToHashSet().SetEquals(Tiles(tree)), "重进卡库没有复用同一批控件。");

            GD.Print($"ASSET_PIPELINE_OK cards={count} source={expected} sampleHash={sampleHash} reusedAcrossScenes={originalTiles.Length} retained=12");
            tree.Quit();
        }
        catch (Exception error)
        {
            GD.PushError("[AssetPipelineCheck] " + error);
            tree.Quit(1);
        }
    }

    private static ulong[] Tiles(SceneTree tree) =>
        tree.CurrentScene.FindChild("CardList", true, false).GetChildren().OfType<TemplateCardTile>()
            .Select(tile => tile.GetInstanceId()).ToArray();

    private static async Task Navigate(SceneTree tree, ClientHostMode mode)
    {
        GameBootstrap.OpenTool(mode);
        await tree.ToSignal(tree, SceneTree.SignalName.SceneChanged);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
